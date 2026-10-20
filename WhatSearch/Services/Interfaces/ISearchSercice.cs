using System.Collections.Generic;
using System.IO;
using WhatSearch.Models;

namespace WhatSearch.Service
{
    public interface ISearchSercice
    {
        void Build(IEnumerable<FileInfo> deals);
        void Remove(string docId);
        List<IndexedFileDoc> Query(string queryString, int maxDoc = 100);
        int DocCount { get; }
    }
}
