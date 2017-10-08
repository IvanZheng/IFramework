using System;
using System.Data;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.Exceptions;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;

namespace IFramework.Infrastructure
{
    /// <summary>
    /// API 响应结果
    /// </summary>
    public class ApiResult
    {
        public ApiResult()
        {
            Success = true;
            ErrorCode = 0;
        }

        public ApiResult(int errorCode, string message = null)
        {
            ErrorCode = errorCode;
            Message = message;
            Success = false;
        }

        /// <summary>
        /// API 执行是否成功
        /// </summary>
        public bool Success { get; set; }
        /// <summary>
        /// ErrorCode 为 0 表示执行无异常
        /// </summary>
        public int ErrorCode { get; set; }
        /// <summary>
        /// 当API执行有异常时, 对应的错误信息
        /// </summary>
        public string Message { get; set; }
    }

    /// <inheritdoc />
    /// <summary>
    /// Api返回结果
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    public class ApiResult<TResult> : ApiResult
    {
        public ApiResult()
        {
            Success = true;
        }

        public ApiResult(TResult result)
            : this()
        {
            Result = result;
        }

        public ApiResult(int errorCode, string message = null)
            : base(errorCode, message) { }

        /// <summary>
        /// API 执行返回的结果
        /// </summary>
        public TResult Result { get; set; }
    }

    public static class ExceptionManager
    {
        private static readonly ILogger Logger = IoCFactory.IsInit()
                                                      ? IoCFactory.Resolve<ILoggerFactory>().Create(typeof(ExceptionManager))
                                                      : null;

        private static string _unKnownMessage = ErrorCode.UnknownError.ToString();

        public static void SetUnKnownMessage(string unknownMessage)
        {
            _unKnownMessage = unknownMessage;
        }

        private static string GetExceptionMessage(Exception ex)
        {
            return ex.GetBaseException().Message;
        }

        public static async Task<ApiResult<T>> ProcessAsync<T>(Func<Task<T>> func,
                                                               bool continueOnCapturedContext = false,
                                                               bool needRetry = false,
                                                               int retryCount = 50,
                                                               Func<Exception, string> getExceptionMessage = null)
        {
            ApiResult<T> apiResult = null;
            getExceptionMessage = getExceptionMessage ?? GetExceptionMessage;

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
                    if (!(ex is OptimisticConcurrencyException) || !needRetry)
                    {
                        var baseException = ex.GetBaseException();
                        if (baseException is DomainException)
                        {
                            var sysException = baseException as DomainException;
                            apiResult = new ApiResult<T>(sysException.ErrorCode, getExceptionMessage(sysException));
                            Logger?.Warn(ex);
                        }
                        else
                        {
                            apiResult = new ApiResult<T>(ErrorCode.UnknownError, getExceptionMessage(ex));
                            Logger?.Error(ex);
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

        public static async Task<ApiResult> ProcessAsync(Func<Task> func,
                                                         bool continueOnCapturedContext = false,
                                                         bool needRetry = false,
                                                         int retryCount = 50,
                                                         Func<Exception, string> getExceptionMessage = null)
        {
            getExceptionMessage = getExceptionMessage ?? GetExceptionMessage;
            ApiResult apiResult = null;
            do
            {
                try
                {
                    await func().ConfigureAwait(continueOnCapturedContext);
                    needRetry = false;
                    apiResult = new ApiResult();
                }
                catch (Exception ex)
                {
                    if (!(ex is OptimisticConcurrencyException) || !needRetry)
                    {
                        var baseException = ex.GetBaseException();
                        if (baseException is DomainException)
                        {
                            var sysException = baseException as DomainException;
                            apiResult = new ApiResult(sysException.ErrorCode, getExceptionMessage(sysException));
                            Logger?.Warn(ex);
                        }
                        else
                        {
                            apiResult = new ApiResult(ErrorCode.UnknownError, getExceptionMessage(ex));
                            Logger?.Error(ex);
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

        public static ApiResult Process(Action action,
                                        bool needRetry = false,
                                        int retryCount = 50,
                                        Func<Exception, string> getExceptionMessage = null)
        {
            ApiResult apiResult = null;
            getExceptionMessage = getExceptionMessage ?? GetExceptionMessage;
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
                        if (baseException is DomainException)
                        {
                            var sysException = baseException as DomainException;
                            apiResult = new ApiResult(sysException.ErrorCode, getExceptionMessage(sysException));
                            Logger?.Warn(ex);
                        }
                        else
                        {
                            apiResult = new ApiResult(ErrorCode.UnknownError, getExceptionMessage(ex));
                            Logger?.Error(ex);
                        }
                        needRetry = false;
                    }
                }
            } while (needRetry && retryCount-- > 0);
            return apiResult;
        }

        public static ApiResult<T> Process<T>(Func<T> func,
                                              bool needRetry = false,
                                              int retryCount = 50,
                                              Func<Exception, string> getExceptionMessage = null)
        {
            ApiResult<T> apiResult = null;
            getExceptionMessage = getExceptionMessage ?? GetExceptionMessage;
            do
            {
                try
                {
                    var result = func();
                    needRetry = false;
                    apiResult = result != null ? new ApiResult<T>(result) 
                                               : new ApiResult<T>();
                }
                catch (Exception ex)
                {
                    if (!(ex is OptimisticConcurrencyException) || !needRetry)
                    {
                        var baseException = ex.GetBaseException();
                        if (baseException is DomainException)
                        {
                            var sysException = baseException as DomainException;
                            apiResult = new ApiResult<T>(sysException.ErrorCode, getExceptionMessage(sysException));
                            Logger?.Warn(ex);
                        }
                        else
                        {
                            apiResult = new ApiResult<T>(ErrorCode.UnknownError, getExceptionMessage(ex));
                            Logger?.Error(ex);
                        }
                        needRetry = false;
                    }
                }
            } while (needRetry && retryCount-- > 0);
            return apiResult;
        }
    }
}