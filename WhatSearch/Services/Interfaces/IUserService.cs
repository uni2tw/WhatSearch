using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WhatSearch.DataModels;
using WhatSearch.Models;

namespace WhatSearch.Services.Interfaces
{
    public interface IUserService
    {
        void SetIdentityByToken(HttpContext context, string accessToken);
        bool SaveMember(MemberModel mem, out string accessToken);
        Task RecordLastAccessTimeByLineName(string lineName);
        Task UpdateMemberStatus(string name, MemberStatus status);
        void ForceLogin(HttpResponse response, string accessToken, int cookieDays);
        Task<MemberModel> GetMemberByLineName(string lineName);
        List<MemberOld> GetMembers();
        MemberOld GetMemberByToken(string token);
        Task UpgradeFromJsonToSqliteAsync();
        Task<MemberModel> GetMemberModelByToken(string accessToken);


    }
}
