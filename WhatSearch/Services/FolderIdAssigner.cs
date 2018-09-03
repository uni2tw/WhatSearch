using System;
using System.Collections.Concurrent;
using WhatSearch.Services.Interfaces;

namespace WhatSearch.Services
{
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


    }
}
