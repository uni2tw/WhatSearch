using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace WhatSearch.WebAPIs.Filters
{
    public class UserAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.HttpContext.User.Identity.IsAuthenticated == false)
            {
                var returnUrl = context.HttpContext.Request.Path.ToString();
                context.Result = new RedirectResult("/linelogin?returnUrl=" + Uri.EscapeDataString(returnUrl));
                //context.Result = new RedirectResult("/linelogin");
                return;
            }
            base.OnActionExecuting(context);
        }
    }
}
