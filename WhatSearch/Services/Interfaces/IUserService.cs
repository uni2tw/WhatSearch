using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using WhatSearch.Models;

namespace WhatSearch.Services.Interfaces
{
    public interface IUserService
    {
        void SetIdentityByToken(HttpContext context, string accessToken);
        bool SaveMember(Member mem, out string accessToken);
        void UpdateMember(string name);
        void UpdateMemberStatus(string name, MemberStatus status);
        void ForceLogin(HttpResponse response, string accessToken);
        Member GetMember(string name);
        List<Member> GetMembers();        
    }
}
