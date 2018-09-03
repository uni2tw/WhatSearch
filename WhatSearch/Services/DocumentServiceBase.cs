using WhatSearch.Core;
using WhatSearch.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace WhatSearch
{
    public abstract class DocumentServiceBase : IDocumentService
    {
        Action<string> taskQueuedCallback;
        Action<string> taskDoneCallback;
        Action<int> allDoneCallback;

        private int _progressMaxinum;
        private int _progressValue;
        public abstract string GetProgressPercent();

        protected ISearchSercice search = Ioc.Get<ISearchSercice>();        

        protected void BuildSearchDoc(DirectoryInfo dirInfo)
        {
            IEnumerable<FileInfo> fileInfos = dirInfo.GetFiles()
                .Where(t => t.Attributes.HasFlag(FileAttributes.Hidden) == false);
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

        protected void TriggerSeekFolderStart(string folderPath)
        {
            Interlocked.Increment(ref _progressMaxinum);
            if (taskQueuedCallback != null)
            {
                taskQueuedCallback(folderPath);
            }
        }

        protected void TriggerSeekFolderDone(string folderPath)
        {
            Interlocked.Increment(ref _progressValue);
            if (taskDoneCallback != null)
            {
                taskDoneCallback(folderPath);
            }
            if (_progressValue == _progressMaxinum)
            {                
                allDoneCallback(search.DocCount);
            }
        }

        public abstract void Start(List<FolderConfig> startFolders);
        public abstract void RemoveFolderOrFile(string oldFullPath);
        public abstract void AppendFolderOrFile(string fullPath);
    }
}
