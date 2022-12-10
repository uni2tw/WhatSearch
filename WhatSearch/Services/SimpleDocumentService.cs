using WhatSearch.Core;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WhatSearch.Jobs;
using System;
using NLog;

namespace WhatSearch.Services
{
    public class SimpleDocumentService : DocumentServiceBase
    {
        static ILogger logger = LogManager.GetCurrentClassLogger();
        public override string GetProgressPercent()
        {
            return string.Empty;
        }

        public override void Start(List<FolderConfig> startFolders)
        {
            foreach (var folder in startFolders)
            {
                string folderPath = folder.Path;
                Task.Run(() =>
                {
                    StartSeek(folderPath);
                });                
            }            
        }

        private void SeekFile(string filePath)
        {
            BuildIndex(new FileInfo(filePath));
        }

        private void StartSeek(string folder)
        {
            TriggerSeekFolderStart(folder);
            var dirInfo = new DirectoryInfo(folder);
            if (dirInfo.Exists == false)
            {
                return;
            }
            foreach (var subDirInfo in dirInfo.GetDirectories())
            {
                SeekFolder(subDirInfo.FullName);
            }
            BuildSearchDoc(dirInfo);
            TriggerSeekFolderDone(folder);
        }

        private void SeekFolder(string folderPath)
        {
            TriggerSeekFolderStart(folderPath);
            DirectoryInfo dirInfo = null;
            DirectoryInfo[] subDirInfos = new DirectoryInfo[0];
            try
            {
                dirInfo = new DirectoryInfo(folderPath);
                if (dirInfo.Exists == false)
                {
                    return;
                }
                subDirInfos = dirInfo.GetDirectories();
            }
            catch (Exception ex)
            {
                logger.Info("SeekFolder遇到錯誤，重新排訂:" + folderPath, ex);
                Ioc.Get<IReseekFolderJob>().Queue(folderPath);
                return;
            }
                
            foreach (var subDirInfo in subDirInfos)
            {
                SeekFolder(subDirInfo.FullName);
            }
            BuildSearchDoc(dirInfo);

            TriggerSeekFolderDone(folderPath);
        }

        private void RemoveFile(string filePath)
        {
            RemoveSearchDoc(filePath);
        }

        private void RemoveFolder(string dirPath)
        {
            RemoveSearchDoc(new DirectoryInfo(dirPath).GetFiles().Select(t => t.FullName).ToArray());
        }


        public override void AppendFolderOrFile(string fullPath)
        {
            if (Directory.Exists(fullPath))
            {
                SeekFolder(fullPath);
            } else
            {
                SeekFile(fullPath);
            }
        }

        public override void RemoveFolderOrFile(string fullPath)
        {
            if (Directory.Exists(fullPath))
            {
                RemoveFolder(fullPath);
            }
            else
            {
                RemoveFile(fullPath);
            }
        }
    }
}
