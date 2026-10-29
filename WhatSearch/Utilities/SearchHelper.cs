using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Lucene.Net.Search;
using WhatSearch.Core;
using WhatSearch.Models;

namespace WhatSearch.Utilities
{
    public class SearchHelper
    {
        private static Dictionary<UnicodeCategory, int> CharCategoryGroup = new Dictionary<UnicodeCategory, int>();

        static SearchHelper()
        {
            CharCategoryGroup.Add(UnicodeCategory.UppercaseLetter, 1); //0
            CharCategoryGroup.Add(UnicodeCategory.LowercaseLetter, 1); //1
            CharCategoryGroup.Add(UnicodeCategory.ClosePunctuation, 0); // 21
            CharCategoryGroup.Add(UnicodeCategory.ConnectorPunctuation, 0); // 18 含「_」，索引端的 Tokenizer 也以「_」為分隔
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
            int leftPos = keyword.IndexOf('\"');
            int rightPos = keyword.LastIndexOf('\"');

            if (leftPos >= 0 && leftPos != rightPos)
            {
                //有2個「"」才當成引號處理，引號內整段當一個詞
                string leftStr = keyword.Substring(0, leftPos);
                string rightStr = keyword.Substring(rightPos + 1);
                string middleStr = keyword.Substring(leftPos + 1, rightPos - leftPos - 1);

                result.AddRange(SplitStringForSearchByCharCategory(leftStr));
                if (middleStr.Length > 0)
                {
                    result.Add(middleStr);
                }
                result.AddRange(SplitStringForSearchByCharCategory(rightStr));
            }
            else
            {
                //沒有「"」或只有 1 個「"」（「"」本身是分隔字元，會被略過）
                result.AddRange(SplitStringForSearchByCharCategory(keyword));
            }
            return result.ToArray();
        }

        private static string[] SplitStringForSearchByCharCategory(string s)
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
        /// 索引端 n-gram 最長只建到 MicroAnalyzer.MaxGramSize，更長的詞索引裡沒有，直接查會搜不到，所以截斷成前 MaxGramSize 個字元
        /// </summary>
        private static string TruncateForIndex(string word)
        {
            if (word.Length <= MicroAnalyzer.MaxGramSize)
            {
                return word;
            }
            int len = MicroAnalyzer.MaxGramSize;
            if (char.IsHighSurrogate(word[len - 1]))
            {
                len--;
            }
            return word.Substring(0, len);
        }

        public static Query GetLuceneQuery(string filed, string queryString)
        {
            string[] words = SplitStringForSearch(queryString);
            BooleanQuery mainQuery = new BooleanQuery();
            foreach(string word in words)
            {
                TermQuery tq = new TermQuery(new Lucene.Net.Index.Term(filed, TruncateForIndex(word)));
                mainQuery.Add(tq, Occur.MUST);
            }
            return mainQuery;
        }
    }
}
