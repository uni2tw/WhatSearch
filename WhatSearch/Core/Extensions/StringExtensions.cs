using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WhatSearch.Core.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// 指定今天為準的往前或往後幾週的星期一(第一天 00:00:00)和星期日(最後一天 59:59:59)
        /// </summary>
        /// <param name="time"></param>
        /// <param name="addWeekNum">指定今天為準的往前或往後幾週,addWeekNum=0或沒填代表當週</param>
        /// <returns> 一週的星期一(第一天)和星期日(最後一天)</returns>
        public static (DateTime, DateTime) GetWeek(this DateTime time, int addWeekNum = 0)
        {
            var newWeek = time.AddDays(addWeekNum * 7);
            var theLastWeekDay = time.DayOfWeek == DayOfWeek.Sunday ? newWeek : newWeek.AddDays(7 - Convert.ToInt16(time.DayOfWeek)); //00:00:00

            var firstDay = theLastWeekDay.AddDays(-6);
            var lastWeekDay = theLastWeekDay.AddDays(1).AddSeconds(-1); //59:59:59

            return (firstDay, lastWeekDay);
        }

        /// <summary>
        /// 指定今天為準的往前或往後幾月的第一天和最後一天
        /// </summary>
        /// <param name="time"></param>
        /// <param name="addMonthNum">指定今天為準的往前或往後幾週,addMonthNum=0或沒填代表當月</param>
        /// <returns> 月份的第一天和最後一天</returns>
        public static (DateTime FirstDate, DateTime LastDate) GetMonth(this DateTime time, int addMonthNum = 0)
        {
            var firstDay = Convert.ToDateTime(time.Year.ToString() + "-" + time.AddMonths(addMonthNum).Month.ToString() + "-1"); ;
            var lastDay = firstDay.AddMonths(1).AddSeconds(-1);
            return (firstDay, lastDay);
        }


        /// <summary>
        /// 檢核是否為數字型態的字串
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool CheckOnlyNumber(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return false;

            return Regex.IsMatch(str, @"^[0-9]+$");
        }

        /// <summary>
        /// 檢核是否為hhmm
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool CheckTime(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return false;

            return Regex.IsMatch(str, @"^([0-1][0-9]|2[0-3])([0-5][0-9])$");
        }

        /// <summary>
        /// String To Int?
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static int? StringToIntOrNull(this string str)
        {
            return int.TryParse(str, out int number) ? number : null;
        }

        /// <summary>
        /// String To Int
        /// 轉不過則回傳0
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static int StringToInt(this string str)
        {
            return int.TryParse(str, out int number) ? number : 0;
        }

        /// <summary>
        /// String To decimal?
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static decimal? StringToDecimalOrNull(this string str)
        {
            return decimal.TryParse(str, out decimal number) ? number : null;
        }

        public static bool StringToBoolean(this string str)
        {
            if (str == "1") return true;
            bool value;
            if (bool.TryParse(str, out value))
            {
                return value;
            }
            return false;
        }

        /// <summary>
        /// StringToSubString
        /// </summary>
        /// <param name="str"></param>
        /// <param name="length">準備截取的長度</param>
        /// <param name="startLen">從哪裡開始</param>
        /// <returns></returns>
        public static string StringToSubString(this string str, int length, int startLen = 0)
        {
            return str.Length > length ? str.Substring(startLen, length) : str;
        }

        /// <summary>
        /// String To DateTime
        /// 轉不過則回傳null
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static DateTime? StringToDateTime(this string str)
        {
            return DateTime.TryParse(str, out DateTime dateTime) ? dateTime : null;
        }

        /// <summary>
        /// String To Bool
        /// 轉不過則回傳null
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool? StringToBool(this string str)
        {
            if (str == "1")
            {
                return true;
            }
            else if (str == "0")
            {
                return false;
            }
            return bool.TryParse(str, out bool result) ? result : null;
        }
    }
}
