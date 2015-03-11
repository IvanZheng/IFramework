using Sample.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Sample.CommandService.Controllers
{
    public class ApiResult
    {
        public ErrorCode ErrorCode { get; set; }
        public string Message { get; set; }
    }

    public class ApiResult<TResult> : ApiResult
    {
        public TResult Result { get; set; }

        public ApiResult() { }
        public ApiResult(TResult result)
        {
            Result = result;
        }
    }

    public static class ExceptionManager
    {
        public async static Task<ApiResult> Process(Func<Task> func)
        {
            ApiResult apiResult = null;
            try
            {
                var t = func();
                await t;
                if (t.GetType().IsGenericType)
                {
                    var result = (t as Task<object>).Result;
                    if (result != null)
                    {
                        var resultType = result.GetType();
                        var apiResultType = typeof(ApiResult<>).MakeGenericType(resultType);
                        apiResult = Activator.CreateInstance(apiResultType, result) as ApiResult;
                    }
                    else
                    {
                        apiResult = new ApiResult();
                    }
                }
                else
                {
                    apiResult = new ApiResult();
                }
            }
            catch (Exception ex)
            {
                var baseException = ex.GetBaseException();
                if (baseException is SysException)
                {
                    var sysException = baseException as SysException;
                    apiResult = new ApiResult { ErrorCode = sysException.ErrorCode, Message = sysException.Message };
                }
                else
                {
                    apiResult = new ApiResult { ErrorCode = ErrorCode.UnknownError, Message = baseException.Message };
                }
            }
            return apiResult;
        }
    }
}