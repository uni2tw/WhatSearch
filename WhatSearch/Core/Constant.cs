using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WhatSearch.Core
{
    public class Constants
    {
        private static string _rootId = Guid.Empty.ToString();
        public static string RootId
        {
            get
            {
                return _rootId;
            }
        }

        public const string _ADMIN = "Admin";
    }
}
