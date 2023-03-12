using Dapper.Contrib.Extensions;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using WhatSearch.Core;
using WhatSearch.DataAccess;
using WhatSearch.DataProviders.Interfaces;
using WhatSearch.Models;
using WhatSearch.Models.JsonConverters;
using WhatSearch.Utility;

namespace WhatSearch.DataProviders
{
    [Table("Member")]
    public class MemberModel : IMember
    {
        [Key]
        public long Id { get; set; }
        public string Username { get;set;}
        public string Password { get; set; }
        public string LineName { get; set; }
        public string DisplayName { get; set; }
        public string Picture { get; set; }
        public string LineToken { get; set; }
        public MemberStatus Status { get; set; }
        public bool IsAdmin { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime LastAccessTime { get; set; }

    }
    public interface IBaseDao
    {
        Type GetModelType();
        DbConnection GetDbConnection();
    }
    public interface IMemberDao : IBaseDao
    {        
        Task<List<MemberModel>> GetMembers();
        Task<long> InsertAsync(MemberModel memberModel);
    }
    public class MemberDao : BaseDao<MemberModel>, IMemberDao
    {
        public MemberDao(IDbConnectionFactory dbConnectionFactory, IHttpContextService httpContextService) : base(dbConnectionFactory, httpContextService)
        {
            
        }

        public async Task<List<MemberModel>> GetMembers()
        {
            QueryOptions options = new QueryOptions
            {

            };
            return (await base.QueryAsync<MemberModel>(options)).ToList();
        }

    }
    public class MemberProvider : IMemberProvider
    {
        static ILogger logger = LogManager.GetCurrentClassLogger();
        static object thisLock = new object();

        IMemberDao _memberDao;
        public MemberProvider()
        {
            _memberDao = ObjectResolver.Get<IMemberDao>();
        }

        public List<IMember> GetMembers()
        {
            lock (thisLock)
            {
                try
                {
                    string dataPath = Helper.GetRelativePath("data", "userData", "users.json");
                    if (File.Exists(dataPath))
                    {
                        string jsonStr = File.ReadAllText(dataPath);
                        List<IMember> members = JsonHelper.Deserialize<List<IMember>>(jsonStr);

                        var memberModels = _memberDao.GetMembers().Result;

                        foreach(var member in members)
                        {
                            var memberModel = memberModels.FirstOrDefault(x => x.LineToken == member.LineToken);
                            if (memberModel == null)
                            {
                                memberModel = (member as Member).ConvertToMemberModel();
                                _memberDao.InsertAsync(memberModel).Wait();
                            }
                        }

                
                        return members;
                    }
                }
                catch (Exception ex)
                {
                    logger.Warn(ex);
                }
                return new List<IMember>();
            }
        }

        public IMember GetMember(string name)
        {
            return GetMembers().FirstOrDefault(t => t.Username == name);
        }

        public IMember GetMemberByToken(string accessToken)
        {
            return GetMembers().FirstOrDefault(t => t.LineToken == accessToken);
        }

        public void SaveMember(IMember mem)
        {
            var members = GetMembers();

            lock (thisLock)
            {
                IMember oldData = members.FirstOrDefault(t => t.Username == mem.Username);
                if (oldData == null)
                {
                    members.Add(mem);
                }
                else
                {
                    oldData.Status  = mem.Status;
                    oldData.LastAccessTime = mem.LastAccessTime;
                }
                string dataFolder = Helper.GetRelativePath("data", "userData");
                string dataPath = Helper.GetRelativePath("data", "userData", "users.json");
                if (Directory.Exists(dataFolder) == false)
                {
                    Directory.CreateDirectory(dataFolder);
                }
                string jsonStr = JsonHelper.Serialize(members, indented: true);
                File.WriteAllText(dataPath, jsonStr);                
            }
        }
    }
}
