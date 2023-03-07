using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WhatSearch.Core.Extensions
{
    public static class MemoryCacheExtensions
    {
        private static readonly Func<MemoryCache, object> GetEntriesCollection = Delegate.CreateDelegate(
            typeof(Func<MemoryCache, object>),
            typeof(MemoryCache).GetProperty("EntriesCollection", BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod(true),
            throwOnBindFailure: true) as Func<MemoryCache, object>;

        public static IEnumerable GetKeys(this IMemoryCache memoryCache) =>
            ((IDictionary)GetEntriesCollection((MemoryCache)memoryCache)).Keys;

        public static IEnumerable<T> GetKeys<T>(this IMemoryCache memoryCache) =>
            memoryCache.GetKeys().OfType<T>();

        public static void Clear(this IMemoryCache memoryCache)
        {
            object state = typeof(MemoryCache).GetField("_coherentState", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(memoryCache);
            var entries = state.GetType().GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(state);
            var keys = entries.GetType().GetProperty("Keys", BindingFlags.Public | BindingFlags.Instance).GetValue(entries)
                as System.Collections.ObjectModel.ReadOnlyCollection<object>;
            foreach (var key in keys)
            {
                memoryCache.Remove(key);
            }
        }
    }
}
