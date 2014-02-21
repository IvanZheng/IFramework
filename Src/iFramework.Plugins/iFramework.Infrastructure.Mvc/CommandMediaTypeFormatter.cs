using IFramework.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace IFramework.Infrastructure.Mvc
{
    public class CommandMediaTypeFormatter : JsonMediaTypeFormatter
    {
        public CommandMediaTypeFormatter()
        {
            this.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/command"));
            this.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/x-www-form-urlencoded"));
        }
        public override bool CanReadType(Type type)
        {
            return true;
        }

        public override bool CanWriteType(Type type)
        {
            return true;
        }
        public override Task WriteToStreamAsync(Type type, object value, System.IO.Stream writeStream, HttpContent content, System.Net.TransportContext transportContext)
        {
            return base.WriteToStreamAsync(value.GetType(), value, writeStream, content, transportContext);
        }

        public override Task<object> ReadFromStreamAsync(Type type, System.IO.Stream readStream, HttpContent content, System.Net.Http.Formatting.IFormatterLogger formatterLogger)
        {
            var commandType = type;
            if (type.IsAbstract || type.IsInterface)
            {
                commandType = Type.GetType(string.Format(Configuration.GetAppConfig("CommandTypeTemplate"), 
                                                         HttpContext.Current.Request.Url.Segments.Last()));
            }
            var part = content.ReadAsStringAsync();
            var mediaType = content.Headers.ContentType.MediaType;
            return Task.Factory.StartNew<object>(() =>
            {
                object command = null;
                if (mediaType == "application/x-www-form-urlencoded")
                {
                    command = new FormDataCollection(part.Result).ConvertToObject(commandType);
                }
                else
                {
                    command = part.Result.ToJsonObject(commandType);
                }
                return command;
            });

        }
    }
}
