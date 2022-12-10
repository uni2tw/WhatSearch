using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Threading;
using WhatSearch.Core;
using WhatSearch.Utility;

namespace WhatSearch.Controllers
{
    public class MergeImagesController : Controller
    {
        
        static SystemConfig config = Ioc.GetConfig();
        static ILogger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>        
        [Route("mergeImages")]
        public IActionResult List([FromRoute]Guid? secret)
        {
           

            return View();
        }
    }
}
