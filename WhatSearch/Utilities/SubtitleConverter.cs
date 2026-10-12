using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace WhatSearch.Utilities
{
    /// <summary>
    /// 把外掛字幕（.srt/.vtt/.ass/.ssa）轉成瀏覽器 &lt;track&gt; 看得懂的 WebVTT。
    /// ASS/SSA 只取對話文字，樣式、位置、特效、向量繪圖一律丟棄。
    /// </summary>
    public static class SubtitleConverter
    {
        public static readonly string[] Extensions = { ".srt", ".vtt", ".ass", ".ssa" };

        static SubtitleConverter()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public static bool IsSubtitleExtension(string ext)
        {
            return Extensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
        }

        public static string ToWebVtt(byte[] data, string ext)
        {
            string text = DecodeText(data).Replace("\r\n", "\n").Replace('\r', '\n');
            switch ((ext ?? string.Empty).ToLowerInvariant())
            {
                case ".vtt":
                    return text.TrimStart().StartsWith("WEBVTT") ? text : "WEBVTT\n\n" + text;
                case ".ass":
                case ".ssa":
                    return AssToVtt(text);
                default:
                    return SrtToVtt(text);
            }
        }

        // BOM -> 嚴格 UTF-8 -> 失敗就當 Big5
        public static string DecodeText(byte[] data)
        {
            if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
            {
                return Encoding.UTF8.GetString(data, 3, data.Length - 3);
            }
            if (data.Length >= 2 && data[0] == 0xFF && data[1] == 0xFE)
            {
                return Encoding.Unicode.GetString(data, 2, data.Length - 2);
            }
            if (data.Length >= 2 && data[0] == 0xFE && data[1] == 0xFF)
            {
                return Encoding.BigEndianUnicode.GetString(data, 2, data.Length - 2);
            }
            try
            {
                return new UTF8Encoding(false, true).GetString(data);
            }
            catch (DecoderFallbackException)
            {
                return Encoding.GetEncoding(950).GetString(data);
            }
        }

        static readonly Regex SrtTimeRegex = new Regex(@"(\d{1,2}:\d{2}:\d{2}),(\d{1,3})", RegexOptions.Compiled);
        static readonly Regex OverrideBlockRegex = new Regex(@"\{[^}]*\}", RegexOptions.Compiled);
        static readonly Regex DrawingRegex = new Regex(@"\{[^}]*\\p[1-9][^}]*\}", RegexOptions.Compiled);
        static readonly Regex AssTimeRegex = new Regex(@"^(\d+):(\d{2}):(\d{2})[.:](\d{1,3})$", RegexOptions.Compiled);

        static string SrtToVtt(string text)
        {
            StringBuilder sb = new StringBuilder("WEBVTT\n\n");
            foreach (string rawLine in text.Split('\n'))
            {
                string line = rawLine;
                if (line.Contains("-->"))
                {
                    line = SrtTimeRegex.Replace(line, m => m.Groups[1].Value + "." + m.Groups[2].Value.PadRight(3, '0'));
                }
                else
                {
                    line = OverrideBlockRegex.Replace(line, string.Empty);
                }
                sb.Append(line).Append('\n');
            }
            return sb.ToString();
        }

        static string AssToVtt(string text)
        {
            // 預設欄位順序（ASS 與 SSA v4 的 Start/End/Text 位置相同）
            int fieldCount = 10, idxStart = 1, idxEnd = 2, idxText = 9;
            bool inEvents = false;
            List<Tuple<TimeSpan, TimeSpan, string>> cues = new List<Tuple<TimeSpan, TimeSpan, string>>();

            foreach (string rawLine in text.Split('\n'))
            {
                string line = rawLine.Trim();
                if (line.StartsWith("["))
                {
                    inEvents = line.Equals("[Events]", StringComparison.OrdinalIgnoreCase);
                    continue;
                }
                if (inEvents == false)
                {
                    continue;
                }
                if (line.StartsWith("Format:", StringComparison.OrdinalIgnoreCase))
                {
                    string[] names = line.Substring(7).Split(',').Select(t => t.Trim()).ToArray();
                    int s = Array.FindIndex(names, n => n.Equals("Start", StringComparison.OrdinalIgnoreCase));
                    int e = Array.FindIndex(names, n => n.Equals("End", StringComparison.OrdinalIgnoreCase));
                    int t2 = Array.FindIndex(names, n => n.Equals("Text", StringComparison.OrdinalIgnoreCase));
                    if (s >= 0 && e >= 0 && t2 >= 0)
                    {
                        fieldCount = names.Length;
                        idxStart = s;
                        idxEnd = e;
                        idxText = t2;
                    }
                    continue;
                }
                if (line.StartsWith("Dialogue:", StringComparison.OrdinalIgnoreCase) == false)
                {
                    continue;
                }

                // Text 欄位本身可能含逗號，所以只切成 fieldCount 份
                string[] parts = line.Substring(9).TrimStart().Split(new[] { ',' }, fieldCount);
                if (parts.Length < fieldCount)
                {
                    continue;
                }
                TimeSpan start, end;
                if (TryParseAssTime(parts[idxStart], out start) == false ||
                    TryParseAssTime(parts[idxEnd], out end) == false)
                {
                    continue;
                }
                string content = parts[idxText];
                if (DrawingRegex.IsMatch(content))
                {
                    continue;
                }
                content = content.Replace("\\N", "\n").Replace("\\n", "\n").Replace("\\h", " ");
                content = OverrideBlockRegex.Replace(content, string.Empty);
                content = content.Replace("&", "&amp;").Replace("<", "&lt;").Replace("-->", "--&gt;");
                string[] contentLines = content.Split('\n').Select(t => t.Trim()).Where(t => t.Length > 0).ToArray();
                if (contentLines.Length == 0)
                {
                    continue;
                }
                cues.Add(Tuple.Create(start, end, string.Join("\n", contentLines)));
            }

            StringBuilder sb = new StringBuilder("WEBVTT\n\n");
            foreach (var cue in cues.OrderBy(c => c.Item1))
            {
                sb.Append(FormatVttTime(cue.Item1)).Append(" --> ").Append(FormatVttTime(cue.Item2)).Append('\n');
                sb.Append(cue.Item3).Append("\n\n");
            }
            return sb.ToString();
        }

        static bool TryParseAssTime(string value, out TimeSpan result)
        {
            result = TimeSpan.Zero;
            Match m = AssTimeRegex.Match(value.Trim());
            if (m.Success == false)
            {
                return false;
            }
            int ms = int.Parse(m.Groups[4].Value.PadRight(3, '0').Substring(0, 3));
            result = new TimeSpan(0, int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value),
                int.Parse(m.Groups[3].Value), ms);
            return true;
        }

        static string FormatVttTime(TimeSpan ts)
        {
            return string.Format("{0:00}:{1:00}:{2:00}.{3:000}", (int)ts.TotalHours, ts.Minutes, ts.Seconds, ts.Milliseconds);
        }
    }
}
