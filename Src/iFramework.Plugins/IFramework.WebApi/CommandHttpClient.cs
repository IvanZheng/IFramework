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
        private static readonly JsonMediaTypeFormatter[] DefaultJsonMediaTypeFormatters = {
            new JsonMediaTypeFormatter
            {
                SerializerSettings = JsonHelper.GetCustomJsonSerializerSettings(true, false, true)
            }
        };

        public static async Task<ApiResult<T>> ApiGetAsync<T>(this HttpClient client, string requestUrl,
                                                              object request = null,
                                                              MediaTypeFormatter[] responseFormatters = null)
        {
            //解析参数
            var nameValueCollection = request.ToNameValueCollection();
            requestUrl += requestUrl.Contains("?") ? "&" : "?" + nameValueCollection;
            var requestUri = client.BaseAddress == null ? new Uri(requestUrl) : new Uri(client.BaseAddress, requestUrl);
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
            var response = await client.SendAsync(requestMessage).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.OK)
                return await response.Content.ReadAsAsync<ApiResult<T>>(responseFormatters ?? DefaultJsonMediaTypeFormatters).ConfigureAwait(false);
            return new ApiResult<T>(ErrorCode.HttpStatusError, response.StatusCode.ToString());
        }

        public static async Task<T> GetAsync<T>(this HttpClient client, string requestUrl,
                                                object request = null, MediaTypeFormatter[] formatters = null)
        {
            var response = await GetAsync(client, requestUrl, request).ConfigureAwait(false);
            return await response.Content.ReadAsAsync<T>(formatters ?? DefaultJsonMediaTypeFormatters).ConfigureAwait(false);
        }

        public static async Task<ApiResult> ApiPostAsync<T>(this HttpClient apiClient,
                                                            string requestUri, T value,
                                                            MediaTypeFormatter requsetFormatter = null,
                                                            MediaTypeFormatter[] responseFormatters = null)
        {
            var response = await apiClient.PostAsync(requestUri, value, requsetFormatter ?? DefaultJsonMediaTypeFormatters[0]).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.OK)
                return response.Content.ReadAsAsync<ApiResult>(responseFormatters ?? DefaultJsonMediaTypeFormatters).Result;
            return new ApiResult(ErrorCode.HttpStatusError, response.StatusCode.ToString());
        }

        public static async Task<ApiResult<TResult>> ApiPostAsync<T, TResult>(this HttpClient apiClient,
                                                                              string requestUri, T value,
                                                                              MediaTypeFormatter requsetFormatter = null,
                                                                              MediaTypeFormatter[] responseFormatters = null)
        {
            var response = await apiClient.PostAsync(requestUri, value, requsetFormatter ?? DefaultJsonMediaTypeFormatters[0]).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.OK)
                return await response.Content.ReadAsAsync<ApiResult<TResult>>(responseFormatters ?? DefaultJsonMediaTypeFormatters).ConfigureAwait(false);
            return new ApiResult<TResult>(ErrorCode.HttpStatusError, response.StatusCode.ToString());
        }

        public static async Task<T> PostAsync<T>(this HttpClient apiClient, string requestUri, object value,
                                                 MediaTypeFormatter requsetFormatter = null,
                                                 MediaTypeFormatter[] responseFormatters = null)
        {
            var response = await apiClient.PostAsync(requestUri, value, requsetFormatter ?? DefaultJsonMediaTypeFormatters[0]).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsAsync<T>(responseFormatters ?? DefaultJsonMediaTypeFormatters);
        }

        //static readonly string CommandActionUrlTemplate = Configuration.GetAppConfig("CommandActionUrlTemplate");    

        public static async Task<TResult> DoCommand<TResult>(this HttpClient apiClient, ICommand command,
                                                             string requestUrl = "api/command",
                                                             MediaTypeFormatter requestFormatter = null,
                                                             MediaTypeFormatter[] responseFormatters = null)
        {
            var resposne = await apiClient.PostCommandAsync(command, requestUrl).ConfigureAwait(false);
            return await resposne.Content.ReadAsAsync<TResult>(responseFormatters ?? DefaultJsonMediaTypeFormatters).ConfigureAwait(false);
        }


        public static async Task<TResult> DoCommand<TResult>(this HttpClient apiClient, ICommand command,
                                                             TimeSpan timeout, string requestUrl = "api/command", MediaTypeFormatter[] responseFormatters = null)
        {
            apiClient.Timeout = timeout;
            var response = await apiClient.PostCommandAsync(command, requestUrl).ConfigureAwait(false);
            return await response.Content.ReadAsAsync<TResult>(responseFormatters ?? DefaultJsonMediaTypeFormatters).ConfigureAwait(false);
        }

        public static async Task<HttpResponseMessage> DoCommand(this HttpClient apiClient, ICommand command,
                                                                string requestUrl = "api/command",
                                                                MediaTypeFormatter requestFormatter = null)
        {
            return await apiClient.PostCommandAsync(command, requestUrl, requestFormatter).ConfigureAwait(false);
        }

        public static async Task<HttpResponseMessage> GetAsync(this HttpClient client, string requestUrl,
                                                               object request)
        {
            //解析参数
            var nameValueCollection = request.ToNameValueCollection();
            requestUrl += requestUrl.Contains("?") ? "&" : "?" + nameValueCollection;
            var requestUri = client.BaseAddress == null ? new Uri(requestUrl) : new Uri(client.BaseAddress, requestUrl);
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
            var response = await client.SendAsync(requestMessage).ConfigureAwait(false);
            return response.EnsureSuccessStatusCode();
        }


        //static string GetCommandUrl(ICommand command)
        //{
        //    return string.Format(CommandActionUrlTemplate, command.GetType().Name);
        //}

        public static async Task<HttpResponseMessage> PostCommandAsync(this HttpClient client,
                                                                      ICommand command,
                                                                       string requestUrl = "api/command",
                                                                       MediaTypeFormatter requestFormatter = null)
        {
            var mediaType = new MediaTypeWithQualityHeaderValue("application/command");
            mediaType.Parameters.Add(new NameValueHeaderValue("command",
                                                              HttpUtility.UrlEncode($"{command.GetType().FullName}, {command.GetType().Assembly.GetName().Name}")));

            var requestUri = client.BaseAddress == null ? new Uri(requestUrl) : new Uri(client.BaseAddress, requestUrl);
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri);
            requestMessage.Content = new ObjectContent(command.GetType(), command, requestFormatter ?? DefaultJsonMediaTypeFormatters[0],
                                                       mediaType);
            //requestMessage.Method = HttpMethod.Post;
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var response = await client.SendAsync(requestMessage).ConfigureAwait(false);
            return response.EnsureSuccessStatusCode();
        }

        public static async Task<HttpResponseMessage> PutCommandAsync(this HttpClient client, ICommand command,
                                                                      string requestUrl = "api/command",
                                                                      MediaTypeFormatter requestFormatter = null)
        {
            var mediaType = new MediaTypeWithQualityHeaderValue("application/command");
            mediaType.Parameters.Add(new NameValueHeaderValue("command",
                                                              HttpUtility.UrlEncode($"{command.GetType().FullName}, {command.GetType().Assembly.GetName().Name}")));
            var requestUri = client.BaseAddress == null ? new Uri(requestUrl) : new Uri(client.BaseAddress, requestUrl);
            var requestMessage = new HttpRequestMessage(HttpMethod.Put, requestUri);
            requestMessage.Content = new ObjectContent(command.GetType(), command, requestFormatter ?? DefaultJsonMediaTypeFormatters[0],
                                                       mediaType);
            //requestMessage.Method = HttpMethod.Post;
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var response = await client.SendAsync(requestMessage).ConfigureAwait(false);
            return response.EnsureSuccessStatusCode();
        }
    }
}