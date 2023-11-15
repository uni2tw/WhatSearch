using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using WhatSearch.Core;

namespace WhatSearch
{
    public class FileWatcherService : IFileWatcherService
    {
        static IDocumentService seekService = Ioc.Get<IDocumentService>();
        static ILogger logger = LogManager.GetCurrentClassLogger();
        static List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();
        public void Start(List<FolderConfig> shareFolders)
        {
            foreach (var shareFolder in shareFolders)
            {
                try
                {
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
                catch (Exception)
                {
                    Console.WriteLine($"Path {shareFolder} not found");
                }
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