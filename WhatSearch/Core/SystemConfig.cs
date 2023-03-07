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
        [JsonPropertyName("db")]
        public DbConfig Db { get; set; }

        [JsonPropertyName("folders")]
        public List<FolderConfig> Folders { get; set; }

        [JsonPropertyName("playtypes")]
        public HashSet<string> PlayTypes { get; set; }

        [JsonPropertyName("playWhiteIps")]
        public HashSet<string> PlayWhiteIps { get; set; }

        /// <summary>
        /// 改由 HostEnviroment 取得
        /// </summary>
        [JsonIgnore]
        public string ContentRootPath { get; set; }

        [JsonPropertyName("maxSearchResult")]
        public int MaxSearchResult { get; set; }

        [JsonPropertyName("line")]
        public LineConfig Line { get; set; }

        [JsonPropertyName("upload")]
        public UploadConfig Upload { get; set; }

        [JsonPropertyName("mmplay")]
        public MMPlayConfig MMPlay { get; set; }

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

    public class LineConfig {
        [JsonPropertyName("clientId")]
        public string ClientId { get; set; }
        [JsonPropertyName("clientSecret")]
        public string ClientSecret { get; set; }
        [JsonPropertyName("callback")]
        public string Callback { get; set; }
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

    public class DbConfig
    {
        public string Primary { get; set; }
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

    public class MMPlayConfig
    {
        public string Index { get; set; }
        public List<MMPlayPageSection> Pages { get; set; }
        public bool Develop { get; set; }
    }

    public class MMPlayPageSection
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Folder { get; set; }
        public bool ShowOnTop { get; set; }

        public override string ToString()
        {
            return string.Format("{0}: {1}", this.Id, this.Title);
        }
    }
}
