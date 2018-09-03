using Newtonsoft.Json;
using System;
using System.IO;

namespace WhatSearch.Models
{
    public class IndexedFileDoc
    {
        public class Columns
        {
            public const string Name = "Name";
            public const string DirectoryName = "DirectoryName";
            public const string FullName = "FullName";
            public const string Length = "Length";
            public const string CreationTime = "CreationTime";
            public const string LastWriteTime = "LastWriteTime";
            public const string Extension = "Extension";
            /// <summary>
            /// it's unique key
            /// </summary>
            public const string Id = "Id";
        }        
        [JsonProperty("file")]
        public string Name { get; set; }
        [JsonProperty("path")]
        public string DirectoryName { get; set; }
        [JsonIgnore]
        public string FullName { get; set; }
        [JsonProperty("size")]
        public long Length { get; set; }
        [JsonProperty("create")]
        public DateTime CreationTime { get; set; }
        [JsonProperty("modify")]
        public DateTime LastWriteTime { get; set; }


    }

}
