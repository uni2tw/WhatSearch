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
        [Route("/page/login")]
        public IActionResult Login()
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
        public dynamic UpdateMember([FromBody] UpdateMemberModel model)
        {
            IUserService srv = Ioc.Get<IUserService>();
            Member mem = model == null || string.IsNullOrEmpty(model.name) ? null : srv.GetMember(model.name);
            if (mem == null)
            {
                return new { Success = 0, Message = "找不到這個會員" };
            }
            // 管理者帳號不能被停用，避免沒有人能再進管理頁
            if (mem.IsAdmin && model.active == false)
            {
                return new { Success = 0, Message = string.Format("{0} 是管理者，不能停用", mem.DisplayName) };
            }
            // 只在「啟用」與否有變時才更新（狀態 2 TryPasswrodTooMany 沒有任何程式會設定，只可能是手動編輯 users.json 的舊資料，登入行為與未啟用相同，沒勾選啟用就維持原狀）
            bool wasActive = mem.Status == MemberStatus.Active;
            if (wasActive != model.active && model.active)
            {
                srv.UpdateMemberStatus(mem.Name, MemberStatus.Active);
            }
            else if (wasActive && model.active == false)
            {
                srv.UpdateMemberStatus(mem.Name, MemberStatus.Invalice);
            }
            return new
            {
                Success = 1,
                Message = string.Format("{0} {1}", mem.DisplayName, model.active ? "已啟用" : (wasActive ? "已停用" : "未啟用"))
            };
        }

        public class UpdateMemberModel
        {
            public string name { get; set; }
            /// <summary>是否啟用（Active）</summary>
            public bool active { get; set; }
        }
    }
}
