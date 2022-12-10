using System.Text.Json.Serialization;

namespace WhatSearch.Models
{
    public class FileInfoView
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("get_url")]
        public string GetUrl { get; set; }
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("modify")]
        public string Modify { get; set; }
        [JsonPropertyName("size")]
        public string Size { get; set; }

        public override string ToString()
        {
            return Title;
        }
    }

}
