using WhatSearch.Core;
using System;
using System.Collections.Generic;

namespace WhatSearch
{
    public interface IDocumentService
    {
        void Start(List<FolderConfig> shareFolders);
        void SetCallback(Action<string> taskQueuedCallback, 
            Action<string> taskDoneCallback, Action<int> allDoneCallback);
        string GetProgressPercent();
        void RemoveFolderOrFile(string oldFullPath);
        void AppendFolderOrFile(string fullPath);
    }
}
