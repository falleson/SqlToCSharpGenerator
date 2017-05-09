using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace SqlToCSharpGenerator
{
    public class SqlHelper
    {
        private static string connString = ConfigurationManager.ConnectionStrings["CNMP"].ConnectionString;
    
        /// <summary>
        /// 返回多条记录
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="strSql"></param>
        /// <returns></returns>
        public static List<T> Query<T>(string strSql)
        {
            using (var conn = new SqlConnection(connString))
            {
                return conn.Query<T>(strSql).ToList();
            }
        }

        /// <summary>
        /// 返回一条记录
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="strSql"></param>
        /// <returns></returns>
        public static T Single<T>(string strSql)
        {
            using (var conn = new SqlConnection(connString))
            {
                return conn.Query<T>(strSql).SingleOrDefault();
            }
        }

    }
}
