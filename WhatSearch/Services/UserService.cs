using Microsoft.AspNetCore.Http;
using System;
using System.Security.Claims;
using WhatSearch.Core;
using WhatSearch.Services.Interfaces;
using WhatSearch.DataProviders.Interfaces;
using WhatSearch.Models;
using System.Collections.Generic;
using NLog;
using WhatSearch.DataProviders;
using System.Linq;
using System.Threading.Tasks;
using WhatSearch.DataModels;
using WhatSearch.Middlewares;
using WhatSearch.Controllers;

namespace WhatSearch.Services
{
    public interface IUserService
    {
        void SetIdentityByToken(HttpContext context, string accessToken);
        Task<bool> InsertMemberAsync(MemberModel mem);
        Task RecordLastAccessTimeByLineName(string lineName);
        Task UpdateMemberStatus(string name, MemberStatus status);
        void ForceLogin(HttpResponse response, string accessToken, int cookieDays);
        Task<MemberModel> GetMemberByLineName(string lineName);
        List<MemberOld> GetMembers();
        MemberOld GetMemberByToken(string token);
        Task UpgradeFromJsonToSqliteAsync();
        Task<MemberModel> GetMemberModelByToken(string accessToken);
        Task UpdateMember(MemberModel member, string username, string password);
        Task<MemberModel> InsertMemberAsync(LineModels.LineUser lineUser, string accessToken);
        Task<MemberModel> GetMemberByUsername(string username, string password);
    }

    public class UserService : IUserService
    {
        IMemberDao _memberDao = ObjectResolver.Get<IMemberDao>();
        IMemberProvider mp = ObjectResolver.Get<IMemberProvider>();
        static ILogger logger = LogManager.GetCurrentClassLogger();

        public UserService()
        {
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

        public async Task<MemberModel> GetMemberByLineName(string lineName)
        {
            return await _memberDao.GetMemberByLineName(lineName);
        }

        public async Task<MemberModel> GetMemberByUsername(string username, string password)
        {
            return await _memberDao.GetMemberModelByUsername(username, password);
        }

        public MemberOld GetMemberByToken(string token)
        {
            return _memberDao.GetMemberByToken(token).Result?.ConvertToMember();
        }

        public List<MemberOld> GetMembers()
        {
            return _memberDao.GetMembers().Result.Select(x => x.ConvertToMember()).ToList();
        }

        public async Task<bool> InsertMemberAsync(MemberModel mem)
        {        
            if (mem == null || string.IsNullOrEmpty(mem.LineName))
            {
                return false;
            }

            MemberModel memberModel = await _memberDao.GetMemberModel(mem.LineName);
            if (memberModel != null)
            {         
                return false;
            }
            try
            {
                mem.CreatedOn = DateTime.Now;
                mem.LastAccessTime = DateTime.Now;
                mem.LineToken = Guid.NewGuid().ToString("N");             
                return await _memberDao.UpdateAsync(memberModel);
            }
            catch (Exception ex)
            {
                logger.Error("SaveMember fail.", ex);
                return false;
            }
        }

        public void SetIdentityByToken(HttpContext context, string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                return;
            }
            var mem = _memberDao.GetMemberByToken(accessToken).Result;
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

        public async Task RecordLastAccessTimeByLineName(string name)
        {
            var mem = await _memberDao.GetMemberByLineName(name);
            if (mem != null)
            {
                mem.LastAccessTime = DateTime.Now;
                await _memberDao.UpdateAsync(mem);
            }
        }

        public async Task UpdateMemberStatus(string name, MemberStatus status)
        {
            MemberModel mem = await _memberDao.GetMemberByLineName(name);
            if (mem != null)
            {
                mem.Status = status;
                mem.LastAccessTime = DateTime.Now;
                await _memberDao.UpdateAsync(mem);
            }
        }

        public async Task UpgradeFromJsonToSqliteAsync()
        {
            List<MemberOld> result = new List<MemberOld>();
            var memberModels = _memberDao.GetMembers().Result;
            if (memberModels.Any())
            {
                return;
            }

            List<MemberOld> members = mp.GetMembers();
            foreach (var member in members)
            {
                var memberModel = memberModels.FirstOrDefault(x => x.LineToken == member.LineToken);
                if (memberModel == null)
                {
                    memberModel = (member as MemberOld).ConvertToMemberModel();
                    await _memberDao.InsertAsync(memberModel);
                }
            }

        }

        public async Task<MemberModel> GetMemberModelByToken(string accessToken)
        {
            return await _memberDao.GetMemberByToken(accessToken);
        }

        public async Task UpdateMember(MemberModel member, string username, string password)
        {
            member.Username = username;
            member.Password = password;
            await _memberDao.UpdateAsync(member);
        }

        public async Task<MemberModel> InsertMemberAsync(LineModels.LineUser lineUser, string accessToken)
        {
            var mem = new MemberModel
            {
                LineName = lineUser.UserId,
                DisplayName = lineUser.DisplayName,
                Picture = lineUser.PictureUrl,
                Status = MemberStatus.Inactive,
                LineToken = accessToken
            };
            if (await _memberDao.UpdateAsync(mem))
            {
                return mem;
            }
            return null;
        }
    }
}