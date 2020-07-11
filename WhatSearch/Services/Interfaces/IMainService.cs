using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WhatSearch.Models;

namespace WhatSearch.Services.Interfaces
{
    public interface IMainService
    {
        List<FileInfoView> GetRootShareFolders();

        List<FileInfoView> GetFileInfoViewsInTheFolder(string folderGuid);
        List<FileInfoView> GetBreadcrumbs(string folderGuid);
        FileInfoView GetFileInfoView(DirectoryInfo di, string preferenceTitle = null);
    }
}
