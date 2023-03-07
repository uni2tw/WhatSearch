using Dapper;
using System.Linq;
using System.Text;

namespace WhatSearch.DataAccess.Extensions
{
    public static class QueryOptionsExtensions
    {
        /// <summary>
        /// 產生 Dapper.SqlBuilder.Templat
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public static SqlBuilder.Template GenerateQueryTemplate<T>(this QueryOptions options)
        {
            var template = GenerateTemplateString<T>(options, false);
            var builder = options.GenerateSqlBuilder();

            return builder.AddTemplate(template);
        }

        /// <summary>
        /// 產生 Dapper.SqlBuilder.Templat
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public static SqlBuilder.Template GenerateCountTemplate<T>(this QueryOptions options)
        {
            options.OrderBySql = string.Empty;

            var template = GenerateTemplateString<T>(options, true);
            var builder = options.GenerateSqlBuilder();

            return builder.AddTemplate(template);
        }

        /// <summary>
        /// 產生 Dapper.SqlBuilder.Templat 字串
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private static string GenerateTemplateString<T>(QueryOptions options, bool isCount)
        {
            var type = typeof(T);
            var tableName = type.GetTableName();
            var primaryKey = type.GetPrimaryKey();

            if (options is null)
            {
                options = new QueryOptions();
            }

            var withNolock = options.WithNolck ? "with(nolock)" : string.Empty;
            var template = $"SELECT /**select**/ FROM {tableName} {withNolock} /**innerjoin**/ /**join**/ /**leftjoin**/ /**rightjoin**/ /**where**/ /**groupby**/ /**orderby**/";

            if (isCount)
            {
                var trimS = options.SelectSql.TrimStart().ToUpper();
                if (trimS.StartsWith("DISTINCT ") || trimS.StartsWith($"DISTINCT{System.Environment.NewLine}"))
                {
                    return $@"
SELECT COUNT(*) FROM (
    SELECT /**select**/ FROM {tableName} {withNolock} /**innerjoin**/ /**join**/ /**leftjoin**/ /**rightjoin**/ /**where**/ /**groupby**/
) TMP";
                }

                return $"SELECT COUNT(*) FROM {tableName} {withNolock} /**innerjoin**/ /**join**/ /**leftjoin**/ /**rightjoin**/ /**where**/ /**groupby**/";
            }

            var stringBuilder = new StringBuilder();
            stringBuilder.Append(template);

            if (!string.IsNullOrWhiteSpace(options.OrderBySql) && !(options.OrderBySql.Contains(".") || options.OrderBySql.Split(',').Where(o => int.TryParse(o, out var p)).Any()))
            {
                options.OrderBySql = $"{tableName}.{options.OrderBySql}";
            }

            if (options.PageIndex.HasValue && options.PageIndex.Value >= 0 &&
                    options.PageSize.HasValue && options.PageSize.Value > 0)
            {
                if (string.IsNullOrWhiteSpace(options.OrderBySql))
                {
                    options.OrderBySql = $"{tableName}.{primaryKey}";
                }

                stringBuilder.Append($" OFFSET {options?.PageIndex} * {options?.PageSize} ROWS");
                stringBuilder.Append($" FETCH NEXT {options?.PageSize} ROWS ONLY");
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// 產生 Dapper.SqlBuilder
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private static SqlBuilder GenerateSqlBuilder(this QueryOptions options)
        {
            var builder = new SqlBuilder();
            builder.Select(string.IsNullOrWhiteSpace(options?.SelectSql) ? "*" : options?.SelectSql);
            builder.AddParameters(options?.Parameters);

            if (!string.IsNullOrWhiteSpace(options?.WhereSql))
            {
                builder.Where(options?.WhereSql);
            }

            if (!string.IsNullOrWhiteSpace(options?.OrderBySql))
            {
                builder.OrderBy(options?.OrderBySql);
            }

            if (!string.IsNullOrWhiteSpace(options?.Join))
            {
                builder.Join(options?.Join);
            }

            if (!string.IsNullOrWhiteSpace(options?.LeftJoin))
            {
                builder.LeftJoin(options?.LeftJoin);
            }

            if (!string.IsNullOrWhiteSpace(options?.RightJoin))
            {
                builder.RightJoin(options?.RightJoin);
            }

            if (!string.IsNullOrWhiteSpace(options?.InnerJoin))
            {
                builder.InnerJoin(options?.InnerJoin);
            }

            if (!string.IsNullOrWhiteSpace(options?.GroupBy))
            {
                builder.GroupBy(options?.GroupBy);
            }

            return builder;
        }
    }
}
