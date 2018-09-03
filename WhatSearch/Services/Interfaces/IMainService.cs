using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WhatSearch.Models;

namespace WhatSearch.Services.Interfaces
{
    public interface IMainService
    {
        List<FileInfoView> GetRootShareFolders();
        List<FileInfoView> GetFileInfoViewsInTheFolder(Guid folderGuid);
        List<FileInfoView> GetBreadcrumbs(Guid folderGuid);
        string GetRelativePath(List<FileInfoView> breadcrumbs);
        bool TryGetAbsolutePath(string relPath, out string absolutePath);
    }
}
