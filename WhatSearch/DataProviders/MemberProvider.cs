using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WhatSearch.DataProviders.Interfaces;
using WhatSearch.Models;
using WhatSearch.Utility;

namespace WhatSearch.DataProviders
{
    public class MemberProvider : IMemberProvider
    {
        static ILogger logger = LogManager.GetCurrentClassLogger();
        static object thisLock = new object();
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
