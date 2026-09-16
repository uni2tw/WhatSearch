using WhatSearch.Utility;
using System.Collections.Generic;
using System.IO;
using System;
using System.ComponentModel;
using System.Diagnostics;
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
            SystemConfig result;
            try
            {
                result = JsonHelper.Deserialize<SystemConfig>(json, caseInsensitive: true);
            } catch (Exception ex)
            {
                result = new SystemConfig();
            }
            if (result.PlayTypes == null)
            {
                result.PlayTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

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
