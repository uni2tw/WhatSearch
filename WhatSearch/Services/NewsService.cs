using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using NLog;
using WhatSearch.Core;
using WhatSearch.Utility;

namespace WhatSearch.Services
{
    public interface INewsService
    {
        /// <summary>設定檔有指定要通知的分享根才啟用</summary>
        bool Enabled { get; }
        /// <summary>啟動背景定時掃描（沒啟用就什麼都不做）</summary>
        void Start();
        /// <summary>最近的新上架事件（新到舊）；已經不存在的資料夾不列出</summary>
        List<NewsEvent> GetEvents(int limit);
        /// <summary>這個會員「上次看新上架」的時間（毫秒）；第一次問到時以現在為準，舊的事件不算未讀</summary>
        long GetLastRead(string member);
        int GetUnread(string member);
        void MarkRead(string member);
    }

    public class NewsEvent
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("share")]
        public string Share { get; set; }
        /// <summary>series（新作品）／episodes（既有作品新增集數）／album（新專輯）</summary>
        [JsonPropertyName("kind")]
        public string Kind { get; set; }
        /// <summary>動畫資料夾或專輯資料夾的相對路徑，例如 /Anime/2026-4/作品</summary>
        [JsonPropertyName("path")]
        public string Path { get; set; }
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("count")]
        public int Count { get; set; }
        /// <summary>UTC 毫秒</summary>
        [JsonPropertyName("time")]
        public long Time { get; set; }
    }

    public class NewsFileState
    {
        public long Size { get; set; }
        public long LastChange { get; set; }
        public bool Announced { get; set; }
    }

    public class NewsUnitState
    {
        public bool Announced { get; set; }
        /// <summary>上次列出這個作品資料夾時，資料夾本身的修改時間（UTC ticks）；沒變就不用再列出裡面的檔案</summary>
        public long Mtime { get; set; }
        public Dictionary<string, NewsFileState> Files { get; set; } = new Dictionary<string, NewsFileState>();
    }

    public class NewsAlbumState
    {
        public long Size { get; set; }
        public long LastChange { get; set; }
        public bool Announced { get; set; }
        /// <summary>專輯資料夾本身的修改時間（UTC ticks）；已通知過的專輯，沒變就整個略過，不再列出內容</summary>
        public long Mtime { get; set; }
    }

    public class NewsShareState
    {
        /// <summary>第一次掃描只建立基準（把現有的全部當成已知），不產生事件，不然一啟用就噴出整個資料庫</summary>
        public bool Baselined { get; set; }
        /// <summary>上次「完整掃描」（不看資料夾修改時間）的時間（UTC 毫秒）</summary>
        public long LastFullScan { get; set; }
        public Dictionary<string, NewsUnitState> Units { get; set; } = new Dictionary<string, NewsUnitState>();
        public Dictionary<string, NewsAlbumState> Albums { get; set; } = new Dictionary<string, NewsAlbumState>();
    }

    public class NewsState
    {
        public Dictionary<string, NewsShareState> Shares { get; set; } = new Dictionary<string, NewsShareState>();
    }

    /// <summary>
    /// 站內「新上架」通知：背景定時掃描設定的分享根（網路磁碟上的檔案監看不可靠，所以用定時掃描與快照比對），
    /// 以「系統第一次看到」的時間為準，並且要等檔案大小穩定一段時間（避免下載到一半就通知）才算。
    /// 兩種模式：episodes（動畫：出現新作品，或既有作品資料夾多了新的影片檔）、albums（音樂：只有新的專輯資料夾出現才算，之後補進歌曲不算）。
    /// albums 模式自動辨認專輯資料夾：直接放著音樂檔的資料夾；多片裝（Disc 1 / CD2…）以母資料夾為一張專輯，所以歌手\專輯與專輯直接放在根目錄的混合結構都可以。
    ///
    /// 為了在網路磁碟上也不吃力：
    ///  - 只在設定的時段內掃描（預設 08:00–22:00），間隔預設 2 小時，每個分享根可各自指定；
    ///  - 資料夾每次只做「一次」查詢（同時取得子資料夾與檔案）；
    ///  - albums：已經通知過（或基準裡）的專輯當成葉節點，修改時間沒變就整個略過，不列出內容；
    ///  - episodes：作品資料夾本身的修改時間（對該資料夾重新查詢，不用父層列表裡可能過期的值）沒變、也沒有等待確認的檔案，就不列出裡面的檔案
    ///    （新集數一定是資料夾直接多了一個檔案，所以這樣判斷是可靠的）；
    ///  - 資料夾修改時間不會往上一層傳，而且父層列表裡的子資料夾日期在 NTFS 上會過期，所以只在上述「葉節點」用；
    ///    每隔幾小時（fullScanHours）再做一次不看修改時間的完整掃描當保險；
    ///  - 有等待確認的新項目時，只提早排一次短的複查，不必等滿一個間隔。
    /// 每輪掃描的耗時與查詢數會以 Info 寫進日誌（搜尋「新上架掃描」）。
    /// 事件存 data/news.json，每個會員的已讀時間存 data/userData/news-read.json。
    /// </summary>
    public class NewsService : INewsService
    {
        static ILogger logger = LogManager.GetCurrentClassLogger();
        const int MaxEvents = 300;
        const int MaxEventAgeDays = 90;
        const long MergeWindowMs = 12L * 3600 * 1000;
        static readonly HashSet<string> TempExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".part", ".!qb", ".crdownload", ".tmp", ".aria2", ".download", ".partial" };

        class ScanStats
        {
            public int DirQueries;
            public int Skipped;
            public int Events;
            public bool Full;
        }

        class ShareRuntime
        {
            public DateTime NextDue;
            public bool HasPending;
        }

        readonly SystemConfig config = Ioc.GetConfig();
        readonly IRatingService ratingService = Ioc.Get<IRatingService>();
        readonly object gate = new object();
        List<NewsEvent> events;
        Dictionary<string, long> lastRead;
        NewsState state;
        Thread worker;

        public bool Enabled
        {
            get { return config.News != null && config.News.Shares != null && config.News.Shares.Count > 0; }
        }

        static string DataPath(params string[] parts)
        {
            return parts.Length == 1
                ? Helper.GetRelativePath("data", parts[0])
                : Helper.GetRelativePath("data", parts[0], parts[1]);
        }

        static long NowMs()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        T LoadJson<T>(string path, Func<T> fallback)
        {
            try
            {
                if (File.Exists(path))
                {
                    T value = JsonHelper.Deserialize<T>(File.ReadAllText(path));
                    if (value != null)
                    {
                        return value;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "讀取 {0} 失敗，以空白資料開始", path);
            }
            return fallback();
        }

        static void SaveJson(string path, object value)
        {
            string folder = Path.GetDirectoryName(path);
            if (Directory.Exists(folder) == false)
            {
                Directory.CreateDirectory(folder);
            }
            // 先寫暫存檔再取代，避免寫到一半當掉把資料清空
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonHelper.Serialize(value, indented: true));
            if (File.Exists(path))
            {
                File.Replace(tmp, path, null);
            }
            else
            {
                File.Move(tmp, path);
            }
        }

        void EnsureLoaded()
        {
            lock (gate)
            {
                if (events == null)
                {
                    events = LoadJson(DataPath("news.json"), () => new List<NewsEvent>());
                    lastRead = LoadJson(DataPath("userData", "news-read.json"), () => new Dictionary<string, long>());
                }
            }
        }

        // ---------- 讀取（網頁 API 使用） ----------

        public List<NewsEvent> GetEvents(int limit)
        {
            EnsureLoaded();
            List<NewsEvent> list;
            lock (gate)
            {
                list = events.OrderByDescending(e => e.Time).Take(limit).ToList();
            }
            return list.Where(Exists).ToList();
        }

        static bool Exists(NewsEvent e)
        {
            string abs;
            return PathUtility.TryGetAbsolutePath(e.Path, out abs) && Directory.Exists(abs);
        }

        public long GetLastRead(string member)
        {
            EnsureLoaded();
            lock (gate)
            {
                long value;
                if (string.IsNullOrEmpty(member) == false && lastRead.TryGetValue(member, out value))
                {
                    return value;
                }
                value = NowMs();
                if (string.IsNullOrEmpty(member) == false)
                {
                    lastRead[member] = value;
                    try { SaveJson(DataPath("userData", "news-read.json"), lastRead); } catch (Exception ex) { logger.Warn(ex, "寫入 news-read.json 失敗"); }
                }
                return value;
            }
        }

        public int GetUnread(string member)
        {
            long last = GetLastRead(member);
            List<NewsEvent> candidates;
            lock (gate)
            {
                candidates = events.Where(e => e.Time > last).ToList();
            }
            return candidates.Count(Exists);
        }

        public void MarkRead(string member)
        {
            if (string.IsNullOrEmpty(member))
            {
                return;
            }
            EnsureLoaded();
            lock (gate)
            {
                lastRead[member] = NowMs();
                try { SaveJson(DataPath("userData", "news-read.json"), lastRead); } catch (Exception ex) { logger.Warn(ex, "寫入 news-read.json 失敗"); }
            }
        }

        // ---------- 掃描時段與間隔 ----------

        static bool TryParseTime(string text, out TimeSpan value)
        {
            value = TimeSpan.Zero;
            return string.IsNullOrWhiteSpace(text) == false && TimeSpan.TryParse(text.Trim(), out value);
        }

        // 沒設定（或格式錯誤）就不限制時段
        bool TryGetWindow(out TimeSpan from, out TimeSpan to)
        {
            from = TimeSpan.Zero;
            to = TimeSpan.Zero;
            return TryParseTime(config.News.ActiveFrom, out from) && TryParseTime(config.News.ActiveTo, out to) && from != to;
        }

        bool InWindow(DateTime local)
        {
            TimeSpan from, to;
            if (TryGetWindow(out from, out to) == false)
            {
                return true;
            }
            TimeSpan t = local.TimeOfDay;
            return from < to ? (t >= from && t < to) : (t >= from || t < to);       // from > to 表示跨午夜
        }

        // 時段外就延到下一個時段的開始；時段內回傳原時間
        DateTime NextWindowStart(DateTime local)
        {
            TimeSpan from, to;
            if (InWindow(local) || TryGetWindow(out from, out to) == false)
            {
                return local;
            }
            TryGetWindow(out from, out to);
            DateTime start = local.Date + from;
            return start > local ? start : start.AddDays(1);
        }

        double IntervalSeconds(NewsShareConfig sc)
        {
            double minutes = sc.ScanMinutes.HasValue ? sc.ScanMinutes.Value : config.News.ScanMinutes;
            return Math.Max(1.0, minutes * 60);
        }

        // ---------- 背景掃描 ----------

        public void Start()
        {
            if (Enabled == false || worker != null)
            {
                return;
            }
            EnsureLoaded();
            state = LoadJson(DataPath("news-state.json"), () => new NewsState());
            worker = new Thread(Loop) { IsBackground = true, Priority = ThreadPriority.BelowNormal, Name = "NewsScanner" };
            worker.Start();
            TimeSpan from, to;
            string window = TryGetWindow(out from, out to) ? (from.ToString(@"hh\:mm") + "–" + to.ToString(@"hh\:mm")) : "不限時段";
            logger.Info("新上架掃描已啟動：{0}；時段 {1}；檔案穩定 {2} 分鐘才算；每 {3} 小時做一次完整掃描",
                string.Join("、", config.News.Shares.Select(s => s.Name + "(" + s.Mode + "，每 " + Math.Round(IntervalSeconds(s) / 60, 1) + " 分鐘)")),
                window, config.News.StableMinutes, config.News.FullScanHours);
        }

        void Loop()
        {
            Dictionary<string, ShareRuntime> runtime = new Dictionary<string, ShareRuntime>(StringComparer.OrdinalIgnoreCase);
            // 啟動後稍等一下再掃，不要拖慢啟動
            double startupDelay = Math.Min(30.0, config.News.Shares.Min(s => IntervalSeconds(s)));
            DateTime first = DateTime.Now.AddSeconds(startupDelay);
            foreach (NewsShareConfig sc in config.News.Shares)
            {
                runtime[sc.Name] = new ShareRuntime { NextDue = first };
            }
            while (true)
            {
                try
                {
                    foreach (NewsShareConfig sc in config.News.Shares)
                    {
                        ShareRuntime rt = runtime[sc.Name];
                        if (rt.NextDue > DateTime.Now)
                        {
                            continue;
                        }
                        NewsShareState st;
                        bool baselined = state.Shares.TryGetValue(sc.Name, out st) && st.Baselined;
                        // 時段外不掃描；但還沒建立基準的分享根要先掃一次建立基準，不然之後這段期間進來的東西會被當成已知而漏掉
                        if (baselined && InWindow(DateTime.Now) == false)
                        {
                            rt.NextDue = NextWindowStart(DateTime.Now);
                            logger.Info("新上架掃描 {0}：目前不在掃描時段內，下一次 {1:MM/dd HH:mm:ss}", sc.Name, rt.NextDue);
                            continue;
                        }
                        ScanShare(sc, rt);
                        double wait = IntervalSeconds(sc);
                        if (rt.HasPending)
                        {
                            // 有新出現、還在等大小穩定的項目：不必等滿一個間隔，穩定時間一過就複查一次
                            wait = Math.Min(wait, Math.Max(1.0, config.News.StableMinutes * 60 + 5));
                        }
                        DateTime planned = DateTime.Now.AddSeconds(wait);
                        rt.NextDue = NextWindowStart(planned);
                        if (rt.NextDue > planned.AddSeconds(1))
                        {
                            logger.Info("新上架掃描 {0}：下一次排在 {1:MM/dd HH:mm:ss}（掃描時段外，延到時段開始）", sc.Name, rt.NextDue);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Warn(ex, "新上架掃描失敗");
                }
                double sleep = runtime.Values.Min(r => (r.NextDue - DateTime.Now).TotalSeconds);
                Thread.Sleep(TimeSpan.FromSeconds(Math.Min(300.0, Math.Max(1.0, sleep))));
            }
        }

        void ScanShare(NewsShareConfig sc, ShareRuntime rt)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            FolderConfig root = config.Folders.FirstOrDefault(f => string.Equals(f.Title, sc.Name, StringComparison.OrdinalIgnoreCase));
            if (root == null || Directory.Exists(root.Path) == false)
            {
                // 網路磁碟暫時連不上時不要動快照，否則恢復連線後所有東西都會被當成新的
                logger.Warn("新上架掃描：找不到分享根 {0}，略過", sc.Name);
                return;
            }
            NewsShareState st;
            if (state.Shares.TryGetValue(sc.Name, out st) == false)
            {
                st = new NewsShareState();
                state.Shares[sc.Name] = st;
            }
            long now = NowMs();
            ScanStats stats = new ScanStats { Full = st.Baselined == false || now - st.LastFullScan >= (long)(config.News.FullScanHours * 3600 * 1000) };
            bool albums = string.Equals(sc.Mode, "albums", StringComparison.OrdinalIgnoreCase);
            try
            {
                bool reliable = albums ? ScanAlbums(sc, root, st, stats) : ScanEpisodes(sc, root, st, stats);
                if (reliable == false)
                {
                    return;
                }
                if (stats.Full)
                {
                    st.LastFullScan = now;
                }
                rt.HasPending = albums
                    ? st.Albums.Values.Any(a => a.Announced == false)
                    : st.Units.Values.Any(u => u.Announced == false || u.Files.Values.Any(f => f.Announced == false));
                try { SaveJson(DataPath("news-state.json"), state); } catch (Exception ex) { logger.Warn(ex, "寫入 news-state.json 失敗"); }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "新上架掃描 {0} 失敗", sc.Name);
                return;
            }
            logger.Info("新上架掃描 {0}({1})：耗時 {2}ms，查詢 {3} 個資料夾，略過 {4} 個沒變動的{5}，新增 {6} 筆通知{7}{8}",
                sc.Name, albums ? "albums" : "episodes", watch.ElapsedMilliseconds, stats.DirQueries, stats.Skipped,
                albums ? "專輯" : "作品資料夾", stats.Events, stats.Full ? "（完整掃描）" : "", rt.HasPending ? "，有項目等待確認穩定" : "");
        }

        bool IsStable(long lastChange, long now)
        {
            return now - lastChange >= (long)(config.News.StableMinutes * 60 * 1000);
        }

        static string RelPath(string fullPath)
        {
            string rel;
            return PathUtility.TryGetRelPath(fullPath, out rel) ? rel : null;
        }

        static bool IsHidden(DirectoryInfo d)
        {
            return d.Name.StartsWith(".") || d.Name.StartsWith("$") || d.Attributes.HasFlag(FileAttributes.Hidden);
        }

        // 回傳 false 表示這次掃描不可信（沒動快照）
        bool ScanEpisodes(NewsShareConfig sc, FolderConfig root, NewsShareState st, ScanStats stats)
        {
            // 走到作品資料夾那一層；子資料夾是從父資料夾的查詢順便拿到的（含修改時間），不必再各查一次
            List<DirectoryInfo> units = new List<DirectoryInfo> { new DirectoryInfo(root.Path) };
            for (int level = 0; level < Math.Max(1, sc.Depth); level++)
            {
                List<DirectoryInfo> next = new List<DirectoryInfo>();
                foreach (DirectoryInfo d in units)
                {
                    stats.DirQueries++;
                    next.AddRange(d.GetDirectories().Where(x => IsHidden(x) == false));
                }
                units = next;
            }
            // 列不到任何資料夾、但快照裡有很多：很可能是磁碟暫時沒回應，不可信
            if (units.Count == 0 && st.Units.Count > 5)
            {
                logger.Warn("新上架掃描：{0} 列不到任何作品資料夾（快照有 {1} 筆），略過這次", sc.Name, st.Units.Count);
                return false;
            }

            long now = NowMs();
            bool baseline = st.Baselined == false;
            foreach (DirectoryInfo unit in units)
            {
                string rel = RelPath(unit.FullName);
                if (rel == null)
                {
                    continue;
                }
                // 先取資料夾修改時間再列檔案：列檔案期間如果又有變動，下一輪日期對不上就會再列一次。
                // 一定要對這個資料夾「本身」重新查詢（Refresh）：從父資料夾的列表拿到的子資料夾修改時間，在 NTFS 上會延遲更新（實測舊了一個多月都有可能），
                // 拿它判斷會漏掉新集數。重新查詢是一次輕量的屬性查詢，比列出所有檔案便宜，而且準
                unit.Refresh();
                stats.DirQueries++;
                long mtime = unit.LastWriteTimeUtc.Ticks;
                NewsUnitState us;
                bool known = st.Units.TryGetValue(rel, out us);
                if (known == false)
                {
                    us = new NewsUnitState { Announced = baseline };
                    st.Units[rel] = us;
                }
                bool pending = us.Announced == false || us.Files.Values.Any(f => f.Announced == false);
                if (known && stats.Full == false && pending == false && us.Mtime == mtime)
                {
                    stats.Skipped++;       // 新集數一定是資料夾「直接」多一個檔案（資料夾修改時間會變），沒變就不用列
                    continue;
                }
                stats.DirQueries++;
                int newlyStable = 0;
                foreach (FileInfo f in unit.GetFiles())
                {
                    if (f.Attributes.HasFlag(FileAttributes.Hidden) || TempExtensions.Contains(f.Extension)
                        || Helper.GetFileDocType(f.Extension) != Helper.ConstStrings.Video)
                    {
                        continue;
                    }
                    NewsFileState fs;
                    if (us.Files.TryGetValue(f.Name, out fs) == false)
                    {
                        fs = new NewsFileState { Size = f.Length, LastChange = now, Announced = baseline };
                        us.Files[f.Name] = fs;
                    }
                    if (fs.Announced)
                    {
                        continue;
                    }
                    if (fs.Size != f.Length)
                    {
                        fs.Size = f.Length;
                        fs.LastChange = now;
                    }
                    else if (IsStable(fs.LastChange, now))
                    {
                        fs.Announced = true;
                        newlyStable++;
                    }
                }
                us.Mtime = mtime;
                if (newlyStable > 0)
                {
                    string kind = us.Announced ? "episodes" : "series";
                    us.Announced = true;
                    AddEvent(sc.Name, kind, rel, ratingService.Resolve(unit.Name).Plain, newlyStable, now);
                    stats.Events++;
                }
            }
            st.Baselined = true;
            return true;
        }

        class FoundAlbum
        {
            public string Name;
            public int Tracks;
            public long Size;
            public long Mtime;
        }

        bool ScanAlbums(NewsShareConfig sc, FolderConfig root, NewsShareState st, ScanStats stats)
        {
            // 找出所有專輯資料夾：相對路徑 → 資訊（已通知過、修改時間沒變的專輯不會被列出內容，也就不在這裡）
            Dictionary<string, FoundAlbum> found = new Dictionary<string, FoundAlbum>();
            WalkAlbums(new DirectoryInfo(root.Path), 0, Math.Max(1, sc.MaxDepth), found, st, stats);
            if (found.Count + stats.Skipped == 0 && st.Albums.Count > 5)
            {
                logger.Warn("新上架掃描：{0} 找不到任何專輯（快照有 {1} 筆），略過這次", sc.Name, st.Albums.Count);
                return false;
            }

            long now = NowMs();
            bool baseline = st.Baselined == false;
            foreach (var kv in found)
            {
                NewsAlbumState a;
                if (st.Albums.TryGetValue(kv.Key, out a) == false)
                {
                    a = new NewsAlbumState { Size = kv.Value.Size, LastChange = now, Announced = baseline };
                    st.Albums[kv.Key] = a;
                }
                a.Mtime = kv.Value.Mtime;
                if (a.Announced)
                {
                    continue;      // 已經通知過（或是基準）的專輯，之後補進歌曲不再通知
                }
                if (a.Size != kv.Value.Size)
                {
                    a.Size = kv.Value.Size;
                    a.LastChange = now;
                }
                else if (IsStable(a.LastChange, now))
                {
                    a.Announced = true;
                    AddEvent(sc.Name, "album", kv.Key, kv.Value.Name, kv.Value.Tracks, now);
                    stats.Events++;
                }
            }
            st.Baselined = true;
            return true;
        }

        void WalkAlbums(DirectoryInfo dir, int depth, int maxDepth, Dictionary<string, FoundAlbum> found, NewsShareState st, ScanStats stats)
        {
            // 一次查詢同時取得子資料夾與檔案
            stats.DirQueries++;
            FileSystemInfo[] entries = dir.GetFileSystemInfos();
            DirectoryInfo[] subs = entries.OfType<DirectoryInfo>().Where(d => IsHidden(d) == false).ToArray();
            HashSet<string> skip = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (depth > 0)
            {
                List<FileInfo> music = entries.OfType<FileInfo>().Where(IsMusicFile).ToList();
                if (music.Count > 0)
                {
                    AddFound(dir, music.Count, music.Sum(f => f.Length), found);
                }
                else
                {
                    // 多片裝：至少 2 個光碟資料夾，裡面有音樂，就以母資料夾為一張專輯
                    DirectoryInfo[] discs = subs.Where(d => MainService.DiscNameRegex.IsMatch(d.Name)).ToArray();
                    if (discs.Length >= 2)
                    {
                        List<FileInfo> all = new List<FileInfo>();
                        foreach (DirectoryInfo d in discs)
                        {
                            stats.DirQueries++;
                            all.AddRange(d.GetFiles().Where(IsMusicFile));
                        }
                        if (all.Count > 0)
                        {
                            AddFound(dir, all.Count, all.Sum(f => f.Length), found);
                            foreach (DirectoryInfo d in discs)
                            {
                                skip.Add(d.Name);
                            }
                        }
                    }
                }
            }
            if (depth >= maxDepth)
            {
                return;
            }
            foreach (DirectoryInfo sub in subs)
            {
                if (skip.Contains(sub.Name) || MainService.AuxFolderRegex.IsMatch(sub.Name))
                {
                    continue;
                }
                // 已通知過（或基準裡）的專輯當成葉節點：修改時間沒變就整個略過，連裡面都不列
                string rel = RelPath(sub.FullName);
                NewsAlbumState known;
                if (stats.Full == false && rel != null && st.Albums.TryGetValue(rel, out known)
                    && known.Announced && known.Mtime == sub.LastWriteTimeUtc.Ticks)
                {
                    stats.Skipped++;
                    continue;
                }
                WalkAlbums(sub, depth + 1, maxDepth, found, st, stats);
            }
        }

        static bool IsMusicFile(FileInfo f)
        {
            return f.Attributes.HasFlag(FileAttributes.Hidden) == false
                && TempExtensions.Contains(f.Extension) == false
                && Helper.GetFileDocType(f.Extension) == Helper.ConstStrings.Music;
        }

        static void AddFound(DirectoryInfo dir, int tracks, long size, Dictionary<string, FoundAlbum> found)
        {
            string rel = RelPath(dir.FullName);
            if (rel != null)
            {
                found[rel] = new FoundAlbum { Name = dir.Name, Tracks = tracks, Size = size, Mtime = dir.LastWriteTimeUtc.Ticks };
            }
        }

        // ---------- 事件 ----------

        void AddEvent(string share, string kind, string path, string title, int count, long now)
        {
            EnsureLoaded();
            lock (gate)
            {
                // 同一部動畫 12 小時內陸續上架的集數，合併成同一筆（並更新時間，所以會再算成未讀）
                if (kind != "album")
                {
                    NewsEvent last = events.Where(e => e.Path == path && e.Kind != "album").OrderByDescending(e => e.Time).FirstOrDefault();
                    if (last != null && now - last.Time < MergeWindowMs)
                    {
                        last.Count += count;
                        last.Time = now;
                        Trim();
                        Persist();
                        logger.Info("新上架（合併）：{0} +{1}", title, count);
                        return;
                    }
                }
                events.Add(new NewsEvent { Id = Guid.NewGuid().ToString("N"), Share = share, Kind = kind, Path = path, Title = title, Count = count, Time = now });
                Trim();
                Persist();
                logger.Info("新上架：[{0}] {1} {2}（{3}）", share, kind, title, count);
            }
        }

        void Trim()
        {
            long cutoff = NowMs() - MaxEventAgeDays * 86400000L;
            events.RemoveAll(e => e.Time < cutoff);
            if (events.Count > MaxEvents)
            {
                events = events.OrderByDescending(e => e.Time).Take(MaxEvents).ToList();
            }
        }

        void Persist()
        {
            try { SaveJson(DataPath("news.json"), events); } catch (Exception ex) { logger.Warn(ex, "寫入 news.json 失敗"); }
        }
    }
}
