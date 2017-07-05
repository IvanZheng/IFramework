using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using IFramework.Command;
using IFramework.Exceptions;
using IFramework.Infrastructure;
using JsonHelper = IFramework.JsonNet.JsonHelper;

namespace IFramework.AspNet
{
    public static class CommandHttpClient
    {
        public static async Task<ApiResult<T>> ApiGetAsync<T>(this HttpClient client,
                                                              string requestUrl,
                                                              object request = null)
        {
            //解析参数
            var nameValueCollection = request.ToNameValueCollection();
            requestUrl += requestUrl.Contains("?") ? "&" : "?" + nameValueCollection;
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(client.BaseAddress, requestUrl));
            var response = await client.SendAsync(requestMessage).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return await response.Content.ReadAsAsync<ApiResult<T>>().ConfigureAwait(false);
            }
            return new ApiResult<T>(ErrorCode.HttpStatusError, response.StatusCode.ToString());
        }

        public static async Task<T> GetAsync<T>(this HttpClient client, string requestUrl, object request = null)
        {
            var response = await GetAsync(client, requestUrl, request).ConfigureAwait(false);
            return await response.Content.ReadAsAsync<T>().ConfigureAwait(false);
        }

        public static async Task<ApiResult> ApiPostAsync<T>(this HttpClient apiClient, string requestUri, T value)
        {
            var response = await apiClient.PostAsJsonAsync(requestUri, value).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return response.Content.ReadAsAsync<ApiResult>().Result;
            }
            return new ApiResult(ErrorCode.HttpStatusError, response.StatusCode.ToString());
        }

        public static async Task<ApiResult<TResult>> ApiPostAsync<T, TResult>(this HttpClient apiClient,
                                                                              string requestUri,
                                                                              T value)
        {
            var response = await apiClient.PostAsJsonAsync(requestUri, value).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return await response.Content.ReadAsAsync<ApiResult<TResult>>().ConfigureAwait(false);
            }
            return new ApiResult<TResult>(ErrorCode.HttpStatusError, response.StatusCode.ToString());
        }

        public static async Task<T> PostAsync<T>(this HttpClient apiClient, string requestUri, object value)
        {
            var response = await apiClient.PostAsJsonAsync(requestUri, value).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsAsync<T>();
        }

        //static readonly string CommandActionUrlTemplate = Configuration.GetAppConfig("CommandActionUrlTemplate");    

        public static async Task<TResult> DoCommand<TResult>(this HttpClient apiClient,
                                                             ICommand command,
                                                             string requestUrl = "api/command")
        {
            var resposne = await apiClient.PostCommandAsync(command, requestUrl).ConfigureAwait(false);
            return await resposne.Content.ReadAsAsync<TResult>().ConfigureAwait(false);
        }


        public static async Task<TResult> DoCommand<TResult>(this HttpClient apiClient,
                                                             ICommand command,
                                                             TimeSpan timeout,
                                                             string requestUrl = "api/command")
        {
            apiClient.Timeout = timeout;
            var response = await apiClient.PostCommandAsync(command, requestUrl).ConfigureAwait(false);
            return await response.Content.ReadAsAsync<TResult>().ConfigureAwait(false);
        }

        public static async Task<HttpResponseMessage> DoCommand(this HttpClient apiClient,
                                                                ICommand command,
                                                                string requestUrl = "api/command")
        {
            return await apiClient.PostCommandAsync(command, requestUrl).ConfigureAwait(false);
        }

        public static async Task<HttpResponseMessage> GetAsync(this HttpClient client,
                                                               string requestUrl,
                                                               object request)
        {
            //解析参数
            var nameValueCollection = request.ToNameValueCollection();
            requestUrl += requestUrl.Contains("?") ? "&" : "?" + nameValueCollection;
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(client.BaseAddress, requestUrl));
            var response = await client.SendAsync(requestMessage).ConfigureAwait(false);
            return response.EnsureSuccessStatusCode();
        }


        //static string GetCommandUrl(ICommand command)
        //{
        //    return string.Format(CommandActionUrlTemplate, command.GetType().Name);
        //}

        public static async Task<HttpResponseMessage> PostCommandAsync(this HttpClient client,
                                                                       ICommand command,
                                                                       string requestUrl = "api/command")
        {
            var mediaType = new MediaTypeWithQualityHeaderValue("application/command");
            mediaType.Parameters.Add(new NameValueHeaderValue("command",
                                                              HttpUtility.UrlEncode(string.Format("{0}, {1}",
                                                                                                  command.GetType().FullName,
                                                                                                  command.GetType().Assembly.GetName().Name))));
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(client.BaseAddress, requestUrl));
            requestMessage.Content = new ObjectContent(command.GetType(), command, new JsonMediaTypeFormatter(),
                                                       mediaType);
            //requestMessage.Method = HttpMethod.Post;
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var response = await client.SendAsync(requestMessage).ConfigureAwait(false);
            return response.EnsureSuccessStatusCode();
        }

        public static async Task<HttpResponseMessage> PutCommandAsync(this HttpClient client,
                                                                      ICommand command,
                                                                      string requestUrl = "api/command")
        {
            var mediaType = new MediaTypeWithQualityHeaderValue("application/command");
            mediaType.Parameters.Add(new NameValueHeaderValue("command",
                                                              HttpUtility.UrlEncode(string.Format("{0}, {1}",
                                                                                                  command.GetType().FullName,
                                                                                                  command.GetType().Assembly.GetName().Name))));
            var requestMessage = new HttpRequestMessage(HttpMethod.Put, new Uri(client.BaseAddress, requestUrl));
            requestMessage.Content = new ObjectContent(command.GetType(), command, new JsonMediaTypeFormatter(),
                                                       mediaType);
            //requestMessage.Method = HttpMethod.Post;
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var response = await client.SendAsync(requestMessage).ConfigureAwait(false);
            return response.EnsureSuccessStatusCode();
        }
    }
}