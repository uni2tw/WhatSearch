using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using WhatSearch.DataModels;
using WhatSearch.Models;

namespace WhatSearch.Services.Interfaces
{
    public interface IUserService
    {
        void SetIdentityByToken(HttpContext context, string accessToken);
        bool SaveMember(Member mem, out string accessToken);
        void RecordLastAccessTimeByLineName(string lineName);
        void UpdateMemberStatus(string name, MemberStatus status);
        void ForceLogin(HttpResponse response, string accessToken, int cookieDays);
        Member GetMember(string name);
        List<Member> GetMembers();
        Member GetMemberByToken(string token);
        Task UpgradeFromJsonToSqliteAsync();
        Task<MemberModel> GetMemberModelByToken(string accessToken);
    }
}
