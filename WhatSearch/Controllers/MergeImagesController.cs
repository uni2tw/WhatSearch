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
using WhatSearch.Utility;

namespace WhatSearch.Controllers
{
    public class MergeImagesController : Controller
    {
        
        static SystemConfig config = Ioc.GetConfig();
        static ILog logger = LogManager.GetLogger(typeof(MergeImagesController));

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
