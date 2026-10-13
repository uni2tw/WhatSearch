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

        List<FileInfoView> GetFileInfoViewsInTheFolder(string folderId);
        List<FileInfoView> GetBreadcrumbs(string folderId);
        FileInfoView GetFileInfoView(DirectoryInfo di, string preferenceTitle = null);
        List<FileInfoView> GetMusicItems(string folderId);
        /// <summary>多片裝專輯（子資料夾全是 Disc 1/CD2 之類）時回傳各光碟的歌曲，否則回傳 null</summary>
        List<DiscGroup> GetMultiDiscGroups(string folderId);
        /// <summary>資料夾存在回傳 null；不存在時回傳最近存在的上層與名稱相近的同層建議</summary>
        FolderNotFoundResult ResolveMissingFolder(string folderId);
    }
}
