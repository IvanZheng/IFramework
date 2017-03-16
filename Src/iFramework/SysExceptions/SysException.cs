using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using IFramework.Infrastructure;
using System.ComponentModel;

namespace IFramework.SysExceptions
{
    public class ErrorCodeDictionary
    {
        private static Dictionary<object, string> _errorcodeDic = new Dictionary<object, string>();

        public static string GetErrorMessage(object errorcode, params object[] args)
        {
            string errorMessage = _errorcodeDic.TryGetValue(errorcode, string.Empty);
            if (string.IsNullOrEmpty(errorMessage))
            {
                errorMessage = errorcode.GetCustomAttribute<DescriptionAttribute>()?.Description;
                if (string.IsNullOrEmpty(errorMessage))
                {
                    errorMessage = errorcode.ToString();
                }
            }

            if (args != null && args.Length > 0)
            {
                return string.Format(errorMessage, args);
            }
            return errorMessage;
        }

        public static void AddErrorCodeMessages(IDictionary<object, string> dictionary)
        {
            dictionary.ForEach(p =>
            {
                if (_errorcodeDic.ContainsKey(p.Key))
                {
                    throw new Exception($"ErrorCode dictionary has already had the key {p.Key}");
                }
                _errorcodeDic.Add(p.Key, p.Value);
            });
        }

    }

    public class SysException : DomainException
    {
        public object ErrorCode { get; set; }
        public SysException() { }
        protected SysException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ErrorCode = info.GetValue("ErrorCode", typeof(object));
        }
        public SysException(object errorCode, string message = null)
            : base(message ?? ErrorCodeDictionary.GetErrorMessage(errorCode))
        {
            ErrorCode = errorCode;
        }

        public SysException(object errorCode, object[] args)
            : base(ErrorCodeDictionary.GetErrorMessage(errorCode, args))
        {
            ErrorCode = errorCode;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("ErrorCode", this.ErrorCode);
            base.GetObjectData(info, context);
        }
    }
}
