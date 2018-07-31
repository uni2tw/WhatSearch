using log4net;
using System.Collections.Generic;
using System.IO;
using WhatSearch.Core;

namespace WhatSearch
{
    internal interface IFileWatcherService
    {
        void Start(List<FolderConfig> shareFolders);
        void Stop();
    }

    public class FileWatcherService : IFileWatcherService
    {
        static ISeekService seekService = Ioc.Get<ISeekService>();
        static ILog logger = LogManager.GetLogger(typeof(FileWatcherService));
        static List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();
        public void Start(List<FolderConfig> shareFolders)
        {
            foreach (var shareFolder in shareFolders)
            {
                FileSystemWatcher watcher = new FileSystemWatcher(shareFolder.Path);
                watcher.IncludeSubdirectories = true;
                watcher.Renamed += delegate (object sender, RenamedEventArgs e)
                {
                    seekService.RemoveFolderOrFile(e.OldFullPath);
                    seekService.AppendFolderOrFile(e.FullPath);
                    logger.InfoFormat("{0}: {1}, {2}", e.ChangeType, e.Name, e.FullPath);
                };
                watcher.Created += delegate (object sender, FileSystemEventArgs e)
                {
                    seekService.AppendFolderOrFile(e.FullPath);
                    logger.InfoFormat("{0}: {1}", e.ChangeType, e.FullPath);
                };
                watcher.Deleted += delegate (object sender, FileSystemEventArgs e)
                {
                    seekService.RemoveFolderOrFile(e.FullPath);
                    logger.InfoFormat("{0}: {1}", e.ChangeType, e.FullPath);
                };
                watcher.EnableRaisingEvents = true;
                watchers.Add(watcher);
            }
        }

        public void Stop()
        {
            foreach (var watcher in watchers)
            {
                watcher.Dispose();
            }
        }
    }
}