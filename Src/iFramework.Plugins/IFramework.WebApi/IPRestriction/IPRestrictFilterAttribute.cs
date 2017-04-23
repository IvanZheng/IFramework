using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using IFramework.Infrastructure;
using System.Web.Http;
using System.Net.Http;
using System.Net;

namespace IFramework.AspNet
{
    public class IPFilterAttribute : ActionFilterAttribute
    {
        List<string> WhiteList = null;
        /// <summary>
        /// restrict client request by ip
        /// </summary>
        /// <param name="entry">entry as key in config to find while list or black list.
        /// if entry is empty, use global config.
        /// </param>
        public IPFilterAttribute(string entry = null)
        {
            if (string.IsNullOrEmpty(entry))
            {
                WhiteList = IPRestrictExtension.IPRestrictConfig?.GlobalWhiteList;
            }
            else
            {
                WhiteList = IPRestrictExtension.IPRestrictConfig
                                               .EntryWhiteListDictionary
                                               .TryGetValue(entry, null);
            }
            WhiteList = WhiteList ?? new List<string>();
        }

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            base.OnActionExecuting(actionContext);
            var clientIP = actionContext.Request.GetClientIp();
            if (clientIP != "::1" && clientIP != "127.0.0.1" && !WhiteList.Contains(clientIP))
            {
                throw new HttpResponseException(actionContext.Request
                                                             .CreateErrorResponse(HttpStatusCode.Forbidden,
                                                                                  "Client IP is not allowed!"));
            }
        }
    }
}
