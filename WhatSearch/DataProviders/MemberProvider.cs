using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using WhatSearch.Core;
using WhatSearch.DataModels;
using WhatSearch.DataProviders.Interfaces;
using WhatSearch.Models;
using WhatSearch.Models.JsonConverters;
using WhatSearch.Utility;

namespace WhatSearch.DataProviders
{

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
            return GetMembers().FirstOrDefault(t => t.LineName == name);
        }

        public Member GetMemberByToken(string accessToken)
        {
            return GetMembers().FirstOrDefault(t => t.LineToken == accessToken);
        }

        public void SaveMember(Member mem)
        {
            if (mem == null)
            {
                throw new Exception("mem can't be null.");
            }
            if (string.IsNullOrEmpty(mem.LineToken) && string.IsNullOrEmpty(mem.LineName))
            {
                throw new Exception("mem's line token and username can't both be null.");
            }
            var member = _memberDao.GetMemberByToken(mem.LineToken).Result;

            if (member == null)
            {
                throw new Exception("mem can't be found.");
            }

            


            //var members = GetMembers();

            //lock (thisLock)
            //{
            //    IMember oldData = members.FirstOrDefault(t => t.Username == mem.Username);
            //    if (oldData == null)
            //    {
            //        members.Add(mem);
            //    }
            //    else
            //    {
            //        oldData.Status  = mem.Status;
            //        oldData.LastAccessTime = mem.LastAccessTime;
            //    }
            //    string dataFolder = Helper.GetRelativePath("data", "userData");
            //    string dataPath = Helper.GetRelativePath("data", "userData", "users.json");
            //    if (Directory.Exists(dataFolder) == false)
            //    {
            //        Directory.CreateDirectory(dataFolder);
            //    }
            //    string jsonStr = JsonHelper.Serialize(members, indented: true);
            //    File.WriteAllText(dataPath, jsonStr);                
            //}
        }
    }
}
