using Markdig.Helpers;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.Web;

namespace WhatSearch.Core
{
    public static class MvcPageExtensions
    {
        public static IHtmlContent SerializeJson(this IHtmlHelper htmlHelper, object model)
        {
            string jsonStr = JsonConvert.SerializeObject(model);
            string encodedStr = HttpUtility.JavaScriptStringEncode(JsonConvert.SerializeObject(model), true);
            return new HtmlString(encodedStr);
        }
    }

}
