using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WhatSearch.Models.MMPlayModels
{
    public class MediaMetadata
    {
        public string id { get;  set; }
        public string title { get; set; }
        public string cover { get; set; }
        public int like { get; set; }
        public int dislike { get; set; }
        public int hidden { get; set; }
        public int uncensored { get; set; }
        public string category { get; set; }
    }
}
