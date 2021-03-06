﻿using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;

namespace UzEx.Dapper.Oracle.BulkSql
{
    public static class BulkOperation
    {
        /// <summary>
        ///     Executes a bulk SQL statement against database and returns the number of rows affected
        ///     Works with UPDATE / INSERT / DELETE statements, and stored procedures.
        /// </summary>
        /// <typeparam name="T">Entity type for the bulk operation object</typeparam>
        /// <param name="connection">The <see cref="IDbConnection">Database connection to use</see></param>
        /// <param name="sql">
        ///     Sql statement to execute.
        ///     <remarks>
        ///         Parameter names MUST MATCH property names in object entity.
        ///     </remarks>
        /// </param>
        /// <param name="objects">IEnumerable containing object for bulk operation</param>
        /// <param name="cmdType">Command type;Text or StoredProcedure</param>
        /// <returns>Number of rows affected by bulk statement</returns>
        public static int SqlBulk<T>(this IDbConnection connection, string sql, IEnumerable<T> objects,
            IEnumerable<BulkMapping<T>> mapping,
            CommandType? cmdType = CommandType.Text)
        {
            return SqlBulk(connection, sql, objects, mapping, out _, cmdType);
        }


        /// <summary>
        ///     Executes a bulk SQL statement against database and returns the number of rows affected
        ///     Works with UPDATE / INSERT / DELETE statements, and stored procedures.
        /// </summary>
        /// <typeparam name="T">Entity type for the bulk operation object</typeparam>
        /// <param name="connection">The <see cref="IDbConnection">Database connection to use</see></param>
        /// <param name="sql">
        ///     Sql statement to execute.
        ///     <remarks>
        ///         Parameter names MUST MATCH property names in object entity.
        ///     </remarks>
        /// </param>
        /// <param name="objects">IEnumerable containing object for bulk operation</param>
        /// <param name="parameters">
        ///     Instance of <see cref="OracleDynamicParameters" /> used for executing sql statements.  Can be
        ///     used to retreive value from a refcursor
        /// </param>
        /// <param name="cmdType">Command type;Text or StoredProcedure</param>
        /// <returns>Number of rows affected by bulk statement</returns>
        public static int SqlBulk<T>(this IDbConnection connection, string sql, IEnumerable<T> objects,
            IEnumerable<BulkMapping<T>> mapping, out OracleDynamicParameters parameters,
            CommandType? cmdType = CommandType.Text)
        {
            parameters = CreateParameterFromObject(objects, mapping);

            return connection.Execute(sql, parameters, commandType: cmdType);
        }

        public static async Task<AsyncQueryResult> SqlBulkAsync<T>(this IDbConnection connection, string sql,
            IEnumerable<T> objects, IEnumerable<BulkMapping<T>> mapping, CommandType? cmdType = CommandType.Text,
            IDbTransaction transaction = null)
        {
            var parameters = CreateParameterFromObject(objects, mapping);
            var result = await connection.ExecuteAsync(sql, parameters, transaction, commandType: cmdType);

            return new AsyncQueryResult
            {
                ExecuteResult = result,
                Parameters = parameters
            };
        }

        private static OracleDynamicParameters CreateParameterFromObject<T>(IEnumerable<T> objects,
            IEnumerable<BulkMapping<T>> mapping)
        {
            var parameters = new OracleDynamicParameters();
            var obj = objects.ToList();

            parameters.ArrayBindCount = obj.Count;
            parameters.BindByName = true;

            foreach (var map in mapping)
            {
                var values = map.Property != null ? obj.Select(map.Property).ToArray() : null;
                var dbType = map.DbType ?? OracleMapper.GuessType(obj.First().GetType());

                parameters.Add(
                    Clean(map.Name),
                    values,
                    dbType,
                    map.ParameterDirection,
                    map.Size,
                    map.IsNullable,
                    map.Precision,
                    map.Scale,
                    map.SourceColumn,
                    map.SourceVersion,
                    map.CollectionType,
                    map.ArrayBindSize);
            }

            return parameters;
        }

        private static string Clean(string name)
        {
            if (name.StartsWith("@") || name.StartsWith(":")) return name.Substring(1);

            return name;
        }
    }
}