using Newtonsoft.Json;
using WhatSearch.Utility;
using System.Collections.Generic;
using System.IO;

namespace WhatSearch.Core
{
    public class SystemConfig
    {
        [JsonProperty("folders")]
        public List<FolderConfig> Folders { get; set; }

        public static SystemConfig Reload()
        {
            string json = File.ReadAllText(Helper.GetRelativePath("config.json"));
            return JsonConvert.DeserializeObject<SystemConfig>(json);
        }
    }

    public class FolderConfig
    {
        [JsonProperty("path")]
        public string Path { get; set; }

        public override string ToString()
        {
            return this.Path;
        }
    }
}
