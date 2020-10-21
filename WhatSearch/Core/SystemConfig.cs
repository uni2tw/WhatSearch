using Newtonsoft.Json;
using WhatSearch.Utility;
using System.Collections.Generic;
using System.IO;
using System;
using Newtonsoft.Json.Converters;
using System.ComponentModel;
using log4net.Repository.Hierarchy;

namespace WhatSearch.Core
{
    public class SystemConfig
    {
        public string HostName { get; set; }
        public int Port { get; set; }
        [JsonProperty("debug")]
        public bool IsDebug { get; set; }
        public string Version { get; set; }

        [JsonProperty("watch")]
        public bool EnableWatch { get; set; }

        public LoginConfig Login { get; set; }

        [JsonProperty("folders")]
        public List<FolderConfig> Folders { get; set; }

        [JsonProperty("playtypes")]
        public HashSet<string> PlayTypes { get; set; }

        [JsonProperty("playWhiteIps")]
        public HashSet<string> PlayWhiteIps { get; set; }
        [JsonProperty("ContentsFolder")]
        public string ContentsFolder { get; set; }

        [JsonProperty("maxSearchResult", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(100)]
        public int MaxSearchResult { get; set; }

        [JsonProperty("line")]
        public LineConfig Line { get; set; }

        [JsonProperty("upload")]
        public UploadConfig Upload { get; set; }

        [JsonProperty("mmplay")]
        public MMPlayConfig MMPlay { get; set; }

        //public HashSet

        public static SystemConfig Reload()
        {
            string configPath = Helper.GetRelativePath("config.json");
            string json = File.ReadAllText(configPath);
            Console.WriteLine("config " + configPath + " ok");
            var result = JsonConvert.DeserializeObject<SystemConfig>(json,
                new HashSetIgnoreCaseCreationConverter<string>(StringComparer.OrdinalIgnoreCase));
            if (result.PlayTypes == null)
            {
                result.PlayTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
            return result;
        }

        public class HashSetIgnoreCaseCreationConverter<T> : CustomCreationConverter<HashSet<T>>
        {
            public IEqualityComparer<T> Comparer { get; private set; }

            public HashSetIgnoreCaseCreationConverter(IEqualityComparer<T> comparer)
            {
                this.Comparer = comparer;
            }

            public override HashSet<T> Create(Type objectType)
            {
                return new HashSet<T>(Comparer);
            }
        }

    }

    public class LineConfig {
        [JsonProperty("clientId")]
        public string ClientId { get; set; }
        [JsonProperty("clientSecret")]
        public string ClientSecret { get; set; }
        [JsonProperty("callback")]
        public string Callback { get; set; }
    }

    public class FolderConfig
    {
        [JsonProperty("path")]
        public string Path { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("protected")]
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
