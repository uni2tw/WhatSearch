using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using WhatSearch.Core;
using WhatSearch.Models;
using WhatSearch.Services;
using WhatSearch.Services.Interfaces;

namespace WhatSearch.Controllers
{
    //[Route("page/[action]")]


    //[Route("[controller]")]
    public class PageController : Controller
    {
        IFileSystemInfoIdAssigner idAssigner = Ioc.Get<IFileSystemInfoIdAssigner>();
        [Route("")]
        public IActionResult Index()
        {
            return Redirect("/page");
        }
        SystemConfig config = Ioc.GetConfig();
        [Route("page/{*pathInfo}")]
        public IActionResult List(string pathInfo)
        {

            IMainService mainService = Ioc.Get<IMainService>();
            List<FileInfoView> folders = mainService.GetRootShareFolders();
            string absPath;
            if (PathUtility.TryGetAbsolutePath(pathInfo, out absPath))
            {
                System.Guid? folderId = idAssigner.GetFolderId(absPath);
                if (folderId != null)
                {
                    ViewBag.StartFolderId = folderId.Value.ToString();
                }
            }
            return View();
        }
    }
}
