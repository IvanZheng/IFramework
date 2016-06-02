
using IFramework.Infrastructure.Logging;
using IFramework.SysExceptions;
using IFramework.SysExceptions.ErrorCodes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace IFramework.Infrastructure
{
    public class ApiResult
    {
        public bool success { get; set; }
        public object errorCode { get; set; }
        public string message { get; set; }

        public ApiResult()
        {
            success = true;
            errorCode = 0;
        }

        public ApiResult(object errorCode, string message = null)
        {
            this.errorCode = errorCode;
            this.message = message;
            success = false;
        }

    }

    public class ApiResult<TResult> : ApiResult
    {
        public TResult result { get; set; }

        public ApiResult()
        {
            success = true;
        }
        public ApiResult(TResult result)
            : this()
        {
            this.result = result;
        }

        public ApiResult(object errorCode, string message = null)
            : base(errorCode, message)
        {

        }
    }

    public static class ExceptionManager
    {
        static ILogger _logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(ExceptionManager));
        public static async Task<ApiResult<T>> ProcessAsync<T>(Func<Task<T>> func, bool continueOnCapturedContext = false, bool needRetry = false, int retryCount = 50)
        {
            ApiResult<T> apiResult = null;
            do
            {
                try
                {
                    var result = await func().ConfigureAwait(continueOnCapturedContext);
                    apiResult = new ApiResult<T>(result);
                    needRetry = false;
                }
                catch (Exception ex)
                {
                    if (!(ex.GetBaseException() is OptimisticConcurrencyException) || !needRetry)
                    {
                        var baseException = ex.GetBaseException();
                        if (baseException is SysException)
                        {
                            var sysException = baseException as SysException;
                            apiResult = new ApiResult<T>(sysException.ErrorCode, sysException.Message);
                            _logger.Debug(ex);
                        }
                        else
                        {
                            apiResult = new ApiResult<T>(ErrorCode.UnknownError, baseException.Message);
                            _logger.Error(ex);
                        }
                        needRetry = false;
                    }
                }
            } while (needRetry && retryCount-- > 0);
            return apiResult;

            #region Old Method for .net 4
            /*
             * old method for .net 4
            return func().ContinueWith<Task<ApiResult<T>>>(t =>
            {
                ApiResult<T> apiResult = null;
                try
                {
                    if (t.IsFaulted)
                    {
                        throw t.Exception.GetBaseException();
                    }
                    needRetry = false;
                    apiResult = new ApiResult<T>(t.Result);
                }
                catch (Exception ex)
                {
                    if (!(ex is OptimisticConcurrencyException) || !needRetry)
                    {
                        var baseException = ex.GetBaseException();
                        if (baseException is SysException)
                        {
                            var sysException = baseException as SysException;
                            apiResult = new ApiResult<T>(sysException.ErrorCode, sysException.Message);
                        }
                        else
                        {
                            apiResult = new ApiResult<T>(ErrorCode.UnknownError, baseException.Message);
                            _logger.Error(ex);
                        }
                        needRetry = false;
                    }
                }
                if (needRetry)
                {
                    return ProcessAsync(func, needRetry);
                }

                return Task.FromResult(apiResult);
            }).Unwrap();
            */
            #endregion
        }

        public static async Task<ApiResult> ProcessAsync(Func<Task> func, bool continueOnCapturedContext = false, bool needRetry = false, int retryCount = 50)
        {
            ApiResult apiResult = null;
            do
            {
                try
                {
                    await func().ConfigureAwait(continueOnCapturedContext);
                    needRetry = false;
                }
                catch (Exception ex)
                {
                    if (!(ex.GetBaseException() is OptimisticConcurrencyException) || !needRetry)
                    {
                        var baseException = ex.GetBaseException();
                        if (baseException is SysException)
                        {
                            var sysException = baseException as SysException;
                            apiResult = new ApiResult(sysException.ErrorCode, sysException.Message);
                        }
                        else
                        {
                            apiResult = new ApiResult(ErrorCode.UnknownError, baseException.Message);
                            _logger.Error(ex);
                        }
                        needRetry = false;
                    }
                }
            } while (needRetry && retryCount-- > 0);
            return apiResult;

            #region Old Method for .net 4
            //return func().ContinueWith<Task<ApiResult>>(t =>
            //{
            //    ApiResult apiResult = null;
            //    try
            //    {
            //        if (t.IsFaulted)
            //        {
            //            throw t.Exception.GetBaseException();
            //        }
            //        needRetry = false;
            //        apiResult = new ApiResult();
            //    }
            //    catch (Exception ex)
            //    {
            //        if (!(ex is OptimisticConcurrencyException) || !needRetry)
            //        {
            //            var baseException = ex.GetBaseException();
            //            if (baseException is SysException)
            //            {
            //                var sysException = baseException as SysException;
            //                apiResult = new ApiResult(sysException.ErrorCode, sysException.Message);
            //            }
            //            else
            //            {
            //                apiResult = new ApiResult(ErrorCode.UnknownError, baseException.Message);
            //                _logger.Error(ex);
            //            }
            //            needRetry = false;
            //        }
            //    }
            //    if (needRetry)
            //    {
            //        return ProcessAsync(func, needRetry);
            //    }

            //    return Task.FromResult(apiResult);
            //}).Unwrap();
            #endregion
        }

        public static ApiResult Process(Action action, bool needRetry = false, int retryCount = 50)
        {
            ApiResult apiResult = null;
            do
            {
                try
                {
                    action();
                    apiResult = new ApiResult();
                    needRetry = false;
                }
                catch (Exception ex)
                {
                    if (!(ex is OptimisticConcurrencyException) || !needRetry)
                    {
                        var baseException = ex.GetBaseException();
                        if (baseException is SysException)
                        {
                            var sysException = baseException as SysException;
                            apiResult = new ApiResult(sysException.ErrorCode, sysException.Message);
                        }
                        else
                        {
                            apiResult = new ApiResult(ErrorCode.UnknownError, baseException.Message);
                            _logger.Error(ex);
                        }
                        needRetry = false;
                    }
                }
            }
            while (needRetry && retryCount -- > 0);
            return apiResult;
        }

        public static ApiResult<T> Process<T>(Func<T> func, bool needRetry = false, int retryCount = 50)
        {
            ApiResult<T> apiResult = null;
            do
            {
                try
                {
                    var result = func();
                    needRetry = false;
                    if (result != null)
                    {
                        apiResult = new ApiResult<T>(result);
                    }
                    else
                    {
                        apiResult = new ApiResult<T>();
                    }
                }
                catch (Exception ex)
                {
                    if (!(ex is OptimisticConcurrencyException) || !needRetry)
                    {
                        var baseException = ex.GetBaseException();
                        if (baseException is SysException)
                        {
                            var sysException = baseException as SysException;
                            apiResult = new ApiResult<T>(sysException.ErrorCode, sysException.Message);
                        }
                        else
                        {
                            apiResult = new ApiResult<T>(ErrorCode.UnknownError, baseException.Message);
                            _logger.Error(ex);
                        }
                        needRetry = false;
                    }
                }
            }
            while (needRetry && retryCount -- > 0);
            return apiResult;
        }
    }
}