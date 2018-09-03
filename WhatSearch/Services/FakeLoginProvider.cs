namespace WhatSearch.Core
{
    public class FakeLoginProvider : ILoginProvider
    {
        public bool Validate(string loginId, string password, out string message)
        {
            message = string.Empty;
            if (loginId == "unicorn")
            {
                return true;
            }
            return false;
        }
    }
}
