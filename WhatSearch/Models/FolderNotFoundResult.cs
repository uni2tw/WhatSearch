using System.Collections.Generic;

namespace WhatSearch.Models
{
    public class FolderSuggestion
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public double Score { get; set; }
    }

    /// <summary>要求的資料夾不存在時的解析結果：最近一層還存在的上層，以及名稱相近的同層資料夾建議</summary>
    public class FolderNotFoundResult
    {
        public string RequestedPath { get; set; }
        public string NearestId { get; set; }
        public string NearestTitle { get; set; }
        public List<FolderSuggestion> Suggestions { get; set; }
    }
}
