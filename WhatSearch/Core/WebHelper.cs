namespace WhatSearch.Core
{
    public class WebHelper
    {
        public static string GetClientIp()
        {
            var context = WebContext.Current;
            string clientIp = context.Connection.RemoteIpAddress.MapToIPv4().ToString();
            return clientIp;
        }
    }
}