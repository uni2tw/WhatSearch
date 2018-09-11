using Newtonsoft.Json;
using WhatSearch.Utility;
using System.Collections.Generic;
using System.IO;
using System;
using Newtonsoft.Json.Converters;

namespace WhatSearch.Core
{
    public class SystemConfig
    {
        public string HostName { get; set; }
        public int Port { get; set; }
        [JsonProperty("debug")]
        public bool IsDebug { get; set; }

        [JsonProperty("watch")]
        public bool EnableWatch { get; set; }

        [JsonProperty("folders")]
        public List<FolderConfig> Folders { get; set; }

        [JsonProperty("playtypes")]
        public HashSet<string> PlayTypes { get; set; }

        [JsonProperty("playWhiteIps")]
        public HashSet<string> PlayWhiteIps { get; set; }

        //public HashSet

        public static SystemConfig Reload()
        {            
            string json = File.ReadAllText(Helper.GetRelativePath("config.json"));
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

    public class FolderConfig
    {
        [JsonProperty("path")]
        public string Path { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }

        public override string ToString()
        {
            return this.Path;
        }
    }
}
