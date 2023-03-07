using Dapper.Contrib.Extensions;
using System;
using System.Linq;
using System.Reflection;

namespace WhatSearch.DataAccess.Extensions
{
    public static class DapperExtensions
    {
        public static string GetTableName(this Type type)
        {
            var attribute = type.GetCustomAttributes(typeof(TableAttribute), true).FirstOrDefault() as TableAttribute;

            return attribute?.Name ?? type.Name;
        }

        /// <summary>
        /// 從 Model 取得 Pramiry Key 名稱
        /// </summary>
        /// <returns></returns>
        public static string GetPrimaryKey(this Type type)
        {
            return type.GetPrimaryKeyProperty()?.Name;
        }

        /// <summary>
        /// 取得 Pramiry Key 屬性
        /// </summary>
        /// <returns></returns>
        public static PropertyInfo GetPrimaryKeyProperty(this Type type)
        {
            var tableName = type.GetTableName();

            // 取得標記 [ExplicitKey] 之屬性
            var properties = type.GetProperties()
                .Where(c =>
                    c.GetCustomAttributes(typeof(KeyAttribute), true).Any() ||
                    c.GetCustomAttributes(typeof(ExplicitKeyAttribute), true).Any()
                );

            // 回傳名為 Id 或 TableName + Id 之屬性
            return
                properties.FirstOrDefault(c => c.Name.Equals("ID", StringComparison.CurrentCultureIgnoreCase)) ??
                properties.FirstOrDefault(c => c.Name.Equals($"{tableName}ID", StringComparison.CurrentCultureIgnoreCase)) ??
                properties.FirstOrDefault(c => c.Name.EndsWith($"{tableName.Substring(2)}ID", StringComparison.CurrentCultureIgnoreCase)) ??
                properties.FirstOrDefault(c => c.Name.EndsWith($"{tableName.Substring(3)}ID", StringComparison.CurrentCultureIgnoreCase)) ??
                properties.SingleOrDefault();
        }
    }
}
