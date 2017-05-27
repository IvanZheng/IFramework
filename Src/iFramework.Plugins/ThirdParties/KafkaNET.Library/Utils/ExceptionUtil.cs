using System;
using System.IO;
using System.Text;

namespace Microsoft.KafkaNET.Library.Util
{
    public class ExceptionUtil
    {
        public static string GetExceptionDetailInfo(Exception exception, bool flattenAggregateException = true)
        {
            var builder = new StringBuilder();

            if (exception != null)
            {
                builder.Append(Environment.NewLine);

                using (var writer = new StringWriter(builder))
                {
                    new TextExceptionFormatter(writer, exception)
                    {
                        FlattenAggregateException = flattenAggregateException
                    }.Format();
                }
            }
            return builder.ToString();
        }
    }
}