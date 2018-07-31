namespace WhatSearch.Core
{
    public interface IMemberService
    {
        bool Validate(string loginId, string password, out string message);
    }
}