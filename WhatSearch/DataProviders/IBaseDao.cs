using System;
using System.Data.Common;

namespace WhatSearch.DataProviders
{
    public interface IBaseDao
    {
        Type GetModelType();
        DbConnection GetDbConnection();
    }
}
