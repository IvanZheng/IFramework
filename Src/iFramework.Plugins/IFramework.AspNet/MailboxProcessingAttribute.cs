using IFramework.Infrastructure;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IFramework.Infrastructure.Mailboxes;

namespace IFramework.AspNet
{
    public class MailboxProcessingAttribute: ActionFilterAttribute
    {
        private readonly string _keyArgumentName;
        private readonly string _keyPropertyName;

        public MailboxProcessingAttribute(string keyArgumentName, string keyPropertyName)
        {
            if (string.IsNullOrWhiteSpace(keyArgumentName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(keyArgumentName));
            }

            if (string.IsNullOrWhiteSpace(keyPropertyName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(keyPropertyName));
            }

            _keyArgumentName = keyArgumentName;
            _keyPropertyName = keyPropertyName;
        }

        public override Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var keyArgument = context.ActionArguments.TryGetValue(_keyArgumentName);
            var key = keyArgument?.GetPropertyValue(_keyPropertyName)?.ToString();
            if (string.IsNullOrWhiteSpace(key))
            {
                return base.OnActionExecutionAsync(context, next);
            }
            else
            {
                return context.HttpContext
                              .RequestServices
                              .GetService<IMailboxProcessor>()
                              .Process(key, () => base.OnActionExecutionAsync(context, next));
            }
        }
    }
}
