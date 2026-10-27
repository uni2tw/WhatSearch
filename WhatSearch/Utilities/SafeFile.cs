using System;
using System.IO;
using System.Threading;
using NLog;

namespace WhatSearch.Utilities
{
    /// <summary>
    /// 資料檔（JSON）的安全寫入：先寫暫存檔再取代，避免寫到一半當掉把資料清空。
    /// 防毒軟體、搜尋索引、備份或同步軟體常會短暫鎖住檔案，Windows 的 File.Replace 遇到這種情況會丟出
    /// 「無法移除要被取代的檔案」，所以失敗時短暫等待後重試；重試成功只記 Debug（不進 error.log），
    /// 全部重試完還是失敗才丟出例外，由呼叫端決定怎麼處理（通常記 Warn）。
    /// </summary>
    public static class SafeFile
    {
        static readonly ILogger logger = LogManager.GetLogger("SafeFile");
        // 第一次失敗後依序等待的毫秒數（最多重試 5 次，約 1 秒），夠涵蓋一般的暫時鎖定
        static readonly int[] RetryDelaysMs = { 30, 60, 120, 250, 500 };

        public static void WriteAllTextAtomic(string path, string text)
        {
            string folder = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(folder) == false && Directory.Exists(folder) == false)
            {
                Directory.CreateDirectory(folder);
            }
            string tmp = path + ".tmp";
            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    File.WriteAllText(tmp, text);
                    if (File.Exists(path))
                    {
                        File.Replace(tmp, path, null);
                    }
                    else
                    {
                        File.Move(tmp, path);
                    }
                    if (attempt > 0)
                    {
                        logger.Debug("寫入 {0} 時檔案被其他程式暫時鎖住，重試 {1} 次後成功", path, attempt);
                    }
                    return;
                }
                catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) && attempt < RetryDelaysMs.Length)
                {
                    Thread.Sleep(RetryDelaysMs[attempt]);
                }
            }
        }
    }
}
