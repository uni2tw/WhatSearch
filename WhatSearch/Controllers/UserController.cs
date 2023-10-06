using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WhatSearch.Core;
using WhatSearch.Services;
using WhatSearch.Services.Interfaces;

namespace WhatSearch.Controllers
{
    public class UserController : Controller
    {
        [HttpGet]
        [Route("user/reset_password")]
        public IActionResult ResetPassword()
        {
            return View();
        }

        [HttpPost]
        [Route("user/confirm_reset_password")]
        public async Task<IActionResult> ConfirmResetPassword(string username, string password)
        {
            string logonUser = this.User.Identity.Name;
            //重設一個帳號
            //

            var userService = ObjectResolver.Get<IUserService>();
            var member = await userService.GetMemberByLineName(logonUser);
            
            await userService.UpdateMember(member, username, password);
            
            

            return View();
        }
    }
}
