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

namespace WhatSearch.Services
{
    public class UserService2 : IUserService
    {
        IMemberDao _memberDao = ObjectResolver.Get<IMemberDao>();
        IMemberProvider mp = ObjectResolver.Get<IMemberProvider>();
        static ILogger logger = LogManager.GetCurrentClassLogger();

        public void ForceLogin(HttpResponse response, string accessToken, int cookieDays)
        {
            throw new NotImplementedException();
        }

        public Member GetMember(string name)
        {
            return _memberDao.GetMemberByLineName(name)?.ConvertToMember();
        }

        public Member GetMemberByToken(string token)
        {
            return _memberDao.GetMemberByToken(token).Result?.ConvertToMember();
        }

        public List<Member> GetMembers()
        {
            return _memberDao.GetMembers().Result.Select(x => x.ConvertToMember()).ToList();
        }

        public bool SaveMember(Member mem, out string accessToken)
        {
            accessToken = string.Empty;
            if (mem == null || string.IsNullOrEmpty(mem.LineName))
            {
                return false;
            }

            MemberModel memberModel = _memberDao.GetMemberModel(mem.LineName).Result;
            if (memberModel != null)
            {
                accessToken = memberModel.LineToken;
                return false;
            }
            try
            {
                memberModel = mem.ConvertToMemberModel();
                mem.CreatedOn = DateTime.Now;
                mem.LastAccessTime = DateTime.Now;
                mem.LineToken = Guid.NewGuid().ToString("N");
                accessToken = mem.LineToken;
                _memberDao.SaveMember(memberModel);
                return true;
            }
            catch (Exception ex)
            {
                logger.Error("SaveMember fail.", ex);
                return false;
            }
        }

        public void SetIdentityByToken(HttpContext context, string accessToken)
        {
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

        public void RecordLastAccessTimeByLineName(string name)
        {
            var mem = _memberDao.GetMemberByLineName(name);
            if (mem != null)
            {
                mem.LastAccessTime = DateTime.Now;
                _memberDao.SaveMember(mem);
            }
        }

        public void UpdateMemberStatus(string name, MemberStatus status)
        {
            MemberModel mem = _memberDao.GetMemberByLineName(name);
            if (mem != null)
            {
                mem.Status = status;
                mem.LastAccessTime = DateTime.Now;
                _memberDao.SaveMember(mem);
            }
        }

        public async Task UpgradeFromJsonToSqliteAsync()
        {
            List<Member> result = new List<Member>();
            var memberModels = _memberDao.GetMembers().Result;
            if (memberModels.Any())
            {
                return;
            }

            List<Member> members = mp.GetMembers();
            foreach (var member in members)
            {
                var memberModel = memberModels.FirstOrDefault(x => x.LineToken == member.LineToken);
                if (memberModel == null)
                {
                    memberModel = (member as Member).ConvertToMemberModel();
                    await _memberDao.InsertAsync(memberModel);
                }
            }

        }

        public async Task<MemberModel> GetMemberModelByToken(string accessToken)
        {
            return await _memberDao.GetMemberByToken(accessToken);
        }
    }
}