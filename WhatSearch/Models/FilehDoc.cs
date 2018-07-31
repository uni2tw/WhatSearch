using System;
using System.IO;

namespace WhatSearch.Models
{
    public class FilehDoc
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

        public string Name { get; set; }
        public string DirectoryName { get; set; }
        public string FullName { get; set; }
        public long Length { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime LastWriteTime { get; set; }


    }

}
