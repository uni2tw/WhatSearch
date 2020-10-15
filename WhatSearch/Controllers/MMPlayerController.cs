using log4net;
using log4net.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Threading;
using WhatSearch.Core;
using WhatSearch.Models.MMPlayModels;
using WhatSearch.Utilities;
using WhatSearch.Utility;

namespace WhatSearch.Controllers
{
    public class MMPlayerController : Controller
    {
        
        static SystemConfig config = Ioc.GetConfig();
        static ILog logger = LogManager.GetLogger(typeof(MMPlayerController));

        private string MMFolder
        {
            get
            {
                return @"D:\動作\0000";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>        
        [Route("mmplay/list")]
        public IActionResult List([FromRoute]string id)
        {
            return Redirect("/mmplay/aa/list");
        }

        [Route("mmplay/getfile/{*pathInfo}")]
        public dynamic GetFile(string pathInfo)
        {
            var fileInfo = new FileInfo(Path.Combine(MMFolder, pathInfo));
            if (fileInfo.Exists == false)
            {
                return NotFound();
            }
            var mimeType = MimeTypeMap.GetMimeType(fileInfo.Extension);
            return this.PhysicalFile(fileInfo.FullName, mimeType, true);
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
            DirectoryInfo mmFolder = new DirectoryInfo(MMFolder);
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
                    cover = Helper.UrlEncodeLite("getfile/" + dirInfo.Name + "/" + coverFile.Name),
                    media = Helper.UrlEncodeLite("getfile/" + dirInfo.Name + "/" + mediaFile.Name),
                    title = System.Web.HttpUtility.HtmlEncode(dirInfo.Name)
                };
                AVProperty avProp = null;
                if (infoFile != null && infoFile.Exists)
                {
                    try
                    {
                        avProp = JsonConvert.DeserializeObject<AVProperty>(System.IO.File.ReadAllText(infoFile.FullName));
                    }
                    catch
                    {
                    }
                }
                if (avProp != null)
                {
                    //if (avProp.uncensored == 1)
                    {
                        myItem.tags.Add("無碼");
                        myItem.tags.Add("推薦");
                        myItem.tags.Add("差評");
                    }
                }
                if (config.MMPlay.Develop)
                {
                    myItem.cover = "/assets/images/fake_cover.jpg";
                    myItem.title = new System.Text.RegularExpressions.Regex(@"\S").Replace(myItem.title, "＊");
                }
                myItems.Add(myItem);
            }
            ViewBag.PageTitle = config.MMPlay.Pages[0].Title;
            ViewBag.MyItems = myItems;
            ViewBag.ItemsAsJson = System.Web.HttpUtility.JavaScriptStringEncode(JsonConvert.SerializeObject(myItems));            
            return View("List");
        }



        public class MyItem
        {
            public MyItem()
            {
                tags = new List<string>();
            }
            public string cover { get; set; }
            public string media { get; set; }
            public string title { get; set; }
            public List<string> tags { get; set; }
        }

    }
}
