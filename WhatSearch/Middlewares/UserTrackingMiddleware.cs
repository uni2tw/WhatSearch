using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace WhatSearch.Middlewares
{
    public class UserTrackingMiddleware
    {
        private readonly RequestDelegate _next;

        DateTime now;
        Guid userTrackingGuid;
        public UserTrackingMiddleware(RequestDelegate next)
        {            
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            now = DateTime.Now;
            userTrackingGuid = Guid.NewGuid();
            //CookieManager.SetVisitorIdentity();
            //new System.Threading.ThreadLocal<string>().Values[]
            await _next(context);
            UserTracking ut = new UserTracking
            {
                Guid = userTrackingGuid,
                StartTime = now,
                Request = context.Request.Path,
                QueryString = context.Request.QueryString.ToString(),
                UserAgent = context.Request.Headers["UserAgnet"],
                ElapsedSecs = (DateTime.Now - now).TotalSeconds,
                ServerBy = Environment.MachineName,
                IsHttps = context.Request.IsHttps,
                RequestType = context.Request.Method,
                UserIp = context.Connection.RemoteIpAddress.MapToIPv4().ToString()
            };
        }

        [Serializable]
        public class UserTracking
        {
            public const string _QUEUE_NAME = "userTracking";

            [JsonPropertyName("guid")]
            public Guid Guid { get; set; }
            [JsonPropertyName("sess")]
            public string SessionId { get; set; }
            [JsonPropertyName("req")]
            public string Request { get; set; }
            [JsonPropertyName("q")]
            public string QueryString { get; set; }
            [JsonPropertyName("ty")]
            public string RequestType { get; set; }
            [JsonPropertyName("ref")]
            public string UrlReferrer { get; set; }
            [JsonPropertyName("ip")]
            public string UserIp { get; set; }
            [JsonPropertyName("start")]
            public DateTime StartTime { get; set; }
            [JsonPropertyName("agent")]
            public string UserAgent { get; set; }
            [JsonPropertyName("user")]
            public int UserId { get; set; }
            [JsonPropertyName("elap")]
            public double ElapsedSecs { get; set; }
            [JsonPropertyName("srvby")]
            public string ServerBy { get; set; }
            [JsonPropertyName("rsrc")]
            public string ReferrerSourceId { get; set; }
            [JsonPropertyName("device")]
            public int DeviceType { get; set; }
            [JsonPropertyName("visitorIdentity")]
            public Guid? VisitorIdentity { get; set; }
            [JsonPropertyName("https")]
            public bool? IsHttps { get; set; }
        }
    }
}
