using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;

namespace WhatSearch.DataAccess.Extensions
{
    public class PagingQueryResponse<T>
    {
        public PagingQueryResponse(List<T> items, int totalRowCount, int pageIndex, int pageSize)
        {
            Items = items;
            TotalRowCount = totalRowCount;
            CurrentPage = pageIndex + 1;
            TotalPages = (totalRowCount + pageSize - 1) / pageSize;
            PageSize = pageSize;
        }
        public int TotalRowCount { get; private set; }
        public int CurrentPage { get; private set; }
        public int TotalPages { get; private set; }
        public int PageSize { get; private set; }
        public List<T> Items { get; private set; }
    }
    public interface IFilterDescriptor
    {

    }
    public class FilterDescriptor : IFilterDescriptor
    {
        public string Member { get; set; }
        public object Value { get; set; }
        public FilterOperator Operator { get; set; }
    }
    public class CompositeFilterDescriptor : IFilterDescriptor
    {
        public List<IFilterDescriptor> FilterDescriptors { get; internal set; }
        public FilterCompositionLogicalOperator LogicalOperator { get; internal set; }
    }

    public class DataSourceRequest
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public DataSourceSort[] Sorts { get; set; }
        public IFilterDescriptor[] Filters { get; set; }
    }
    public class DataSourceSort
    {
        public string Member { get; set; }
        public ListSortDirection SortDirection { get; set; }
    }
    public enum FilterCompositionLogicalOperator
    {
        Or,
        And
    }
    public enum FilterOperator
    {
        IsEqualTo,
        IsNotEqualTo,
        Contains,
        DoesNotContain,
        IsLessThan,
        IsLessThanOrEqualTo,
        IsGreaterThan,
        IsGreaterThanOrEqualTo,
        StartsWith,
        EndsWith,
        IsContainedIn,
        IsNull,
        IsNotNull,
        IsEmpty,
        IsNotEmpty,
        IsNullOrEmpty,
        IsNotNullOrEmpty
    }
    public static class DataSourceRequestExtensions
    {
        public static QueryOptions ToQueryOptions<T>(this DataSourceRequest request, bool useLikeStatement = false)
        {
            var tableName = typeof(T).GetTableName() ?? string.Empty;

            var orderBy = string.Empty;
            var pageIndex = 0;
            var pageSize = 0;

            if (request.Sorts != null)
            {
                var sorts = request.Sorts.Where(c => !string.IsNullOrEmpty(c.Member)).Select(c =>
                {
                    var direction = c.SortDirection == ListSortDirection.Descending ? " DESC" : string.Empty;
                    return $"[{c.Member}]{direction}";
                });

                if (sorts.Any())
                {
                    orderBy = string.Join(",", sorts);
                }
            }

            if (request.Page > 0 && request.PageSize > 0)
            {
                pageIndex = request.Page - 1;
                pageSize = request.PageSize;
            }

            var whereSql = string.Empty;
            var parameters = new ExpandoObject() as IDictionary<string, object>;
            if (request.Filters != null)
            {
                whereSql = ResolveCompositeFilterDescriptor(
                    request.Filters,
                    FilterCompositionLogicalOperator.And,
                    tableName,
                    useLikeStatement,
                    ref parameters);
            }

            return new QueryOptions()
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                OrderBySql = orderBy,
                WhereSql = whereSql,
                Parameters = parameters
            };
        }

        /// <summary>
        /// 將 FilterDescriptors 轉換成 SQL
        /// </summary>
        /// <param name="descriptors"></param>
        /// <param name="logicalOperator"></param>
        /// <param name="tableName"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private static string ResolveCompositeFilterDescriptor(
            IEnumerable<IFilterDescriptor> descriptors,
            FilterCompositionLogicalOperator logicalOperator,
            string tableName,
            bool useLikeStatement,
            ref IDictionary<string, object> parameters)
        {
            if (descriptors is null)
            {
                return string.Empty;
            }

            var conditions = new List<string>();

            foreach (var descriptor in descriptors)
            {
                if (descriptor is CompositeFilterDescriptor _compositeDescriptor)
                {
                    conditions.Add(ResolveCompositeFilterDescriptor(
                        _compositeDescriptor.FilterDescriptors,
                        _compositeDescriptor.LogicalOperator,
                        tableName,
                        useLikeStatement,
                        ref parameters));
                }
                else if (descriptor is FilterDescriptor _descriptor)
                {
                    conditions.Add(ResolveFilterDescriptor(_descriptor, tableName, useLikeStatement, ref parameters));
                }
            }

            if (!conditions.Any())
            {
                return string.Empty;
            }

            switch (logicalOperator)
            {
                case FilterCompositionLogicalOperator.And:
                    return "(" + string.Join(" AND ", conditions) + ")";

                case FilterCompositionLogicalOperator.Or:
                    return "(" + string.Join(" OR ", conditions) + ")";

                default:
                    return "(" + string.Join(" AND ", conditions) + ")";
            }
        }

        private static string ResolveFilterDescriptor(FilterDescriptor descriptor, string tableName, bool useLikeStatement, ref IDictionary<string, object> parameters)
        {
            var columnName = descriptor.Member;
            if (!string.IsNullOrWhiteSpace(columnName) && !columnName.Contains("."))
            {
                columnName = $"{tableName}.{descriptor.Member}";
            }

            // 避免重覆
            var parameterName = descriptor.Member.Replace(".", "_");
            if (parameters.ContainsKey(parameterName))
            {
                var count = parameters.Keys.Count(c => c.StartsWith(parameterName + "_"));
                parameterName = $"{parameterName}_{count + 1}";
            }

            parameters.Add(parameterName, descriptor.Value);

            switch (descriptor.Operator)
            {
                case FilterOperator.IsEqualTo:
                    return $"{columnName} = @{parameterName}";

                case FilterOperator.IsNotEqualTo:
                    return $"{columnName} <> @{parameterName}";

                case FilterOperator.Contains:
                    if (useLikeStatement)
                        return $"{columnName} LIKE '%'+ @{parameterName} +'%'";
                    return $"CONTAINS({columnName}, @{parameterName})";

                case FilterOperator.DoesNotContain:
                    return $"NOT CONTAINS({columnName}, @{parameterName})";

                case FilterOperator.IsLessThan:
                    return $"{columnName} < @{parameterName}";

                case FilterOperator.IsLessThanOrEqualTo:
                    return $"{columnName} <= @{parameterName}";

                case FilterOperator.IsGreaterThan:
                    return $"{columnName} > @{parameterName}";

                case FilterOperator.IsGreaterThanOrEqualTo:
                    return $"{columnName} >= @{parameterName}";

                case FilterOperator.StartsWith:
                    return $"{columnName} LIKE @{parameterName} + '%'";

                case FilterOperator.EndsWith:
                    return $"{columnName} LIKE '%' + @{parameterName}";

                case FilterOperator.IsContainedIn:
                    return $"{columnName} IN {descriptor.Member}";

                case FilterOperator.IsNull:
                    return $"{columnName} IS NULL";

                case FilterOperator.IsNotNull:
                    return $"{columnName} IS NOT NULL";

                case FilterOperator.IsEmpty:
                    return $"ISEMPTY({columnName})";

                case FilterOperator.IsNotEmpty:
                    return $"{columnName} <> ''";

                case FilterOperator.IsNullOrEmpty:
                    return $"{columnName} IS NULL OR ISEMPTY({columnName})";

                case FilterOperator.IsNotNullOrEmpty:
                    return $"{columnName} IS NOT NULL OR {columnName} <> ''";

                default:
                    return string.Empty;
            }
        }
    }
}
