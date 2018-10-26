using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sample.CommandServiceCore.Filters
{
    public class ApiResultWrapAttribute: IFramework.AspNet.ApiResultWrapAttribute
    {
        public override Exception OnException(Exception ex)
        {
            return base.OnException(ex);
        }
    }
}
