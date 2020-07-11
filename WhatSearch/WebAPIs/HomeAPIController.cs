using Microsoft.AspNetCore.Mvc;
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
    public class HomeAPIController : Controller
    {
        ISearchSercice searchService = Ioc.Get<ISearchSercice>();
        IFolderIdManager fimgr = Ioc.Get<IFolderIdManager>();
        IMainService mainService = Ioc.Get<IMainService>();
        SystemConfig config = Ioc.GetConfig();

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
                string efid = fimgr.GetIdByFilePath(doc.FullName);

                string relPath;
                PathUtility.TryGetRelPath(doc.FullName, out relPath);
                items.Add(new FileInfoView
                {
                    Id = efid,
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

                string efid = fimgr.GetId(pathname);
                
                return Ok(new { PathId = efid });
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
            List<FileInfoView> items = new List<FileInfoView>();            
            if (model == null || string.IsNullOrEmpty(model.p))
            {
                items.AddRange(mainService.GetRootShareFolders());
            }
            else
            {
                items.AddRange(mainService.GetFileInfoViewsInTheFolder(model.p));
            }            
            return new
            {
                //pushState會用到url
                url = PathUtility.GetRelativePath(fimgr.GetPath(model.p)),
                message = "找到 " + items.Count + " 筆.",
                items
            };
        }

        [HttpPost]
        [Route("api/breadcrumbs")]
        public dynamic Breadcrumbs([FromBody] FolderInputModel model)
        {
            List<FileInfoView> breadcrumbs = new List<FileInfoView>();
            if (model == null || string.IsNullOrEmpty( model.p))
            {
                breadcrumbs = mainService.GetBreadcrumbs(string.Empty);
            }
            else
            {
                string folderId = model.p;
                breadcrumbs = mainService.GetBreadcrumbs(folderId);
            }
            return Ok(breadcrumbs.Select(t => new { t.Id, Text = t.Title, Link = t.GetUrl, Type = t.Type })
                .ToList());
        }

        #region Obsolete

        [Obsolete]
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
        #endregion

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
