using Markdig;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using WhatSearch.Utility;

namespace WhatSearch.Middlewares
{
    public class MarkdownHanlderMiddleware
    {
        private RequestDelegate _next;

        public MarkdownHanlderMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            string readmePath = Helper.GetRelativePath("readme.md");
            string readmeHtml = string.Empty;
            if (File.Exists(readmePath))
            {
                string readmeMd = File.ReadAllText(readmePath);
                readmeHtml = Markdown.ToHtml(readmeMd);
            }

            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync
                (@"
<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'>
<link href='/styles/github-markdown.css' type='text/css' rel='stylesheet' />
</head>
<body>
<div class='markdown-body'>
" + readmeHtml + @"
</div>
</body>
</html>
", Encoding.UTF8);
        }
    }

    public static class MarkdownHanlderMiddlewareExtensions
    {
        public static IApplicationBuilder UseCustomHanlderMiddleware
                                      (this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<MarkdownHanlderMiddleware>();
        }
    }
}
