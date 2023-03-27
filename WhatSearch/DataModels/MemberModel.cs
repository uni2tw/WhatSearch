using Dapper.Contrib.Extensions;
using System;
using WhatSearch.Models;

namespace WhatSearch.DataModels
{
    [Table("Member")]
    public class MemberModel
    {
        [Key]
        public long Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string LineName { get; set; }
        public string DisplayName { get; set; }
        public string Picture { get; set; }
        public string LineToken { get; set; }
        public MemberStatus Status { get; set; }
        public bool IsAdmin { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime LastAccessTime { get; set; }

        public MemberOld ConvertToMember()
        {
            return new MemberOld
            {
                LineName = LineName,
                IsAdmin = IsAdmin,
                LastAccessTime = LastAccessTime,
                CreatedOn = CreatedOn,
                LineToken = LineToken,
                Status = Status,
                Picture = Picture,
                DisplayName = DisplayName
            };
        }

        public override string ToString()
        {
            return this.DisplayName ?? this.Username ?? this.LineName;  
        }
    }
}
