using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
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
    [ApiController]
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
            var docs = searchService.Query(q, config.MaxSearchResult);
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
        public dynamic Search([FromBody]SearchModel model)
        {
            string q = model.q;
            q = q.ToLower();
            List<FileInfoView> items = new List<FileInfoView>();
            List<IndexedFileDoc> docs = searchService.Query(q, config.MaxSearchResult);
            HashSet<string> importedDirs = new HashSet<string>();
            foreach (IndexedFileDoc doc in docs)
            {
                if (importedDirs.Contains(doc.DirectoryName) == false)
                {
                    importedDirs.Add(doc.DirectoryName);
                    FileInfoView fiv = mainService.GetFileInfoView(new DirectoryInfo(doc.DirectoryName));
                    items.Add(fiv);
                }
            }
            foreach (IndexedFileDoc doc in docs)
            {
                string fileType = Helper.GetFileDocType(Path.GetExtension(doc.Name));
                Guid guid = idAssigner.GetOrAdd(doc.FullName);
                string relPath;
                PathUtility.TryGetRelPath(doc.FullName, out relPath);
                items.Add(new FileInfoView
                {
                    Id = guid.ToString(),
                    Size = Helper.GetReadableByteSize(doc.Length, 2),
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
        [Route("api/pathId")]
        public dynamic PathId([FromBody] PathIdInputModel model)
        {
            try
            {
                string pathname = Encoding.UTF8.GetString(HttpUtility.UrlDecodeToBytes(model.pathname));
                pathname = pathname.TrimStart("/page/", StringComparison.OrdinalIgnoreCase);
                string result = string.Empty;
                IMainService mainService = Ioc.Get<IMainService>();
                string absPath;
                if (PathUtility.TryGetAbsolutePath(pathname, out absPath))
                {
                    Guid? folderId = idAssigner.GetFolderId(absPath);
                    if (folderId != null)
                    {
                        result = folderId.Value.ToString();
                    }
                }
                return Ok(new { PathId = result });
            }
            catch (Exception ex)
            {
                return BadRequest("發生錯誤，請稍候再試");
            }
        }

        [HttpPost]
        [Route("api/folder")]
        public dynamic Folder([FromBody]FolderInputModel model)
        {
            string p = (model == null || model.p == null) ? string.Empty : model.p.ToLower();
            List<FileInfoView> items = new List<FileInfoView>();
            List<FileInfoView> breadcrumbs = new List<FileInfoView>();
            if (string.IsNullOrEmpty(p) || p == Constants.RootId)
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
            string relativeUrl = PathUtility.GetRelativePath(breadcrumbs);
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

        [HttpPost]
        [Route("api/breadcrumbs")]
        public dynamic Breadcrumbs([FromBody] FolderInputModel model)
        {
            string p = (model == null || model.p == null) ? string.Empty : model.p.ToLower();
            List<FileInfoView> items = new List<FileInfoView>();
            List<FileInfoView> breadcrumbs = new List<FileInfoView>();
            if (string.IsNullOrEmpty(p) || p == Constants.RootId)
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
            string relativeUrl = PathUtility.GetRelativePath(breadcrumbs);
            string rssUrl = relativeUrl == "/" ? string.Empty : "/rss" + relativeUrl;
            return Ok(breadcrumbs.Select(t => new { t.Id, Text = t.Title, Link = t.GetUrl, Type = t.Type })
                .ToList());
        }

        [HttpGet]
        [Route("rss/{*pathInfo}")]
        public ContentResult Rss(string pathInfo)
        {
            string targetPath;
            if (PathUtility.TryGetAbsolutePath("/" + pathInfo, out targetPath) == false)
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
        //[AllowIpsAuthorizationFilter(includeLocalIp: true)]
        [UserAuthorize]
        public dynamic GetFile(string pathInfo)
        {
            string targetPath;
            if (PathUtility.TryGetAbsolutePath("/" + pathInfo, out targetPath) == false)
            {
                return NotFound();
            }
            string fileExt = Path.GetExtension(targetPath);
            if (config.PlayTypes.Contains(fileExt) == false)
            {
                return this.NotFound(string.Format("{0}　格式不支援{1}目前支援格式為: {2}",
                    fileExt,
                    Environment.NewLine,
                    string.Join(",", config.PlayTypes)));
                //return this.Forbid();
            }
            return this.PhysicalFile(targetPath, MimeTypeMap.GetMimeType(fileExt), true);
        }

        [Route("api/info")]
        public dynamic Info()
        {
            return new
            {
                Name = User.Identity.Name,
                Ip = WebHelper.GetClientIp(),
                Host = Environment.MachineName
            };
        }


        #region input models

        public class PathIdInputModel
        {
            public string pathname { get; set; }
        }

        public class FolderInputModel
        {
            public string p { get; set; }
        }

        public class SearchModel
        {
            public string q { get; set; }
        }

        #endregion
    }
}
