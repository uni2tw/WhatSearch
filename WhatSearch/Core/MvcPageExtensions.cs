using Markdig.Helpers;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Web;
using WhatSearch.Utility;

namespace WhatSearch.Core
{
    public static class MvcPageExtensions
    {
        public static IHtmlContent SerializeJson(this IHtmlHelper htmlHelper, object model)
        {
            string jsonStr = JsonHelper.Serialize(model);
            string encodedStr = HttpUtility.JavaScriptStringEncode(JsonHelper.Serialize(model), true);
            return new HtmlString(encodedStr);
        }
    }

}
