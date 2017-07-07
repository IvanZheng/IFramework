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
        private static readonly Dictionary<object, string> _errorcodeDic = new Dictionary<object, string>();

        public static string GetErrorMessage(object errorcode, params object[] args)
        {
            var errorMessage = _errorcodeDic.TryGetValue(errorcode, string.Empty);
            if (string.IsNullOrEmpty(errorMessage))
            {
                var errorcodeFieldInfo = errorcode.GetType().GetField(errorcode.ToString());
                if (errorcodeFieldInfo != null)
                {
                    errorMessage = errorcodeFieldInfo.GetCustomAttribute<DescriptionAttribute>()?.Description;
                    if (string.IsNullOrEmpty(errorMessage))
                        errorMessage = errorcode.ToString();
                }
            }

            if (args != null && args.Length > 0)
                return string.Format(errorMessage, args);
            return errorMessage;
        }

        public static void AddErrorCodeMessages(IDictionary<object, string> dictionary)
        {
            dictionary.ForEach(p =>
            {
                if (_errorcodeDic.ContainsKey(p.Key))
                    throw new Exception($"ErrorCode dictionary has already had the key {p.Key}");
                _errorcodeDic.Add(p.Key, p.Value);
            });
        }
    }

    public class DomainException: Exception
    {
        public IDomainExceptionEvent DomainExceptionEvent { get; protected set; }

        public DomainException()
        {
        }

        public DomainException(IDomainExceptionEvent domainExceptionEvent)
            : this(domainExceptionEvent.GetType().Name, domainExceptionEvent.ToString())
        {
            DomainExceptionEvent = domainExceptionEvent;
        }

        protected DomainException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ErrorCode = info.GetValue("ErrorCode", typeof(object));
        }

        public DomainException(object errorCode, string message = null)
            : base(message ?? ErrorCodeDictionary.GetErrorMessage(errorCode))
        {
            ErrorCode = errorCode;
        }

        public DomainException(object errorCode, object[] args)
            : base(ErrorCodeDictionary.GetErrorMessage(errorCode, args))
        {
            ErrorCode = errorCode;
        }

        public object ErrorCode { get; set; }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("ErrorCode", ErrorCode);
            info.AddValue("DomainExceptionEvent", DomainExceptionEvent);
            base.GetObjectData(info, context);
        }
    }
}