using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace WhatSearch.Core.Extensions
{
    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum enumValue)
        {
            return enumValue.GetType()?
                            .GetMember(enumValue.ToString())?
                            .First()?
                            .GetCustomAttribute<DisplayAttribute>()?
                            .Name;
        }

        /// <summary>
        /// 取得：列舉(Enum) 項目，預設依Display.Order排序；若無設定Display.Order，則依Enum.value
        /// </summary>
        /// <typeparam name="T">Enum</typeparam>
        /// <returns>
        /// </returns>
        public static IEnumerable<EnumSource> GenToEnumSource<T>() where T : struct
        {
            IEnumerable<EnumSource> results = null;
            Type type = typeof(T);
            if (type.IsEnum)
            {
                results = type
                    .GetEnumNames()
                    .Select(name =>
                                 new EnumSource
                                 {
                                     DisplayName = ((Enum)Enum.Parse(type, name)).GetDisplayName(),
                                     Value = Enum.Parse(type, name),
                                     Order = ((Enum)Enum.Parse(type, name)).GetDisplayOrder() ?? Convert.ToInt32(Enum.Parse(type, name))
                                 })
                    .OrderBy(o => o.Order);
            }
            return results;
        }

        /// <summary>
        /// 取得：Display.Order
        /// </summary>
        /// <param name="enumValue"></param>
        /// <returns></returns>
        public static int? GetDisplayOrder(this Enum enumValue)
        {
            return enumValue.GetType()?
                            .GetMember(enumValue.ToString())?
                            .First()?
                            .GetCustomAttribute<DisplayAttribute>()?
                            .GetOrder();
        }

        public class EnumSource
        {
            public object DisplayName { get; set; }
            public object Value { get; set; }
            public int Order { get; set; }
        }

        public static string GetDescriptionText(this Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] attributes =
                (DescriptionAttribute[])fi.GetCustomAttributes(
                    typeof(DescriptionAttribute),
                    false);

            if (attributes != null &&
                attributes.Length > 0)
                return attributes[0].Description;
            else
                return value.ToString();
        }
    }
}
