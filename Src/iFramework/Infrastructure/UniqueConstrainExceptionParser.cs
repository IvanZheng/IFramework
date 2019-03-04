using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace IFramework.Infrastructure
{
    public class UniqueConstrainExceptionParser: IUniqueConstrainExceptionParser
    {
        public bool IsUniqueConstrainException(Exception exception, string[] uniqueConstrainNames)
        {
            var needRetry = false;
            if (uniqueConstrainNames?.Length > 0 && exception.GetBaseException() is DbException dbException)
            {
                var number = dbException.GetPropertyValue<int>("Number");
                needRetry = (dbException.Source.Contains("MySql") && number == 1062 ||
                             dbException.Source.Contains("SqlClient") && (number == 2601 || number == 2627 || number == 547) ||
                             dbException.Source.Contains("Npgsql") && number == 23505) &&
                            uniqueConstrainNames.Any(dbException.Message.Contains);
            }

            return needRetry;
        }
    }
}
