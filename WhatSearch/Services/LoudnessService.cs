using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading;
using NLayer;
using NLog;
using WhatSearch.Core;
using WhatSearch.Models;
using WhatSearch.Utilities;
using WhatSearch.Utility;

namespace WhatSearch.Services
{
    public interface ILoudnessService
    {
        /// <summary>把響度與調整量填進檢視（不存在的結果會排進背景分析）；回傳這首是否還在等分析</summary>
        bool Apply(FileInfoView view, string fullPath);
        /// <summary>只排進背景分析（例如剛收藏時）</summary>
        void Enqueue(string fullPath);
        double TargetLufs { get; }
    }

    public class LoudnessEntry
    {
        [JsonPropertyName("lufs")]
        public double? Lufs { get; set; }
        [JsonPropertyName("peak")]
        public double Peak { get; set; }
        /// <summary>解碼或量測失敗（檔案毀損等）；檔案不變就不再重試</summary>
        [JsonPropertyName("failed")]
        public bool Failed { get; set; }
    }

    /// <summary>
    /// 「我的最愛」單曲的音量平衡：以純 C#（NLayer 解碼 MP3 + BS.1770 響度量測）在背景分析，
    /// 結果依「路徑＋大小＋修改時間」存在 data/loudness.json。目前只支援 MP3；其他格式回報 unsupported，播放時維持原音量。
    /// 只分析被收藏的單曲，數量少，單一背景執行緒、低優先權依序處理即可。
    /// </summary>
    public class LoudnessService : ILoudnessService
    {
        public const string StateReady = "ready";
        public const string StatePending = "pending";
        public const string StateFailed = "failed";
        public const string StateUnsupported = "unsupported";

        static ILogger logger = LogManager.GetCurrentClassLogger();
        readonly SystemConfig config = Ioc.GetConfig();
        readonly object gate = new object();
        readonly Dictionary<string, LoudnessEntry> entries;
        readonly Queue<string> queue = new Queue<string>();
        readonly HashSet<string> queued = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Thread worker;

        public LoudnessService()
        {
            entries = LoadEntries();
        }

        public double TargetLufs
        {
            get { return config.Loudness.Target; }
        }

        static string DataPath()
        {
            return Helper.GetRelativePath("data", "loudness.json");
        }

        static Dictionary<string, LoudnessEntry> LoadEntries()
        {
            try
            {
                string path = DataPath();
                if (File.Exists(path))
                {
                    var loaded = JsonHelper.Deserialize<Dictionary<string, LoudnessEntry>>(File.ReadAllText(path));
                    if (loaded != null)
                    {
                        return new Dictionary<string, LoudnessEntry>(loaded, StringComparer.OrdinalIgnoreCase);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "讀取 loudness.json 失敗，以空白資料開始");
            }
            return new Dictionary<string, LoudnessEntry>(StringComparer.OrdinalIgnoreCase);
        }

        // 與其他快取相同的鍵：檔案變了（大小或修改時間）就視為新檔案重算
        static string KeyOf(FileInfo fi)
        {
            return fi.FullName + "|" + fi.Length + "|" + fi.LastWriteTimeUtc.Ticks;
        }

        static bool IsSupported(FileInfo fi)
        {
            return string.Equals(fi.Extension, ".mp3", StringComparison.OrdinalIgnoreCase);
        }

        public bool Apply(FileInfoView view, string fullPath)
        {
            view.Loudness = null;
            view.GainDb = null;
            FileInfo fi = string.IsNullOrEmpty(fullPath) ? null : new FileInfo(fullPath);
            if (fi == null || fi.Exists == false)
            {
                view.LoudnessState = StateFailed;
                return false;
            }
            if (IsSupported(fi) == false)
            {
                view.LoudnessState = StateUnsupported;
                return false;
            }
            string key = KeyOf(fi);
            LoudnessEntry entry;
            lock (gate)
            {
                entries.TryGetValue(key, out entry);
            }
            if (entry == null)
            {
                Enqueue(fullPath);
                view.LoudnessState = StatePending;
                return true;
            }
            if (entry.Failed || entry.Lufs.HasValue == false)
            {
                view.LoudnessState = StateFailed;
                return false;
            }
            view.Loudness = Math.Round(entry.Lufs.Value, 1);
            view.GainDb = Math.Round(ComputeGainDb(entry), 1);
            view.LoudnessState = StateReady;
            return false;
        }

        /// <summary>
        /// 增益 = 目標 - 量測響度；調高時受峰值限制（加完最高 -1 dBFS，避免爆音），再夾在設定的上下限內
        /// </summary>
        double ComputeGainDb(LoudnessEntry entry)
        {
            double gain = config.Loudness.Target - entry.Lufs.Value;
            if (gain > 0 && entry.Peak > 0)
            {
                // 峰值已經很高的歌沒有空間再調大，維持原音量（不會因此反而被調小）
                double peakDb = 20 * Math.Log10(entry.Peak);
                gain = Math.Max(0, Math.Min(gain, -1.0 - peakDb));
            }
            gain = Math.Min(gain, config.Loudness.MaxBoostDb);
            return Math.Max(gain, -config.Loudness.MaxCutDb);
        }

        public void Enqueue(string fullPath)
        {
            try
            {
                FileInfo fi = new FileInfo(fullPath);
                if (fi.Exists == false || IsSupported(fi) == false)
                {
                    return;
                }
                string key = KeyOf(fi);
                lock (gate)
                {
                    if (entries.ContainsKey(key) || queued.Contains(key))
                    {
                        return;
                    }
                    queued.Add(key);
                    queue.Enqueue(fullPath);
                    if (worker == null)
                    {
                        worker = new Thread(WorkLoop) { IsBackground = true, Priority = ThreadPriority.BelowNormal, Name = "LoudnessWorker" };
                        worker.Start();
                    }
                    Monitor.Pulse(gate);
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "排入響度分析失敗 {0}", fullPath);
            }
        }

        void WorkLoop()
        {
            while (true)
            {
                string path;
                lock (gate)
                {
                    while (queue.Count == 0)
                    {
                        Monitor.Wait(gate);
                    }
                    path = queue.Dequeue();
                }
                FileInfo fi = new FileInfo(path);
                string key = fi.Exists ? KeyOf(fi) : null;
                LoudnessEntry entry = new LoudnessEntry { Failed = true };
                if (key != null)
                {
                    try
                    {
                        var watch = System.Diagnostics.Stopwatch.StartNew();
                        LoudnessResult r = AnalyzeMp3(path);
                        entry = new LoudnessEntry { Lufs = r.IntegratedLufs, Peak = r.SamplePeak, Failed = r.IntegratedLufs.HasValue == false };
                        logger.Info("響度分析 {0}：{1} LUFS，峰值 {2:F3}，耗時 {3}ms", path,
                            r.IntegratedLufs.HasValue ? r.IntegratedLufs.Value.ToString("F2") : "無法量測", r.SamplePeak, watch.ElapsedMilliseconds);
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex, "響度分析失敗 {0}", path);
                    }
                }
                lock (gate)
                {
                    if (key != null)
                    {
                        queued.Remove(key);
                        entries[key] = entry;
                        Save();
                    }
                }
            }
        }

        /// <summary>解碼整個 MP3 並量測；交給 LoudnessMeter 逐批處理，不把整首 PCM 放進記憶體</summary>
        public static LoudnessResult AnalyzeMp3(string path)
        {
            using (var mpeg = new MpegFile(path))
            {
                int channels = mpeg.Channels;
                // 解碼器會回報實際聲道數；超過 2 聲道（MP3 不會發生）不處理
                if (channels < 1 || channels > 2)
                {
                    throw new NotSupportedException("不支援的聲道數 " + channels);
                }
                var meter = new LoudnessMeter(mpeg.SampleRate, channels);
                float[] buffer = new float[16384 * channels];
                int read;
                while ((read = mpeg.ReadSamples(buffer, 0, buffer.Length)) > 0)
                {
                    meter.Add(buffer, read);
                }
                return meter.Finish();
            }
        }

        // 呼叫端已持有 gate
        void Save()
        {
            try
            {
                SafeFile.WriteAllTextAtomic(DataPath(), JsonHelper.Serialize(entries, indented: true));        // 被防毒等程式暫時鎖住時會自動重試
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "寫入 loudness.json 失敗");
            }
        }
    }
}
