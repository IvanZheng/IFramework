using IFramework.SysExceptions;
using IFramework.SysExceptions.ErrorCodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;

namespace IFramework.WebApi
{
    public class ApiControllerBase : ApiController
    {
        #region process wrapping
        protected ApiResult<T> Process<T>(Func<T> func)
        {
            if (ModelState.IsValid)
            {
                var apiResult = ExceptionManager.Process<T>(func);
                return apiResult;
            }
            else
            {
                return
                    new ApiResult<T>
                    (
                        ErrorCode.InvalidParameters,
                       string.Join(",", ModelState.Values
                                                       .SelectMany(v => v.Errors
                                                                         .Select(e => e.ErrorMessage)))
                    );
            }
        }
        protected ApiResult Process(Action action)
        {
            if (ModelState.IsValid)
            {
                var apiResult = ExceptionManager.Process(action);
                return apiResult;
            }
            else
            {
                return
                    new ApiResult
                    (
                        ErrorCode.InvalidParameters,
                        string.Join(",", ModelState.Values
                                                       .SelectMany(v => v.Errors
                                                                         .Select(e => e.ErrorMessage)))
                    );
            }
        }


        protected async Task<ApiResult> ProcessAsync(Func<Task> func)
        {
            if (ModelState.IsValid)
            {
                return await ExceptionManager.ProcessAsync(func);
            }
            else
            {
                return
                    new ApiResult
                    (
                        ErrorCode.InvalidParameters,
                        string.Join(",", ModelState.Values
                                                       .SelectMany(v => v.Errors
                                                                         .Select(e => e.ErrorMessage)))
                    );
            }
        }

        protected async Task<ApiResult<T>> ProcessAsync<T>(Func<Task<T>> func)
        {
            if (ModelState.IsValid)
            {
                return await ExceptionManager.ProcessAsync(func);
            }
            else
            {
                return
                   new ApiResult<T>
                   (
                      ErrorCode.InvalidParameters,
                      string.Join(",", ModelState.Values
                                                      .SelectMany(v => v.Errors
                                                                        .Select(e => e.ErrorMessage)))
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