using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WhatSearch.DataAccess;
using WhatSearch.DataModels;
using WhatSearch.Models;

namespace WhatSearch.DataProviders
{
    public interface IMemberDao : IBaseDao
    {
        Task<List<MemberModel>> GetMembers();
        Task<MemberModel> GetMemberByToken(string token);
        Task<long> InsertAsync(MemberModel memberModel);
        Task<MemberModel> GetMemberByLineName(string lineName);
        Task<MemberModel> GetMemberModel(string lineName);
        Task<bool> UpdateAsync(MemberModel member);
        Task<MemberModel> GetMemberModelByUsername(string username, string password);
    }

    public class MemberDao : BaseDao<MemberModel>, IMemberDao
    {
        public MemberDao(IDbConnectionFactory dbConnectionFactory, IHttpContextService httpContextService) : base(dbConnectionFactory, httpContextService)
        {
            
        }

        public async Task<MemberModel> GetMemberByLineName(string lineName)
        {
            return await GetMemberModel(lineName);
        }

        public async Task<MemberModel> GetMemberByToken(string token)
        {
            QueryOptions options = new QueryOptions
            {
                WhereSql = "LineToken=@LineToken",
                Parameters = new
                {
                    LineToken = token
                }
            };
            return await base.QueryFirstOrDefaultAsync<MemberModel>(options);
        }


        public async Task<List<MemberModel>> GetMembers()
        {
            QueryOptions options = new QueryOptions
            {

            };
            return (await base.QueryAsync<MemberModel>(options)).ToList();
        }

        public async Task<MemberModel> GetMemberModel(string lineName)
        {
            QueryOptions options = new QueryOptions
            {
                WhereSql = "LineName=@LineName",
                Parameters = new
                {
                    LineName = lineName
                }
            };
            var memberModel = await base.QueryFirstOrDefaultAsync<MemberModel>(options);
            return memberModel;
        }

        public async Task<MemberModel> GetMemberModelByUsername(string username, string password)
        {
            QueryOptions options = new QueryOptions
            {
                WhereSql = "Username=@username and Password=@password",
                Parameters = new
                {
                    username = username,
                    password = password
                }
            };
            var memberModel = await base.QueryFirstOrDefaultAsync<MemberModel>(options);
            return memberModel;
        }
    }
}
