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
