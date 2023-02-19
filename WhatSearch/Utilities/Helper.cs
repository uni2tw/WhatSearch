using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using WhatSearch.Core;

namespace WhatSearch.Utility
{
    public class Helper
    {
        public class ConstStrings
        {
            public const string Folder = "folder";
            public const string Application = "application";
            public const string Video = "video";
            public const string Music = "music";
            public const string Text = "text";
            public const string Image = "image";
        }

        private static string _rootPath;
        public static void SetRootPath(string rootPath)
        {
            Helper._rootPath = rootPath;
        }
        public static string GetRootPath()
        {            
            if (_rootPath != null)
            {
                return _rootPath;
            }                   
            if (_rootPath == null)
            {
                _rootPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) +
                    Path.DirectorySeparatorChar;
            }
            return _rootPath;
        }

        public static string GetReadableTimeSpan(TimeSpan t)
        {
            if (t.TotalSeconds <= 1)
            {
                return $@"{t:s\.ff} seconds";
            }
            if (t.TotalMinutes <= 1)
            {
                return $@"{t:%s} seconds";
            }
            if (t.TotalHours <= 1)
            {
                return $@"{t:%m} minutes";
            }
            if (t.TotalDays <= 1)
            {
                return $@"{t:%h} hours";
            }

            return $@"{t:%d} days";
        }

        public static string UrlEncodeLite(string v)
        {
            return v.Replace(" ", "%20");
        }

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

        static string _productVersion;
        public static string GetProductVersion()
        {
            if (_productVersion == null)
            {
                FileVersionInfo fi = FileVersionInfo.GetVersionInfo(Process.GetCurrentProcess().MainModule.FileName);
                _productVersion = fi.FileVersion.ToString();
            }
            return "WhatSearch " + _productVersion;
        }

        public static string GetMD5(string s)
        {
            return GetMD5(Encoding.ASCII.GetBytes(s));
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
        public static string GetFileDocType(string fileExtension)
        {
            if (fileTypeDisplayNames == null)
            {
                fileTypeDisplayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                fileTypeDisplayNames.Add(".exe", ConstStrings.Application);
                fileTypeDisplayNames.Add(".mp4", ConstStrings.Video);
                fileTypeDisplayNames.Add(".avi", ConstStrings.Video);
                fileTypeDisplayNames.Add(".mkv", ConstStrings.Video);
                fileTypeDisplayNames.Add(".wmv", ConstStrings.Video);
                fileTypeDisplayNames.Add(".rmvb", ConstStrings.Video);
                fileTypeDisplayNames.Add(".mp3", ConstStrings.Music);
                fileTypeDisplayNames.Add(".m4a", ConstStrings.Music);
                fileTypeDisplayNames.Add(".flac", ConstStrings.Music);
                fileTypeDisplayNames.Add(".jpg", ConstStrings.Image);
                fileTypeDisplayNames.Add(".jpeg", ConstStrings.Image);
                fileTypeDisplayNames.Add(".png", ConstStrings.Image);
                fileTypeDisplayNames.Add(".gif", ConstStrings.Image);
                fileTypeDisplayNames.Add(".txt", ConstStrings.Text);
                fileTypeDisplayNames.Add(".md", ConstStrings.Text);
            }
            if (fileTypeDisplayNames.ContainsKey(fileExtension) == false)
            {
                return "其它";
            }
            return fileTypeDisplayNames[fileExtension];
        }

        public static int ToInt32(bool val)
        {
            if (val) { return 1; }
            return 0;
        }

        static readonly string[] SizeSuffixes =
            { "Bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        public static string GetReadableByteSize(long value, int decimalPlaces = 2)
        {
            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
            if (value < 0) { return "-" + GetReadableByteSize(-value); }
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


        /// <summary>
        /// 目前被管控的清單
        /// * /Themes/PCweb/css/ppon_item.min.css
        /// * /Themes/mobile/css/style.css
        /// </summary>
        /// <param name="cssFile"></param>
        /// <returns></returns>
        public static HtmlString ContentCss(string cssFile)
        {
            string contentFolder = Ioc.GetConfig().ContentRootPath;
            string cacheKey = string.Format("css://{0}", cssFile);
            long? lastWriteTime = Ioc.GetCache().Get<long?>(cacheKey);
            string cssPath = Path.Combine(contentFolder, "wwwroot", "css", cssFile);
            if (lastWriteTime == null)
            {                
                FileInfo fiCss = new FileInfo(cssPath);
                if (fiCss.Exists == false)
                {
                    return HtmlString.Empty;
                }
                lastWriteTime = fiCss.LastWriteTime.Ticks;
                Ioc.GetCache().Set(cacheKey, lastWriteTime, TimeSpan.FromMinutes(5));
            }            
            return new HtmlString(string.Format("<link type=\"text/css\" rel=\"stylesheet\" href=\"/assets/css/{0}?{1}\" />",
                cssFile, lastWriteTime.Value));
        }

        //TODO 這邊錯了
        public static HtmlString ContentJs(string jsFile)
        {
            string contentFolder = Ioc.GetConfig().ContentRootPath;
            var config = Ioc.GetConfig();
            string cacheKey = string.Format("js://{0}", jsFile);
            long? lastWriteTime = Ioc.GetCache().Get<long?>(cacheKey);
            string jsPath = Path.Combine(contentFolder, "wwwroot", "js", jsFile);
            if (lastWriteTime == null)
            {
                FileInfo fiCss = new FileInfo(jsPath);
                if (fiCss.Exists == false)
                {
                    return HtmlString.Empty;
                }
                lastWriteTime = fiCss.LastWriteTime.Ticks;
                Ioc.GetCache().Set(cacheKey, lastWriteTime, TimeSpan.FromMinutes(5));
            }
            return new HtmlString(string.Format("<script type=\"text/javascript\" src=\"/assets/js/{0}?{1}\"></script>",
                jsFile, lastWriteTime.Value));
        }
        /// <summary>
        /// 字串轉成網址安全的變形base64
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string EncodeUrlBase64(string url)
        {
            string returnValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(url))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
            return returnValue;
        }
        /// <summary>
        /// 將安全的變形base64轉換回原網址
        /// </summary>
        /// <param name="base64Url"></param>
        /// <returns></returns>
        public static string DecodeUrlBase64(string base64Url)
        {
            string incoming = base64Url.Replace('_', '/').Replace('-', '+');
            switch (base64Url.Length % 4)
            {
                case 2: incoming += "=="; break;
                case 3: incoming += "="; break;
            }
            byte[] bytes = Convert.FromBase64String(incoming);
            string originalText = Encoding.UTF8.GetString(bytes);
            return originalText;
        }
    }
}
