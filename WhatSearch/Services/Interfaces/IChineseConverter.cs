using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WhatSearch.Services.Interfaces
{
    /// <summary>
    /// 簡繁轉
    /// </summary>
    public interface IChineseConverter
    {
        string ToSimplifiedChinese(string word);
        string ToTraditionalChinese(string word);
    }
}
