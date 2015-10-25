using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.SysExceptions
{
    public class ErrorCodeDictionary
    {
        private static Dictionary<int, string> errorcodeDic;

        public static string GetErrorMessage(int errorcode)
        {
            string errorMessage = errorcodeDic.FirstOrDefault(c => c.Key == errorcode).Value;
            if (String.IsNullOrEmpty(errorMessage))
                errorMessage = errorcode.ToString();
            return errorMessage;
        }

        public static void InitErrorCodeDictionary(IDictionary<int, string> dictionary)
        {
            errorcodeDic = new Dictionary<int, string>(dictionary);
        }
       
    }

    public class SysException : DomainException
    {
        public int ErrorCode { get; set; }
        public SysException() { }
        protected SysException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ErrorCode = (int)info.GetValue("ErrorCode", typeof(int));
        }
        public SysException(int errorCode, string message = null)
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
