using System;
using Dapper;
using Dapper.Contrib.Extensions;
using NLog;
using System.Collections.Generic;
using System.Data.Common;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using WhatSearch.Core.Extensions;
using WhatSearch.DataAccess.Extensions;

namespace WhatSearch.DataAccess
{
    public class SqlDataModel
    {
        public string sql { get; set; }
        public object parameters { get; set; }
    }
    /// <summary>
    /// 提供 Query、Insert、Update、Delete、QuerySingleOrDefault、QueryFirstOrDefault、GetAll、Count、Any…等常用方法
    /// </summary>
    /// <typeparam name="TEntity">Model</typeparam>
    public abstract partial class BaseDao<TEntity> : BaseReadonlyDao<TEntity>
        where TEntity : class
    {
        private const int _limit = 200;

        //protected readonly ILogger _logger;
        protected readonly IDbConnectionFactory _dbConnectionFactory;
        protected readonly IHttpContextService _httpContextService;

        protected string TableName => typeof(TEntity).GetTableName();
        protected string PrimaryKey => typeof(TEntity).GetPrimaryKey();

        protected BaseDao(IDbConnectionFactory dbConnectionFactory, IHttpContextService httpContextService)
            : base(dbConnectionFactory, httpContextService)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _httpContextService = httpContextService;
            //  _logger = LogManager.GetCurrentClassLogger();
        }

        public override DbConnection GetDbConnection()
        {
            return _dbConnectionFactory.Create();
        }

        /// <summary>
        /// 使用 執行SQL 指令
        /// </summary>
        /// <param name="sql">SQL 指令</param>
        /// <param name="parameters">參數 new { param = value }</param>
        /// <returns>異動筆數</returns>
        public async Task<int> ExecuteAsync(string sql, object parameters)
        {
            using (var connection = GetDbConnection())
            {
                return await connection.ExecuteAsync(sql, parameters);
            }
        }

        /// <summary>
        /// 使用 執行複數SQL 指令
        /// </summary>
        /// <param name="listSqlData">複數SQL 指令和對應的參數</param>
        /// <returns>成功與否</returns>
        public async Task<bool> ExecuteMulAsync(List<SqlDataModel> listSqlData)
        {
            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            using (var connection = GetDbConnection())
            {
                foreach (var sqlData in listSqlData)
                {
                    var successed = await connection.ExecuteAsync(sqlData.sql, sqlData.parameters) > 0;
                    if (!successed)
                    {
                        return false;
                    }
                }
                transaction.Complete();
                return true;
            }
        }

        #region Insert - Dapper.Contrib

        /// <summary>
        /// (非同步) 新增資料
        /// </summary>
        /// <param name="entity">資料</param>
        /// <returns>Identity of inserted entity</returns>
        public async Task<long> InsertAsync(TEntity entity)
        {
            var userId = _httpContextService.GetCurrentUserId();

            entity.ApplyCreatedInfo(userId);

            /** 參閱：Dapper.Contrib https://dapper-tutorial.net/dapper-contrib **/
            using (var connection = GetDbConnection())
            {
                var id = await connection.InsertAsync(entity);
                if (id == 0 && PrimaryKey != null)
                {
                    var property = typeof(TEntity).GetProperty(PrimaryKey);
                    if (property.PropertyType == typeof(long))
                    {
                        return (long)property.GetValue(entity);
                    }
                    else if (property.PropertyType == typeof(int))
                    {
                        return (int)property.GetValue(entity);
                    }
                }
                return id;
            }
        }

        /// <summary>
        /// (非同步) 新增多筆資料
        /// </summary>
        /// <param name="entities">資料</param>
        /// <returns>Nnumber of inserted rows</returns>
        public async Task<int> InsertRangeAsync(List<TEntity> entities, int? timeoutSeconds = null)
        {
            var userId = _httpContextService.GetCurrentUserId();
            /** 參閱：Dapper.Contrib https://dapper-tutorial.net/dapper-contrib **/
            timeoutSeconds = timeoutSeconds ?? 60;
            Console.WriteLine($"timeoutSeconds: {timeoutSeconds}");
            TimeSpan scopeTimeout = TimeSpan.FromSeconds(timeoutSeconds.Value);
            using (var transaction = new TransactionScope(
                TransactionScopeOption.Required, scopeTimeout, TransactionScopeAsyncFlowOption.Enabled))
            {
                int rowCount = 0;
                using (var connection = GetDbConnection())
                {
                    foreach (var list in entities.Split(_limit))
                    {
                        rowCount += list.Count;
                        _logger.Debug($"InsertRangeAsync: {typeof(TEntity).Name} {rowCount}/{entities.Count}");
                        list.ApplyCreatedInfos(userId);
                        var insertedRowCount = await connection.InsertAsync(list);
                        if (insertedRowCount != list.Count)
                        {
                            return 0;
                        }
                    }
                }
                transaction.Complete();
            }
            return entities.Count;
        }

        #endregion

        #region Update - Dapper.Contrib

        /// <summary>
        /// (非同步) 更新資料
        /// </summary>
        /// <param name="entity">資料</param>
        /// <returns>是否成功</returns>
        public async Task<bool> UpdateAsync(TEntity entity)
        {
            var userId = _httpContextService.GetCurrentUserId();

            entity.ApplyUpdateInfo(userId);

            /** 參閱：Dapper.Contrib https://dapper-tutorial.net/dapper-contrib **/
            using (var connection = GetDbConnection())
            {
                return await connection.UpdateAsync(entity);
            }
        }

        /// <summary>
        /// (非同步) 更新多筆資料
        /// </summary>
        /// <param name="entities">資料</param>
        /// <returns>是否成功</returns>
        public async Task<bool> UpdateRangeAsync(List<TEntity> entities)
        {
            var userId = _httpContextService.GetCurrentUserId();
            /** 參閱：Dapper.Contrib https://dapper-tutorial.net/dapper-contrib **/
            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            using (var connection = GetDbConnection())
            {
                foreach (var list in entities.Split(_limit))
                {
                    list.ApplyUpdateInfo(userId);
                    if (!await connection.UpdateAsync(list))
                    {
                        return false;
                    }
                }

                transaction.Complete();

                return true;
            }
        }

        /// <summary>
        /// BatchUpdate
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="key">需要BatchUpdate的參數</param>
        /// <param name="param">剩下的參數</param>
        /// <returns>受影響的筆數</returns>
        public async Task<int> BatchUpdate<T>(string sql, List<T> key, DynamicParameters param)
        {
            var count = 0;
            var size = 300; //TODO 預設一次300筆

            for (int i = 0; i < key.Count; i += size)
            {
                using (var connection = GetDbConnection())
                {
                    param.Add("key", key.Skip(i).Take(size));
                    count += await connection.QueryFirstAsync<int>($"{sql} select @@rowcount", param); //trigger會影響回傳的行數,故強制回傳select @@rowcount
                }
            }

            return count;
        }

        #endregion

        #region Delete - Dapper.Contrib

        /// <summary>
        /// (非同步) 資料刪除資料
        /// </summary>
        /// <param name="entity">資料</param>
        /// <returns>是否成功</returns>
        public async Task<bool> DeleteAsync(TEntity entity)
        {
            var userId = _httpContextService.GetCurrentUserId();

            entity.ApplyDeleteInfo(userId);

            /** 參閱：Dapper.Contrib https://dapper-tutorial.net/dapper-contrib **/
            using (var connection = GetDbConnection())
            {
                return await connection.DeleteAsync(entity);
            }
        }

        /// <summary>
        /// (非同步) 刪除多筆資料
        /// </summary>
        /// <param name="entities">資料</param>
        /// <returns>是否成功</returns>
        public async Task<bool> DeleteRangeAsync(List<TEntity> entities)
        {
            var userId = _httpContextService.GetCurrentUserId();

            entities.ApplyDeleteInfo(userId);

            /** 參閱：Dapper.Contrib https://dapper-tutorial.net/dapper-contrib **/
            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            using (var connection = GetDbConnection())
            {
                foreach (var list in entities.Split(_limit))
                {
                    if (!await connection.DeleteAsync(list))
                    {
                        return false;
                    }
                }

                transaction.Complete();

                return true;
            }
        }

        #endregion
    }
}
