using WhatSearch.Core;
using WhatSearch.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using WhatSearch.Utility;

namespace WhatSearch
{

    class Program
    {
        private static readonly log4net.ILog logger =
            log4net.LogManager.GetLogger(typeof(Program));
        static void Main(string[] args)
        {
            InitLog4net();
            Ioc.Init();

            var shareFolders = Ioc.GetConfig().Folders;

            ISearchManager searchManager = Ioc.Get<ISearchManager>();
            ISeekService seekService = Ioc.Get<ISeekService>();
            seekService.SetCallback(delegate (string folderPath)
            {
                //Console.WriteLine(folderPath + " was queued.");
            }, delegate (string folderPath)
            {
                //Console.WriteLine(folderPath + " was done.");
            }, delegate (int docCount)
            {
                Console.WriteLine("總共有 {0} 檔案己加入索引", docCount);
            });

            seekService.Start(shareFolders);
            IFileWatcherService watcherService = Ioc.Get<IFileWatcherService>();
            watcherService.Start(shareFolders);
            
            do
            {
                string line = Console.ReadLine();
                if (line == "cls")
                {
                    Console.Clear();
                    continue;
                }
                else if (line == "")
                {
                    break;
                }
                List<string> items = searchManager.Query(line.Trim());
                Console.WriteLine("找到 " + items.Count + " 筆.");
                if (items.Count > 0)
                {
                    foreach (var item in items)
                    {
                        Console.WriteLine(item);
                    }
                }
                
            } while (true);

            watcherService.Stop();
        }

        static void InitLog4net()
        {
            var repo = log4net.LogManager.CreateRepository(
                Assembly.GetEntryAssembly(), typeof(log4net.Repository.Hierarchy.Hierarchy));

            string logConfigFilePath = Helper.GetRelativePath("log4net.config");
            log4net.Config.XmlConfigurator.Configure(repo, new FileInfo(logConfigFilePath));

            logger.Info("Application - Main is invoked");
        }
    }
}
