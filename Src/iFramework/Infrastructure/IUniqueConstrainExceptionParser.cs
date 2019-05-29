using System;
using System.Collections.Generic;
using System.Text;

namespace IFramework.Infrastructure
{
    public interface IUniqueConstrainExceptionParser
    {
        bool IsUniqueConstrainException(Exception exception, string[] uniqueConstrainNames);
    }
}
