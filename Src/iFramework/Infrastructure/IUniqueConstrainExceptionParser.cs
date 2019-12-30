using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace IFramework.Infrastructure
{
    public interface IUniqueConstrainExceptionParser
    {
        bool IsUniqueConstrainException(Exception exception, string[] uniqueConstrainNames);

        void RegisterUniqueConstrainHandler(string sqlSource, Func<DbException, string[], bool> handler);
    }
}
