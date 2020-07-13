using Microsoft.AspNetCore.Mvc;
using WhatSearch.Core;
using WhatSearch.Models;
using WhatSearch.Services.Interfaces;
using WhatSearch.WebAPIs.Filters;

namespace WhatSearch.Controllers
{
    //[Route("[controller]")]
    public class PageController : Controller
    {        
        //[Route("")]
        //public IActionResult Index()
        //{
        //    return Redirect("/page");
        //}
        SystemConfig config = Ioc.GetConfig();

        [Route("")]
        public IActionResult Index()
        {
            return Redirect("/page/");
        }
        [Route("page/{*pathInfo}")]
        public IActionResult List(string pathInfo)
        {
            return View();
        }
        [Route("debug")]
        public IActionResult Debug()
        {
            return View();
        }

        [Route("admin")]
        [RoleAuthorize("Admin")]
        public IActionResult Members()
        {
            IUserService srv = Ioc.Get<IUserService>();
            dynamic model = srv.GetMembers();
            return View(model);
        }

        [HttpPost]
        [Route("admin/updateMember")]
        [RoleAuthorize("Admin")]
        public dynamic UpdateMember([FromBody]dynamic model)
        {
            IUserService srv = Ioc.Get<IUserService>();
            Member mem = srv.GetMember((string)model.name);
            if (mem != null) {
                MemberStatus newStatus = (MemberStatus)((int)model.status);
                if (mem.Status != newStatus)
                {
                    srv.UpdateMemberStatus(mem.Name, newStatus);
                }
                string message = string.Format("{0} 狀態為 {1}",
                        mem.DisplayName,
                        newStatus == MemberStatus.Active ? "啟動" : "停用");
                return new
                {
                    Success = 1,
                    Message = message
                };
            }
            return new
            {
                Success = 0
            };
        }
    }
}
