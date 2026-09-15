using Microsoft.AspNetCore.Http;
using System;
using System.Security.Claims;
using WhatSearch.Core;
using WhatSearch.Services.Interfaces;
using WhatSearch.DataProviders.Interfaces;
using WhatSearch.Models;
using WhatSearch.Middlewares;
using System.Collections.Generic;
using NLog;
using WhatSearch.Utilities;

namespace WhatSearch.Services
{
    public class UserService : IUserService
    {
        static ILogger logger = LogManager.GetCurrentClassLogger();
        IMemberProvider mp = Ioc.Get<IMemberProvider>();
        public void SetIdentityByToken(HttpContext context, string accessToken)
        {
            Member mem = mp.GetMemberByToken(accessToken);
            if (mem != null && mem.Status == MemberStatus.Active)
            {
                var claimIdentity = new ClaimsIdentity(new UserIdentity(mem.Name))
                {
                    Label = mem.DisplayName
                };
                claimIdentity.AddClaim(new Claim("Source", "Local"));
                if (mem.IsAdmin)
                {
                    claimIdentity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
                }
                context.User = new ClaimsPrincipal();
                context.User.AddIdentity(claimIdentity);
            }
        }

        public bool SaveMember(Member mem, out string accessToken)
        {
            accessToken = string.Empty;
            if (mem == null || string.IsNullOrEmpty(mem.Name))
            {
                return false;
            }

            Member oldMem = mp.GetMember(mem.Name);
            if (oldMem != null)
            {
                accessToken = oldMem.AccessToken;
                return false;
            }
            try
            {
                mem.CreateTime = DateTime.Now;
                mem.LastAccessTime = DateTime.Now;
                mem.AccessToken = Guid.NewGuid().ToString("N");
                accessToken = mem.AccessToken;
                mp.SaveMember(mem);
                return true;
            }
            catch (Exception ex)
            {
                logger.Error("SaveMember fail.", ex);
                return false;
            }
        }

        public void UpdateMember(string name)
        {
            Member mem = mp.GetMember(name);
            if (mem != null)
            {                
                mem.LastAccessTime = DateTime.Now;             
                mp.SaveMember(mem);
            }
        }

        public void UpdateMemberStatus(string name, MemberStatus status)
        {
            Member mem = mp.GetMember(name);
            if (mem != null)
            {
                mem.Status = status;
                mem.LastAccessTime = DateTime.Now;
                mp.SaveMember(mem);
            }
        }

        public void ForceLogin(HttpResponse response, string accessToken, int cookieDays)
        {
            response.Cookies.Delete(UserAuthenticationMiddleware._AUTH_COOKIE_NAME);
            response.Cookies.Append(UserAuthenticationMiddleware._AUTH_COOKIE_NAME, accessToken,
                new CookieOptions
                {
                    Expires = new DateTimeOffset(DateTime.Now.AddDays(cookieDays))
                });
        }

        public Member GetMember(string name)
        {
            return mp.GetMember(name);
        }

        public List<Member> GetMembers()
        {
            return mp.GetMembers();
        }

        public bool Register(string name, string displayName, string password, out string message)
        {
            message = string.Empty;
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(password))
            {
                message = "帳號與密碼不可空白";
                return false;
            }
            if (mp.GetMember(name) != null)
            {
                message = "此帳號已被使用";
                return false;
            }
            bool isFirstMember = mp.GetMembers().Count == 0;
            Member mem = new Member
            {
                Name = name,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? name : displayName,
                PasswordHash = PasswordHasher.Hash(password),
                PasswordFormat = PasswordFormat.Hashed,
                Status = isFirstMember ? MemberStatus.Active : MemberStatus.Invalice,
                IsAdmin = isFirstMember,
                CreateTime = DateTime.Now,
                LastAccessTime = DateTime.Now,
                AccessToken = Guid.NewGuid().ToString("N")
            };
            mp.SaveMember(mem);
            message = isFirstMember ? "註冊成功，已自動設為管理者" : "註冊成功，請等待管理者啟用帳號";
            return true;
        }

        public bool Login(HttpResponse response, string name, string password, int cookieDays, out string message)
        {
            message = string.Empty;
            Member mem = mp.GetMember(name);
            if (mem == null || string.IsNullOrEmpty(mem.PasswordHash))
            {
                message = "帳號或密碼錯誤";
                return false;
            }

            bool passwordOk;
            if (mem.PasswordFormat == PasswordFormat.PlainText)
            {
                // 管理者直接改 users.json 明碼密碼時走這裡，驗證成功後立刻轉存為雜湊，明碼不再留存
                passwordOk = mem.PasswordHash == password;
                if (passwordOk)
                {
                    mem.PasswordHash = PasswordHasher.Hash(password);
                    mem.PasswordFormat = PasswordFormat.Hashed;
                }
            }
            else
            {
                passwordOk = PasswordHasher.Verify(password, mem.PasswordHash);
            }
            if (passwordOk == false)
            {
                message = "帳號或密碼錯誤";
                return false;
            }
            if (mem.Status != MemberStatus.Active)
            {
                message = "帳號尚未啟用，請等待管理者啟用";
                return false;
            }
            mem.LastAccessTime = DateTime.Now;
            mp.SaveMember(mem);
            ForceLogin(response, mem.AccessToken, cookieDays);
            return true;
        }
    }
}
