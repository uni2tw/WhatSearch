using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatSearch.DataAccess
{
    public class QueryOptions
    {
        /// <summary>
        /// 預設 *
        /// </summary>
        public string SelectSql { get; set; } = "*";

        /// <summary>
        /// Field1 = @param1 AND Field2 = @param2
        /// </summary>
        public string WhereSql { get; set; } = null;

        /// <summary>
        /// ORDER BY Field1 ASC, Field2 DESC
        /// </summary>
        public string OrderBySql { get; set; } = null;

        /// <summary>
        /// new { param1 = value1, param2 = value2 }
        /// </summary>
        public object Parameters { get; set; } = null;

        /// <summary>
        /// 分頁：每頁顯示數量
        /// </summary>
        public int? PageSize { get; set; } = null;

        /// <summary>
        /// 分頁：頁數（base 0）
        /// </summary>
        public int? PageIndex { get; set; } = null;

        /// <summary>
        /// options.Add("Table1 ON Table1.Field = Table.Field")
        /// </summary>
        public List<string> JoinList { get; set; } = new List<string>();

        /// <summary>
        /// 取得 LEFT JOIN Script
        /// </summary>
        public string Join { get => string.Join(" JOIN ", JoinList); }


        /// <summary>
        /// options.Add("Table1 ON Table1.Field = Table.Field")
        /// </summary>
        public List<string> LeftJoinList { get; set; } = new List<string>();

        /// <summary>
        /// 取得 LEFT JOIN Script
        /// </summary>
        public string LeftJoin { get => string.Join(" LEFT JOIN ", LeftJoinList); }

        /// <summary>
        /// options.Add("Table1 ON Table1.Field = Table.Field")
        /// </summary>
        public List<string> RightJoinList { get; set; } = new List<string>();

        /// <summary>
        /// 取得 RIGHT JOIN Script
        /// </summary>
        public string RightJoin { get => string.Join(" RIGHT JOIN ", RightJoinList); }

        /// <summary>
        /// options.Add("Table1 ON Table1.Field = Table.Field")
        /// </summary>
        public List<string> InnerJoinList { get; set; } = new List<string>();

        /// <summary>
        /// 取得 INNER JOIN Script
        /// </summary>
        public string InnerJoin { get => string.Join(" INNER JOIN ", InnerJoinList); }

        public List<string> GroupByList { get; set; } = new List<string>();

        public string GroupBy { get => string.Join(",", GroupByList); }

        public bool WithNolck { get; set; } = false;

        public QueryOptions()
        {

        }
    }
}