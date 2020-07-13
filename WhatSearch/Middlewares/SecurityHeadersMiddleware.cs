using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WhatSearch.Middlewares
{
    public class CustomSecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public CustomSecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            IHeaderDictionary headers = context.Response.Headers;
            headers.Remove("X-Powered-By");
            headers.Add("X-Frame-Options", "SAMEORIGIN");
            headers.Add("X-XSS-Protection", "1; mode=block");
            headers.Add("X-Content-Type-Options", "nosniff");
            headers.Add("Access-Control-Allow-Origin", "*");
            await _next(context);
        }
    }
}
