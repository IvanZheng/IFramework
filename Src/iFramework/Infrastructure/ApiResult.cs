using System;
using System.Collections.Generic;
using System.Text;

namespace IFramework.Infrastructure
{
    /// <summary>
    ///     API 响应结果
    /// </summary>
    public class ApiResult
    {
        public ApiResult()
        {
            Success = true;
            ErrorCode = Exceptions.ErrorCode.NoError;
        }

        public ApiResult(object errorCode, string message = null)
        {
            ErrorCode = errorCode;
            Message = message;
            Success = false;
        }

        /// <summary>
        ///     API 执行是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        ///     ErrorCode 为 0 表示执行无异常
        /// </summary>
        public object ErrorCode { get; set; }

        /// <summary>
        ///     当API执行有异常时, 对应的错误信息
        /// </summary>
        public string Message { get; set; }
    }

    /// <inheritdoc />
    /// <summary>
    ///     Api返回结果
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    public class ApiResult<TResult> : ApiResult
    {
        public ApiResult()
        {
            ErrorCode = Exceptions.ErrorCode.NoError;
            Success = true;
        }

        public ApiResult(TResult result)
            : this()
        {
            ErrorCode = Exceptions.ErrorCode.NoError;
            Result = result;
        }

        public ApiResult(object errorCode, string message = null)
            : base(errorCode, message) { }

        /// <summary>
        ///     API 执行返回的结果
        /// </summary>
        public TResult Result { get; set; }
    }
}
