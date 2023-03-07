using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Net;

namespace WhatSearch.Core.Extensions
{
    public static class HttpContextExtensions
    {
        public static string GetClientIp(this HttpContext context)
        {
            string clientIp;
            StringValues strValues;
            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out strValues))
            {
                clientIp = strValues.FirstOrDefault() ?? string.Empty;
                if (clientIp.Contains(','))
                {
                    clientIp = clientIp.Split(',')[0].Trim();
                }
                return clientIp;
            }
            return context.Connection.RemoteIpAddress?.ToString() ?? "";
        }
    }
}
