using Newtonsoft.Json;
using WhatSearch.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WhatSearch.Service
{
    public class FolderModifyChecker : IFolderChecker
    {
        Dictionary<string, DateTime> dirChanges = new Dictionary<string, DateTime>();
        public string filePath = Helper.GetRelativePath("dirChanges.json");
        public bool Check(DirectoryInfo dirInfo)
        {
            if (dirChanges.ContainsKey(dirInfo.FullName))
            {
                if (dirChanges[dirInfo.FullName] == dirInfo.LastWriteTime)
                {
                    return false;
                }
            }
            dirChanges[dirInfo.FullName] = dirInfo.LastWriteTime;
            return true;
        }

        public void Commit()
        {
            string jsonStr = JsonConvert.SerializeObject(dirChanges);
            try
            {
                File.WriteAllText(filePath, jsonStr);
            }
            catch (Exception ex)
            {
                Console.WriteLine("FolderModifyChecker commit fail. {0}", ex);
            }
        }

        public void Init()
        {
            if (File.Exists(filePath) == false)
            {
                return;
            }
            string jsonStr = File.ReadAllText(filePath);
            try
            {
                dirChanges = JsonConvert.DeserializeObject<Dictionary<string, DateTime>>(jsonStr);
            }
            catch (Exception ex)
            {
                Console.WriteLine("FolderModifyChecker init fail. {0}", ex);
            }
        }
    }


}
