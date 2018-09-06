using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml.Linq;
using WhatSearch.Core;
using WhatSearch.Models;
using WhatSearch.Service;
using WhatSearch.Services.Interfaces;
using WhatSearch.Utility;

namespace WhatSearch.WebAPIs
{
    public class HomeController : Controller
    {
        ISearchSercice searchService = Ioc.Get<ISearchSercice>();
        IFileSystemInfoIdAssigner idAssigner = Ioc.Get<IFileSystemInfoIdAssigner>();
        IMainService mainService = Ioc.Get<IMainService>();
        SystemConfig config = Ioc.GetConfig();
        [HttpGet]
        [Route("api/search")]
        public dynamic QuickSearch(string q)
        {
            List<FileInfoView> items = new List<FileInfoView>();
            var docs = searchService.Query(q);
            foreach (IndexedFileDoc doc in docs)
            {
                string contentType;
                new FileExtensionContentTypeProvider().TryGetContentType(doc.Name, out contentType);
                contentType = contentType ?? "application/octet-stream";

                Guid guid = idAssigner.GetOrAdd(doc.FullName);

                items.Add(new FileInfoView
                {
                    Id = guid.ToString(),
                    Size = doc.Length.ToString(),
                    Title = doc.Name,
                    Modify = doc.LastWriteTime.ToString(),
                    Type = contentType
                });
            }

            return new
            {
                Message = "找到 " + items.Count + " 筆.",
                Result = items
            };
        }
        [HttpPost]
        [Route("api/search")]
        public dynamic Search([FromBody]dynamic model)
        {
            string q = model.q;
            List<FileInfoView> items = new List<FileInfoView>();
            var docs = searchService.Query(q);
            foreach (IndexedFileDoc doc in docs)
            {                
                string fileType = Helper.GetFileDocType(Path.GetExtension(doc.Name));
                Guid guid = idAssigner.GetOrAdd(doc.FullName);
                items.Add(new FileInfoView
                {
                    Id = guid.ToString(),
                    Size = Helper.SizeSuffix(doc.Length, 2),
                    Title = doc.Name,
                    Modify = doc.LastWriteTime.ToString(),
                    Type = fileType
                });
            }

            return new
            {
                message = "找到 " + items.Count + " 筆.",
                items
            };
        }

        [HttpPost]
        [Route("api/folder")]
        public dynamic Folder([FromBody]dynamic model)
        {
            string p = model.p;
            List<FileInfoView> items = new List<FileInfoView>();
            List<FileInfoView> breadcrumbs = new List<FileInfoView>();
            if (string.IsNullOrEmpty(p) || p == Constant.RootId)
            {
                items.AddRange(mainService.GetRootShareFolders());
                breadcrumbs = mainService.GetBreadcrumbs(Guid.Empty);
            }
            else
            {                
                Guid folderGuid;
                if (Guid.TryParse(p, out folderGuid))
                {
                    items.AddRange(mainService.GetFileInfoViewsInTheFolder(folderGuid));
                    breadcrumbs = mainService.GetBreadcrumbs(folderGuid);
                }
            }
            string relativeUrl = mainService.GetRelativePath(breadcrumbs);
            string rssUrl = relativeUrl == "/" ? string.Empty : "/rss" + relativeUrl;
            return new
            {
                message = "找到 " + items.Count + " 筆.",
                items,
                breadcrumbs,
                rssUrl,
                url = "/page?t=" + relativeUrl
            };
        }

        [HttpGet]
        [Route("rss/{*pathInfo}")]
        public dynamic Rss(string pathInfo)
        {
            string targetPath;
            if (mainService.TryGetAbsolutePath("/" + pathInfo, out targetPath) == false)
            {
                return new
                {
                    Success = 0,
                    Message = "Rss error."
                };
            }

            IRssService rssService = Ioc.Get<IRssService>();
            return rssService.GetFolderRss(targetPath);
        }
    }



}
