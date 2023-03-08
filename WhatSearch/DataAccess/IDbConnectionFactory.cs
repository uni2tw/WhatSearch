using System.Data.Common;
using System.Data.SQLite;

namespace WhatSearch.DataAccess
{
    public interface IDbConnectionFactory
    {
        string DefaultConnectionString { get; }

        DbConnection Create(string connectionString = null);
        DbConnection CreateReadonly();
    }

    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly IHttpContextService _httpContextService;

        public string DefaultConnectionString { get; private set; }

        private ConnectionStringSetting _connectionStringSetting;

        public DbConnectionFactory(IHttpContextService httpContextService, ConnectionStringSetting connectionStringSetting)
        {
            _httpContextService = httpContextService;
            _connectionStringSetting = connectionStringSetting;
            DefaultConnectionString = _connectionStringSetting.Primary;
        }

        public DbConnection Create(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = _connectionStringSetting.Primary;
            }

            DbConnection connection = new SQLiteConnection(connectionString);
            connection = new CustomizedDbConnection(connection, _httpContextService);
            return connection;
        }

        /// <summary>
        /// 取得唯讀的資料庫連線
        /// </summary>
        /// <returns></returns>
        public DbConnection CreateReadonly()
        {
            return Create(_connectionStringSetting.Secondary);
        }

    }

    public class ConnectionStringSetting 
    {
        public string Primary { get; set; }
        public string Secondary { get; set; }
    }
}
