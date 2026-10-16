using System;
using System.Collections.Generic;
using System.Text;

namespace WhatSearch.Utilities
{
    /// <summary>
    /// 修正 ID3 標籤的中日文亂碼。很多中文 MP3 把 Big5 / GBK / Shift-JIS（或 UTF-8）的位元組直接放進標示為 Latin-1 的欄位，
    /// TagLib 照 Latin-1 解出來就是 `·R±¡Âà²¾` 這種亂碼。Latin-1 的解碼是無損的（每個位元組對應一個字元），
    /// 所以可以還原成原始位元組，再用 <see cref="TextDecoder"/> 的編碼偵測重新解碼。
    /// 為了不把歐洲語系的 Latin-1 文字（Björk、Åge、Größe）誤判成中文（「大寫帶重音字母＋小寫字母」剛好也是合法的 Big5 / GBK 雙位元組）：
    ///  - 同一首歌的欄位（標題、演出者、專輯）一起偵測、用同一種編碼，並且整體要有夠高比例的雙位元組落在「常用字區」
    ///    （Big5 首位元組 A1-C5、GB2312 A1-A9 與 B0-D7 加尾位元組 A1-FE、Shift-JIS 的 81-9F / E0-EA）；個別罕用字不會單獨被拒絕；
    ///  - 每個欄位解出來必須幾乎全是中日韓字元（不含半形片假名），而且漢字不能緊貼著英文字母（像 Bj?rk、?ge 這種是歐洲文字）。
    /// 本來就正確的字串（含任何大於 U+00FF 的字元，例如正確解出來的 UTF-16 / UTF-8 欄位）完全不動。
    /// </summary>
    public static class TagTextFixer
    {
        static TagTextFixer()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public static string[] Fix(params string[] fields)
        {
            string[] result = (string[])fields.Clone();
            List<int> candidates = new List<int>();
            for (int i = 0; i < fields.Length; i++)
            {
                if (IsCandidate(fields[i]))
                {
                    candidates.Add(i);
                }
            }
            if (candidates.Count == 0)
            {
                return result;
            }

            Encoding latin1 = Encoding.GetEncoding(28591);
            // 所有可疑欄位串成一份位元組一起偵測（用換行字元隔開，它不會是任何雙位元組編碼的尾位元組）
            List<byte> joint = new List<byte>();
            foreach (int i in candidates)
            {
                joint.AddRange(latin1.GetBytes(fields[i]));
                joint.Add(0x0A);
            }
            byte[] jointBytes = joint.ToArray();
            Encoding encoding = TextDecoder.DetectEncoding(jointBytes);
            if (encoding == null)
            {
                return result;
            }
            // 整體檢查：非 UTF-8 時，雙位元組要有夠高比例落在常用字區
            if (encoding.CodePage != 65001 && CommonPairRatio(jointBytes, encoding.CodePage) < 0.7)
            {
                return result;
            }

            foreach (int i in candidates)
            {
                byte[] bytes = latin1.GetBytes(fields[i]);
                string text;
                try
                {
                    text = encoding.GetString(bytes);
                }
                catch (DecoderFallbackException)
                {
                    continue;
                }
                if (IsPlausible(text, encoding))
                {
                    result[i] = text;
                }
            }
            return result;
        }

        // 全部字元都在 U+0000~U+00FF，而且至少有一個 >= U+0080：可能是被當成 Latin-1 的其他編碼
        static bool IsCandidate(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return false;
            }
            bool high = false;
            foreach (char c in s)
            {
                if (c > 0xFF)
                {
                    return false;
                }
                if (c >= 0x80)
                {
                    high = true;
                }
            }
            return high;
        }

        static bool IsPlausible(string text, Encoding encoding)
        {
            if (text.IndexOf('�') >= 0)
            {
                return false;
            }
            // UTF-8：嚴格解碼成功且有多位元組序列，純屬巧合的機率極低，直接接受
            if (encoding.CodePage == 65001)
            {
                return true;
            }
            int nonAscii = 0, cjk = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c < 0x80)
                {
                    continue;
                }
                nonAscii++;
                if (IsCjkish(c))
                {
                    cjk++;
                }
                // 漢字緊貼英文字母（Bj?rk、?ge、Gr?e）是歐洲文字被誤當成雙位元組編碼的特徵；假名不受影響（日文常見羅馬字與假名相鄰）
                if (IsIdeograph(c))
                {
                    bool letterBefore = i > 0 && IsAsciiLetter(text[i - 1]);
                    bool letterAfter = i + 1 < text.Length && IsAsciiLetter(text[i + 1]);
                    if (letterBefore || letterAfter)
                    {
                        return false;
                    }
                }
            }
            return nonAscii > 0 && cjk * 100 >= nonAscii * 80;
        }

        static bool IsIdeograph(char c)
        {
            return (c >= 0x3400 && c <= 0x4DBF) || (c >= 0x4E00 && c <= 0x9FFF) || (c >= 0xF900 && c <= 0xFAFF);
        }

        static bool IsAsciiLetter(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
        }

        // 中日韓文字、假名、諺文、全形符號、常見的標點
        static bool IsCjkish(char c)
        {
            if (c >= 0xFF61 && c <= 0xFF9F)
            {
                return false;       // 半形片假名：Latin-1 大寫字母（À、Æ、Ö…）在 Shift-JIS 下會變成它，不算
            }
            return (c >= 0x2E80 && c <= 0x9FFF) || (c >= 0xAC00 && c <= 0xD7AF) || (c >= 0xF900 && c <= 0xFAFF)
                || (c >= 0xFF00 && c <= 0xFFEF) || (c >= 0x2010 && c <= 0x206F) || c == 0x00B7;
        }

        // 高位元組（>= 0x80）組成的雙位元組中，落在「常用字區」的比例（罕用字、擴充字不算常用，所以要看整體比例，不能單看一個短欄位）
        static double CommonPairRatio(byte[] bytes, int codePage)
        {
            int pairs = 0, common = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                byte lead = bytes[i];
                if (lead < 0x80)
                {
                    continue;
                }
                if (codePage == 932 && lead >= 0xA1 && lead <= 0xDF)
                {
                    pairs++;       // 半形片假名（單位元組）：算一個，但不算常用（Latin-1 大寫字母會變成它）
                    continue;
                }
                byte trail = i + 1 < bytes.Length ? bytes[i + 1] : (byte)0;
                i++;
                pairs++;
                if (codePage == 950)
                {
                    if (lead >= 0xA1 && lead <= 0xC5 && ((trail >= 0x40 && trail <= 0x7E) || (trail >= 0xA1 && trail <= 0xFE)))
                    {
                        common++;
                    }
                }
                else if (codePage == 54936 || codePage == 936)
                {
                    if (((lead >= 0xA1 && lead <= 0xA9) || (lead >= 0xB0 && lead <= 0xD7)) && trail >= 0xA1 && trail <= 0xFE)
                    {
                        common++;
                    }
                }
                else if (codePage == 932)
                {
                    if (((lead >= 0x81 && lead <= 0x9F) || (lead >= 0xE0 && lead <= 0xEA)) && trail >= 0x40 && trail <= 0xFC && trail != 0x7F)
                    {
                        common++;
                    }
                }
                else
                {
                    common++;
                }
            }
            return pairs == 0 ? 0 : (double)common / pairs;
        }
    }
}
