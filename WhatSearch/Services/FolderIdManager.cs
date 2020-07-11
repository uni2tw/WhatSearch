using System;
using System.Collections.Concurrent;
using WhatSearch.Services.Interfaces;
using WhatSearch.Utility;

namespace WhatSearch.Services
{
    public interface IFolderIdManager
    {
        string GetId(string pathname);
        string GetIdByFilePath(string pathname);
        string GetPath(string folderId);        
    }
    public class FolderIdManager : IFolderIdManager
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pathname"></param>
        /// <returns></returns>
        public string GetId(string pathname)
        {
            return Helper.EncodeUrlBase64(pathname);
        }

        public string GetIdByFilePath(string filePath)
        {
            string relPath;
            if (PathUtility.TryGetRelPath(filePath, out relPath))
            {
                return Helper.EncodeUrlBase64(relPath);
            }
            return string.Empty;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="folderId"></param>
        /// <returns></returns>
        public string GetPath(string folderId)
        {
            string absPath;
            string relUrlPath = Helper.DecodeUrlBase64(folderId);
            if (PathUtility.TryGetAbsolutePath(relUrlPath, out absPath))
            {
                return absPath;
            }
            return string.Empty;
        }

    }
}
