using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using IFramework.Infrastructure;

namespace IFramework.AspNet
{
    public class IpFilterAttribute : ActionFilterAttribute
    {
        private readonly string _entry;

        public IpFilterAttribute(string entry = null)
        {
            _entry = entry;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            var option = context.HttpContext
                                .RequestServices
                                .GetService<IOptions<IpFilterOption>>()
                                ?.Value ?? new IpFilterOption();
            if (option.Enabled)
            {
                string clientIp = context.HttpContext.Request.GetClientIp();
                var whiteList = GetWhileList(option);
                if (clientIp != "127.0.0.1" && clientIp != "::1" && !whiteList.Contains(clientIp))
                {
                    throw new HttpException(HttpStatusCode.Forbidden, $"Client IP {clientIp} is not allowed!");
                }
            }
        }

        private List<string> _whiteList;
        private List<string> GetWhileList(IpFilterOption option)
        {
            if (_whiteList == null)
            {
                if (string.IsNullOrEmpty(_entry))
                {
                    _whiteList = option.GlobalWhiteList;
                }
                else
                {
                    _whiteList = option.EntryWhiteListDictionary.TryGetValue(_entry, null);
                }
                _whiteList = (_whiteList ?? new List<string>());
            }
            return _whiteList;
        }
    }
}