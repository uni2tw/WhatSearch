using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace WhatSearch.Core.Extensions
{
    public static class DisplayAttributeExtensions
    {
        public static string GetDisplayName(this PropertyInfo prop)
        {
            var displayName = prop.GetCustomAttribute<DisplayAttribute>()?.Name;
            if (string.IsNullOrEmpty(displayName))
            {
                return prop.Name;
            }

            return displayName;
        }

        public static Dictionary<string, string> GetDisplayNameDictionary(this Type type)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(c => new
                {
                    c.Name,
                    DisplayName = c.GetCustomAttribute<DisplayAttribute>()?.Name
                })
                .ToDictionary(
                    c => c.Name,
                    c => string.IsNullOrWhiteSpace(c.DisplayName) ? c.Name : c.DisplayName
                );
        }

        /// <summary>
        /// 取得：Model.Property.DisplayName
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static string GetDisplayName<T>(string propertyName) where T : class
        {
            var model = typeof(T);
            var property = model.GetProperty(propertyName);
            if (property == null) return string.Empty;
            var displayName = property.GetCustomAttribute<DisplayAttribute>()?.Name;
            if (string.IsNullOrEmpty(displayName)) return string.Empty;
            return displayName;
        }
    }
}
