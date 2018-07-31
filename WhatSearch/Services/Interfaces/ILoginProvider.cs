namespace WhatSearch.Core
{
    public interface ILoginProvider
    {
        bool Validate(string loginId, string password, out string message);
    }
}
