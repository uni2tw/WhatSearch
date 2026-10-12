using System;
using System.Text;

namespace WhatSearch.Utilities
{
    /// <summary>
    /// 自動偵測文字檔編碼並解碼（不保證 100% 正確，但涵蓋常見情況）。
    /// 順序：BOM（UTF-8/UTF-16）→ 嚴格 UTF-8 → 依特徵判斷 Shift-JIS / Big5 / GBK → 都不合法就寬鬆 Big5。
    /// Big5 與 GBK 的有效編碼範圍重疊很多，只能用啟發式判斷，短文字可能判錯。
    /// </summary>
    public static class TextDecoder
    {
        static TextDecoder()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public static string Decode(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return string.Empty;
            }
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

            string text;
            if (TryDecode(new UTF8Encoding(false, true), data, out text))
            {
                return text;
            }

            string big5Text, gbText, sjisText;
            bool okBig5 = TryDecode(Strict(950), data, out big5Text);
            bool okGb = TryDecode(Strict(54936), data, out gbText);
            bool okSjis = TryDecode(Strict(932), data, out sjisText);

            // 日文：解碼後有明顯比例的全形假名（中文位元組幾乎不會解出假名）
            if (okSjis && LooksJapanese(sjisText))
            {
                return sjisText;
            }
            if (okBig5 && okGb)
            {
                return LooksBig5(data) ? big5Text : gbText;
            }
            if (okBig5)
            {
                return big5Text;
            }
            if (okGb)
            {
                return gbText;
            }
            if (okSjis)
            {
                return sjisText;
            }
            return Encoding.GetEncoding(950).GetString(data);
        }

        static Encoding Strict(int codePage)
        {
            return Encoding.GetEncoding(codePage, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        }

        static bool LooksJapanese(string text)
        {
            int kana = 0, nonAscii = 0;
            foreach (char c in text)
            {
                if (c < 0x80)
                {
                    continue;
                }
                nonAscii++;
                if (c >= 0x3041 && c <= 0x30FF && c != 0x30FB)
                {
                    kana++;
                }
            }
            return kana >= 2 && kana * 5 >= nonAscii;
        }

        // Big5 與 GBK 的有效範圍重疊，改看雙位元組的分布：
        // 1) Big5 的第二位元組常落在 0x40-0x7E，GB2312 常用字則一律 >= 0xA1
        // 2) Big5 常用字的第一位元組集中在 0xA4-0xC6，簡體 GBK 常用字則散在 0xB0-0xD7
        static bool LooksBig5(byte[] data)
        {
            int pairs = 0, lowTrail = 0, commonLead = 0, nonPunct = 0;
            for (int i = 0; i < data.Length - 1; i++)
            {
                byte lead = data[i];
                if (lead < 0x81 || lead > 0xFE)
                {
                    continue;
                }
                byte trail = data[i + 1];
                i++;
                pairs++;
                if (trail >= 0x40 && trail <= 0x7E)
                {
                    lowTrail++;
                }
                if (lead >= 0xA4)
                {
                    nonPunct++;
                    if (lead <= 0xC6)
                    {
                        commonLead++;
                    }
                }
            }
            if (pairs == 0 || nonPunct == 0)
            {
                return true;
            }
            if (lowTrail * 100 >= pairs * 15)
            {
                return true;
            }
            return commonLead * 100 >= nonPunct * 80;
        }

        static bool TryDecode(Encoding encoding, byte[] data, out string text)
        {
            try
            {
                text = encoding.GetString(data);
                return true;
            }
            catch (DecoderFallbackException)
            {
                text = null;
                return false;
            }
        }
    }
}
