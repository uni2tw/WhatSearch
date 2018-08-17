using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WhatSearch.Core;
using WhatSearch.Models;
using WhatSearch.Service;
using WhatSearch.Services.Interfaces;
using WhatSearch.Utility;

namespace WhatSearch.WebAPIs
{
    //[Route("api")]
    //[Route("api/monitor/[action]")]
    //[Route("api/[action]")]
    public class HomeController : Controller
    {
        ISearchSercice searchService = Ioc.Get<ISearchSercice>();

        [HttpGet]
        [Route("api/search")]
        public dynamic QuickSearch(string q)
        {
            List<FolderInfo> items = new List<FolderInfo>();
            var docs = searchService.Query(q);
            foreach (FileDoc doc in docs)
            {
                string contentType;
                new FileExtensionContentTypeProvider().TryGetContentType(doc.Name, out contentType);
                contentType = contentType ?? "application/octet-stream";
                items.Add(new FolderInfo
                {
                    Id = Guid.NewGuid().ToString(),
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
            List<FolderInfo> items = new List<FolderInfo>();
            var docs = searchService.Query(q);
            foreach (FileDoc doc in docs)
            {                
                string fileType = Helper.GetFileType(Path.GetExtension(doc.Name));
                items.Add(new FolderInfo
                {
                    Id = Guid.NewGuid().ToString(),
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
            List<FolderInfo> items = new List<FolderInfo>();
            if (string.IsNullOrEmpty(p))
            {
                foreach(var folder in Ioc.GetConfig().Folders)
                {
                    DirectoryInfo di = new DirectoryInfo(folder.Path);
                    if (di.Exists == false)
                    {
                        continue;
                    }
                    items.Add(new FolderInfo
                    {
                        Id = Guid.NewGuid().ToString(),
                        Title = folder.Title,
                        Modify = di.CreationTime.ToString(),
                        Type = "檔案資料夾",
                        Size = string.Empty
                    });                    
                }
                
            }
            return new
            {
                message = "找到 " + items.Count + " 筆.",
                items
            };
        }

        #region ViewModels

        public class FolderInfo
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public string Type { get; set; }
            public string Modify { get; set; }
            public string Size { get; set; }
        }

        #endregion
    }



}
