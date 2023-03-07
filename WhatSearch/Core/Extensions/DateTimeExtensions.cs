using System;
using System.Globalization;

namespace WhatSearch.Core.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime StartOfHour(this DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0);
        }

        public static bool Between(this DateTime date, DateTime startTime, DateTime endTime)
        {
            return date >= startTime && date <= endTime;
        }

        public static string ToISO8601(this DateTime time)
        {
            DateTimeOffset localTimeAndOffset = new DateTimeOffset(time, TimeZoneInfo.Local.GetUtcOffset(time));
            return localTimeAndOffset.ToString("s");
        }
    }
}
