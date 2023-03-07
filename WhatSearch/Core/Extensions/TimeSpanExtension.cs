using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatSearch.Core.Extensions
{
    public static class TimeSpanExtension
    {
        public static string ToUserFriendlyString(this TimeSpan ts)
        {
            int month = (int)ts.TotalDays / 30;
            if (month >= 1)
            {
                return $"約{month + 1}月內";
            }
            if (ts.TotalDays >= 8 && ts.TotalDays <= 15)
            {
                return "半個月內";
            }
            if (ts.TotalDays >= 2 && ts.TotalDays < 8)
            {
                return "數天後";
            }
            else if (ts.TotalDays >= 1 && ts.TotalDays < 2)
            {
                return "1天後";
            }
            else if (ts.Hours > 12)
            {
                return "半天後";
            }
            else if (ts.Hours >= 1)
            {
                return $"{ts.Hours}小時後";
            }
            else if (ts.TotalMinutes > 5)
            {
                return $"{ts.Minutes}分鐘後";
            }
            else if (ts.TotalMinutes < 5)
            {
                return $"{ts.Minutes + 1}分鐘內";
            }

            return "";
        }

    }
}
