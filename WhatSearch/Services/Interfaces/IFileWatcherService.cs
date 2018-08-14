using System.Collections.Generic;
using WhatSearch.Core;

namespace WhatSearch
{
    public interface IFileWatcherService
    {
        void Start(List<FolderConfig> shareFolders);
        void Stop();
    }
}