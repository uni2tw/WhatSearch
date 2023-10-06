using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WhatSearch.DataAccess.Extensions
{
    public static class ReflectionCache
    {
        private static readonly ConcurrentDictionary<(Type, string), PropertyInfo> PropertyCache = new();

        public static PropertyInfo GetCachedProperty(Type type, string propertyName)
        {
            return PropertyCache.GetOrAdd((type, propertyName), key => key.Item1.GetProperty(key.Item2));
        }
    }

    public static class AuditableExtensions
    {
        //我想寫 如果本來就有值，就不自動塞值
        //static
        public static void ApplyCreatedInfo<T>(this T entity, long userId, DateTime? datetime = null) where T : class
        {
            var now = datetime ?? DateTime.Now;
            var type = typeof(T);

            var createdOnProperty = ReflectionCache.GetCachedProperty(type, "CreatedOn");
            var createdByProperty = ReflectionCache.GetCachedProperty(type, "CreatedBy");
            var updatedOnProperty = ReflectionCache.GetCachedProperty(type, "UpdatedOn");
            var updatedByProperty = ReflectionCache.GetCachedProperty(type, "UpdatedBy");

            if (createdOnProperty != null && Equals(createdOnProperty.GetValue(entity), default(DateTime)))
            {
                createdOnProperty.SetValue(entity, now);
            }

            if (createdByProperty != null && Equals(createdByProperty.GetValue(entity), default(long)))
            {
                createdByProperty.SetValue(entity, userId);
            }

            if (updatedOnProperty != null && Equals(updatedOnProperty.GetValue(entity), default(DateTime)))
            {
                updatedOnProperty.SetValue(entity, now);
            }

            if (updatedByProperty != null && Equals(updatedByProperty.GetValue(entity), default(long)))
            {
                updatedByProperty.SetValue(entity, userId);
            }
        }

        public static void ApplyCreatedInfos<T>(this List<T> entities, long userId) where T : class
        {
            DateTime now = DateTime.Now;
            foreach (var entity in entities)
            {
                entity.ApplyCreatedInfo(userId, now);
            }
        }

        public static void ApplyUpdateInfo<T>(this T entity, long userId, DateTime? datetime = null) where T : class
        {
            var now = datetime ?? DateTime.Now;
            var type = typeof(T);

            var updatedOnProperty = ReflectionCache.GetCachedProperty(type, "UpdatedOn");
            var updatedByProperty = ReflectionCache.GetCachedProperty(type, "UpdatedBy");

            if (updatedOnProperty != null && Equals(updatedOnProperty.GetValue(entity), default(DateTime)))
            {
                updatedOnProperty.SetValue(entity, now);
            }

            if (updatedByProperty != null && Equals(updatedByProperty.GetValue(entity), default(long)))
            {
                updatedByProperty.SetValue(entity, userId);
            }
        }

        public static void ApplyUpdateInfo<T>(this IEnumerable<T> entities, long userId) where T : class
        {
            DateTime now = DateTime.Now;
            foreach (var entity in entities)
            {
                entity.ApplyUpdateInfo(userId, now);
            }
        }

        public static void ApplyDeleteInfo<T>(this T entity, long userId, DateTime? datetime=null) where T : class
        {
            var now = datetime ?? DateTime.Now;
            var type = typeof(T);

            var deletedProperty = ReflectionCache.GetCachedProperty(type, "Deleted");
            var updatedOnProperty = ReflectionCache.GetCachedProperty(type, "UpdatedOn");
            var updatedByProperty = ReflectionCache.GetCachedProperty(type, "UpdatedBy");

            if (deletedProperty != null && Equals(deletedProperty.GetValue(entity), default(DateTime)))
            {
                deletedProperty.SetValue(entity, now);
            }

            if (updatedOnProperty != null && Equals(updatedOnProperty.GetValue(entity), default(DateTime)))
            {
                updatedOnProperty.SetValue(entity, now);
            }

            if (updatedByProperty != null && Equals(updatedByProperty.GetValue(entity), default(long)))
            {
                updatedByProperty.SetValue(entity, userId);
            }
        }

        public static void ApplyDeleteInfo<T>(this IEnumerable<T> entities, long userId) where T : class
        {
            DateTime now = DateTime.Now;
            foreach (var entity in entities)
            {
                entity.ApplyDeleteInfo(userId, now);
            }
        }
    }
}
