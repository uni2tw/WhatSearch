using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using WhatSearch.Services;

namespace WhatSearch.WebAPIs.Filters
{
    public class UserAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.HttpContext.User.Identity.IsAuthenticated == false)
            {
                var returnUrl = context.HttpContext.Request.Path.ToString();
                if (IsUnderProtected(returnUrl))
                {
                    context.Result = new RedirectResult("/linelogin?returnUrl=" + Uri.EscapeDataString(returnUrl));
                }
                return;
            }
            base.OnActionExecuting(context);
        }

        private bool IsUnderProtected(string url)
        {
            url = System.Web.HttpUtility.UrlDecode(url);
            string prefix = "/get/";
            string pathinfo;
            if (url.StartsWith(prefix))
            {
                pathinfo = url.Substring(prefix.Length);
            } 
            else
            {
                pathinfo = url;
            }
            string targetPath;
            if (PathUtility.IsProtetedUrl("/" + pathinfo) == false)
            {
                return false;
            }

            return true;
        }
    }
}
