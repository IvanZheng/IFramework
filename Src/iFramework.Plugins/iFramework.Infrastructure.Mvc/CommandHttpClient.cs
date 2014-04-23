using IFramework.Command;
using IFramework.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace IFramework.Infrastructure.Mvc
{
    public static class CommandHttpClient
    {
        //static readonly string CommandActionUrlTemplate = Configuration.GetAppConfig("CommandActionUrlTemplate");    

        public static Task<TResult> DoCommand<TResult>(this HttpClient apiClient, ICommand command, string commandUrl = null)
        {
            return apiClient.PostAsJsonAsync(command, commandUrl)
                            .Result.Content
                            .ReadAsAsync<TResult>();
        }


        public static Task<TResult> DoCommand<TResult>(this HttpClient apiClient, ICommand command, TimeSpan timeout, string commandUrl = null)
        {
            return apiClient.PostAsJsonAsync(command, commandUrl)
                            .Result.Content
                            .ReadAsAsync<TResult>()
                            .Timeout(timeout);
        }

        public static Task<HttpResponseMessage> DoCommand(this HttpClient apiClient, ICommand command, string commandUrl = null)
        {
            return apiClient.PostAsJsonAsync(command, commandUrl);
        }

        //static string GetCommandUrl(ICommand command)
        //{
        //    return string.Format(CommandActionUrlTemplate, command.GetType().Name);
        //}

        public static Task<HttpResponseMessage> PostAsJsonAsync(this HttpClient client, ICommand command, string commandUrl = null)
        {
            var mediaType = new MediaTypeWithQualityHeaderValue("application/command");
            mediaType.Parameters.Add(new NameValueHeaderValue("command",
                 HttpUtility.UrlEncode(string.Format("{0}, {1}",
                                                command.GetType().FullName,
                                                command.GetType().Assembly.GetName().Name))));
            var requestMessage = new HttpRequestMessage();
            requestMessage.Content = new ObjectContent(command.GetType(), command, new JsonMediaTypeFormatter(), mediaType);        
            requestMessage.Method = HttpMethod.Post;
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client.SendAsync(requestMessage);
        }
    }
}
