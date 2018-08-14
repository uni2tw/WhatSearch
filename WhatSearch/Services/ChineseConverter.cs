using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WhatSearch.Services.Interfaces;
using WhatSearch.Utility;

namespace WhatSearch.Services
{
    public class ConvChar
    {
        public char Key { get; set; }
        public char Value { get; set; }
        //public HashSet<ConvChar> NextChars { get; set; }
        public List<Tuple<string, string>> Phrases { get; set; }

        public ConvChar()
        {
            Phrases = new List<Tuple<string, string>>();
        }

        public override int GetHashCode()
        {
            if (this.Key == default(char))
            {
                return 0;
            }
            return Key.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            ConvChar castObj = obj as ConvChar;
            if (castObj == null) return false;
            return GetHashCode() == castObj.GetHashCode();
        }

        public override string ToString()
        {
            return Key.ToString();
        }
    }
    public class ChineseConverter : IChineseConverter
    {
        Dictionary<char, ConvChar> ts = new Dictionary<char, ConvChar>();
        Dictionary<char, ConvChar> st = new Dictionary<char, ConvChar>();
        public ChineseConverter()
        {
            Init();
        }

        private void Init()
        {
            InitTSCharacters();
            //C:\Git\WhatSearch\WhatSearch\data\dictionary\TWPhrasesIT.txt
            {
                string filePath = Helper.GetRelativePath("data", "dictionary", "TWPhrasesIT.txt");
                InitTWPhrases(filePath, false);
            }
            {
                string filePath = Helper.GetRelativePath("data", "dictionary", "TWPhrasesName.txt");
                InitTWPhrases(filePath, false);
            }
            {
                string filePath = Helper.GetRelativePath("data", "dictionary", "TWPhrasesOther.txt");
                InitTWPhrases(filePath, false);
            }

            InitSTCharacters();
        }

        private void InitSTCharacters()
        {
            string filePath = Helper.GetRelativePath("data", "dictionary", "STCharacters.txt");
            string[] lines = System.IO.File.ReadAllLines(filePath, Encoding.UTF8);
            int idx = 0;
            foreach (string line in lines)
            {
                idx++;
                string[] parts = line.Split("\t");
                if (parts.Length < 2)
                {
                    continue;
                }
                //45959 應該也是網路亂找的一個範圍
                char keyChar = parts[0][0];
                char valueChar = parts[1][0];
                if (parts[0].Length != 1 || keyChar >= 45959 || valueChar >= 45959)
                {
                    continue;
                }
                if (st.ContainsKey(keyChar) == false)
                {
                    st.Add(keyChar, new ConvChar { Key = keyChar, Value = valueChar });
                }
            }
        }

        private void InitTWPhrases(string filePath, bool traditionalChineseAtFirstPart)
        {
            string[] lines = System.IO.File.ReadAllLines(filePath, Encoding.UTF8);

            foreach (string line in lines)
            {
                string[] parts = line.Split("\t");
                if (parts.Length < 2)
                {
                    continue;
                }
                //string keyWord = parts[0];
                //string valueWord = parts[1];
                int simplePos = 0;
                int TranadiionalPos = 1;
                if (traditionalChineseAtFirstPart)
                {
                    TranadiionalPos = 0;
                    simplePos = 1;
                }

                string keyWord = parts[TranadiionalPos];
                string valueWord = parts[simplePos];


                bool isKeyValid = keyWord.All(t => InValidRange(t));
                if (isKeyValid == false)
                {
                    continue;
                }

                char key = keyWord[0];
                if (ts.ContainsKey(key) == false)
                {
                    ts.Add(key, new ConvChar() { Key = key, Value = key });
                }
                bool isPhrasesExist = false;
                for (int i = 0; i < ts[key].Phrases.Count; i++)
                {
                    var phrases = ts[key].Phrases;
                    if (keyWord == phrases[i].Item1)
                    {
                        isPhrasesExist = true;
                        break;
                    }
                    if (keyWord.Length > phrases[i].Item1.Length)
                    {
                        phrases.Insert(i - 0, new Tuple<string, string>(keyWord, ToSimplifiedChineseByChar(valueWord)));
                        isPhrasesExist = true;
                        break;
                    }
                }
                if (isPhrasesExist == false)
                {
                    ts[key].Phrases.Add(new Tuple<string, string>(keyWord, ToSimplifiedChineseByChar(valueWord)));
                }
            }
        }

        private bool InValidRange(int n)
        {
            return n <= 45959;
        }

        private void InitTSCharacters()
        {
            string filePath = Helper.GetRelativePath("data", "dictionary", "TSCharacters.txt");
            string[] lines = System.IO.File.ReadAllLines(filePath, Encoding.UTF8);
            int idx = 0;
            foreach (string line in lines)
            {
                idx++;
                string[] parts = line.Split("\t");
                if (parts.Length < 2)
                {
                    continue;
                }
                //45959 應該也是網路亂找的一個範圍
                char keyChar = parts[0][0];
                char valueChar = parts[1][0];
                if (parts[0].Length != 1 || keyChar >= 45959 || valueChar >= 45959)
                {
                    continue;
                }
                if (ts.ContainsKey(keyChar) == false)
                {
                    ts.Add(keyChar, new ConvChar { Key = keyChar, Value = valueChar });
                }
            }
        }

        private string ToSimplifiedChineseByChar(string word)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < word.Length; i++)
            {
                char ch = word[i];
                if (ts.ContainsKey(ch))
                {
                    var cc = ts[ch];
                    sb.Append(ts[ch].Value);
                }
                else
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString();
        }
        private string ToTraditionalChineseByChar(string word)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < word.Length; i++)
            {
                char ch = word[i];
                if (st.ContainsKey(ch))
                {
                    var cc = st[ch];
                    sb.Append(st[ch].Value);
                }
                else
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString();
        }

        public string ToSimplifiedChinese(string word)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < word.Length; i++)
            {
                char ch = word[i];
                if (ts.ContainsKey(ch))
                {
                    bool replaced = false;
                    foreach (var phrase in ts[ch].Phrases)
                    {
                        if (word.Length >= i + phrase.Item1.Length)
                        {
                            if (word.Substring(i, phrase.Item1.Length) == phrase.Item1)
                            {
                                sb.Append(phrase.Item2);
                                i = i + phrase.Item1.Length - 1;
                                replaced = true;
                                break;
                            }
                        }
                    }
                    if (replaced == false)
                    {
                        var cc = ts[ch];
                        sb.Append(ts[ch].Value);
                    }
                }
                else
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString();
        }

        public string ToTraditionalChinese(string word)
        {
            return ToTraditionalChineseByChar(word);
        }
    }
}
