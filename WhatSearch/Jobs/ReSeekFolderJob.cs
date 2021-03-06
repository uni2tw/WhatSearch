﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Timers;
using WhatSearch.Core;

namespace WhatSearch.Jobs
{

    public interface IReseekFolderJob
    {
        void Start();
        void Stop();
        void Queue(string folderPath);
    }
    public class ReseekFolderJob : IReseekFolderJob
    {
        static IDocumentService docSvc = Ioc.Get<IDocumentService>();
        Timer timer = new Timer();
        Queue<QueueItem> retryFolders = new Queue<QueueItem>();
        object thisLock = new object();

        public class QueueItem
        {
            public string Path { get; set; }
            public int RetryTimes { get; set; }
        }

        public void Start()
        {
            timer.AutoReset = false;
            timer.Interval = 5000;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        public void Stop()
        {
            Flush();
            timer.Stop();
        }

        public void Queue(string folderPath)
        {
            lock (thisLock)
            {
                retryFolders.Enqueue(new QueueItem { Path = folderPath, RetryTimes = 0 });
            }
        }

        private void Flush()
        {
            lock (thisLock)
            {                
                do
                {
                    QueueItem item = null;
                    retryFolders.TryDequeue(out item);
                    if (item == null)
                    {
                        break;
                    }
                    try
                    {
                        docSvc.AppendFolderOrFile(item.Path);
                    }
                    catch
                    {
                        if (item.RetryTimes < 5)
                        {
                            retryFolders.Enqueue(new QueueItem
                            {
                                Path = item.Path,
                                RetryTimes = item.RetryTimes + 1
                            });
                        }
                    }
                } while (true);
            }
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                Flush();
            }
            finally
            {
                timer.Start();
            }
        }
    }
}
