using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;
using IFramework.Message.Impl;
using Newtonsoft.Json;
using IFramework.Message;

namespace IFramework.MessageQueue.MessageFormat
{
    public class MessageReply : IMessageReply
    {
        Dictionary<string, string> _Headers;
        public Dictionary<string, string> Headers
        {
            get { return _Headers; }
            set { _Headers = value; }
        }

        public string MessageID { get; set; }

        public MessageReply()
        {
            Headers = new Dictionary<string, string>();
        }

        public MessageReply(string messaegID, Exception e)
            : this()
        {
            MessageID = messaegID;
            Exception = e;
        }
        public MessageReply(string messageID, object result)
            : this()
        {
            MessageID = messageID;
            Result = result;
        }

        object _Result;
        [JsonIgnore]
        public object Result
        {
            get
            {
                if (_Result != null)
                {
                    return _Result;
                }
                string resultBody = string.Empty;
                if (Headers.TryGetValue("Result", out resultBody) && !string.IsNullOrWhiteSpace(resultBody))
                {
                    return _Result ?? (_Result = resultBody
                                                    .ToJsonObject(Type.GetType(Headers["ResultType"])));
                }
                return null;
            }
            set
            {
                _Result = value;
                if (_Result != null)
                {
                    Headers["Result"] = _Result.ToJson();
                    Headers["ResultType"] = _Result.GetType().AssemblyQualifiedName;
                }
            }
        }

        Exception _Exception;
        [JsonIgnore]
        public Exception Exception
        {
            get
            {
                if (_Exception != null)
                {
                    return _Exception;
                }
                string exceptionBody = string.Empty;
                if (Headers.TryGetValue("Exception", out exceptionBody) && !string.IsNullOrWhiteSpace(exceptionBody))
                {
                    return _Exception ?? (_Exception = exceptionBody
                                                      .ToJsonObject(Type.GetType(Headers["ExceptionType"]))
                                                      as Exception) ??
                                         (_Exception = exceptionBody.ToJsonObject<Exception>());
                }
                return null;
            }
            set
            {
                _Exception = value;
                if (_Exception != null)
                {
                    Headers["Exception"] = _Exception.ToJson();
                    Headers["ExceptionType"] = _Exception.GetType().AssemblyQualifiedName;
                }
            }
        }
    }
}
