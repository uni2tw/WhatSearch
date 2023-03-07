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
using WhatSearch.Jobs;
using NLog.Web;
using NLog;
using Lucene.Net.Util.Fst;
using static Lucene.Net.Util.Fst.Util;
using WhatSearch.DataProviders;
using WhatSearch.DataAccess;

namespace WhatSearch
{
    class Program
    {        
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Console.Title = "WhatName " + Assembly.GetExecutingAssembly().GetName().Version;
            
            ObjectResolver.Register();
            var connSetting = ObjectResolver.Get<ConnectionStringSetting>();
            DbUtil.EnsureDBFile(connSetting.Primary);
            DbUtil.EnsureTables();

            string simpStr1 = ObjectResolver.Get<IChineseConverter>().ToSimplifiedChinese(
                "財富管理信託管理費計收方式依開戶總約定書之約定辦理，實際收取時間依本公司公告");
            string simpStr2 = ObjectResolver.Get<IChineseConverter>().ToSimplifiedChinese(
                "請提供預設記憶體大小及硬碟容量，以及所需軟體清單，方便進行資料庫管理");
            string simpStr3 = ObjectResolver.Get<IChineseConverter>().ToSimplifiedChinese(
                "美國總統川普上任以來發生不少風波，邁入2018後回顧2017年，仍可發現他完成許多政策，美媒選出了最具有代表性的十大政績");
            string simpStr4 = ObjectResolver.Get<IChineseConverter>().ToSimplifiedChinese(
                "計程車");

            var shareFolders = ObjectResolver.GetConfig().Folders;

            ISearchSercice searchService = ObjectResolver.Get<ISearchSercice>();
            IDocumentService documentService = ObjectResolver.Get<IDocumentService>();
            documentService.SetCallback(delegate (string folderPath)
            {
                //Console.WriteLine(folderPath + " was queued.");
            }, delegate (string folderPath)
            {
                //Console.WriteLine(folderPath + " was done.");
            }, delegate (int docCount)
            {
                LogManager.GetCurrentClassLogger().Info("總共有 {0} 檔案己加入索引", docCount);                
            });

            //logger.Info(".Net core Version: " + GetNetCoreVersion());

            var config = ObjectResolver.GetConfig();

            documentService.Start(shareFolders);
            IFileWatcherService watcherService = ObjectResolver.Get<IFileWatcherService>();
            if (config.EnableWatch)
            {
                watcherService.Start(shareFolders);
            }

            ObjectResolver.Get<IReseekFolderJob>().Start();
            IWebHostBuilder webHostBuilder = WebHost.CreateDefaultBuilder(args)
                .UseKestrel(t =>
                {
                    t.Limits.MaxConcurrentConnections = 100;                    
                    //t.Listen(IPAddress.Any, 443, listenOptions =>
                    //{
                    //    listenOptions.UseHttps("server.pfx", "password");
                    //});
                })
                .UseNLog()
                .UseStartup<Startup>();

            NLogBuilder.ConfigureNLog("nlog.config");

            var webHost = webHostBuilder.Build();            
            
            try
            {
                webHost.Run();
            }
            catch (Exception ex)
            {               
                LogManager.GetCurrentClassLogger().Fatal("網站啟動失敗", ex);
                Console.WriteLine("網站啟動失敗, ex=" + ex.Message);
            } 
            finally
            {
                ObjectResolver.Get<IReseekFolderJob>().Stop();
            }

            //Console.ReadKey();
            //StartConsole(searchService);
        
            watcherService.Stop();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogManager.GetCurrentClassLogger().Error("DomainException, " +  e.ExceptionObject.ToString());
            Console.WriteLine(e.ExceptionObject);
            Environment.Exit(-1);
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
    }
}
