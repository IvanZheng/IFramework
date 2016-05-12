using IFramework.Command;
using IFramework.Config;
using IFramework.Infrastructure;
using IFramework.SysExceptions.ErrorCodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace IFramework.AspNet
{
    public static class CommandHttpClient
    {
        public async static Task<ApiResult> PostJson<T>(this HttpClient apiClient, string requestUri, T value)
        {
            var response = await apiClient.PostAsJsonAsync(requestUri, value);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return response.Content.ReadAsAsync<ApiResult>().Result;
            }
            else
            {
                return new ApiResult(ErrorCode.HttpStatusError, response.StatusCode.ToString());
            }
        }

        public async static Task<ApiResult<TResult>> PostJson<T, TResult>(this HttpClient apiClient, string requestUri, T value)
        {
            var response = await apiClient.PostAsJsonAsync(requestUri, value);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return response.Content.ReadAsAsync<ApiResult<TResult>>().Result;
            }
            else
            {
                return new ApiResult<TResult>(ErrorCode.HttpStatusError, response.StatusCode.ToString());
            }
        }

        //static readonly string CommandActionUrlTemplate = Configuration.GetAppConfig("CommandActionUrlTemplate");    

        public async static Task<TResult> DoCommand<TResult>(this HttpClient apiClient, ICommand command, string requestUrl = "api/command")
        {
            var resposne = await apiClient.PostAsJsonAsync(command, requestUrl);
            return await resposne.Content.ReadAsAsync<TResult>();
        }


        public async static Task<TResult> DoCommand<TResult>(this HttpClient apiClient, ICommand command, TimeSpan timeout, string requestUrl = "api/command")
        {
            apiClient.Timeout = timeout;
            var response = await apiClient.PostAsJsonAsync(command, requestUrl);
            return await response.Content.ReadAsAsync<TResult>();
        }

        public async static Task<HttpResponseMessage> DoCommand(this HttpClient apiClient, ICommand command, string requestUrl = "api/command")
        {
            return await apiClient.PostAsJsonAsync(command, requestUrl);
        }

        public async static Task<HttpResponseMessage> GetAsync(this HttpClient client, string requestUrl, object request)
        {
            //解析参数
            var nameValueCollection = request.ToNameValueCollection();
            requestUrl += requestUrl.Contains("?") ? "&" : "?" + nameValueCollection.ToString();
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(client.BaseAddress, requestUrl));
            var response = await client.SendAsync(requestMessage);
            return response.EnsureSuccessStatusCode();
        }

        public async static Task<T> GetAsync<T>(this HttpClient client, string requestUrl, object request)
        {
            var response = await GetAsync(client, requestUrl, request);
            return await response.Content.ReadAsAsync<T>();
        }

        //static string GetCommandUrl(ICommand command)
        //{
        //    return string.Format(CommandActionUrlTemplate, command.GetType().Name);
        //}

        public async static Task<HttpResponseMessage> PostAsJsonAsync(this HttpClient client, ICommand command, string requestUrl = "api/command")
        {
            var mediaType = new MediaTypeWithQualityHeaderValue("application/command");
            mediaType.Parameters.Add(new NameValueHeaderValue("command",
                 HttpUtility.UrlEncode(string.Format("{0}, {1}",
                                                command.GetType().FullName,
                                                command.GetType().Assembly.GetName().Name))));
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(client.BaseAddress, requestUrl));
            requestMessage.Content = new ObjectContent(command.GetType(), command, new JsonMediaTypeFormatter(), mediaType);
            //requestMessage.Method = HttpMethod.Post;
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var response = await client.SendAsync(requestMessage);
            return response.EnsureSuccessStatusCode();
        }

        public async static Task<HttpResponseMessage> PutAsJsonAsync(this HttpClient client, ICommand command, string requestUrl = "api/command")
        {
            var mediaType = new MediaTypeWithQualityHeaderValue("application/command");
            mediaType.Parameters.Add(new NameValueHeaderValue("command",
                 HttpUtility.UrlEncode(string.Format("{0}, {1}",
                                                command.GetType().FullName,
                                                command.GetType().Assembly.GetName().Name))));
            var requestMessage = new HttpRequestMessage(HttpMethod.Put, new Uri(client.BaseAddress, requestUrl));
            requestMessage.Content = new ObjectContent(command.GetType(), command, new JsonMediaTypeFormatter(), mediaType);
            //requestMessage.Method = HttpMethod.Post;
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var response = await client.SendAsync(requestMessage);
            return response.EnsureSuccessStatusCode();
        }
    }
}
