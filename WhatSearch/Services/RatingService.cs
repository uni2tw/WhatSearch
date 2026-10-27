using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using WhatSearch.Core;
using WhatSearch.Models;
using WhatSearch.Utility;
using WhatSearch.Utilities;

namespace WhatSearch.Services
{
    public interface IRatingService
    {
        /// <summary>這個資料夾是否可評分（依設定的分享根與層數）</summary>
        bool IsRateable(string folderFullPath);
        /// <summary>把可評分資料夾的「顯示名稱（去掉星等後綴）、評分」填進檢視；不可評分就不動</summary>
        void Apply(FileInfoView view, string folderFullPath);
        /// <summary>資料夾名稱 → 評分用的名稱與有效評分（系統記錄優先，沒有就用名稱結尾的 (★★★☆)）</summary>
        RatingInfo Resolve(string folderName);
        /// <summary>設定評分（0 = 未評分），key 為 Resolve 回傳的 Key</summary>
        void Set(string key, double rating);
        /// <summary>這個資料夾位在啟用專輯評分的分享根底下，而且不是分享根本身、也不是專輯裡的光碟或附屬資料夾（Disc 1、Scans…）；還要由呼叫端確認它是專輯</summary>
        bool IsAlbumShare(string folderFullPath);
        /// <summary>專輯名稱 → 評分用的名稱與評分（key 加 album| 前綴，與動畫分開）；沒有評分為 null</summary>
        RatingInfo ResolveAlbum(string folderName);
        /// <summary>列表上的專輯資料夾：已評分才把評分填進 AlbumRating</summary>
        void ApplyAlbumListRating(FileInfoView view, string folderFullPath);
    }

    public class RatingInfo
    {
        /// <summary>去掉結尾星等、空白收斂後的名稱，同時也是評分的 key</summary>
        public string Key { get; set; }
        /// <summary>去掉結尾星等後的名稱（保留原本的空白，只去頭尾）</summary>
        public string Plain { get; set; }
        /// <summary>有效評分：0.5~5；沒有評分為 null；0 代表使用者明確設為「未評分」</summary>
        public double? Rating { get; set; }
    }

    /// <summary>
    /// 動畫（追番）的星等評分，整個系統共用一份，存在 data/ratings.json（{ 名稱: 評分 }）。
    /// 評分記在系統裡而不是改資料夾名稱，所以不會改變資料夾 id（續播進度、收藏、書籤都不受影響）；
    /// key 是去掉結尾星等的名稱，動畫搬到別的資料夾評分仍然跟著走。
    /// 系統裡沒有評分時，沿用資料夾名稱結尾的「(★★★☆)」（★ 算 1、結尾的 ☆ 算 0.5）當預設值。
    /// </summary>
    public class RatingService : IRatingService
    {
        static ILogger logger = LogManager.GetCurrentClassLogger();
        static readonly object thisLock = new object();
        // 名稱結尾的 (★★★☆)，全形括號也接受
        static readonly Regex SuffixRegex = new Regex(@"\s*[\(（]\s*([★☆]+)\s*[\)）]\s*$", RegexOptions.Compiled);
        static readonly Regex SpaceRegex = new Regex(@"\s+", RegexOptions.Compiled);

        readonly SystemConfig config = Ioc.GetConfig();
        Dictionary<string, double> cache;

        static string DataPath()
        {
            return Helper.GetRelativePath("data", "ratings.json");
        }

        Dictionary<string, double> Load()
        {
            if (cache != null)
            {
                return cache;
            }
            cache = new Dictionary<string, double>(StringComparer.Ordinal);
            try
            {
                string path = DataPath();
                if (File.Exists(path))
                {
                    var loaded = JsonHelper.Deserialize<Dictionary<string, double>>(File.ReadAllText(path));
                    if (loaded != null)
                    {
                        cache = loaded;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "讀取 ratings.json 失敗，以空白資料開始");
            }
            return cache;
        }

        public bool IsRateable(string folderFullPath)
        {
            var rc = config.Rating;
            if (rc == null || rc.Shares == null || rc.Shares.Count == 0 || string.IsNullOrEmpty(folderFullPath))
            {
                return false;
            }
            string rel;
            if (PathUtility.TryGetRelPath(folderFullPath, out rel) == false)
            {
                return false;
            }
            string[] parts = rel.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return false;
            }
            // parts[0] 是分享根的名稱；其下第 N 層才是動畫（Anime/2026-4/作品 = 第 2 層）
            bool shareMatch = rc.Shares.Any(s => string.Equals(s, parts[0], StringComparison.OrdinalIgnoreCase));
            return shareMatch && parts.Length - 1 == Math.Max(1, rc.Depth);
        }

        public void Apply(FileInfoView view, string folderFullPath)
        {
            if (IsRateable(folderFullPath) == false)
            {
                return;
            }
            RatingInfo info = Resolve(Path.GetFileName(folderFullPath.TrimEnd('\\', '/')));
            view.Rateable = true;
            view.TitlePlain = info.Plain;
            view.Rating = info.Rating;
        }

        const string AlbumKeyPrefix = "album|";

        public bool IsAlbumShare(string folderFullPath)
        {
            var shares = config.Rating == null || config.Rating.Albums == null ? null : config.Rating.Albums.Shares;
            if (shares == null || shares.Count == 0 || string.IsNullOrEmpty(folderFullPath))
            {
                return false;
            }
            string rel;
            if (PathUtility.TryGetRelPath(folderFullPath, out rel) == false)
            {
                return false;
            }
            string[] parts = rel.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || shares.Any(s => string.Equals(s, parts[0], StringComparison.OrdinalIgnoreCase)) == false)
            {
                return false;
            }
            string name = parts[parts.Length - 1];
            // 光碟資料夾（Disc 1）與附屬資料夾（Scans、Artwork）是專輯的一部分，評分記在它們的母資料夾
            return MainService.DiscNameRegex.IsMatch(name) == false && MainService.AuxFolderRegex.IsMatch(name) == false;
        }

        public RatingInfo ResolveAlbum(string folderName)
        {
            string plain = (folderName ?? string.Empty).Trim();
            string key = AlbumKeyPrefix + SpaceRegex.Replace(plain, " ");
            double? rating = null;
            lock (thisLock)
            {
                double v;
                if (Load().TryGetValue(key, out v))
                {
                    rating = v;
                }
            }
            return new RatingInfo { Key = key, Plain = plain, Rating = rating };
        }

        public void ApplyAlbumListRating(FileInfoView view, string folderFullPath)
        {
            if (IsAlbumShare(folderFullPath) == false)
            {
                return;
            }
            RatingInfo info = ResolveAlbum(Path.GetFileName(folderFullPath.TrimEnd('\\', '/')));
            if (info.Rating.HasValue && info.Rating.Value > 0)
            {
                view.AlbumRating = info.Rating;
            }
        }

        public RatingInfo Resolve(string folderName)
        {
            folderName = folderName ?? string.Empty;
            double? legacy = null;
            string plain = folderName;
            Match m = SuffixRegex.Match(folderName);
            if (m.Success)
            {
                string stars = m.Groups[1].Value;
                int full = stars.Count(c => c == '★');
                bool half = stars.Contains('☆');
                legacy = Math.Min(5.0, full + (half ? 0.5 : 0.0));
                plain = folderName.Substring(0, m.Index);
            }
            plain = plain.Trim();
            string key = SpaceRegex.Replace(plain, " ");
            double? effective = legacy;
            lock (thisLock)
            {
                double sys;
                if (Load().TryGetValue(key, out sys))
                {
                    effective = sys;
                }
            }
            return new RatingInfo { Key = key, Plain = plain, Rating = effective };
        }

        public void Set(string key, double rating)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }
            lock (thisLock)
            {
                var all = Load();
                all[key] = rating;
                try
                {
                    // 先寫暫存檔再取代，避免寫到一半當掉把評分清空；檔案被防毒等程式暫時鎖住時會自動重試
                    SafeFile.WriteAllTextAtomic(DataPath(), JsonHelper.Serialize(all, indented: true));
                }
                catch (Exception ex)
                {
                    logger.Warn(ex, "寫入 ratings.json 失敗");
                    throw;
                }
            }
        }
    }
}
