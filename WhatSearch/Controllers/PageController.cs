using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WhatSearch.Core;
using WhatSearch.Models;
using WhatSearch.Services;
using WhatSearch.Services.Interfaces;
using WhatSearch.WebAPIs.Filters;

namespace WhatSearch.Controllers
{
    //[Route("[controller]")]
    public class PageController : Controller
    {
        SystemConfig config;
        public PageController()
        {
            config = ObjectResolver.GetConfig();
        }
        //[Route("")]
        //public IActionResult Index()
        //{
        //    return Redirect("/page");
        //}

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
        [Route("user/login")]
        [HttpPost]
        public IActionResult LoginUser([FromForm]string username, [FromForm]string password)
        {
            IUserService userService = ObjectResolver.Get<IUserService>();
            string accessToken = password;
            var mem = userService.GetMemberModelByToken(accessToken).Result;
            if (mem == null)
            {
                mem = userService.GetMemberByUsername(username, password).Result;
            }
            if (mem == null)
            {
                return Content("Token找不到登入身分");
            }
            if (mem.Status == MemberStatus.Inactive)
            {
                return Content("你沒有通過認證，請在Line上跟 unicorn 說一下。");
            }
            if (string.IsNullOrEmpty(accessToken) == false)
            {
                userService.ForceLogin(Response, accessToken, config.Login.CookieDays);
                if (string.IsNullOrEmpty(mem.Username) || string.IsNullOrEmpty(mem.Password))
                {
                    return Redirect("/user/reset_password");
                }
            }
            return RedirectToAction("Index");
        }
        [Route("user/login")]
        public IActionResult Login()
        {
            return View();
        }

        [Route("admin")]
        [RoleAuthorize("Admin")]
        public IActionResult Members()
        {
            IUserService srv = ObjectResolver.Get<IUserService>();
            dynamic model = srv.GetMembers();
            return View(model);
        }

        [HttpPost]
        [Route("admin/updateMember")]
        [RoleAuthorize("Admin")]
        public async Task<dynamic> UpdateMember([FromBody]dynamic model)
        {
            IUserService srv = ObjectResolver.Get<IUserService>();
            var mem = await srv.GetMemberByLineName((string)model.name);
            if (mem != null) {
                MemberStatus newStatus = (MemberStatus)((int)model.status);
                if (mem.Status != newStatus)
                {
                    srv.UpdateMemberStatus(mem.LineName, newStatus);
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
