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

    public class UploadConfig
    {
        public bool Enabled { get; set; }
        public string Folder { get; set; }
        public long? LimitMb { get; set; }
    }

}
