using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WhatSearch.Core;
using WhatSearch.Models;
using WhatSearch.Service;
using WhatSearch.Services.Interfaces;

namespace WhatSearch.WebAPIs
{
    //[Route("api")]
    //[Route("api/monitor/[action]")]
    [Route("api/[action]")]
    public class HomeController : Controller
    {
        ISearchSercice searchService = Ioc.Get<ISearchSercice>();
        public dynamic Echo(string message)
        {
            return new
            {
                Message = message
            };
        }
        [HttpGet]
        public dynamic Search(string q)
        {
            List<string> items = new List<string>();
            items.AddRange(searchService.Query(q).Select(t => t.FullName));

            return new
            {
                Message = "找到 " + items.Count + " 筆.",
                Result = items
            };
        }
        [HttpPost]
        public dynamic Psearch([FromBody]dynamic model)
        {
            string q = model.q;
            List<FileDoc> items = new List<FileDoc>();
            items.AddRange(searchService.Query(q));

            return new
            {
                message = "找到 " + items.Count + " 筆.",
                items
            };
        }

    }


    #region ViewModels

    public class HomeListViewModel
    {

    }

    #endregion
}
