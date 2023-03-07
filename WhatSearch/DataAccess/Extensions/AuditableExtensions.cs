using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatSearch.DataAccess.Extensions
{
    public static class AuditableExtensions
    {
        public static void ApplyCreatedInfo<T>(this T entity, long userId) where T : class
        {
            var now = DateTime.Now;
            var type = typeof(T);

            type.GetProperty("CreateOn")?.SetValue(entity, now);
            type.GetProperty("CreateBy")?.SetValue(entity, userId);
            type.GetProperty("UpdateOn")?.SetValue(entity, now);
            type.GetProperty("UpdateBy")?.SetValue(entity, userId);
        }

        public static void ApplyCreatedInfos<T>(this List<T> entities, long userId) where T : class
        {
            var type = typeof(T);
            var now = DateTime.Now;
            var cd = type.GetProperty("CreateOn");
            var cu = type.GetProperty("CreateBy");
            var ud = type.GetProperty("UpdateOn");
            var uu = type.GetProperty("UpdateBy");
            foreach (var entity in entities)
            {
                cd?.SetValue(entity, now);
                cu?.SetValue(entity, userId);
                ud?.SetValue(entity, now);
                uu?.SetValue(entity, userId);
            }
        }


        public static void ApplyUpdateInfo<T>(this T entity, long userId) where T : class
        {
            var now = DateTime.Now;

            var type = typeof(T);
            type.GetProperty("UpdateOn")?.SetValue(entity, now);
            type.GetProperty("UpdateBy")?.SetValue(entity, userId);
        }

        public static void ApplyDeleteInfo<T>(this T entity, int userId) where T : class
        {
            var now = DateTime.Now;
            var type = typeof(T);
            type.GetProperty("Deleted")?.SetValue(entity, now);
            type.GetProperty("UpdateOn")?.SetValue(entity, now);
            type.GetProperty("UpdateBy")?.SetValue(entity, userId);
        }

        public static void ApplyDeleteInfo<T>(this IEnumerable<T> entities, int userId) where T : class
        {
            foreach (var entity in entities)
            {
                entity.ApplyDeleteInfo(userId);
            }
        }
    }
}
