using IFramework.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Results;
using System.Net.Http.Headers;
using System.Threading;
using System.Web.Http.Controllers;
using IFramework.Infrastructure;
using System.Web.Http.ModelBinding;

namespace IFramework.AspNet
{
    public class ApiControllerBase : ApiController
    {

        protected string GetModelErrorMessage(ModelStateDictionary modelState)
        {
            var isDeubg = Config.Configuration.GetCompliationSection()?.Debug ?? false;
            return string.Join(";",
                               modelState.Where(m => (m.Value?.Errors?.Count ?? 0) > 0)
                                         .Select(m => $"{m.Key}:{(isDeubg ? string.Join(",", m.Value.Errors.Select(e => e.ErrorMessage + e.Exception?.Message)) : "invalid")}"));
        }

        #region process wrapping
        protected ApiResult<T> Process<T>(Func<T> func, bool needRetry = true,
            Func<Exception, string> getExceptionMessage = null,
            Func<ModelStateDictionary, string> getModelErrorMessage = null)
        {
            if (ModelState.IsValid)
            {
                var apiResult = ExceptionManager.Process<T>(func, needRetry, getExceptionMessage: getExceptionMessage);
                return apiResult;
            }
            else
            {
                getModelErrorMessage = getModelErrorMessage ?? GetModelErrorMessage;
                return
                    new ApiResult<T>
                    (
                        ErrorCode.InvalidParameters,
                        getModelErrorMessage(ModelState)
                    );
            }
        }
        protected ApiResult Process(Action action, bool needRetry = true,
            Func<Exception, string> getExceptionMessage = null,
            Func<ModelStateDictionary, string> getModelErrorMessage = null)
        {
            if (ModelState.IsValid)
            {
                var apiResult = ExceptionManager.Process(action, needRetry, getExceptionMessage: getExceptionMessage);
                return apiResult;
            }
            else
            {
                getModelErrorMessage = getModelErrorMessage ?? GetModelErrorMessage;
                return
                    new ApiResult
                    (
                        ErrorCode.InvalidParameters,
                        getModelErrorMessage(ModelState)
                    );
            }
        }
        List<CookieHeaderValue> _cookies;
        protected void AddCookies(params CookieHeaderValue[] cookies)
        {
            if (_cookies == null)
            {
                _cookies = new List<CookieHeaderValue>();
            }
            _cookies.AddRange(cookies);
        }

        protected string TryGetCookie(string key, string defaultValue)
        {
            try
            {
                var value = defaultValue;
                CookieHeaderValue cookie = Request.Headers.GetCookies(key).FirstOrDefault();
                if (cookie != null)
                {
                    value = cookie[key].Value;
                }
                return value;
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        protected void RemoveCookies(string key)
        {
            var cookies = Request.Headers.GetCookies(key).ToList();
            cookies.ForEach(c => c.Expires = DateTimeOffset.Now.AddDays(-1));
            AddCookies(cookies.ToArray());
        }

        public override Task<HttpResponseMessage> ExecuteAsync(HttpControllerContext controllerContext, CancellationToken cancellationToken)
        {
            return base.ExecuteAsync(controllerContext, cancellationToken)
                .ContinueWith(t =>
                {
                    if (_cookies != null && _cookies.Count > 0)
                    {
                        t.Result.Headers.AddCookies(_cookies);
                    }
                    return t.Result;
                }); ;
        }


        protected async Task<ApiResult> ProcessAsync(Func<Task> func,
                                                     bool continueOnCapturedContext = false,
                                                     bool needRetry = true,
                                                     Func<Exception, string> getExceptionMessage = null,
                                                     Func<ModelStateDictionary, string> getModelErrorMessage = null)
        {
            if (ModelState.IsValid)
            {
                return await ExceptionManager.ProcessAsync(func,
                                                           continueOnCapturedContext,
                                                           needRetry,
                                                           getExceptionMessage: getExceptionMessage)
                                             .ConfigureAwait(continueOnCapturedContext);
            }
            else
            {
                getModelErrorMessage = getModelErrorMessage ?? GetModelErrorMessage;
                return
                    new ApiResult
                    (
                        ErrorCode.InvalidParameters,
                        getModelErrorMessage(ModelState)
                    );
            }
        }

        protected async Task<ApiResult<T>> ProcessAsync<T>(Func<Task<T>> func,
                                                           bool continueOnCapturedContext = false,
                                                           bool needRetry = true,
                                                           Func<Exception, string> getExceptionMessage = null,
                                                           Func<ModelStateDictionary, string> getModelErrorMessage = null)
        {
            if (ModelState.IsValid)
            {
                return await ExceptionManager.ProcessAsync(func, continueOnCapturedContext, needRetry,
                                                           getExceptionMessage: getExceptionMessage)
                                             .ConfigureAwait(continueOnCapturedContext);
            }
            else
            {
                getModelErrorMessage = getModelErrorMessage ?? GetModelErrorMessage;
                return
                    new ApiResult<T>
                    (
                        ErrorCode.InvalidParameters,
                        getModelErrorMessage(ModelState)
                    );
            }

        }

        protected string GetClientIp(HttpRequestMessage request = null)
        {
            request = request ?? Request;

            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                return ((HttpContextWrapper)request.Properties["MS_HttpContext"]).Request.UserHostAddress;
            }
            else if (request.Properties.ContainsKey(RemoteEndpointMessageProperty.Name))
            {
                RemoteEndpointMessageProperty prop = (RemoteEndpointMessageProperty)request.Properties[RemoteEndpointMessageProperty.Name];
                return prop.Address;
            }
            else if (HttpContext.Current != null)
            {
                return HttpContext.Current.Request.UserHostAddress;
            }
            else
            {
                return null;
            }
        }
        #endregion
    }
}