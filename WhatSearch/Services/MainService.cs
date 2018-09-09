using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WhatSearch.Core;
using WhatSearch.Models;
using WhatSearch.Services.Interfaces;
using WhatSearch.Utility;

namespace WhatSearch.Services
{
    public class MainService : IMainService
    {
        IFileSystemInfoIdAssigner idAssigner = Ioc.Get<IFileSystemInfoIdAssigner>();
        SystemConfig config = Ioc.GetConfig();

        public List<FileInfoView> GetFileInfoViewsInTheFolder(Guid folderGuid)
        {
            List<FileInfoView> result = new List<FileInfoView>();
            string path = idAssigner.GetFolderPath(folderGuid);
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
                Guid subGuid = idAssigner.GetOrAdd(subDirInfo.FullName);
                result.Add(new FileInfoView
                {
                    Id = subGuid.ToString(),
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
                Guid subGuid = idAssigner.GetOrAdd(subFileInfo.FullName);
                string relPath;
                if (TryGetRelPath(subFileInfo.FullName, out relPath)==false)
                {
                    throw new Exception("不預期的意外，" + subFileInfo.FullName);
                }
                result.Add(new FileInfoView
                {
                    Id = subGuid.ToString(),
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
                Guid guid = idAssigner.GetOrAdd(di.FullName);
                result.Add(GetFileInfoView(di, folder.Title));
            }
            return result;
        }

        public List<FileInfoView> GetBreadcrumbs(Guid folderGuid)
        {
            List<FileInfoView> result = new List<FileInfoView>();

            if (folderGuid == Guid.Empty)
            {
                result.Add(new FileInfoView
                {
                    Id = Constant.RootId,
                    Title = "我的分享",
                    Type = "檔案資料夾",
                });
                return result;
            }

            string dirPath = idAssigner.GetFolderPath(folderGuid);
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
                Id = Constant.RootId,
                Title = "我的分享",
                Type = "檔案資料夾",
            });
            result.Reverse();
            return result;
        }

        private FileInfoView GetFileInfoView(DirectoryInfo di, string preferenceTitle = null)
        {
            if (di == null || di.Exists == false)
            {
                return null;
            }
            Guid guid = idAssigner.GetOrAdd(di.FullName);

            return new FileInfoView
            {
                Id = guid.ToString(),
                Title = preferenceTitle ?? di.Name,
                Modify = di.CreationTime.ToString(),
                Type = Helper.ConstStrings.Folder,
                Size = string.Empty
            };
        }

        public string GetRelativePath(List<FileInfoView> breadcrumbs)
        {
            if (breadcrumbs.Count <= 1)
            {
                return "/";
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.Append('/');
                for (int i = 1; i < breadcrumbs.Count - 1; i++)
                {
                    sb.Append(breadcrumbs[i].Title);
                    sb.Append('/');
                }
                sb.Append(breadcrumbs[breadcrumbs.Count - 1]);
                return sb.ToString();
            }            
        }
        
        public bool TryGetRelPath(string absPath, out string relPath)
        {
            //~/Anime/2018連載-3/高分少女
            relPath = string.Empty;
            FolderConfig targetFolder = null;
            foreach (var sf in config.Folders)
            {
                string sfPath = sf.Path;
                if (absPath.StartsWith(sfPath, StringComparison.OrdinalIgnoreCase))
                {
                    targetFolder = sf;
                    break;
                }
            }

            if (targetFolder == null)
            {
                return false;
            }
            relPath = absPath.Substring(targetFolder.Path.Length);
            if (relPath == string.Empty)
            {
                relPath = "/";
            }
            else
            {
                relPath = relPath.Replace(Path.DirectorySeparatorChar, '/');
                relPath = "/" + targetFolder.Title + relPath;
            }


            return true;
        }
        /// <summary>
        /// 跟 configFolder比對的部份有問題
        /// /H-Anime123 也會被判斷符合
        /// 要改成先拆會一個一個 folder 再針對第1個folder做比對
        /// </summary>
        /// <param name="relPath"></param>
        /// <returns></returns>
        public bool TryGetAbsolutePath(string relPath, out string absolutePath)
        {
            //~/Anime/2018連載-3/高分少女
            absolutePath = string.Empty;
            FolderConfig targetFolder = null;
            string[] relPathParts = relPath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
            if (relPathParts.Length == 0)
            {
                return false;
            }
            string topRelPath = relPathParts[0];
            string[] bottomRelPaths = relPathParts.Skip(1).ToArray();
            foreach (var sf in config.Folders)
            {
                string shareRelPath = sf.Title;
                if (topRelPath.Equals(shareRelPath, StringComparison.OrdinalIgnoreCase))
                {
                    targetFolder = sf;
                    break;
                }
            }
            if (targetFolder == null)
            {
                return false;
            }
            absolutePath = Path.Combine(
                targetFolder.Path,
                string.Join(Path.DirectorySeparatorChar, bottomRelPaths));
            return true;
        }
    }
}
