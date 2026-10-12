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

        public static string DecodeText(byte[] data)
        {
            return TextDecoder.Decode(data);
        }

        static readonly Regex SrtTimeRegex = new Regex(@"(\d{1,2}:\d{2}:\d{2}),(\d{1,3})", RegexOptions.Compiled);
        static readonly Regex OverrideBlockRegex = new Regex(@"\{[^}]*\}", RegexOptions.Compiled);
        static readonly Regex DrawingRegex = new Regex(@"\{[^}]*\\p[1-9][^}]*\}", RegexOptions.Compiled);
        static readonly Regex AssTimeRegex = new Regex(@"^(\d+):(\d{2}):(\d{2})[.:](\d{1,3})$", RegexOptions.Compiled);

        // 底部字幕不指定位置，使用瀏覽器預設的最低位置（單行/多行一致，也會自動避開控制列）。
        // 置頂/置中依 \an 數字鍵盤配置（7-9 置頂、4-6 置中）。
        // 不使用 ",start/,end" 對齊寫法：部分瀏覽器（如 Chrome）不支援，會整個設定被忽略
        static string PositionSetting(int alignment)
        {
            if (alignment >= 7 && alignment <= 9)
            {
                return " line:8%";
            }
            if (alignment >= 4 && alignment <= 6)
            {
                return " line:45%";
            }
            return string.Empty;
        }

        static readonly Regex AnRegex = new Regex(@"\\an([1-9])", RegexOptions.Compiled);
        static readonly Regex SrtTimingRegex = new Regex(
            @"^\s*(\d{1,2}:\d{2}:\d{2})[,.](\d{1,3})\s*-->\s*(\d{1,2}:\d{2}:\d{2})[,.](\d{1,3})", RegexOptions.Compiled);

        static string SrtToVtt(string text)
        {
            StringBuilder sb = new StringBuilder("WEBVTT\n\n");
            foreach (string block in Regex.Split(text.Trim('\n'), "\n{2,}"))
            {
                string[] lines = block.Split('\n');
                int timingIdx = Array.FindIndex(lines, l => l.Contains("-->"));
                if (timingIdx < 0)
                {
                    continue;
                }
                Match m = SrtTimingRegex.Match(lines[timingIdx]);
                if (m.Success == false)
                {
                    continue;
                }
                string body = string.Join("\n", lines.Skip(timingIdx + 1));
                Match an = AnRegex.Match(body);
                int alignment = an.Success ? int.Parse(an.Groups[1].Value) : 2;
                body = OverrideBlockRegex.Replace(body, string.Empty).Trim();
                if (body.Length == 0)
                {
                    continue;
                }
                if (timingIdx > 0)
                {
                    sb.Append(lines[timingIdx - 1].Trim()).Append('\n');
                }
                sb.Append(m.Groups[1].Value).Append('.').Append(m.Groups[2].Value.PadRight(3, '0').Substring(0, 3));
                sb.Append(" --> ");
                sb.Append(m.Groups[3].Value).Append('.').Append(m.Groups[4].Value.PadRight(3, '0').Substring(0, 3));
                sb.Append(PositionSetting(alignment)).Append('\n');
                sb.Append(body).Append("\n\n");
            }
            return sb.ToString();
        }

        static string AssToVtt(string text)
        {
            // 預設欄位順序（ASS 與 SSA v4 的 Start/End/Text 位置相同）
            int fieldCount = 10, idxStart = 1, idxEnd = 2, idxText = 9, idxStyle = 3;
            bool inEvents = false, inStyles = false;
            int styleFieldCount = 0, styleIdxName = -1, styleIdxAlign = -1;
            Dictionary<string, int> styleAlign = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            List<Tuple<TimeSpan, TimeSpan, string, int>> cues = new List<Tuple<TimeSpan, TimeSpan, string, int>>();

            foreach (string rawLine in text.Split('\n'))
            {
                string line = rawLine.Trim();
                if (line.StartsWith("["))
                {
                    inEvents = line.Equals("[Events]", StringComparison.OrdinalIgnoreCase);
                    inStyles = line.Equals("[V4+ Styles]", StringComparison.OrdinalIgnoreCase);
                    continue;
                }
                if (inStyles)
                {
                    // 只處理 V4+（Alignment 為數字鍵盤配置 1-9）；SSA 舊版的配置數字不同，略過
                    if (line.StartsWith("Format:", StringComparison.OrdinalIgnoreCase))
                    {
                        string[] sn = line.Substring(7).Split(',').Select(t => t.Trim()).ToArray();
                        styleFieldCount = sn.Length;
                        styleIdxName = Array.FindIndex(sn, n => n.Equals("Name", StringComparison.OrdinalIgnoreCase));
                        styleIdxAlign = Array.FindIndex(sn, n => n.Equals("Alignment", StringComparison.OrdinalIgnoreCase));
                    }
                    else if (line.StartsWith("Style:", StringComparison.OrdinalIgnoreCase) && styleIdxName >= 0 && styleIdxAlign >= 0)
                    {
                        string[] sp = line.Substring(6).TrimStart().Split(',');
                        int al;
                        if (sp.Length >= styleFieldCount && int.TryParse(sp[styleIdxAlign].Trim(), out al))
                        {
                            styleAlign[sp[styleIdxName].Trim()] = al;
                        }
                    }
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
                    int st = Array.FindIndex(names, n => n.Equals("Style", StringComparison.OrdinalIgnoreCase));
                    if (s >= 0 && e >= 0 && t2 >= 0)
                    {
                        fieldCount = names.Length;
                        idxStart = s;
                        idxEnd = e;
                        idxText = t2;
                        idxStyle = st;
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
                int alignment = 2;
                int styleAl;
                if (idxStyle >= 0 && styleAlign.TryGetValue(parts[idxStyle].Trim(), out styleAl))
                {
                    alignment = styleAl;
                }
                Match anMatch = AnRegex.Match(content);
                if (anMatch.Success)
                {
                    alignment = int.Parse(anMatch.Groups[1].Value);
                }
                content = content.Replace("\\N", "\n").Replace("\\n", "\n").Replace("\\h", " ");
                content = OverrideBlockRegex.Replace(content, string.Empty);
                content = content.Replace("&", "&amp;").Replace("<", "&lt;").Replace("-->", "--&gt;");
                string[] contentLines = content.Split('\n').Select(t => t.Trim()).Where(t => t.Length > 0).ToArray();
                if (contentLines.Length == 0)
                {
                    continue;
                }
                cues.Add(Tuple.Create(start, end, string.Join("\n", contentLines), alignment));
            }

            StringBuilder sb = new StringBuilder("WEBVTT\n\n");
            foreach (var cue in cues.OrderBy(c => c.Item1))
            {
                sb.Append(FormatVttTime(cue.Item1)).Append(" --> ").Append(FormatVttTime(cue.Item2)).Append(PositionSetting(cue.Item4)).Append('\n');
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
