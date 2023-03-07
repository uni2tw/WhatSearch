using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using WhatSearch.Core;
using WhatSearch.Models;
using WhatSearch.Services.Interfaces;
using WhatSearch.Utility;

namespace WhatSearch.Services
{
    public class MainService : IMainService
    {        
        IFolderIdManager fimgr = ObjectResolver.Get<IFolderIdManager>();
        SystemConfig config = ObjectResolver.GetConfig();

        public List<FileInfoView> GetFileInfoViewsInTheFolder(string folderId)
        {
            string path = fimgr.GetPath(folderId);

            List<FileInfoView> result = new List<FileInfoView>();
            if (string.IsNullOrEmpty(path))
            {
                return result;
            }
            DirectoryInfo dirInfo = new DirectoryInfo(path);
            if (dirInfo.Exists == false)
            {
                return result;
            }

            foreach (var subDirInfo in dirInfo.GetDirectories())
            {
                string efid = fimgr.GetIdByFilePath(subDirInfo.FullName);
                result.Add(new FileInfoView
                {
                    Id = efid,
                    Title = subDirInfo.Name,
                    Modify = subDirInfo.LastWriteTime.ToString(),
                    Type = Helper.ConstStrings.Folder,
                    Size = string.Empty
                });
            }
            foreach (var subFileInfo in dirInfo.GetFiles())
            {
                if (subFileInfo.Attributes.HasFlag(FileAttributes.Hidden))
                {
                    continue;
                }
                string subEfid = fimgr.GetIdByFilePath(subFileInfo.FullName);
                string relPath;
                if (PathUtility.TryGetRelPath(subFileInfo.FullName, out relPath) == false)
                {
                    throw new Exception("不預期的意外，" + subFileInfo.FullName);
                }
                result.Add(new FileInfoView
                {
                    Id = subEfid,
                    GetUrl = "/get" + relPath,
                    Title = subFileInfo.Name,
                    Modify = subFileInfo.LastWriteTime.ToString(),
                    Type = Helper.GetFileDocType(subFileInfo.Extension),
                    Size = subFileInfo.Length.ToString()
                });
            }

            return result;
        }

        public List<FileInfoView> GetRootShareFolders()
        {
            List<FileInfoView> result = new List<FileInfoView>();
            foreach (var folder in config.Folders)
            {
                DirectoryInfo di = new DirectoryInfo(folder.Path);
                if (di.Exists == false)
                {
                    continue;
                }                
                result.Add(GetFileInfoView(di, folder.Title));
            }
            return result;
        }

        public List<FileInfoView> GetBreadcrumbs(string efid)
        {
            List<FileInfoView> result = new List<FileInfoView>();

            if (efid == string.Empty)
            {
                result.Add(new FileInfoView
                {
                    Id = string.Empty,
                    Title = "我的分享",
                    Type = "檔案資料夾",
                });
                return result;
            }

            string dirPath = fimgr.GetPath(efid);
            if (string.IsNullOrEmpty(dirPath))
            {
                return result;
            }
            DirectoryInfo dirInfo = new DirectoryInfo(dirPath);
            do
            {
                if (dirInfo == null)
                {
                    break;
                }
                result.Add(GetFileInfoView(dirInfo));
                bool atTop = config.Folders.Any(t => t.Path.Equals(dirInfo.FullName, StringComparison.OrdinalIgnoreCase));
                if (atTop)
                {
                    break;
                }
                dirInfo = dirInfo.Parent;
            } while (true);
            result.Add(new FileInfoView
            {
                Id = string.Empty,
                Title = "我的分享",
                Type = "檔案資料夾",
            });
            result.Reverse();
            return result;
        }

        public FileInfoView GetFileInfoView(DirectoryInfo di, string preferenceTitle = null)
        {
            if (di == null || di.Exists == false)
            {
                return null;
            }
            string efid = fimgr.GetIdByFilePath(di.FullName);

            return new FileInfoView
            {
                Id = efid,
                Title = preferenceTitle ?? di.Name,
                Modify = di.CreationTime.ToString(),
                Type = Helper.ConstStrings.Folder,
                Size = string.Empty
            };
        }


        public List<FileInfoView> GetMusicItems(string folderId)
        {
            string path = fimgr.GetPath(folderId);

            List<FileInfoView> result = new List<FileInfoView>();
            if (string.IsNullOrEmpty(path))
            {
                return result;
            }
            DirectoryInfo dirInfo = new DirectoryInfo(path);
            if (dirInfo.Exists == false)
            {
                return result;
            }

            foreach (var fi in dirInfo.GetFiles())
            {
                if (fi.Attributes.HasFlag(FileAttributes.Hidden))
                {
                    continue;
                }
                if (Helper.GetFileDocType(fi.Extension) != Helper.ConstStrings.Music)
                {
                    continue;
                }
                string subEfid = fimgr.GetIdByFilePath(fi.FullName);
                string relPath;
                if (PathUtility.TryGetRelPath(fi.FullName, out relPath) == false)
                {
                    throw new Exception("不預期的意外，" + fi.FullName);
                }
                result.Add(new FileInfoView
                {
                    Id = subEfid,
                    GetUrl = "/get" + relPath,
                    Title = fi.Name,
                    Modify = fi.LastWriteTime.ToString(),
                    Type = Helper.GetFileDocType(fi.Extension),
                    Size = fi.Length.ToString()
                });
            }

            return result;
        }
    }
}
