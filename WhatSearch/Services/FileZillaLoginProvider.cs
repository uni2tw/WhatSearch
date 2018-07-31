namespace WhatSearch.Core
{
    public class FileZillaLoginProvider : ILoginProvider
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
