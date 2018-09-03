using Newtonsoft.Json;

namespace WhatSearch.Models
{
    public class FileInfoView
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("modify")]
        public string Modify { get; set; }
        [JsonProperty("size")]
        public string Size { get; set; }

        public override string ToString()
        {
            return Title;
        }
    }

}
