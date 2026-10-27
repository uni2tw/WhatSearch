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
        /// <summary>設定身分（管理者／會員／待啟用）；找不到會員回傳 false</summary>
        bool UpdateMemberRole(string name, MemberRole role);
        /// <summary>管理者重設密碼：存成雜湊；不會更換存取權杖（對方已登入的裝置不受影響）。找不到會員回傳 false</summary>
        bool ResetPassword(string name, string newPassword);
        void ForceLogin(HttpResponse response, string accessToken, int cookieDays);
        Member GetMember(string name);
        List<Member> GetMembers();
        bool Register(string name, string displayName, string password, out string message);
        bool Login(HttpResponse response, string name, string password, int cookieDays, out string message);
    }
}
