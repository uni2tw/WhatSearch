using System;
using System.Collections.Concurrent;
using WhatSearch.Services.Interfaces;
using WhatSearch.Utility;

namespace WhatSearch.Services
{
    public interface IFolderIdManager
    {
        string GetPath(string folderId);
        string GetId(string pathname);
    }
    public class FolderIdManager : IFolderIdManager
    {
        public string GetId(string pathname)
        {
            return Helper.EncodeUrlBase64(pathname);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="folderId"></param>
        /// <returns></returns>
        public string GetPath(string folderId)
        {
            string absPath;
            string did = Helper.DecodeUrlBase64(folderId);
            if (PathUtility.TryGetAbsolutePath(did, out absPath))
            {
                return absPath;
            }
            return string.Empty;
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="pathname">url pathname</param>
        ///// <returns></returns>
        //public string GetId(string pathname)
        //{
        //    string absPath;
        //    string did = Helper.DecodeUrlBase64(folderId);
        //    if (PathUtility.TryGetAbsolutePath(did, out absPath))
        //    {
        //        return absPath;
        //    }
        //    return string.Empty;
        //}
    }
    public class FolderIdAssigner : IFileSystemInfoIdAssigner
    {
        ConcurrentDictionary<string, Guid> folderIds = new ConcurrentDictionary<string, Guid>();
        ConcurrentDictionary<Guid, string> idFolders = new ConcurrentDictionary<Guid, string>();
        public Guid GetOrAdd(string path)
        {
            Guid guid;
            if (folderIds.TryGetValue(path, out guid))
            {
                return guid;
            }
            else
            {
                guid = Guid.NewGuid();
                folderIds.TryAdd(path, guid);
                idFolders.TryAdd(guid, path);
            }
            return guid;
        }

        public string GetFolderPath(Guid id)
        {
            string path;
            idFolders.TryGetValue(id, out path);
            return path ?? string.Empty;
        }
        public Guid? GetFolderId(string filePath)
        {
            Guid result;
            if (folderIds.TryGetValue(filePath, out result))
            {
                return result;
            }
            return null;
        }
    }
}
