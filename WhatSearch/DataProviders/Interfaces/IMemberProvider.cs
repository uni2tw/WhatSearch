using System.Collections.Generic;
using WhatSearch.Models;

namespace WhatSearch.DataProviders.Interfaces
{
    public interface IMemberProvider
    {
        List<IMember> GetMembers();
        void SaveMember(IMember mem);
        IMember GetMember(string name);
        IMember GetMemberByToken(string accessToken);
    }
}
