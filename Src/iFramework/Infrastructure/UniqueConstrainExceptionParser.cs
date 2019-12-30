using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace IFramework.Infrastructure
{
    public class UniqueConstrainExceptionParser : IUniqueConstrainExceptionParser
    {
        private readonly Dictionary<string, Func<DbException, string[], bool>> _sqlUniqueConstrainHandlers = new Dictionary<string, Func<DbException, string[], bool>>();

        public UniqueConstrainExceptionParser()
        {
            RegisterUniqueConstrainHandler("Core Microsoft SqlClient Data Provider", (dbException, uniqueConstrainNames) =>
            {
                var number = dbException.GetPropertyValue<int>("Number");
                return (number == 2601 || number == 2627 || number == 547) &&
                       uniqueConstrainNames.Any(dbException.Message.Contains);
            });

            RegisterUniqueConstrainHandler("MySql.Data", (dbException, uniqueConstrainNames) =>
            {
                var number = dbException.GetPropertyValue<int>("Number");
                return number == 1062 &&
                       uniqueConstrainNames.Any(dbException.Message.Contains);
            });

            RegisterUniqueConstrainHandler("Npgsql", (dbException, uniqueConstrainNames) =>
            {
                var code = dbException.GetPropertyValue<int>("Code");
                var constraintName = dbException.GetPropertyValue<string>("ConstraintName");
                return code == 23505 &&
                       uniqueConstrainNames.Any(constraintName.Contains);
            });
        }

        public bool IsUniqueConstrainException(Exception exception, string[] uniqueConstrainNames)
        {
            var needRetry = false;
            if (uniqueConstrainNames?.Length > 0 && exception.GetBaseException() is DbException dbException)
            {
                var sqlSource = dbException.Source;

                var handler = _sqlUniqueConstrainHandlers.TryGetValue(sqlSource);
                if (handler != null)
                {
                    needRetry = handler(dbException, uniqueConstrainNames);
                }
            }
            return needRetry;
        }

        public void RegisterUniqueConstrainHandler(string sqlSource, Func<DbException, string[], bool> handler)
        {
            if (sqlSource == null)
            {
                throw new ArgumentNullException(nameof(sqlSource));
            }

            _sqlUniqueConstrainHandlers[sqlSource] = handler ?? throw new ArgumentNullException(nameof(handler));
        }
    }
}