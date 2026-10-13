using System.Collections.Generic;

namespace WhatSearch.Models
{
    /// <summary>多片裝專輯的一張光碟（母資料夾下的 Disc 1 / CD2 …）及其歌曲</summary>
    public class DiscGroup
    {
        public string Id { get; set; }
        public int No { get; set; }
        public string Title { get; set; }
        public List<FileInfoView> Items { get; set; }
    }
}
