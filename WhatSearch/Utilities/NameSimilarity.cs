using System;
using System.Text.RegularExpressions;

namespace WhatSearch.Utilities
{
    /// <summary>
    /// 資料夾名稱相似度（0~1），用來在「找不到資料夾」時建議可能是改名後的同層資料夾。
    /// 規則：只差空白/大小寫 1.0；去掉括號註記後相同 0.95；其中一個完整包含另一個 0.85；否則用編輯距離相似度。
    /// </summary>
    public static class NameSimilarity
    {
        public const double SuggestThreshold = 0.7;

        static readonly Regex Brackets = new Regex(@"[\(\[（【][^\)\]）】]*[\)\]）】]", RegexOptions.Compiled);
        static readonly Regex Spaces = new Regex(@"\s+", RegexOptions.Compiled);

        public static string Plain(string s)
        {
            return Spaces.Replace(s ?? string.Empty, string.Empty).ToLowerInvariant();
        }

        public static double Score(string oldName, string candidate)
        {
            string po = Plain(oldName), pn = Plain(candidate);
            if (po.Length == 0 || pn.Length == 0)
            {
                return 0;
            }
            if (po == pn)
            {
                return 1.0;
            }
            string bo = Plain(Brackets.Replace(oldName, string.Empty));
            string bn = Plain(Brackets.Replace(candidate, string.Empty));
            if (bo.Length > 0 && bo == bn)
            {
                return 0.95;
            }
            if (Math.Min(po.Length, pn.Length) >= 3 && (po.Contains(pn) || pn.Contains(po)))
            {
                return 0.85;
            }
            if (po.Length > 200 || pn.Length > 200)
            {
                return 0;
            }
            return 1.0 - (double)Levenshtein(po, pn) / Math.Max(po.Length, pn.Length);
        }

        static int Levenshtein(string a, string b)
        {
            int[] prev = new int[b.Length + 1];
            int[] cur = new int[b.Length + 1];
            for (int j = 0; j <= b.Length; j++)
            {
                prev[j] = j;
            }
            for (int i = 1; i <= a.Length; i++)
            {
                cur[0] = i;
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    cur[j] = Math.Min(Math.Min(prev[j] + 1, cur[j - 1] + 1), prev[j - 1] + cost);
                }
                int[] t = prev;
                prev = cur;
                cur = t;
            }
            return prev[b.Length];
        }
    }
}
