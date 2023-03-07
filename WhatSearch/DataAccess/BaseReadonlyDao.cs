using Dapper;
using Dapper.Contrib.Extensions;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using WhatSearch.DataAccess.Extensions;

namespace WhatSearch.DataAccess
{
    /// <summary>
    /// 只提供 Query 相關
    /// </summary>
    /// <typeparam name="TEntity">Model</typeparam>
    public abstract class BaseReadonlyDao<TEntity> where TEntity : class
    {
        protected readonly ILogger _logger;
        protected readonly IDbConnectionFactory _dbConnectionFactory;
        protected readonly IHttpContextService _httpContextService;

        protected string TableName => typeof(TEntity).GetTableName();
        protected string PrimaryKey => typeof(TEntity).GetPrimaryKey();

        protected BaseReadonlyDao(IDbConnectionFactory dbConnectionFactory, IHttpContextService httpContextService)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _httpContextService = httpContextService;
            _logger = LogManager.GetCurrentClassLogger();
        }

        public virtual DbConnection GetDbConnection()
        {
            return _dbConnectionFactory.CreateReadonly();
        }

        public virtual Type GetModelType()
        {
            return typeof(TEntity);
        }

        public async Task<SqlMapper.GridReader> QueryMultipleAsync(string sql, object parameters)
        {
            using (var connection = GetDbConnection())
            {
                var result = await connection.QueryMultipleAsync(sql, parameters);

                return result;
            }
        }

        #region Query

        /// <summary>
        /// (非同步) 使用 SQL 指令搜尋資料庫
        /// </summary>
        /// <typeparam name="T">型別</typeparam>
        /// <param name="sql">SQL 指令</param>
        /// <param name="parameters">參數 new { param = value }</param>
        /// <returns>搜尋結果</returns>
        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object parameters)
        {
            using (var connection = GetDbConnection())
            {
                var result = await connection.QueryAsync<T>(sql, parameters);

                return result;
            }
        }

        /// <summary>
        /// (非同步) 使用 QueryOptions 搜尋資料庫
        /// </summary>
        /// <typeparam name="T">型別</typeparam>
        /// <param name="options">QueryOptions</param>
        /// <returns>搜尋結果</returns>
        public Task<IEnumerable<T>> QueryAsync<T>(QueryOptions options)
        {
            var template = options.GenerateQueryTemplate<TEntity>();

            return QueryAsync<T>(template.RawSql, template.Parameters);
        }

        #endregion

        #region QuerySingleOrDefault

        /// <summary>
        /// (非同步) 傳回單一特定項目；如果找不到該項目或找到多個，則傳回預設值。
        /// </summary>
        /// <typeparam name="T">型別</typeparam>
        /// <param name="sql">SQL 指令</param>
        /// <param name="parameters">參數 new { param = value }</param>
        /// <returns>搜尋結果</returns>
        public async Task<T> QuerySingleOrDefaultAsync<T>(string sql, object parameters)
        {
            using (var connection = GetDbConnection())
            {
                var result = await connection.QuerySingleOrDefaultAsync<T>(sql, parameters);

                return result;
            }
        }

        /// <summary>
        /// (非同步) 傳回單一特定項目；如果找不到該項目或找到多個，則傳回預設值。
        /// </summary>
        /// <typeparam name="T">型別</typeparam>
        /// <param name="options">QueryOptions</param>
        /// <returns>搜尋結果</returns>
        public Task<T> QuerySingleOrDefaultAsync<T>(QueryOptions options)
        {
            var template = options.GenerateQueryTemplate<TEntity>();

            return QuerySingleOrDefaultAsync<T>(template.RawSql, template.Parameters);
        }

        #endregion

        #region QueryFirstOrDefault

        /// <summary>
        /// (非同步) 傳回第一個項目；如果找不到該項目，則傳回預設值。
        /// </summary>
        /// <typeparam name="T">型別</typeparam>
        /// <param name="sql">SQL 指令</param>
        /// <param name="parameters">參數 new { param = value }</param>
        /// <returns>搜尋結果</returns>
        public async Task<T> QueryFirstOrDefaultAsync<T>(string sql, object parameters)
        {
            using (var connection = GetDbConnection())
            {
                var result = await connection.QueryFirstOrDefaultAsync<T>(sql, parameters);

                return result;
            }
        }

        /// <summary>
        /// (非同步) 傳回第一個項目；如果找不到該項目，則傳回預設值。
        /// </summary>
        /// <typeparam name="T">型別</typeparam>
        /// <param name="options">QueryOptions</param>
        /// <returns>搜尋結果</returns>
        public Task<T> QueryFirstOrDefaultAsync<T>(QueryOptions options)
        {
            var template = options.GenerateQueryTemplate<TEntity>();

            return QueryFirstOrDefaultAsync<T>(template.RawSql, template.Parameters);
        }

        #endregion

        #region Count

        /// <summary>
        /// (非同步) 回傳數量
        /// </summary>
        /// <param name="sql">SQL 指令</param>
        /// <param name="parameters">參數 new { param = value }</param>
        /// <returns></returns>
        public async Task<int> CountAsync(string sql, object parameters)
        {
            using (var connection = GetDbConnection())
            {
                return await connection.ExecuteScalarAsync<int>(sql, parameters);
            }
        }

        /// <summary>
        /// (非同步) 回傳數量
        /// </summary>
        /// <param name="options">QueryOptions</param>
        /// <returns>數量</returns>
        public Task<int> CountAsync(QueryOptions options)
        {
            var template = options.GenerateCountTemplate<TEntity>();
            return CountAsync(template.RawSql, template.Parameters);

        }

        #endregion

        #region Any

        /// <summary>
        /// (非同步) 回傳是否符合條件
        /// </summary>
        /// <param name="sql">SQL 指令</param>
        /// <param name="parameters">參數 new { param = value }</param>
        /// <returns>數量</returns>
        public async Task<bool> AnyAsync(string sql, object parameters)
        {
            var count = await CountAsync(sql, parameters);

            return count > 0;
        }

        /// <summary>
        /// (非同步) 回傳是否符合條件
        /// </summary>
        /// <param name="options">QueryOptions</param>
        /// <returns>數量</returns>
        public async Task<bool> AnyAsync(QueryOptions options)
        {
            var count = await CountAsync(options);

            return count > 0;
        }

        #endregion

        #region Get By Id - Dapper.Contrib
        /// <summary>
        /// (非同步) 利用 Id 取得單一項目
        /// </summary>
        /// <returns>搜尋結果</returns>
        public async Task<TEntity> GetByIdAsync(long id)
        {
            /** 參閱：Dapper.Contrib https://dapper-tutorial.net/dapper-contrib **/
            using (var connection = GetDbConnection())
            {
                return await connection.GetAsync<TEntity>(id);
            }
        }


        public Task<IEnumerable<TEntity>> GetByIdsAsync(List<long> ids)
        {
            var options = new QueryOptions
            {
                WhereSql = "id in @ids",
                Parameters = new
                {
                    ids
                }
            };

            return QueryAsync<TEntity>(options);
        }

        #endregion

        #region Get All - Dapper.Contrib

        /// <summary>
        /// (非同步) 取得所有資料
        /// </summary>
        /// <returns>所有資料</returns>
        public async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            /** 參閱：Dapper.Contrib https://dapper-tutorial.net/dapper-contrib **/
            using (var connection = GetDbConnection())
            {
                return await connection.GetAllAsync<TEntity>();
            }
        }

        #endregion
    }
}
