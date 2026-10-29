using System.Collections.Generic;
using System.IO;
using WhatSearch.Models;

namespace WhatSearch.Service
{
    public interface ISearchSercice
    {
        void Build(IEnumerable<FileInfo> deals);
        /// <summary>移除指定的檔案索引；傳多個時只 commit 一次</summary>
        void Remove(params string[] docIds);
        /// <summary>移除這個資料夾底下所有檔案（含子資料夾）的索引；資料夾本身已不存在（刪除、改名之後）也可以用，不需要讀檔案系統</summary>
        void RemoveFolder(string dirPath);
        List<IndexedFileDoc> Query(string queryString, int maxDoc = 100);
        int DocCount { get; }
    }
}
