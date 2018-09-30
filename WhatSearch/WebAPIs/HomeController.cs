using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using System;
using System.Collections.Generic;
using System.IO;
using WhatSearch.Core;
using WhatSearch.Models;
using WhatSearch.Service;
using WhatSearch.Services;
using WhatSearch.Services.Interfaces;
using WhatSearch.Utilities;
using WhatSearch.Utility;
using WhatSearch.WebAPIs.Filters;

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
                string relPath;
                mainService.TryGetRelPath(doc.FullName, out relPath);
                items.Add(new FileInfoView
                {
                    Id = guid.ToString(),
                    Size = Helper.SizeSuffix(doc.Length, 2),
                    GetUrl = "/get" + relPath,
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
                url = "/page" + relativeUrl
            };
        }

        [HttpGet]
        [Route("rss/{*pathInfo}")]
        public ContentResult Rss(string pathInfo)
        {
            string targetPath;
            if (mainService.TryGetAbsolutePath("/" + pathInfo, out targetPath) == false)
            {
                return new ContentResult
                {
                    ContentType = "application/rss+xml",
                    Content = "no data"
                };
            }
            IRssService rssService = Ioc.Get<IRssService>();
            string content = rssService.GetFolderRss(targetPath);

            return new ContentResult
            {
                ContentType = "application/rss+xml",
                Content = content
            };
        }

        [HttpGet]
        [Route("get/{*pathInfo}")]
        [AllowIpsAuthorizationFilter(includeLocalIp: true)]
        public dynamic GetFile(string pathInfo)
        {

            string targetPath;
            if (mainService.TryGetAbsolutePath("/" + pathInfo, out targetPath) == false)
            {
                return NotFound();
            }
            string fileExt = Path.GetExtension(targetPath);
            if (config.PlayTypes.Contains(fileExt) == false)
            {
                return this.Forbid();
            }
            return this.PhysicalFile(targetPath, MimeTypeMap.GetMimeType(fileExt), true);
        }
    }

}
