using System;
using System.Text;

namespace Kafka.Client
{
    public static class ExceptionExtensions
    {
        /// <summary>
        ///     Formats an exception object into a printable string.
        /// </summary>
        /// <param name="exception">the exception to format</param>
        /// <returns>a string</returns>
        public static string FormatException(this Exception exception)
        {
            if (exception == null)
            {
                return string.Empty;
            }

            var output = new StringBuilder();

            var currentException = exception;
            while (currentException != null)
            {
                output.AppendFormat("Exception Message: {0}\r\n", currentException.Message);
                output.AppendFormat("Source: {0}\r\n", currentException.Source);
                output.AppendFormat("Stack Trace:\r\n {0}\r\n", currentException.StackTrace);
                currentException = currentException.InnerException;
                if (currentException != null)
                {
                    output.Append("\r\n---- Inner Exception ----\r\n");
                }
            }

            return output.ToString();
        }
    }
}