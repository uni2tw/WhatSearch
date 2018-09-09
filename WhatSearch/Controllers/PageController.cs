using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using WhatSearch.Core;

namespace WhatSearch.Controllers
{
    //[Route("page/[action]")]
    [Route("page")]
    [Route("")]
    //[Route("[controller]")]
    public class PageController : Controller
    {
        SystemConfig config = Ioc.GetConfig();
        [Route("")]
        public IActionResult List()
        {
            PageListModel model = new PageListModel
            {
                FoldersJson = JsonConvert.SerializeObject(config.Folders).Replace("\\", "\\\\")
            };
            
            return View(model);
        }

        public class PageListModel
        {
            public string FoldersJson { get; set; }
        }
    }
}
