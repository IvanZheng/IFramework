using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Mailboxes;

namespace IFramework.DependencyInjection
{
    public class MailboxProcessingAttribute : InterceptorAttribute
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

        private string GetKey(MethodInfo method, object[] arguments)
        {
            var parameter = method.GetParameters().FirstOrDefault(p => p.Name == _keyArgumentName);
            if (parameter != null && arguments != null && arguments.Length > parameter.Position)
            {
                return arguments[parameter.Position]?.GetPropertyValue(_keyPropertyName)?.ToString();
            }

            return null;
        }

        public override Task ProcessAsync(Func<Task> funcAsync, IObjectProvider objectProvider, Type targetType, object invocationTarget, MethodInfo method, object[] arguments)
        {
            var key = GetKey(method, arguments);
            if (string.IsNullOrWhiteSpace(key))
            {
                return funcAsync();
            }

            var mailboxProcessor = objectProvider.GetService<IMailboxProcessor>();
            return mailboxProcessor.Process(key, funcAsync);
        }

        public override async Task<T> ProcessAsync<T>(Func<Task<T>> funcAsync, IObjectProvider objectProvider, Type targetType, object invocationTarget, MethodInfo method, object[] arguments)
        {
            var key = GetKey(method, arguments);
            if (string.IsNullOrWhiteSpace(key))
            {
                return await funcAsync().ConfigureAwait(false);
            }

            var mailboxProcessor = objectProvider.GetService<IMailboxProcessor>();
            T result = default;
            await mailboxProcessor.Process(key, async () =>
            {
                result = await funcAsync().ConfigureAwait(false); 
            });
            return result;
        }

        public override object Process(Func<dynamic> func, IObjectProvider objectProvider, Type targetType, object invocationTarget, MethodInfo method, object[] arguments)
        {
            var key = GetKey(method, arguments);
            if (string.IsNullOrWhiteSpace(key))
            {
                return func();
            }

            var mailboxProcessor = objectProvider.GetService<IMailboxProcessor>();
            object result = null;
            mailboxProcessor.Process(key, () =>
                            {
                                result = func();
                                return Task.CompletedTask;
                            })
                            .Wait();
            return result;
        }

        public override void Process(Action func, IObjectProvider objectProvider, Type targetType, object invocationTarget, MethodInfo method, object[] arguments)
        {
            var key = GetKey(method, arguments);
            if (string.IsNullOrWhiteSpace(key))
            {
                func();
            }
            else
            {
                var mailboxProcessor = objectProvider.GetService<IMailboxProcessor>();
                mailboxProcessor.Process(key, () =>
                {
                    func();
                    return Task.CompletedTask;
                });
            }
        }
    }
}