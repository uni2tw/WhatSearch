using log4net;
using log4net.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Threading;
using WhatSearch.Core;
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
        [Route("mmplay/{id}/list")]
        [Route("mmplay/list")]
        public IActionResult List([FromRoute]string id)
        {
            DirectoryInfo mmFolder = new DirectoryInfo(MMFolder);
            List<MyItem> myItems = new List<MyItem>();

            foreach(var dirInfo in mmFolder.GetDirectories().OrderByDescending(t => t.CreationTimeUtc))
            {
                var coverFile = dirInfo.GetFiles()
                    .FirstOrDefault(t => t.Extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase));

                var mediaFile = dirInfo.GetFiles()
                    .FirstOrDefault(t => t.Extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase));

                if (coverFile == null || mediaFile == null)
                {
                    continue;
                }
                MyItem myItem = new MyItem
                {
                    cover = Helper.UrlEncodeLite("getfile/" + dirInfo.Name + "/" + coverFile.Name),
                    media = Helper.UrlEncodeLite("getfile/" + dirInfo.Name + "/" + mediaFile.Name)
                };
                if (config.MMPlay.Develop)
                {
                    myItem.cover = "/assets/images/fake_cover.jpg";
                }
                myItems.Add(myItem);
            }
            ViewBag.PageTitle = config.MMPlay.Pages[0].Title;
            ViewBag.MyItems = myItems;
            return View();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Route("mmplay/{id}/list")]
        public IActionResult List2([FromRoute] string id)
        {
            DirectoryInfo mmFolder = new DirectoryInfo(MMFolder);
            List<MyItem> myItems = new List<MyItem>();

            foreach (var dirInfo in mmFolder.GetDirectories().OrderByDescending(t => t.CreationTimeUtc))
            {
                var coverFile = dirInfo.GetFiles()
                    .FirstOrDefault(t => t.Extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase));

                var mediaFile = dirInfo.GetFiles()
                    .FirstOrDefault(t => t.Extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase));

                if (coverFile == null || mediaFile == null)
                {
                    continue;
                }
                MyItem myItem = new MyItem
                {
                    cover = Helper.UrlEncodeLite("getfile/" + dirInfo.Name + "/" + coverFile.Name),
                    media = Helper.UrlEncodeLite("getfile/" + dirInfo.Name + "/" + mediaFile.Name)
                };
                if (config.MMPlay.Develop)
                {
                    myItem.cover = "/assets/images/fake_cover.jpg";
                }
                myItems.Add(myItem);
            }
            ViewBag.PageTitle = config.MMPlay.Pages[0].Title;
            ViewBag.MyItems = myItems;
            return View();
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

        public class MyItem
        {
            public string cover { get; set; }
            public string media { get; set; }
        }

    }
}
