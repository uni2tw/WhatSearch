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
        IFavoriteService favoriteService = Ioc.Get<IFavoriteService>();
        ILoudnessService loudnessService = Ioc.Get<ILoudnessService>();
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
            List<DiscGroup> discs = null;
            if (model == null || string.IsNullOrEmpty(model.p))
            {
                items.AddRange(mainService.GetRootShareFolders());
                breadcrumbs.AddRange(mainService.GetBreadcrumbs(string.Empty));
            }
            else
            {
                FolderNotFoundResult missing = mainService.ResolveMissingFolder(model.p);
                if (missing != null)
                {
                    // 資料夾不存在（例如改過名）：回傳 200 + notFound，由前端顯示「找不到」畫面與建議
                    return new
                    {
                        notFound = true,
                        id = model.p,
                        message = "找不到資料夾",
                        requested = missing.RequestedPath,
                        nearest = new { id = missing.NearestId, title = missing.NearestTitle },
                        suggestions = missing.Suggestions.Select(x => new { id = x.Id, title = x.Title, score = Math.Round(x.Score, 2) }),
                        items,
                        breadcrumbs = mainService.GetBreadcrumbs(missing.NearestId)
                            .Select(t => new { t.Id, Text = t.Title, Link = t.GetUrl, Type = t.Type })
                    };
                }
                items.AddRange(mainService.GetFileInfoViewsInTheFolder(model.p));
                breadcrumbs.AddRange(mainService.GetBreadcrumbs(model.p));
                discs = mainService.GetMultiDiscGroups(model.p);
            }
            return new
            {
                id = model.p,
                //pushState會用到url
                url = PathUtility.GetRelativePath(fimgr.GetPath(model.p)),
                message = "找到 " + items.Count + " 筆.",
                items,
                discs = discs == null ? null : discs.Select(d => new { id = d.Id, no = d.No, title = d.Title, items = d.Items }),
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

        /// <summary>目前會員的最愛 id（前端用來標示愛心；不解析檔案，很便宜）</summary>
        [HttpGet]
        [Route("api/favorites")]
        public dynamic GetFavorites()
        {
            if (User.Identity.IsAuthenticated == false)
            {
                return Unauthorized();
            }
            FavoriteData data = favoriteService.Get(User.Identity.Name);
            return new { albums = data.Albums, tracks = data.Tracks };
        }

        /// <summary>加入/移除最愛。kind: album（資料夾 id）或 track（音樂檔 id）</summary>
        [HttpPost]
        [Route("api/favorites")]
        public dynamic SetFavorite([FromBody] FavoriteInputModel model)
        {
            if (User.Identity.IsAuthenticated == false)
            {
                return Unauthorized();
            }
            if (model == null || string.IsNullOrEmpty(model.id) ||
                (model.kind != FavoriteService.KindAlbum && model.kind != FavoriteService.KindTrack))
            {
                return BadRequest("參數錯誤");
            }
            if (model.fav)
            {
                // 只接受存在的資料夾/音樂檔，避免塞進任意字串
                bool ok = model.kind == FavoriteService.KindAlbum
                    ? mainService.GetFolderView(model.id) != null
                    : mainService.GetMusicFileView(model.id) != null;
                if (ok == false)
                {
                    return NotFound("找不到這個項目");
                }
            }
            favoriteService.Set(User.Identity.Name, model.kind, model.id, model.fav);
            if (model.fav && model.kind == FavoriteService.KindTrack)
            {
                // 收藏單曲時就先在背景分析響度，進「我的最愛」時通常已經算好
                loudnessService.Enqueue(fimgr.GetPath(model.id));
            }
            return new { ok = true };
        }

        /// <summary>
        /// 「我的最愛」清單：先列專輯（最近加入的在前），再列歌曲（依加入順序，等於一份虛擬精選歌單）。
        /// 已被改名/刪除而找不到的項目不列出（資料保留，檔案回來就會再出現），只在訊息裡提示筆數。
        /// </summary>
        [HttpGet]
        [Route("api/favorites/items")]
        public dynamic GetFavoriteItems()
        {
            if (User.Identity.IsAuthenticated == false)
            {
                return Unauthorized();
            }
            FavoriteData data = favoriteService.Get(User.Identity.Name);
            List<FileInfoView> items = new List<FileInfoView>();
            int missing = 0;
            foreach (string id in Enumerable.Reverse(data.Albums))
            {
                FileInfoView view = mainService.GetFolderView(id);
                if (view == null) { missing++; } else { items.Add(view); }
            }
            int pending = 0;
            foreach (string id in data.Tracks)
            {
                FileInfoView view = mainService.GetMusicFileView(id);
                if (view == null)
                {
                    missing++;
                    continue;
                }
                if (loudnessService.Apply(view, fimgr.GetPath(id)))
                {
                    pending++;
                }
                items.Add(view);
            }
            string message = "我的最愛 " + items.Count + " 筆.";
            if (missing > 0)
            {
                message += "（另有 " + missing + " 筆找不到，可能已改名或刪除）";
            }
            // pending：還在背景分析響度的單曲數；前端據此定時更新。target：目標響度（LUFS）
            return new { message, items, pending, target = loudnessService.TargetLufs };
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

        public class FavoriteInputModel
        {
            public string kind { get; set; }
            public string id { get; set; }
            public bool fav { get; set; }
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
