﻿using System;
using System.IO;
using System.Text.Json.Serialization;

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
        [JsonPropertyName("file")]
        public string Name { get; set; }
        [JsonPropertyName("path")]
        public string DirectoryName { get; set; }
        [JsonIgnore]
        public string FullName { get; set; }
        [JsonPropertyName("size")]
        public long Length { get; set; }
        [JsonPropertyName("create")]
        public DateTime CreationTime { get; set; }
        [JsonPropertyName("modify")]
        public DateTime LastWriteTime { get; set; }


    }

}
