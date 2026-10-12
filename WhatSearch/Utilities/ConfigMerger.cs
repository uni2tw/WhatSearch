using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace WhatSearch.Utilities
{
    /// <summary>
    /// 把本機覆蓋設定（config.local.json）疊加到預設設定（config.json）上。
    /// 物件逐項合併、陣列與純量整個取代；鍵名不分大小寫；兩份都允許註解與結尾逗號。
    /// </summary>
    public static class ConfigMerger
    {
        static readonly JsonDocumentOptions DocumentOptions = new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        public static string Merge(string baseJson, string overrideJson)
        {
            JsonObject target = JsonNode.Parse(baseJson, null, DocumentOptions) as JsonObject;
            JsonObject source = JsonNode.Parse(overrideJson, null, DocumentOptions) as JsonObject;
            if (target == null || source == null)
            {
                throw new FormatException("設定檔的最外層必須是 JSON 物件");
            }
            MergeInto(target, source);
            return target.ToJsonString();
        }

        static void MergeInto(JsonObject target, JsonObject source)
        {
            foreach (var prop in source.ToList())
            {
                string key = target.Select(p => p.Key)
                    .FirstOrDefault(k => k.Equals(prop.Key, StringComparison.OrdinalIgnoreCase)) ?? prop.Key;
                JsonObject sourceObj = prop.Value as JsonObject;
                JsonObject targetObj = target[key] as JsonObject;
                if (sourceObj != null && targetObj != null)
                {
                    MergeInto(targetObj, sourceObj);
                }
                else
                {
                    // net6.0 沒有 JsonNode.DeepClone，改用序列化再解析來複製
                    target[key] = prop.Value == null ? null : JsonNode.Parse(prop.Value.ToJsonString());
                }
            }
        }
    }
}
