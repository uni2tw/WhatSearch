using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using WhatSearch.Core;
using System;
using System.Collections.Generic;
using System.IO;
using Document = Lucene.Net.Documents.Document;
using System.Runtime.CompilerServices;
using WhatSearch.Models;
using WhatSearch.Utilities;

namespace WhatSearch.Service
{

    public interface ISearchManager
    {
        void Build(IEnumerable<FileInfo> deals);
        void Remove(string docId);
        List<string> Query(string queryString, int maxDoc = 100);
        int DocCount { get; }
    }

    public abstract class LuceneSearchBase : ISearchManager
    {

        protected Lucene.Net.Store.Directory _dir;

        public abstract int DocCount { get; }
        public abstract IndexSearcher GetIndexSearcher();
        public abstract LuceneVersion GetLuceneVersion();
        public abstract Analyzer GetAnalyzer();

        public abstract Lucene.Net.Store.Directory GetDirectory();

        private static ICommonLog logger = Ioc.Get<ICommonLog>();

        IndexWriter iwriter;
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void InitWriter()
        {
            if (iwriter == null)
            {
                iwriter = new IndexWriter(GetDirectory(), new IndexWriterConfig(GetLuceneVersion(), GetAnalyzer()));
            }
        }

        public void Build(IEnumerable<FileInfo> searchDocs)
        {
            InitWriter();
            DateTime now = DateTime.Now;
            int count = 0;
            try
            {
                var actAdd = new Action<FileInfo>((fi) =>
                {
                    var oDocument = new Document
                    {
                        new StringField(FilehDoc.Columns.Id, fi.FullName, Field.Store.YES),
                        new TextField(FilehDoc.Columns.FullName, fi.FullName, Field.Store.YES),
                        new StringField(FilehDoc.Columns.DirectoryName, fi.DirectoryName, Field.Store.YES),
                        new StringField(FilehDoc.Columns.Name, fi.Name, Field.Store.YES),
                        new Int64Field(FilehDoc.Columns.Length, fi.Length, Field.Store.YES),
                        new StringField(FilehDoc.Columns.CreationTime, fi.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"), Field.Store.YES),
                        new StringField(FilehDoc.Columns.LastWriteTime, fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"), Field.Store.YES),
                        new StringField(FilehDoc.Columns.Extension, fi.Extension, Field.Store.YES)
                    };
                    
                    iwriter.UpdateDocument(new Term(FilehDoc.Columns.Id, fi.FullName), oDocument);                    
                    
                });

                foreach (var searchDoc in searchDocs)
                {
                    try
                    {
                        actAdd(searchDoc);
                        count++;
                    } catch (Exception ex)
                    {
                        Console.WriteLine("ignore " + searchDoc.FullName + ".");
                    }
                }

                iwriter.Commit();
                //iwriter.Dispose();
            }
            finally
            {
                if (_dir != null)
                {
                    //_dir.Close();
                }
                //logger.Log("更新搜尋索引 " + count + ", 費時 " + (DateTime.Now - now).TotalSeconds.ToString("0.00"));
            }
        }

        public void Remove(string docId)
        {
            if (iwriter == null)
            {
                InitWriter();
            }
            
            iwriter.DeleteDocuments(new Term(FilehDoc.Columns.Id, docId));
            iwriter.Commit();
            //iwriter.Dispose();            
        }

        public List<string> Query(string queryString, int maxDoc = 100)
        {
            List<string> items = new List<string>();
            if (string.IsNullOrEmpty(queryString) || maxDoc == 0)
            {
                return items;
            }
            IndexSearcher searcher = null;
            try
            {
                searcher = GetIndexSearcher();
                Query qq = SearchHelper.GetLuceneQuery(FilehDoc.Columns.FullName, queryString);
                TopDocs cc = searcher.Search(qq, maxDoc);
                
                foreach (var item in cc.ScoreDocs)
                {
                    Document doc = searcher.Doc(item.Doc);
                    items.Add(doc.Get(FilehDoc.Columns.FullName));
                }

                return items;
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                if (searcher != null)
                {
                    //searcher.Close();
                }
            }
        }

        public abstract void Dispose();
    }

    public sealed class LuceneSearchInRam : LuceneSearchBase
    {
        public LuceneSearchInRam()
        {
            _dir = new RAMDirectory();
        }


        public override int DocCount
        {
            get
            {
                IndexReader reader = DirectoryReader.Open(_dir);
                return reader.MaxDoc;
            }
        }

        public override LuceneVersion GetLuceneVersion()
        {
            return LuceneVersion.LUCENE_48;
        }

        public override Analyzer GetAnalyzer()
        {
            return new MicroAnalyzer(GetLuceneVersion());

        }

        public override void Dispose()
        {

        }

        public override IndexSearcher GetIndexSearcher()
        {
            return new IndexSearcher(DirectoryReader.Open(_dir));
        }

        public override Lucene.Net.Store.Directory GetDirectory()
        {
            return _dir;
        }
    }
}
