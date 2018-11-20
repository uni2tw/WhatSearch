using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using WhatSearch.Core;
using WhatSearch.Services.Interfaces;

namespace WhatSearch.Middlewares
{
    public class UserAuthenticationMiddleware
    {
        public const string _AUTH_COOKIE_NAME = "WhatSearch_Auth";

        private readonly RequestDelegate _next;
        IUserService service = Ioc.Get<IUserService>();

        public UserAuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            string accessToken = context.Request.Cookies[_AUTH_COOKIE_NAME];            
            service.SetIdentityByToken(context, accessToken);
            await _next(context);
        }
    }
}
