using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using IFramework.Event;
using IFramework.Infrastructure;

namespace IFramework.Exceptions
{
    public class ErrorCodeDictionary
    {
        private static readonly Dictionary<object, string> ErrorCodeDic = new Dictionary<object, string>();

        public static string GetErrorMessage(object errorCode, params object[] args)
        {
            var errorMessage = ErrorCodeDic.TryGetValue(errorCode, string.Empty);
            if (string.IsNullOrEmpty(errorMessage))
            {
                var errorCodeFieldInfo = errorCode.GetType().GetField(errorCode.ToString());
                if (errorCodeFieldInfo != null)
                {
                    errorMessage = errorCodeFieldInfo.GetCustomAttribute<DescriptionAttribute>()?.Description;
                    if (string.IsNullOrEmpty(errorMessage))
                        errorMessage = errorCode.ToString();
                }
            }

            if (args?.Length > 0 && !string.IsNullOrWhiteSpace(errorMessage))
            {
                try
                {
                    return string.Format(errorMessage, args);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
            return errorMessage;
        }

        public static void AddErrorCodeMessages(IDictionary<object, string> dictionary)
        {
            dictionary.ForEach(p =>
            {
                if (ErrorCodeDic.ContainsKey(p.Key))
                    throw new Exception($"ErrorCode dictionary has already had the key {p.Key}");
                ErrorCodeDic.Add(p.Key, p.Value);
            });
        }
    }
    [Serializable]
    public class DomainException: Exception
    {
        public IDomainExceptionEvent DomainExceptionEvent { get; protected set; }
        public object ErrorCode { get; protected set; }
        internal string ErrorCodeType { get; set; }
        public DomainException()
        {
            
        }

        public DomainException(IDomainExceptionEvent domainExceptionEvent, Exception innerException = null)
            : this(domainExceptionEvent.ErrorCode, domainExceptionEvent.ToString(), innerException)
        {
            DomainExceptionEvent = domainExceptionEvent;
        }

        public DomainException(object errorCode, string message = null, Exception innerException = null)
            : base(message ?? ErrorCodeDictionary.GetErrorMessage(errorCode), innerException)
        {
            ErrorCode = errorCode;
        }

        public DomainException(object errorCode, object[] args, Exception innerException = null)
            : base(ErrorCodeDictionary.GetErrorMessage(errorCode, args), innerException)
        {
            ErrorCode = errorCode;
        }


        protected DomainException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ErrorCodeType = (string)info.GetValue(nameof(ErrorCodeType), typeof(string));
            if (ErrorCodeType != null)
            {
                var errorCodeType = Type.GetType(ErrorCodeType);
                if (errorCodeType != null)
                {
                    ErrorCode = info.GetValue(nameof(ErrorCode), errorCodeType);
                }
            }
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(ErrorCode), ErrorCode);
            info.AddValue(nameof(ErrorCodeType), ErrorCode?.GetType().GetFullNameWithAssembly());
            base.GetObjectData(info, context);
        }
    }
}