using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using WhatSearch.Models;

namespace WhatSearch.Services.Interfaces
{
    public interface IUserService
    {
        void SetIdentityByToken(HttpContext context, string accessToken);
        bool SaveMember(IMember mem, out string accessToken);
        void UpdateMember(string name);
        void UpdateMemberStatus(string name, MemberStatus status);
        void ForceLogin(HttpResponse response, string accessToken, int cookieDays);
        IMember GetMember(string name);
        List<IMember> GetMembers();        
    }
}
