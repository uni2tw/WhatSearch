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
            return new
            {
                message = "找到 " + items.Count + " 筆.",
                items, 
                breadcrumbs,
                rssUrl = "/rss?t=" + HttpUtility.UrlEncode(mainService.GetRelativePath(breadcrumbs))
            };
        }
        [HttpGet]
        [Route("rss")]
        public dynamic Rss([FromQuery]string t)
        {
            /*
<?xml version="1.0" encoding="UTF-8" ?>
<rss version="2.0">
<channel>
 <title>RSS Title</title>
 <description>This is an example of an RSS feed</description>
 <link>http://www.example.com/main.html</link>
 <lastBuildDate>Mon, 06 Sep 2010 00:01:00 +0000 </lastBuildDate>
 <pubDate>Sun, 06 Sep 2009 16:20:00 +0000</pubDate>
 <ttl>1800</ttl>

 <item>
  <title>Example entry</title>
  <description>Here is some text containing an interesting description.</description>
  <link>http://www.example.com/blog/post/1</link>
  <guid isPermaLink="false">7bd204c6-1655-4c27-aeee-53f933c5395f</guid>
  <pubDate>Sun, 06 Sep 2009 16:20:00 +0000</pubDate>
 </item>

</channel>
</rss>
             */
            string targetPath;
            if (mainService.TryGetAbsolutePath(t, out targetPath) == false)
            {
                return new
                {
                    Success = 0,
                    Message = "Rss error."
                };
            }
            
            XDocument doc = new XDocument();
            XElement rss = new XElement("rss");
            rss.Add(new XAttribute("version", "2.0"));
            doc.Add(rss);

            XElement channel = new XElement("channel",
                new XElement("title"),
                new XElement("description"),
                new XElement("link"),
                new XElement("lastBuildDate"),
                new XElement("pubDate"),
                new XElement("ttl")
                );
            rss.Add(channel);
            DirectoryInfo di = null;
            if (string.IsNullOrEmpty(targetPath) == false)
            {
                di = new DirectoryInfo(targetPath);
            }
            if (di != null && di.Exists)
            {
                channel.Element("title").Value = di.Name;
                channel.Element("lastBuildDate").Value = di.CreationTime.ToString("r");
                channel.Element("pubDate").Value = di.LastWriteTime.ToString("r");
                foreach (var fi in di.GetFiles("*.*", SearchOption.AllDirectories)
                    .OrderByDescending(f=>f.LastWriteTime).Take(30))
                {
                    if (fi.Attributes.HasFlag(FileAttributes.Hidden))
                    {
                        continue;
                    }
                    XElement item = new XElement("item",
                        new XElement("title"),
                        new XElement("description"),
                        new XElement("link"),
                        new XElement("guid"),
                        new XElement("pubDate")
                        );

                    item.Element("title").Value = string.Format("【{0}】{1}", fi.Directory.Name, fi.Name);
                    item.Element("guid").Value = Convert.ToBase64String(Encoding.UTF8.GetBytes(fi.Name)).GetHashCode() + "-" + fi.Length;
                    item.Element("pubDate").Value = fi.CreationTime.ToString("r");
                    channel.Add(item);
                }
            }
            else
            {
                string targetName = Path.GetDirectoryName(targetPath);
                channel.Element("title").Value = targetName + " 不存在";
                channel.Element("lastBuildDate").Value = DateTime.Now.ToString("r");
                channel.Element("pubDate").Value = DateTime.Now.ToString("r");
            }

            return doc.ToString();
        }

    }



}
