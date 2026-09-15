using Microsoft.AspNetCore.Mvc;
using System;
using WhatSearch.Core;
using WhatSearch.Middlewares;
using WhatSearch.Services.Interfaces;

namespace WhatSearch.Controllers
{
    public class AuthController : Controller
    {
        static SystemConfig config = Ioc.GetConfig();

        [HttpPost]
        [Route("login")]
        public dynamic Login([FromBody] LoginModel model)
        {
            IUserService userSrv = Ioc.Get<IUserService>();
            string message;
            bool success = userSrv.Login(Response, model?.name, model?.password, config.Login.CookieDays, out message);
            return new
            {
                success,
                message,
                returnUrl = success ? (string.IsNullOrEmpty(model.returnUrl) ? "/page" : model.returnUrl) : null
            };
        }

        [HttpPost]
        [Route("register")]
        public dynamic Register([FromBody] RegisterModel model)
        {
            IUserService userSrv = Ioc.Get<IUserService>();
            string message;
            bool success = userSrv.Register(model?.name, model?.displayName, model?.password, out message);
            return new
            {
                success,
                message
            };
        }

        [HttpGet]
        [Route("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete(UserAuthenticationMiddleware._AUTH_COOKIE_NAME);
            return Redirect("/page/login");
        }

        public class LoginModel
        {
            public string name { get; set; }
            public string password { get; set; }
            public string returnUrl { get; set; }
        }

        public class RegisterModel
        {
            public string name { get; set; }
            public string displayName { get; set; }
            public string password { get; set; }
        }
    }
}
