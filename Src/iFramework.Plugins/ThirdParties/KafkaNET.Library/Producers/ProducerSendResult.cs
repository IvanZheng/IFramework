using System;

namespace Kafka.Client.Producers
{
    public class ProducerSendResult<TReturn>
    {
        public ProducerSendResult(TReturn returnVal)
        {
            ReturnVal = returnVal;
            Success = true;
        }

        public ProducerSendResult(Exception e)
        {
            Exception = e;
            Success = false;
        }

        public ProducerSendResult(TReturn returnVal, Exception e)
        {
            ReturnVal = returnVal;
            Exception = e;
            Success = false;
        }

        public TReturn ReturnVal { get; }
        public Exception Exception { get; }
        public bool Success { get; }
    }
}