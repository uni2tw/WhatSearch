using Dapper.Contrib.Extensions;
using System.Data;
using System.Data.Common;
using static Dapper.Contrib.Extensions.SqlMapperExtensions;

namespace WhatSearch.DataAccess
{

    class CustomizedDbConnection : DbConnection
    {
        private readonly IHttpContextService _httpContextService;

        private DbConnection _connection;
        public static GetDatabaseTypeDelegate GetDatabaseType;
        public override string ConnectionString
        {
            get => _connection.ConnectionString;
            set => _connection.ConnectionString = value;
        }

        public override string Database => _connection.Database;

        public override string DataSource => _connection.DataSource;

        public override string ServerVersion => _connection.ServerVersion;

        public override ConnectionState State => _connection.State;

        internal CustomizedDbConnection(DbConnection dbConnection, IHttpContextService httpContextService)
        {
            if (SqlMapperExtensions.GetDatabaseType == null) {
                SqlMapperExtensions.GetDatabaseType = delegate (IDbConnection conn)
                {
                    CustomizedDbConnection castConn = conn as CustomizedDbConnection;
                    if (castConn != null)
                    {
                        return castConn._connection.GetType().Name.ToLower();
                    }
                    return this._connection.GetType().Name.ToLower();                    
                };
            }
            _connection = dbConnection;            
            _httpContextService = httpContextService;
        }

        public override void ChangeDatabase(string databaseName) => _connection.ChangeDatabase(databaseName);

        public override void Close() => _connection.Close();

        public override void Open() => _connection.Open();

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => _connection.BeginTransaction(isolationLevel);

        protected override DbCommand CreateDbCommand() => new CustomizedDbCommand(_connection.CreateCommand(), _httpContextService);

        protected override void Dispose(bool disposing)
        {
            _connection?.Dispose();
            _connection = null;

            base.Dispose(disposing);
        }
    }
}
