using WhatSearch.Core;
using WhatSearch.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace WhatSearch
{
    public abstract class SeekServiceBase : ISeekService
    {
        Action<string> taskQueuedCallback;
        Action<string> taskDoneCallback;
        Action<int> allDoneCallback;

        private int _progressMaxinum;
        private int _progressValue;
        public abstract string GetProgressPercent();

        protected ISearchManager search = Ioc.Get<ISearchManager>();
        protected void BuildSearchDoc(DirectoryInfo dirInfo)
        {
            IEnumerable<FileInfo> fileInfos = dirInfo.GetFiles();
            search.Build(fileInfos);
        }

        protected void BuildIndex(FileInfo fileInfo)
        {            
            search.Build(new[] { fileInfo });
        }
        

        protected void RemoveSearchDoc(params string[] docIds)
        {
            foreach (string docId in docIds)
            {
                search.Remove(docId);
            }
        }

        public void SetCallback(Action<string> taskQueuedCallback,
            Action<string> taskDoneCallback, Action<int> allDoneCallback)
        {
            this.taskQueuedCallback = taskQueuedCallback;
            this.taskDoneCallback = taskDoneCallback;
            this.allDoneCallback = allDoneCallback;
        }

        protected void TriggerFolderSeekStart(string folderPath)
        {
            Interlocked.Increment(ref _progressMaxinum);
            if (taskQueuedCallback != null)
            {
                taskQueuedCallback(folderPath);
            }
        }

        protected void TriggerFolderSeekDone(string folderPath)
        {
            Interlocked.Increment(ref _progressValue);
            if (taskDoneCallback != null)
            {
                taskDoneCallback(folderPath);
            }
            if (_progressValue == _progressMaxinum)
            {
                allDoneCallback(_progressValue);
            }
        }
        public abstract void Start(List<FolderConfig> startFolders);
        public abstract void RemoveFolderOrFile(string oldFullPath);
        public abstract void AppendFolderOrFile(string fullPath);
    }
}
