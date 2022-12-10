using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using WhatSearch.Core;
using WhatSearch.Models.MMPlayModels;
using WhatSearch.Utilities;
using WhatSearch.Utility;

namespace WhatSearch.Controllers
{
    public class MMPlayerController : Controller
    {

        static SystemConfig config = Ioc.GetConfig();
        static ILogger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>        
        [Route("mmplay/list")]
        public IActionResult List([FromRoute] string id)
        {
            var thePage = config.MMPlay.Pages.FirstOrDefault(t => t.ShowOnTop);
            return Redirect(string.Format("/mmplay/{0}/list", thePage.Id));
        }

        [Route("mmplay/{id}/getfile/{*pathInfo}")]
        public dynamic GetFileV2([FromRoute] string id, string pathInfo)
        {
            var fileInfo = GetFolderFromMMPlayId(id, pathInfo);
            if (fileInfo == null || fileInfo.Exists == false)
            {
                return NotFound();
            }
            var mimeType = MimeTypeMap.GetMimeType(fileInfo.Extension);
            return this.PhysicalFile(fileInfo.FullName, mimeType, true);
        }

        private FileInfo GetFolderFromMMPlayId(string id, string pathInfo)
        {
            var page = config.MMPlay.Pages.FirstOrDefault(t => t.Id == id);
            if (page != null)
            {
                var fileInfo = new FileInfo(Path.Combine(page.Folder, pathInfo));
                return fileInfo;
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Route("mmplay/{id}/list")]
        public IActionResult ListV2([FromRoute] string id)
        {
            var page = config.MMPlay.Pages.FirstOrDefault(t => t.Id == id);
            if (page == null)
            {
                return NotFound();
            }
            DirectoryInfo mmFolder = new DirectoryInfo(page.Folder);
            List<MyItem> myItems = new List<MyItem>();

            foreach (var dirInfo in mmFolder.GetDirectories().OrderByDescending(t => t.CreationTimeUtc))
            {
                var coverFile = dirInfo.GetFiles()
                    .FirstOrDefault(t => t.Extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase));

                var mediaFile = dirInfo.GetFiles()
                    .FirstOrDefault(t => t.Extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase));

                var infoFile = dirInfo.GetFiles()
                    .FirstOrDefault(t => t.Extension.Equals(".json", StringComparison.OrdinalIgnoreCase));

                if (coverFile == null || mediaFile == null)
                {
                    continue;
                }
                MyItem myItem = new MyItem
                {
                    id = MyItem.GetIdFromUrl(dirInfo.Name + "/" + mediaFile.Name),
                    cover = Helper.UrlEncodeLite("getfile/" + dirInfo.Name + "/" + coverFile.Name),
                    url = Helper.UrlEncodeLite("getfile/" + dirInfo.Name + "/" + mediaFile.Name),
                    title = System.Web.HttpUtility.HtmlEncode(dirInfo.Name)
                };
                MediaMetadata avProp = null;
                if (infoFile != null && infoFile.Exists)
                {
                    try
                    {
                        avProp = JsonConvert.DeserializeObject<MediaMetadata>(System.IO.File.ReadAllText(infoFile.FullName));
                    }
                    catch
                    {
                    }
                }
                if (avProp != null)
                {
                    if (avProp.uncensored == 1)
                    {
                        myItem.uncensored = 1;
                    }
                    myItem.like = avProp.like;
                }
                if (config.MMPlay.Develop)
                {
                    myItem.cover = "/assets/images/fake_cover.jpg";
                    myItem.title = new System.Text.RegularExpressions.Regex(@"\S").Replace(myItem.title, "＊");
                }
                myItems.Add(myItem);
            }
            var thePage = config.MMPlay.Pages.FirstOrDefault(t => t.Id == id);
            if (thePage == null)
            {
                return NotFound();
            }
            var pages = config.MMPlay.Pages.Where(t => t.ShowOnTop)
                .Select(t => new
                {
                    title = t.Title,
                    url = string.Format("/mmplay/{0}/list", t.Id),
                    active = t.Id == thePage.Id
                }).ToList();
            ViewBag.PageTitle = thePage.Title;
            ViewBag.PageId = thePage.Id;
            ViewBag.MyItems = myItems;
            ViewBag.ItemsAsJson = System.Web.HttpUtility.JavaScriptStringEncode(JsonConvert.SerializeObject(myItems));
            ViewBag.PagesAsJson = System.Web.HttpUtility.JavaScriptStringEncode(JsonConvert.SerializeObject(pages));
            return View("List");
        }

        /// <summary>
        /// page(config.pages)
        /// item(directoryinfo)        
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("mmplay/setLike")]
        public dynamic SetLike([FromBody] LikeModel model)
        {
            string itemName = ItemFacade.GetItemNameFromEncodedId(model.id);
            if (model == null || ItemFacade.CheckPageOrItemExist(model.pageId, itemName) == false)
            {
                return new
                {
                    success = false
                };
            }

            var metadata = ItemFacade.GetItemMetadata(model.pageId, itemName);
            if (metadata == null)
            {
                ItemFacade.CreateDefaultItemMetadata(model.pageId, itemName, ref metadata);
            }
            metadata.like = model.like;
            ItemFacade.SaveItemMetadata(model.pageId, itemName, metadata);

            return new
            {
                success = true,
                itemId = model.id,
                like = metadata.like
            };
        }

        /// <summary>
        /// page(config.pages)
        /// item(directoryinfo)        
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("mmplay/setUncensored")]
        public dynamic setUncensored([FromBody] UncensoredModel model)
        {
            string itemName = ItemFacade.GetItemNameFromEncodedId(model.id);
            if (model == null || ItemFacade.CheckPageOrItemExist(model.pageId, itemName) == false)
            {
                return new
                {
                    success = false
                };
            }

            var metadata = ItemFacade.GetItemMetadata(model.pageId, itemName);
            if (metadata == null)
            {
                ItemFacade.CreateDefaultItemMetadata(model.pageId, itemName, ref metadata);
            }
            metadata.uncensored = Helper.ToInt32(model.uncensored);
            ItemFacade.SaveItemMetadata(model.pageId, itemName, metadata);

            return new
            {
                success = true,
                itemId = model.id,
                uncensored = metadata.uncensored
            };
        }
        public class LikeModel
        {
            public string pageId { get; set; }
            public string id { get; set; }
            public int like { get; set; }
        }
        public class UncensoredModel
        {
            public string pageId { get; set; }
            public string id { get; set; }
            public bool uncensored { get; set; }
        }
    }

    public class MyItem
    {
        public MyItem()
        {
            tags = new List<string>();
        }
        public string cover { get; set; }
        public string url { get; set; }
        public string title { get; set; }
        public List<string> tags { get; set; }
        public int like { get; set; }
        public int uncensored { get; set; }
        public string id { get; set; }

        public static string GetIdFromUrl(string rawUrl)
        {

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(Helper.UrlEncodeLite(rawUrl)));
        }

        public static bool TryGetUrlFromEncodedId(string encodedId, out string dirName, out string fileName)
        {
            dirName = string.Empty;
            fileName = string.Empty;
            try
            {
                string[] nameParts = HttpUtility.UrlDecode(
                    Encoding.UTF8.GetString(Convert.FromBase64String(encodedId))).Split("/");                
                dirName = nameParts[0];
                fileName = nameParts[1];
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public class ItemFacade
    {
        public static MediaMetadata GetItemMetadata(string pageId, string itemName)
        {
            string itemPath = GetItemFullPath(pageId, itemName);
            FileInfo metadataFileInfo = new FileInfo(Path.Combine(itemPath, itemName + ".json"));
            if (metadataFileInfo.Exists == false)
            {
                return null;
            }
            MediaMetadata metadata = null;
            try
            {
                metadata = JsonConvert.DeserializeObject<MediaMetadata>(System.IO.File.ReadAllText(metadataFileInfo.FullName));
            } 
            catch
            {
            }
            return metadata;
        }

        public static List<MediaMetadata> GetAllItemInfos(string topFolder)
        {
            List<MediaMetadata> result = new List<MediaMetadata>();
            DirectoryInfo mmFolder = new DirectoryInfo(topFolder);
            List<MyItem> myItems = new List<MyItem>();

            foreach (var dirInfo in mmFolder.GetDirectories().OrderByDescending(t => t.CreationTimeUtc))
            {
                var mediaFile = dirInfo.GetFiles()
                    .FirstOrDefault(t => t.Extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase));
                var infoFile = dirInfo.GetFiles()
                    .FirstOrDefault(t => t.Extension.Equals(".json", StringComparison.OrdinalIgnoreCase));
                if (mediaFile == null || mediaFile.Exists == false || infoFile == null || infoFile.Exists == false)
                {
                    continue;
                }

                MediaMetadata itemInfo = null;
                try
                {
                    itemInfo = JsonConvert.DeserializeObject<MediaMetadata>(System.IO.File.ReadAllText(infoFile.FullName));
                    if (itemInfo != null)
                    {
                        itemInfo.id = MyItem.GetIdFromUrl(dirInfo.Name + "/" + mediaFile.Name);
                        result.Add(itemInfo);
                    }
                }
                catch
                {
                }
            }
            return result;
        }

        public static void SaveItemMetadata(string pageId, string itemName, MediaMetadata metadata)
        {
            string itemPath = GetItemFullPath(pageId, itemName);
            if (string.IsNullOrEmpty(itemPath))
            {
                throw new Exception(string.Format("{0} not found.", itemPath));
            }
            string fileName = Path.Combine(itemPath, itemName + ".json");
            var metadataFileInfo = new FileInfo(fileName);
            string jsonText = JsonConvert.SerializeObject(metadata, Formatting.Indented);
            File.WriteAllText(metadataFileInfo.FullName, jsonText);
        }

        public static void CreateDefaultItemMetadata(string pageId, string itemName, ref MediaMetadata metadata)
        {
            string itemPath = GetItemFullPath(pageId, itemName);
            if (string.IsNullOrEmpty(itemPath))
            {
                throw new Exception(string.Format("{0} not found.", itemPath));
            }
            string fileName = Path.Combine(itemPath, itemName + ".json");
            var metadataFileInfo = new FileInfo(fileName);
            if (metadataFileInfo.Exists)
            {
                throw new Exception(string.Format("{0} was exists. it's fail to set default.", itemPath));
            }
            metadata = new MediaMetadata
            {
                id = itemName,
                title = string.Empty,
                like = 0,
                category = string.Empty,
                cover = string.Empty,
                hidden = 0,
                uncensored = 0
            };
            string jsonText = JsonConvert.SerializeObject(metadata, Formatting.Indented);
            File.WriteAllText(metadataFileInfo.FullName, jsonText);
        }

        private static string GetItemFullPath(string pageId, string itemName)
        {
            SystemConfig config = Ioc.GetConfig();
            var page = config.MMPlay.Pages.FirstOrDefault(t => t.Id == pageId);
            if (page == null)
            {
                return string.Empty;
            }
            return Path.Combine(page.Folder, itemName);
        }

        public static bool CheckPageOrItemExist(string pageId, string itemName)
        {
            string itemPath = GetItemFullPath(pageId, itemName);
            if (string.IsNullOrEmpty(itemPath))
            {
                return false;
            }
            var di = new DirectoryInfo(itemPath);
            return di.Exists;
        }

        public static string GetItemNameFromEncodedId(string encodedId)
        {
            string dirName;
            string fileName;
            bool result = MyItem.TryGetUrlFromEncodedId(encodedId, out dirName, out fileName);
            return dirName;
        }
    }
}
