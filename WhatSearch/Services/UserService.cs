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
                claimIdentity.AddClaim(new Claim("Source", "Line"));
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
    }
}
