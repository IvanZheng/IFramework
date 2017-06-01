using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using IFramework.Infrastructure;

namespace IFramework.AspNet
{
    public class IPFilterAttribute : ActionFilterAttribute
    {
        private readonly List<string> WhiteList;

        /// <summary>
        ///     restrict client request by ip
        /// </summary>
        /// <param name="entry">
        ///     entry as key in config to find while list or black list.
        ///     if entry is empty, use global config.
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
            if (IPRestrictExtension.Enabled)
            {
                var clientIP = actionContext.Request.GetClientIp();
                if (clientIP != WebApiUtility.LocalIPv4
                    && clientIP != WebApiUtility.LocalIPv6
                    && !WhiteList.Contains(clientIP))
                {
                    throw new HttpResponseException(actionContext.Request
                                                                 .CreateErrorResponse(HttpStatusCode.Forbidden,
                                                                                      WebApiUtility.ClientIPNotAllowedMessage));
                }
            }
        }
    }
}