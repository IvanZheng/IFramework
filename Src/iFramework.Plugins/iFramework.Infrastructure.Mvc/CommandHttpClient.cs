using IFramework.Command;
using IFramework.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Infrastructure.Mvc
{
    public static class CommandHttpClient
    {
        public static TResult DoCommand<TResult>(this HttpClient apiClient, ICommand command)
        {
            return apiClient.PostAsJsonAsync(command)
                            .Result.Content
                            .ReadAsAsync<TResult>()
                            .Result;
        }

        public static Task<HttpResponseMessage> DoCommand(this HttpClient apiClient, ICommand command)
        {
            return apiClient.PostAsJsonAsync(command);
        }

        static string GetCommandUrl(ICommand command)
        {
            return string.Format(Configuration.GetAppConfig("CommandActionUrlTemplate"), command.GetType().Name);
        }

        public static Task<HttpResponseMessage> PostAsJsonAsync(this HttpClient client, ICommand command)
        {
            return client.PostAsJsonAsync(GetCommandUrl(command), command);
        }
    }
}
