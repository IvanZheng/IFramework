using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using IFramework.Infrastructure;

namespace IFramework.SysExceptions
{
    public class ErrorCodeDictionary
    {
        private static Dictionary<object, string> errorcodeDic = new Dictionary<object, string>();

        public static string GetErrorMessage(object errorcode)
        {
            string errorMessage = errorcodeDic.TryGetValue(errorcode, string.Empty);
            if (String.IsNullOrEmpty(errorMessage))
                errorMessage = errorcode.ToString();
            return errorMessage;
        }

        public static void InitErrorCodeDictionary(IDictionary<object, string> dictionary)
        {
            errorcodeDic = new Dictionary<object, string>(dictionary);
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

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("ErrorCode", this.ErrorCode);
            base.GetObjectData(info, context);
        }
    }
}
