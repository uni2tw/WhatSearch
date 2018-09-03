using System;
using System.IO;

namespace WhatSearch.Services.Interfaces
{
    public interface IFileSystemInfoIdAssigner
    {
        Guid GetOrAdd(string path);
        string GetFolderPath(Guid id);

    }
}
