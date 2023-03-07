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
    public class MemberModel
    {
        [Key]
        public long Id { get; set; }
        public string LoginName { get;set;}
        public string LoginPassword { get; set; }
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

        public List<Member> GetMembers()
        {
            lock (thisLock)
            {
                try
                {
                    string dataPath = Helper.GetRelativePath("data", "userData", "users.json");
                    if (File.Exists(dataPath))
                    {
                        string jsonStr = File.ReadAllText(dataPath);
                        List<Member> members = JsonHelper.Deserialize<List<Member>>(jsonStr);

                        var memberModels = _memberDao.GetMembers().Result;

                        foreach(var member in members)
                        {
                            var memberModel = memberModels.FirstOrDefault(x => x.LineToken == member.AccessToken);
                            if (memberModel == null)
                            {
                                memberModel = member.ConvertToMemberModel();
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
                return new List<Member>();
            }
        }

        public Member GetMember(string name)
        {
            return GetMembers().FirstOrDefault(t => t.Name == name);
        }

        public Member GetMemberByToken(string accessToken)
        {
            return GetMembers().FirstOrDefault(t => t.AccessToken == accessToken);
        }

        public void SaveMember(Member mem)
        {
            var members = GetMembers();

            lock (thisLock)
            {
                Member oldData = members.FirstOrDefault(t => t.Name == mem.Name);
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
