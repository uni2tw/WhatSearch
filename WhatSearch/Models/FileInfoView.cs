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
        [JsonPropertyName("path")]
        public string Path { get; set; }
        [JsonPropertyName("artist")]
        public string Artist { get; set; }
        [JsonPropertyName("album")]
        public string Album { get; set; }
        /// <summary>音樂 ID3 標題（沒有標籤為 null）</summary>
        [JsonPropertyName("track_title")]
        public string TrackTitle { get; set; }
        /// <summary>音樂 ID3 音軌編號</summary>
        [JsonPropertyName("track")]
        public uint? Track { get; set; }
        /// <summary>音樂 ID3 年份</summary>
        [JsonPropertyName("year")]
        public uint? Year { get; set; }
        /// <summary>音樂 ID3 專輯演出者</summary>
        [JsonPropertyName("album_artist")]
        public string AlbumArtist { get; set; }
        /// <summary>檔案大小（位元組）；供前端估算影片位元率</summary>
        [JsonPropertyName("bytes")]
        public long? Bytes { get; set; }
        /// <summary>影片長度（秒）；非影片或讀不到為 null</summary>
        [JsonPropertyName("duration")]
        public double? Duration { get; set; }

        public override string ToString()
        {
            return Title;
        }
    }

}
