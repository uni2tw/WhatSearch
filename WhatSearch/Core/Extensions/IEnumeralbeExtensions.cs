using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatSearch.Core.Extensions
{
    public interface IHasChildren<T>
    {
        IEnumerable<T> Children { get; set; }
    }

    public static class IEnumeralbeExtensions
    {
        /// <summary>
        /// 產生樹狀結構
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="id_selector"></param>
        /// <param name="parent_id_selector"></param>
        /// <param name="parent_id"></param>
        /// <returns></returns>
        public static IEnumerable<T> GenerateTree<T>(this IEnumerable<T> collection, Func<T, int> id_selector, Func<T, int> parent_id_selector, int? parent_id)
            where T : IHasChildren<T>
        {
            var items = collection.Where(c => parent_id_selector(c).Equals(parent_id)).ToList();
            foreach (var item in items)
            {
                var result = item;

                result.Children = collection.GenerateTree(id_selector, parent_id_selector, id_selector(item));

                yield return result;
            }
        }

        public static IEnumerable<List<T>> Split<T>(this List<T> list, int size)
        {
            for (int i = 0; i < list.Count; i += size)
            {
                yield return list.GetRange(i, Math.Min(size, list.Count - i));
            }
        }
    }
}
