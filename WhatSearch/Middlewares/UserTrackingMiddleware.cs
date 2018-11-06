using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
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

            [JsonProperty("guid")]
            public Guid Guid { get; set; }
            [JsonProperty("sess")]
            public string SessionId { get; set; }
            [JsonProperty("req")]
            public string Request { get; set; }
            [JsonProperty("q")]
            public string QueryString { get; set; }
            [JsonProperty("ty")]
            public string RequestType { get; set; }
            [JsonProperty("ref")]
            public string UrlReferrer { get; set; }
            [JsonProperty("ip")]
            public string UserIp { get; set; }
            [JsonProperty("start")]
            public DateTime StartTime { get; set; }
            [JsonProperty("agent")]
            public string UserAgent { get; set; }
            [JsonProperty("user")]
            public int UserId { get; set; }
            [JsonProperty("elap")]
            public double ElapsedSecs { get; set; }
            [JsonProperty("srvby")]
            public string ServerBy { get; set; }
            [JsonProperty("rsrc")]
            public string ReferrerSourceId { get; set; }
            [JsonProperty("device")]
            public int DeviceType { get; set; }
            [JsonProperty("visitorIdentity")]
            public Guid? VisitorIdentity { get; set; }
            [JsonProperty("https")]
            public bool? IsHttps { get; set; }
        }
    }
}
