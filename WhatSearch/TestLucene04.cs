using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using WhatSearch.Core;
using System;
using System.Text;

namespace WhatSearch
{
    public class TestLucene04
    {
        public void Run()
        {
            string text = "蜡笔小新：宇宙人来袭.CRAYON.SHINCHAN.THE.MOVIE.2017.HD720P.X264.AAC.Mandarin.CHT.mp4";
            Console.WriteLine(text);           
            Analyzer analyzer = new MicroAnalyzer(LuceneVersion.LUCENE_48);
            Print(analyzer, text);
            Console.ReadKey();
        }
        private void Print(Analyzer analyzer, string text)
        {

            TokenStream tokenStream = analyzer.GetTokenStream("content", text);

            ICharTermAttribute attribute = tokenStream.AddAttribute<ICharTermAttribute>();
            tokenStream.Reset();
            int i = 0;
            StringBuilder sb = new StringBuilder();
            while (tokenStream.IncrementToken())
            {
                i++;
                sb.AppendLine(i.ToString("00") + "." + attribute.ToString());
                //Console.WriteLine(i.ToString("00") + "." + attribute.ToString());
            }
            System.IO.File.WriteAllText("c:\\temp\\analyzer.txt", sb.ToString());


        }
    }
}
