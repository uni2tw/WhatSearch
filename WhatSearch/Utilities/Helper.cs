﻿using Microsoft.Extensions.PlatformAbstractions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace WhatSearch.Utility
{
    public class Helper
    {
        private static string GetMD5(byte[] buff)
        {
            StringBuilder sBuilder = new StringBuilder();
            var data = MD5.Create().ComputeHash(buff);
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }

        public static string GetProductVersion(bool showVersion)
        {
            if (showVersion)
            {
                return string.Format("{0} - {1}",
                    PlatformServices.Default.Application.ApplicationName,
                    PlatformServices.Default.Application.ApplicationVersion);
            } else
            {
                return PlatformServices.Default.Application.ApplicationName;
            }
        }

        public static string GetMD5(string s)
        {
            return GetMD5(Encoding.ASCII.GetBytes(s));
        }

        public static string GetRootPath()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        public static string GetDisplayName(Type type)
        {
            var attribute = type.GetCustomAttributes(typeof(DisplayNameAttribute), true)
                .Cast<DisplayNameAttribute>().Single();
            if (attribute == null)
            {
                return null;
            }
            return attribute.DisplayName;
        }

        public static string GetRelativePath(params string[] paths)
        {
            if (paths.Length == 1)
            {
                return Path.Combine(GetRootPath(), paths[0]);
            }
            else if (paths.Length == 2)
            {
                return Path.Combine(GetRootPath(), paths[0], paths[1]);
            }
            else if (paths.Length == 3)
            {
                return Path.Combine(GetRootPath(), paths[0], paths[1], paths[2]);
            }
            else if (paths.Length > 3)
            {
                throw new Exception("GetRelativePath 目前不支援超過3個目錄參數陣列");
            }
            return GetRootPath();
        }

        static Dictionary<string, string> fileTypeDisplayNames;
        public static string GetFileType(string fileExtension)
        {
            if (fileTypeDisplayNames == null)
            {
                fileTypeDisplayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                fileTypeDisplayNames.Add(".exe", "應用程式");
                fileTypeDisplayNames.Add(".mp4", "影片");
                fileTypeDisplayNames.Add(".avi", "影片");
                fileTypeDisplayNames.Add(".mkv", "影片");
                fileTypeDisplayNames.Add(".rmvb", "影片");
                fileTypeDisplayNames.Add(".mp3", "音樂");
                fileTypeDisplayNames.Add(".jpg", "圖片");
                fileTypeDisplayNames.Add(".jpeg", "圖片");
                fileTypeDisplayNames.Add(".png", "圖片");
                fileTypeDisplayNames.Add(".gif", "圖片");
                fileTypeDisplayNames.Add(".txt", "文件檔");
                fileTypeDisplayNames.Add(".md", "文件檔");
            }
            if (fileTypeDisplayNames.ContainsKey(fileExtension) == false)
            {
                return "其它";
            }
            return fileTypeDisplayNames[fileExtension];
        }

        static readonly string[] SizeSuffixes =
            { "Bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        public static string SizeSuffix(long value, int decimalPlaces = 2)
        {
            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
            if (value < 0) { return "-" + SizeSuffix(-value); }
            if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
        }

        public static List<string> GetAllIps()
        {
            List<string> result = new List<string>();
            foreach (NetworkInterface netInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                IPInterfaceProperties ipProps = netInterface.GetIPProperties();
                foreach (UnicastIPAddressInformation addr in ipProps.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        continue;
                    }
                    result.Add(addr.Address.ToString());

                }
            }
            return result;
        }

        private static Dictionary<UnicodeCategory, int> CharCategoryGroup = new Dictionary<UnicodeCategory, int>();

        static Helper()
        {
            CharCategoryGroup.Add(UnicodeCategory.UppercaseLetter, 1); //0
            CharCategoryGroup.Add(UnicodeCategory.LowercaseLetter, 1); //1
            CharCategoryGroup.Add(UnicodeCategory.ClosePunctuation, 0); // 21
            CharCategoryGroup.Add(UnicodeCategory.ConnectorPunctuation, 01); // 18
            CharCategoryGroup.Add(UnicodeCategory.Control, 0); // 14
            CharCategoryGroup.Add(UnicodeCategory.CurrencySymbol, 0); //26
            CharCategoryGroup.Add(UnicodeCategory.DashPunctuation, 0); //19
            CharCategoryGroup.Add(UnicodeCategory.DecimalDigitNumber, 1); //8 數字
            CharCategoryGroup.Add(UnicodeCategory.EnclosingMark, 0);  //7
            CharCategoryGroup.Add(UnicodeCategory.FinalQuotePunctuation, 0);  //23
            CharCategoryGroup.Add(UnicodeCategory.Format, 0); //15
            CharCategoryGroup.Add(UnicodeCategory.InitialQuotePunctuation, 0); //22
            CharCategoryGroup.Add(UnicodeCategory.LetterNumber, 0); //9
            CharCategoryGroup.Add(UnicodeCategory.LineSeparator, 0); //12
            CharCategoryGroup.Add(UnicodeCategory.MathSymbol, 0); //25 
            CharCategoryGroup.Add(UnicodeCategory.ModifierLetter, 0); //3
            CharCategoryGroup.Add(UnicodeCategory.ModifierSymbol, 0); //27
            CharCategoryGroup.Add(UnicodeCategory.NonSpacingMark, 0); //5
            CharCategoryGroup.Add(UnicodeCategory.OpenPunctuation, 0); //20
            CharCategoryGroup.Add(UnicodeCategory.OtherLetter, 1); //4
            CharCategoryGroup.Add(UnicodeCategory.OtherNotAssigned, 0); //29
            CharCategoryGroup.Add(UnicodeCategory.OtherNumber, 0); //10
            CharCategoryGroup.Add(UnicodeCategory.OtherPunctuation, 0); //24
            CharCategoryGroup.Add(UnicodeCategory.OtherSymbol, 0); //28
            CharCategoryGroup.Add(UnicodeCategory.ParagraphSeparator, 0); //13

            CharCategoryGroup.Add(UnicodeCategory.PrivateUse, 0); //17
            CharCategoryGroup.Add(UnicodeCategory.SpaceSeparator, 0); //11
            CharCategoryGroup.Add(UnicodeCategory.SpacingCombiningMark, 0); //6

            CharCategoryGroup.Add(UnicodeCategory.Surrogate, 0); //16
            CharCategoryGroup.Add(UnicodeCategory.TitlecaseLetter, 0); //2

        }

        public static string[] SplitStringForSearch(string keyword)
        {
            List<string> result = new List<string>();
            if (keyword.Contains("\""))
            {
                int leftPos = keyword.IndexOf('\"');
                int rightPos = keyword.LastIndexOf('\"');

                if (leftPos != rightPos)
                {
                    //有2個「"」才處理
                    string leftStr = keyword.Substring(0, leftPos);
                    string rightStr = keyword.Substring(rightPos + 1);
                    string middleStr = keyword.Substring(leftPos + 1, rightPos - leftPos - 1);

                    result.AddRange(SplitStringForSearchByCharCategory(leftStr));
                    result.Add(middleStr);
                    result.AddRange(SplitStringForSearchByCharCategory(rightStr));
                }
            }
            else
            {
                result.AddRange(SplitStringForSearchByCharCategory(keyword));
            }
            return result.ToArray();
        }

        public static string[] SplitStringForSearchByCharCategory(string s)
        {
            List<string> result = new List<string>();
            int? prevCategoryGroup = null;
            string segment = String.Empty;
            foreach (char ch in s)
            {
                UnicodeCategory category = Char.GetUnicodeCategory(ch);
                if (prevCategoryGroup != null && CharCategoryGroup[category] != prevCategoryGroup)
                {
                    if (segment.Length > 0)
                    {
                        result.Add(segment);
                    }
                    segment = String.Empty;
                    prevCategoryGroup = null;
                }
                else
                {
                    prevCategoryGroup = CharCategoryGroup[category];
                }
                if (CharCategoryGroup[category] != 0)
                {
                    segment += ch;
                }
            }
            if (segment.Length > 0)
            {
                result.Add(segment);
            }
            return result.ToArray();
        }


    }
}
