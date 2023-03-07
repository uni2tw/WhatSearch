using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace WhatSearch.DataAccess
{
    class CustomizedDbCommand : DbCommand
    {
        private readonly IHttpContextService _httpContextService;

        private DbCommand _command;

        public override string CommandText
        {
            get => _command.CommandText;
            set => _command.CommandText = value;
        }

        public override int CommandTimeout
        {
            get => _command.CommandTimeout;
            set => _command.CommandTimeout = value;
        }

        public override CommandType CommandType
        {
            get => _command.CommandType;
            set => _command.CommandType = value;
        }

        public override UpdateRowSource UpdatedRowSource
        {
            get => _command.UpdatedRowSource;
            set => _command.UpdatedRowSource = value;
        }

        protected override DbConnection DbConnection
        {
            get => _command.Connection;
            set => _command.Connection = value;
        }

        protected override DbParameterCollection DbParameterCollection => _command.Parameters;

        protected override DbTransaction DbTransaction
        {
            get => _command.Transaction;
            set => _command.Transaction = value;
        }

        public override bool DesignTimeVisible
        {
            get => _command.DesignTimeVisible;
            set => _command.DesignTimeVisible = value;
        }

        public CustomizedDbCommand(DbCommand command, IHttpContextService httpContextService)
        {
            _command = command;

            _httpContextService = httpContextService;
        }

        public override void Cancel() => _command.Cancel();

        public override void Prepare() => _command.Prepare();

        protected override DbParameter CreateDbParameter() => _command.CreateParameter();

        public override int ExecuteNonQuery()
        {
            CommandText = SetSessionContext(CommandText);

            return _command.ExecuteNonQuery();
        }

        public override object ExecuteScalar()
        {
            CommandText = SetSessionContext(CommandText);

            return _command.ExecuteScalar();
        }

        private string SetSessionContext(string commandText)
        {
            // 刪除資料時，加入刪除者資訊
            var pattern = new Regex(@"(^|;|\s+)\s*DELETE\s+(FROM\s+)?(\w+)\s+", RegexOptions.IgnoreCase);
            if (pattern.IsMatch(commandText))
            {
                var userId = _httpContextService.GetCurrentUserId();
                var sessionContext = $"EXEC sp_set_session_context 'userId', {userId};";

                return sessionContext + commandText;
            }

            return commandText;
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => _command.ExecuteReader(behavior);

        protected override void Dispose(bool disposing)
        {
            _command?.Dispose();
            _command = null;

            base.Dispose(disposing);
        }
    }
}
