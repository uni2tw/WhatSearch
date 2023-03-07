using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using WhatSearch.Core;

namespace WhatSearch.WebAPIs.Filters
{
    public class AllowIpsAuthorizationFilterAttribute : Attribute,  IAuthorizationFilter
    {
        private bool _includeLocalIp = false;
        public AllowIpsAuthorizationFilterAttribute(bool includeLocalIp = true)
        {
            this._includeLocalIp = includeLocalIp;
        }
        SystemConfig config = ObjectResolver.GetConfig();
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            string clientIp = WebHelper.GetClientIp();
            if (this._includeLocalIp && (clientIp == "127.0.0.1" || clientIp == "::1"))
            {
                return;
            }
            if (config.PlayWhiteIps.Count > 0 && config.PlayWhiteIps.Contains(clientIp) == false)
            {
                context.Result = new ForbidResult();
            }
        }
    }
}
