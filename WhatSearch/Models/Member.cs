using System;
using System.Security.Principal;
using System.Text.Json.Serialization;
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
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("display")]
        public string DisplayName { get; set; }
        [JsonPropertyName("pic")]
        public string Picture { get; set; }
        [JsonPropertyName("token")]
        public string AccessToken { get; set; }
        [JsonPropertyName("status")]
        public MemberStatus Status { get; set; }
        [JsonConverter(typeof(BoolConverter))]
        [JsonPropertyName("admin")]
        public bool IsAdmin { get; set; }
        [JsonPropertyName("create")]
        public DateTime CreateTime { get; set; }
        [JsonPropertyName("access")]
        public DateTime LastAccessTime { get; set; }
    }

    public enum MemberStatus
    {
        Invalice = 0,
        Active = 1,
        TryPasswrodTooMany = 2
    }
}
