using WhatSearch.Core;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WhatSearch
{
    public class SimpleSeekService : SeekServiceBase
    {
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
                    DoSeek(folderPath);
                });                
            }            
        }

        private void SeekFile(string filePath)
        {
            BuildIndex(new FileInfo(filePath));
        }

        private void DoSeek(string folder)
        {
            TriggerFolderSeekStart(folder);
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
            TriggerFolderSeekDone(folder);
        }

        private void SeekFolder(string folderPath)
        {
            TriggerFolderSeekStart(folderPath);
            var dirInfo = new DirectoryInfo(folderPath);
            if (dirInfo.Exists == false)
            {
                return;
            }
            foreach(var subDirInfo in dirInfo.GetDirectories())
            {
                SeekFolder(subDirInfo.FullName);
            }
            BuildSearchDoc(dirInfo);
            TriggerFolderSeekDone(folderPath);
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
