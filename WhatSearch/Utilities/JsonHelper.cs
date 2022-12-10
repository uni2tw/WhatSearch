using System;
using System.Text.Json;

namespace WhatSearch.Utility
{
    public static class JsonHelper
    {
        static JsonSerializerOptions SerializeOption = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        };

        static JsonSerializerOptions SerializeOptionIdented = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };

        static JsonSerializerOptions DeserializeOption = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNameCaseInsensitive = false,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        static JsonSerializerOptions DeserializeOptionCaseInsensitive = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        public static string Serialize(object obj, bool indented = false)
        {
            if (indented)
            {
                return JsonSerializer.Serialize(obj, SerializeOptionIdented);
            } 
            return JsonSerializer.Serialize(obj, SerializeOption);
        }

        public static T? Deserialize<T>(string str, bool caseInsensitive = true)
        {
            if (string.IsNullOrEmpty(str))
            {
                return default(T);
            }
            if (caseInsensitive)
            {
                return JsonSerializer.Deserialize<T>(str, DeserializeOptionCaseInsensitive);
            }
            return JsonSerializer.Deserialize<T>(str, DeserializeOption);

        }

        public static object Deserialize(Type type, string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }
            return JsonSerializer.Deserialize(str, type);
        }
    }
}
