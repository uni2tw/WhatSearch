using WhatSearch.Core;
using WhatSearch.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using WhatSearch.Utility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
using WhatSearch.Services.Interfaces;
using WhatSearch.Models;

namespace WhatSearch
{
    class Program
    {
        private static readonly log4net.ILog logger =
            log4net.LogManager.GetLogger(typeof(Program));
        static void Main(string[] args)
        {
            InitLog4net();
            Ioc.Register();
            string simpStr1 = Ioc.Get<IChineseConverter>().ToSimplifiedChinese(
                "財富管理信託管理費計收方式依開戶總約定書之約定辦理，實際收取時間依本公司公告");
            string simpStr2 = Ioc.Get<IChineseConverter>().ToSimplifiedChinese(
                "請提供預設記憶體大小及硬碟容量，以及所需軟體清單，方便進行資料庫管理");
            string simpStr3 = Ioc.Get<IChineseConverter>().ToSimplifiedChinese(
                "美國總統川普上任以來發生不少風波，邁入2018後回顧2017年，仍可發現他完成許多政策，美媒選出了最具有代表性的十大政績");
            string simpStr4 = Ioc.Get<IChineseConverter>().ToSimplifiedChinese(
                "計程車");

            var shareFolders = Ioc.GetConfig().Folders;

            ISearchSercice searchService = Ioc.Get<ISearchSercice>();
            IDocumentService gatherService = Ioc.Get<IDocumentService>();
            gatherService.SetCallback(delegate (string folderPath)
            {
                //Console.WriteLine(folderPath + " was queued.");
            }, delegate (string folderPath)
            {
                //Console.WriteLine(folderPath + " was done.");
            }, delegate (int docCount)
            {
                Console.WriteLine("總共有 {0} 檔案己加入索引", docCount);
            });

            var config = Ioc.GetConfig();

            gatherService.Start(shareFolders);
            IFileWatcherService watcherService = Ioc.Get<IFileWatcherService>();
            if (config.EnableWatch)
            {
                watcherService.Start(shareFolders);
            }

            var webHostBuilder = WebHost.CreateDefaultBuilder(args)
                .UseKestrel(t =>
                {
                    t.Limits.MaxConcurrentConnections = 10;
                    t.ListenAnyIP(config.Port);
                    //t.Listen(IPAddress.Any, 443, listenOptions =>
                    //{
                    //    listenOptions.UseHttps("server.pfx", "password");
                    //});
                })
                .UseStartup<Startup>();

            var webHost = webHostBuilder.Build();
            webHost.Run();

            Console.ReadKey();
            //StartConsole(searchService);
        
            watcherService.Stop();
        }

        private static void StartConsole(ISearchSercice searchService)
        {        
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
                List<string> items = searchService.Query(line.Trim()).Select(t=>t.FullName).ToList();
                Console.WriteLine("找到 " + items.Count + " 筆.");
                if (items.Count > 0)
                {
                    foreach (var item in items)
                    {
                        Console.WriteLine(item);
                    }
                }

            } while (true);

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
