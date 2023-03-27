using System.Collections.Generic;
using WhatSearch.Models;

namespace WhatSearch.DataProviders.Interfaces
{
    public interface IMemberProvider
    {
        List<MemberOld> GetMembers();
        void SaveMember(MemberOld mem);
        MemberOld GetMember(string name);
        MemberOld GetMemberByToken(string accessToken);
    }
}
