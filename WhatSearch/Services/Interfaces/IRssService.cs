using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WhatSearch.Services.Interfaces
{
    public interface IRssService
    {
        string GetFolderRss(string targetPath);
    }
}
