using Newtonsoft.Json;
using System;
using System.Security.Principal;
using WhatSearch.Models.JsonConverters;

namespace WhatSearch.Models
{
    public class UserIdentity : IIdentity
    {
        public UserIdentity(string name)
        {
            this.Name = name;
        }

        public string AuthenticationType
        {
            get
            {
                return "Token";
            }
        }

        public bool IsAuthenticated
        {
            get
            {
                return string.IsNullOrEmpty(Name) == false;
            }
        }

        public string Name { get; set; }
    }

    public class Member
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("display")]
        public string DisplayName { get; set; }
        [JsonProperty("pic")]
        public string Picture { get; set; }
        [JsonProperty("token")]
        public string AccessToken { get; set; }
        [JsonProperty("status")]
        public MemberStatus Status { get; set; }
        [JsonConverter(typeof(BoolConverter))]
        [JsonProperty("admin")]
        public bool IsAdmin { get; set; }
        [JsonProperty("create")]
        public DateTime CreateTime { get; set; }
        [JsonProperty("access")]
        public DateTime LastAccessTime { get; set; }
    }

    public enum MemberStatus
    {
        Invalice = 0,
        Active = 1,
        TryPasswrodTooMany = 2
    }
}
