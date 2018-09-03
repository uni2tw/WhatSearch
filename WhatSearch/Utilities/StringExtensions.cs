using System;


namespace WhatSearch.Utilities
{
    public static class StringExtension
    {
        public static string TrimStart(this string input, string removedStr, 
            StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        { 
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }
            if (input == string.Empty || string.IsNullOrEmpty(removedStr))
            {
                return input;
            }
            if (input.StartsWith(removedStr, comparison))
            {
                return input.Substring(removedStr.Length);
            }
            return input;
        }

    }
}
