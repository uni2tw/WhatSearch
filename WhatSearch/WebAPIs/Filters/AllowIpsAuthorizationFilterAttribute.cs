using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;
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
        SystemConfig config = Ioc.GetConfig();
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            string remoteIp = context.HttpContext.Connection.RemoteIpAddress.ToString();
            if (this._includeLocalIp && (remoteIp == "127.0.0.1" || remoteIp == "::1"))
            {
                return;
            }
            string remoteIpV4 = context.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();            
            if (config.PlayWhiteIps.Count > 0 && config.PlayWhiteIps.Contains(remoteIpV4) == false)
            {
                context.Result = new ForbidResult();
            }
        }
    }

}
