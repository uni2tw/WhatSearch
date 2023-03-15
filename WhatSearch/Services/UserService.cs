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
using WhatSearch.DataProviders;
using System.Numerics;
using WhatSearch.Utility;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using WhatSearch.DataModels;

namespace WhatSearch.Services
{
    public class UserService : IUserService
    {
        static ILogger logger = LogManager.GetCurrentClassLogger();
        IMemberProvider mp = ObjectResolver.Get<IMemberProvider>();
        IMemberDao _memberDao = ObjectResolver.Get<IMemberDao>();
        public void SetIdentityByToken(HttpContext context, string accessToken)
        {
            Member mem = mp.GetMemberByToken(accessToken);
            if (mem != null && mem.Status == MemberStatus.Active)
            {
                var claimIdentity = new ClaimsIdentity(new UserIdentity(mem.LineName))
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
            if (mem == null || string.IsNullOrEmpty(mem.LineName))
            {
                return false;
            }

            Member oldMem = mp.GetMember(mem.LineName);
            if (oldMem != null)
            {
                accessToken = oldMem.LineToken;
                return false;
            }
            try
            {
                mem.CreatedOn = DateTime.Now;
                mem.LastAccessTime = DateTime.Now;
                mem.LineToken = Guid.NewGuid().ToString("N");
                accessToken = mem.LineToken;
                mp.SaveMember(mem);
                return true;
            }
            catch (Exception ex)
            {
                logger.Error("SaveMember fail.", ex);
                return false;
            }
        }

        public void RecordLastAccessTimeByLineName(string name)
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

        public Member GetMemberByToken(string accessToken)
        {
            return mp.GetMemberByToken(accessToken);
        }

        public async Task<MemberModel> GetMemberModelByToken(string accessToken)
        {
            throw new NotImplementedException();
        }

        public async Task UpgradeFromJsonToSqliteAsync()
        {
            throw new NotImplementedException();
        }
    }
}