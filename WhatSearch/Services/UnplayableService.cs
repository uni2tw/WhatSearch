using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using NLog;
using WhatSearch.Utilities;
using WhatSearch.Utility;

namespace WhatSearch.Services
{
    public interface IUnplayableService
    {
        /// <summary>這個動畫（以評分用的名稱當 key）是否被管理者標註為「無法播放」；沒有為 null</summary>
        UnplayableInfo Get(string key);
        void Set(string key, string by);
        void Clear(string key);
    }

    public class UnplayableInfo
    {
        /// <summary>標註的管理者（顯示名稱）</summary>
        [JsonPropertyName("by")]
        public string By { get; set; }
        /// <summary>標註時間（UTC 毫秒）</summary>
        [JsonPropertyName("time")]
        public long Time { get; set; }
    }

    /// <summary>
    /// 動畫「無法播放」的標註（例如某些 MKV 的編碼瀏覽器不支援）：只有管理者能標註與取消，整個系統共用一份，
    /// 存在 data/playreports.json（{ 名稱: { by, time } }）。key 與評分相同（去掉結尾星等的資料夾名稱），所以資料夾搬位置標註仍然跟著走。
    /// 這只是提醒，不會擋人點進去試播。
    /// </summary>
    public class UnplayableService : IUnplayableService
    {
        static ILogger logger = LogManager.GetCurrentClassLogger();
        readonly object gate = new object();
        Dictionary<string, UnplayableInfo> cache;

        static string DataPath()
        {
            return Helper.GetRelativePath("data", "playreports.json");
        }

        Dictionary<string, UnplayableInfo> Load()
        {
            if (cache != null)
            {
                return cache;
            }
            cache = new Dictionary<string, UnplayableInfo>(StringComparer.Ordinal);
            try
            {
                string path = DataPath();
                if (File.Exists(path))
                {
                    var loaded = JsonHelper.Deserialize<Dictionary<string, UnplayableInfo>>(File.ReadAllText(path));
                    if (loaded != null)
                    {
                        cache = loaded;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "讀取 playreports.json 失敗，以空白資料開始");
            }
            return cache;
        }

        public UnplayableInfo Get(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }
            lock (gate)
            {
                UnplayableInfo info;
                return Load().TryGetValue(key, out info) ? info : null;
            }
        }

        public void Set(string key, string by)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }
            lock (gate)
            {
                Load()[key] = new UnplayableInfo { By = by, Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };
                Save();
            }
        }

        public void Clear(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }
            lock (gate)
            {
                if (Load().Remove(key))
                {
                    Save();
                }
            }
        }

        // 呼叫端已持有 gate；寫檔被防毒等暫時鎖住時 SafeFile 會自動重試，最後還是失敗才丟出例外
        void Save()
        {
            try
            {
                SafeFile.WriteAllTextAtomic(DataPath(), JsonHelper.Serialize(cache, indented: true));
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "寫入 playreports.json 失敗");
                throw;
            }
        }
    }
}
