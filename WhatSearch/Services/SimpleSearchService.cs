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
using log4net;
using WhatSearch.Services.Interfaces;

namespace WhatSearch.Service
{

    public abstract class SearchServiceBase : ISearchSercice
    {

        private IChineseConverter cc = Ioc.Get<IChineseConverter>();

        protected Lucene.Net.Store.Directory _dir;

        public abstract int DocCount { get; }
        public abstract IndexSearcher GetIndexSearcher();
        public abstract LuceneVersion GetLuceneVersion();
        public abstract Analyzer GetAnalyzer();

        public abstract Lucene.Net.Store.Directory GetDirectory();

        private static ILog logger = LogManager.GetLogger(typeof(SearchServiceBase));

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
                    string tcFullName = cc.ToTraditionalChinese(fi.FullName);
                    var oDocument = new Document
                    {
                        new StringField(IndexedFileDoc.Columns.Id, fi.FullName, Field.Store.YES),
                        new TextField(IndexedFileDoc.Columns.FullName, tcFullName, Field.Store.YES),
                        new StringField(IndexedFileDoc.Columns.DirectoryName, fi.DirectoryName, Field.Store.YES),
                        new StringField(IndexedFileDoc.Columns.Name, fi.Name, Field.Store.YES),
                        new Int64Field(IndexedFileDoc.Columns.Length, fi.Length, Field.Store.YES),
                        new StringField(IndexedFileDoc.Columns.CreationTime, fi.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"), Field.Store.YES),
                        new StringField(IndexedFileDoc.Columns.LastWriteTime, fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"), Field.Store.YES),
                        new StringField(IndexedFileDoc.Columns.Extension, fi.Extension, Field.Store.YES)
                    };
                    
                    iwriter.UpdateDocument(new Term(IndexedFileDoc.Columns.Id, fi.FullName), oDocument);                    
                    
                });

                foreach (FileInfo searchDoc in searchDocs)
                {
                    try
                    {
                        logger.Debug(searchDoc.FullName);
                        actAdd(searchDoc);
                        count++;
                    } catch (Exception ex)
                    {
                        Console.WriteLine("ignore " + searchDoc.FullName + ".");
                        throw;
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
            
            iwriter.DeleteDocuments(new Term(IndexedFileDoc.Columns.Id, docId));
            iwriter.Commit();
            //iwriter.Dispose();            
        }

        public List<IndexedFileDoc> Query(string queryString, int maxDoc = 100)
        {
            //轉成繁體再搜尋
            queryString = cc.ToTraditionalChinese(queryString.Trim());
            List<IndexedFileDoc> items = new List<IndexedFileDoc>();

            if (string.IsNullOrEmpty(queryString) || maxDoc == 0)
            {
                return items;
            }
            IndexSearcher searcher = null;
            try
            {
                searcher = GetIndexSearcher();
                Query qq = SearchHelper.GetLuceneQuery(IndexedFileDoc.Columns.FullName, queryString);
                TopDocs cc = searcher.Search(qq, maxDoc);
                
                foreach (var item in cc.ScoreDocs)
                {
                    Document doc = searcher.Doc(item.Doc);
                    items.Add(new IndexedFileDoc
                    {
                        FullName = doc.Get(IndexedFileDoc.Columns.Id),
                        Length = long.Parse(doc.Get(IndexedFileDoc.Columns.Length)),
                        LastWriteTime = DateTime.Parse(doc.Get(IndexedFileDoc.Columns.LastWriteTime)),
                        Name = doc.Get(IndexedFileDoc.Columns.Name),
                        DirectoryName = doc.Get(IndexedFileDoc.Columns.DirectoryName),
                        CreationTime = DateTime.Parse(doc.Get(IndexedFileDoc.Columns.CreationTime))
                    });
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

    public sealed class SimpleSearchService : SearchServiceBase
    {
        public SimpleSearchService()
        {
            _dir = new RAMDirectory();
        }


        public override int DocCount
        {
            get
            {
                IndexReader reader = DirectoryReader.Open(_dir);                
                return reader.NumDocs;
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
