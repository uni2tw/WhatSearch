using System.IO;

namespace WhatSearch.Service
{
    /// <summary>
    /// 用在記錄目錄的最後修改時間，
    /// 減少再次啟動的重複讀取與解析
    /// 
    /// 但目前資料是放在記憶體，比對差異載入無法實現。
    /// </summary>
    public interface IFolderChecker
    {
        void Init();
        bool Check(DirectoryInfo dirInfo);
        void Commit();
    }


}
