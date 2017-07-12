using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.MessageQueue
{
    /// <summary>
    /// Action to take when there is no initial offset in offset store or the desired offset is out of range:
    ///  'smallest','earliest' - automatically reset the offset to the smallest offset, 
    /// 'largest','latest' - automatically reset the offset to the largest offset, 
    /// 'error' - trigger an error which is retrieved by consuming messages and checking 'message->err'. 
    ///* Type: enum value*
    /// </summary>
    public static class AutoOffsetReset
    {
        public const string Smallest = "smallest";
        public const string Earliest = "earliest";
        public const string Beginning = "beginning";
        public const string Largest = "largest";
        public const string Latest = "latest";
        public const string End = "end";
        public const string Error = "error";
    }
}
