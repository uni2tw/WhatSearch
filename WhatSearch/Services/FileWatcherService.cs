using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WhatSearch.Core;

namespace WhatSearch
{
    public class FileWatcherService : IFileWatcherService
    {
        static IDocumentService seekService = ObjectResolver.Get<IDocumentService>();
        static ILogger logger = LogManager.GetCurrentClassLogger();
        static List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();
        ILogger _logger;
        public FileWatcherService()
        {
            _logger = LogManager.GetCurrentClassLogger();
        }
        public void Start(List<FolderConfig> shareFolders)
        {
            bool anyPathFound = false;
            foreach (var shareFolder in shareFolders)
            {
                if (!Directory.Exists(shareFolder.Path))
                {
                    _logger.Error($"Path {shareFolder.Path} was not found.");
                    continue;
                }
                anyPathFound = true;
                FileSystemWatcher watcher = new FileSystemWatcher(shareFolder.Path);
                watcher.IncludeSubdirectories = true;
                watcher.Renamed += delegate (object sender, RenamedEventArgs e)
                {
                    seekService.RemoveFolderOrFile(e.OldFullPath);
                    seekService.AppendFolderOrFile(e.FullPath);
                    logger.Info("{0}: {1}, {2}", e.ChangeType, e.Name, e.FullPath);
                };
                watcher.Created += delegate (object sender, FileSystemEventArgs e)
                {
                    seekService.AppendFolderOrFile(e.FullPath);
                    logger.Info("{0}: {1}", e.ChangeType, e.FullPath);
                };
                watcher.Deleted += delegate (object sender, FileSystemEventArgs e)
                {
                    seekService.RemoveFolderOrFile(e.FullPath);
                    logger.Info("{0}: {1}", e.ChangeType, e.FullPath);
                };
                watcher.EnableRaisingEvents = true;
                watchers.Add(watcher);
            }
            if (anyPathFound == false)
            {
                throw new System.IO.DirectoryNotFoundException("分享的目錄都不存在, ShareFolders=" + string.Join(',', shareFolders.Select(x=>x.Path))); 
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