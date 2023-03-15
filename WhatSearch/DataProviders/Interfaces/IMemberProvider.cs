using System.Collections.Generic;
using WhatSearch.Models;

namespace WhatSearch.DataProviders.Interfaces
{
    public interface IMemberProvider
    {
        List<Member> GetMembers();
        void SaveMember(Member mem);
        Member GetMember(string name);
        Member GetMemberByToken(string accessToken);
    }
}
