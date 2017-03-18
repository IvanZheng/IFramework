using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using IFramework.Infrastructure;
using System.ComponentModel;
using IFramework.Event;

namespace IFramework.Exceptions
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

    public class DomainException : Exception, IEvent
    {
        public object ErrorCode { get; set; }

        public string ID { get; set; }

        public string Key { get; set; }

        public DomainException()
        {
            ID = ObjectId.GenerateNewId().ToString();
        }
        protected DomainException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ID = info.GetValue("ID", typeof(string)) as string;
            Key = info.GetValue("Key", typeof(string)) as string;
            ErrorCode = info.GetValue("ErrorCode", typeof(object));
        }
        public DomainException(object errorCode, string message = null)
            : base(message ?? ErrorCodeDictionary.GetErrorMessage(errorCode))
        {
            ID = ObjectId.GenerateNewId().ToString();
            ErrorCode = errorCode;
        }

        public DomainException(object errorCode, object[] args)
            : base(ErrorCodeDictionary.GetErrorMessage(errorCode, args))
        {
            ID = ObjectId.GenerateNewId().ToString();
            ErrorCode = errorCode;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("ID", ID);
            info.AddValue("Key", Key);
            info.AddValue("ErrorCode", ErrorCode);
            base.GetObjectData(info, context);
        }
    }
}
