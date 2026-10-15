using WhatSearch.Utility;
using WhatSearch.Utilities;
using System.Collections.Generic;
using System.IO;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Serialization;

namespace WhatSearch.Core
{
    public class SystemConfig
    {
        [JsonPropertyName("watch")]
        public bool EnableWatch { get; set; }

        public LoginConfig Login { get; set; }

        [JsonPropertyName("folders")]
        public List<FolderConfig> Folders { get; set; }

        [JsonPropertyName("playtypes")]
        public HashSet<string> PlayTypes { get; set; }

        /// <summary>
        /// 改由 HostEnviroment 取得
        /// </summary>
        [JsonIgnore]
        public string ContentRootPath { get; set; }

        [JsonPropertyName("maxSearchResult")]
        public int MaxSearchResult { get; set; }

        [JsonPropertyName("upload")]
        public UploadConfig Upload { get; set; }

        /// <summary>動畫星等評分的設定；沒設定 shares 就不啟用</summary>
        [JsonPropertyName("rating")]
        public RatingConfig Rating { get; set; } = new RatingConfig();

        /// <summary>音量平衡（我的最愛單曲）的設定；沒寫就用預設值</summary>
        [JsonPropertyName("loudness")]
        public LoudnessConfig Loudness { get; set; } = new LoudnessConfig();

        //public HashSet

        public static SystemConfig Reload()
        {
            string configPath = Helper.GetRelativePath("config.json");
            string json = File.ReadAllText(configPath);
            Console.WriteLine("config " + configPath + " ok");
            // 各機器自己的覆蓋設定（不進版控）：有就疊加在 config.json 上，格式錯誤時忽略並提示，不讓站台起不來
            string localPath = Helper.GetRelativePath("config.local.json");
            if (File.Exists(localPath))
            {
                try
                {
                    json = ConfigMerger.Merge(json, File.ReadAllText(localPath));
                    Console.WriteLine("config " + localPath + " merged");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("config.local.json 解析失敗，已忽略：" + ex.Message);
                }
            }
            SystemConfig result;
            try
            {
                result = JsonHelper.Deserialize<SystemConfig>(json, caseInsensitive: true);
            } catch (Exception ex)
            {
                result = new SystemConfig();
            }
            // JSON 反序列化出來的 HashSet 預設區分大小寫，要重建成不分大小寫，.MP3 才比對得到 .mp3
            result.PlayTypes = new HashSet<string>(
                result.PlayTypes ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);

            if (result.MaxSearchResult == 0)
            {
                result.MaxSearchResult = 100;
            }
            if (result.Loudness == null)
            {
                result.Loudness = new LoudnessConfig();
            }
            if (result.Rating == null)
            {
                result.Rating = new RatingConfig();
            }

            return result;
        }



    }

    public class FolderConfig
    {
        [JsonPropertyName("path")]
        public string Path { get; set; }
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("protected")]
        public bool isProtected { get; set; }

        public override string ToString()
        {
            return this.Path;
        }
    }

    public class LoginConfig
    {
        public int CookieDays { get; set; }
    }

    public class RatingConfig
    {
        /// <summary>啟用評分的分享根名稱（config 裡 folders 的 title），例如 ["Anime"]；空代表不啟用</summary>
        [JsonPropertyName("shares")]
        public List<string> Shares { get; set; } = new List<string>();
        /// <summary>動畫資料夾位在分享根底下第幾層：Anime6-4\作品 為 2，只有這一層的資料夾可評分</summary>
        [JsonPropertyName("depth")]
        public int Depth { get; set; } = 2;
    }

    public class LoudnessConfig
    {
        /// <summary>目標響度（LUFS）。數字越接近 0 整體越大聲；-14 與主流串流平台相同，-18 是 ReplayGain 的傳統基準</summary>
        [JsonPropertyName("target")]
        public double Target { get; set; } = -14.0;
        /// <summary>最多調高幾 dB（還會受歌曲峰值限制，避免爆音）</summary>
        [JsonPropertyName("maxBoostDb")]
        public double MaxBoostDb { get; set; } = 12.0;
        /// <summary>最多調低幾 dB</summary>
        [JsonPropertyName("maxCutDb")]
        public double MaxCutDb { get; set; } = 20.0;
    }

    public class UploadConfig
    {
        public bool Enabled { get; set; }
        public string Folder { get; set; }
        public long? LimitMb { get; set; }
    }

}
