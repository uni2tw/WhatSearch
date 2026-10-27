using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
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
            string me = User.Identity.Name;
            // 只給頁面需要的欄位（不含密碼雜湊與存取權杖）
            var model = srv.GetMembers().Select(m => new MemberView
            {
                name = m.Name,
                display = m.DisplayName,
                status = (int)m.Status,
                admin = m.IsAdmin,
                create = m.CreateTime,
                access = m.LastAccessTime,
                self = string.Equals(m.Name, me, StringComparison.Ordinal)
            }).ToList();
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
            MemberRole role;
            switch ((model.role ?? string.Empty).ToLowerInvariant())
            {
                case "admin": role = MemberRole.Admin; break;
                case "member": role = MemberRole.Member; break;
                case "pending": role = MemberRole.Pending; break;
                default: return new { Success = 0, Message = "不支援的身分" };
            }
            // 不能改自己的身分，避免把自己降級或停用後沒有人能再進管理頁
            if (string.Equals(mem.Name, User.Identity.Name, StringComparison.Ordinal))
            {
                return new { Success = 0, Message = "不能修改自己的身分" };
            }
            // 至少要保留一位管理者
            if (mem.IsAdmin && mem.Status == MemberStatus.Active && role != MemberRole.Admin
                && srv.GetMembers().Count(m => m.IsAdmin && m.Status == MemberStatus.Active) <= 1)
            {
                return new { Success = 0, Message = "至少要保留一位管理者" };
            }
            srv.UpdateMemberRole(mem.Name, role);
            string label = role == MemberRole.Admin ? "管理者" : (role == MemberRole.Member ? "會員" : "待啟用");
            return new { Success = 1, Message = string.Format("{0} 已設為{1}", mem.DisplayName, label), Role = model.role.ToLowerInvariant() };
        }

        public class UpdateMemberModel
        {
            public string name { get; set; }
            /// <summary>admin／member／pending</summary>
            public string role { get; set; }
        }

        /// <summary>管理頁用的會員資料（不含密碼雜湊與存取權杖）</summary>
        public class MemberView
        {
            public string name { get; set; }
            public string display { get; set; }
            public int status { get; set; }
            public bool admin { get; set; }
            public DateTime create { get; set; }
            public DateTime access { get; set; }
            public bool self { get; set; }
        }
    }
}
