using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.NGram;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System.Collections.Generic;
using System.IO;

namespace WhatSearch.Core
{
    public class MicroAnalyzer : Analyzer
    {
        private LuceneVersion ver;
        public MicroAnalyzer(LuceneVersion ver)
        {
            this.ver = ver;
        }
        protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            Tokenizer source = new SeperatorCharsTokenizer(ver, reader);
            TokenStream filter = null;
            filter = new LowerCaseFilter(ver, source);
            filter = new NGramTokenFilter(ver, filter, 1 , 20);
            return new TokenStreamComponents(source, filter);
        }
    }

    public sealed class SeperatorCharsTokenizer : CharTokenizer
    {
        public SeperatorCharsTokenizer(LuceneVersion matchVersion, TextReader input) : base(matchVersion, input)
        {
        }
        static HashSet<char> SepChars = new HashSet<char>(
            new char[] {' ', '/', '.', '\\', '[', ']', '(', ')', '-', '_', '：' });
        protected override bool IsTokenChar(int c)
        {
            return !SepChars.Contains((char)c);
        }
    }



}
