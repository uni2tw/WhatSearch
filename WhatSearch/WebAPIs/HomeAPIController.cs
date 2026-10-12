using Markdig;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using WhatSearch.Core;
using WhatSearch.Middlewares;
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
        static ILogger logger = LogManager.GetCurrentClassLogger();

        private static readonly MarkdownPipeline TextPreviewMarkdownPipeline =
            new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

        [HttpPost]
        [Route("api/search")]
        public dynamic Search([FromBody] SearchModel model)
        {
            string q = model.q?.ToLower();
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
                    Size = Helper.GetReadableByteSize(doc.Length),
                    GetUrl = PathUtility.ToGetUrl(relPath),
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

        [HttpGet]
        [Route("api/recent")]
        public dynamic Recent()
        {
            List<FileInfoView> items = new List<FileInfoView>();
            List<IndexedFileDoc> docs = searchService.GetRecent(config.MaxSearchResult);
            foreach (IndexedFileDoc doc in docs)
            {
                string fileType = Helper.GetFileDocType(Path.GetExtension(doc.Name));
                string efid = fimgr.GetIdByFilePath(doc.FullName);

                string relPath;
                PathUtility.TryGetRelPath(doc.FullName, out relPath);
                string dirRelPath;
                PathUtility.TryGetRelPath(doc.DirectoryName, out dirRelPath);
                items.Add(new FileInfoView
                {
                    Id = efid,
                    Size = Helper.GetReadableByteSize(doc.Length),
                    GetUrl = PathUtility.ToGetUrl(relPath),
                    Title = doc.Name,
                    Modify = doc.CreationTime.ToString(),
                    Type = fileType,
                    Path = dirRelPath
                });
            }
            return new
            {
                message = "最近上傳 " + items.Count + " 筆.",
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
        public dynamic Folder([FromBody] FolderInputModel model)
        {
            List<FileInfoView> items = new ();
            List<FileInfoView> breadcrumbs = new();
            if (model == null || string.IsNullOrEmpty(model.p))
            {
                items.AddRange(mainService.GetRootShareFolders());
                breadcrumbs.AddRange(mainService.GetBreadcrumbs(string.Empty));
            }
            else
            {
                items.AddRange(mainService.GetFileInfoViewsInTheFolder(model.p));
                breadcrumbs.AddRange(mainService.GetBreadcrumbs(model.p));
            }
            return new
            {
                id = model.p,
                //pushState會用到url
                url = PathUtility.GetRelativePath(fimgr.GetPath(model.p)),
                message = "找到 " + items.Count + " 筆.",
                items,
                breadcrumbs = breadcrumbs.Select(t => new { t.Id, Text = t.Title, Link = t.GetUrl, Type = t.Type })
            };
        }

        [HttpGet]
        [Route("get/{*pathInfo}")]
        [UserAuthorize]
        public dynamic GetFile(string pathInfo, [FromQuery] bool preview = false, [FromQuery] bool cover = false,
            [FromQuery] bool subtitle = false, [FromQuery] bool subtitleTracks = false, [FromQuery] int track = 0)
        {
            string targetPath;
            if (PathUtility.TryGetAbsolutePath("/" + pathInfo, out targetPath) == false)
            {
                return NotFound();
            }
            string fileExt = Path.GetExtension(targetPath);
            if (subtitleTracks || (subtitle && track > 0))
            {
                // MKV 內嵌文字字幕：只處理已開放播放的 .mkv
                if (fileExt.Equals(".mkv", StringComparison.OrdinalIgnoreCase) == false ||
                    config.PlayTypes.Contains(fileExt) == false ||
                    System.IO.File.Exists(targetPath) == false)
                {
                    return NotFound();
                }
                if (subtitleTracks)
                {
                    var list = MatroskaSubtitleReader.ListTracks(targetPath)
                        .Where(t => t.IsSupported)
                        .Select(t => new
                        {
                            number = t.Number,
                            name = t.Name,
                            language = t.Language,
                            codec = t.CodecId,
                            isDefault = t.IsDefault,
                            isForced = t.IsForced
                        }).ToList();
                    return new { tracks = list };
                }
                FileInfo mkv = new FileInfo(targetPath);
                string cacheKey = string.Format("mkvsub://{0}|{1}|{2}|{3}", targetPath, mkv.Length, mkv.LastWriteTimeUtc.Ticks, track);
                string cachedVtt = Ioc.GetCache().Get<string>(cacheKey);
                if (cachedVtt == null)
                {
                    try
                    {
                        cachedVtt = MatroskaSubtitleReader.ExtractAsWebVtt(targetPath, track);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "extract mkv subtitle failed: " + targetPath);
                        return NotFound();
                    }
                    if (cachedVtt == null)
                    {
                        return NotFound();
                    }
                    Ioc.GetCache().Set(cacheKey, cachedVtt, TimeSpan.FromMinutes(30));
                }
                return this.Content(cachedVtt, "text/vtt; charset=utf-8");
            }
            if (subtitle)
            {
                // 字幕不受 playtypes 白名單限制；統一轉成 UTF-8 的 WebVTT 回傳
                if (SubtitleConverter.IsSubtitleExtension(fileExt) == false || System.IO.File.Exists(targetPath) == false)
                {
                    return NotFound();
                }
                if (new FileInfo(targetPath).Length > 2 * 1024 * 1024)
                {
                    return BadRequest("字幕檔過大");
                }
                string vtt = SubtitleConverter.ToWebVtt(System.IO.File.ReadAllBytes(targetPath), fileExt);
                return this.Content(vtt, "text/vtt; charset=utf-8");
            }
            if (config.PlayTypes.Contains(fileExt) == false)
            {
                return this.NotFound(string.Format("{0}　格式不支援{1}目前支援格式為: {2}",
                    fileExt,
                    Environment.NewLine,
                    string.Join(",", config.PlayTypes)));
                //return this.Forbid();
            }
            if (cover)
            {
                try
                {
                    using (TagLib.File tagFile = TagLib.File.Create(targetPath))
                    {
                        TagLib.IPicture pic = tagFile.Tag.Pictures.FirstOrDefault();
                        if (pic != null && pic.Data.Count > 0)
                        {
                            Response.Headers["Cache-Control"] = "private, max-age=86400";
                            return this.File(pic.Data.Data, string.IsNullOrEmpty(pic.MimeType) ? "image/jpeg" : pic.MimeType);
                        }
                    }
                }
                catch
                {
                }
                return NotFound();
            }
            if (preview && (fileExt.Equals(".md", StringComparison.OrdinalIgnoreCase) ||
                            fileExt.Equals(".txt", StringComparison.OrdinalIgnoreCase)))
            {
                string text = TextDecoder.Decode(System.IO.File.ReadAllBytes(targetPath));
                if (fileExt.Equals(".md", StringComparison.OrdinalIgnoreCase))
                {
                    return new { type = "md", html = Markdown.ToHtml(text, TextPreviewMarkdownPipeline) };
                }
                return new { type = "txt", text };
            }
            return this.PhysicalFile(targetPath, MimeTypeMap.GetMimeType(fileExt), true);
        }

        /// <summary>
        /// 前端偵測到播放卡住/失敗時回報目前狀態（readyState、緩衝、估算速度…），寫進伺服端日誌，
        /// 這樣不用拿著手機看畫面，也能在伺服器的 log 看到手機端的實際情況。
        /// </summary>
        [HttpPost]
        [Route("api/playdiag")]
        [UserAuthorize]
        public dynamic PlayDiag([FromBody] PlayDiagModel model)
        {
            if (model == null)
            {
                return BadRequest();
            }
            string ua = Request.Headers["User-Agent"];
            string ip = HttpContext.Connection.RemoteIpAddress == null ? "?" : HttpContext.Connection.RemoteIpAddress.ToString();
            PlaybackLogMiddleware.PlaybackLogger.Info("clientdiag {0} {1} [{2}] {3} :: {4}",
                ip,
                PlaybackLogMiddleware.DescribeDevice(ua),
                PlaybackLogMiddleware.Shorten(model.evt, 30),
                PlaybackLogMiddleware.Shorten(model.path, 200),
                PlaybackLogMiddleware.Shorten(model.detail, 1500));
            return new { ok = true };
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

        public class PlayDiagModel
        {
            public string path { get; set; }
            public string evt { get; set; }
            public string detail { get; set; }
        }

        #endregion
    }
}
