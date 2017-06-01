using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using IFramework.Config;
using IFramework.Infrastructure;
using Newtonsoft.Json.Serialization;

namespace IFramework.AspNet.MediaTypeFormatters
{
    public class CommandMediaTypeFormatter : JsonMediaTypeFormatter
    {
        private static readonly string CommandTypeTemplate = Configuration.GetAppConfig("CommandTypeTemplate");
        private readonly bool _useCamelCase;

        public CommandMediaTypeFormatter(bool useCamelCase = true)
        {
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/command"));
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/command+form"));
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/x-www-form-urlencoded"));
            _useCamelCase = useCamelCase;
            if (_useCamelCase)
            {
                SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            }
        }

        public override bool CanReadType(Type type)
        {
            return true;
        }

        public override bool CanWriteType(Type type)
        {
            return true;
        }

        public override Task WriteToStreamAsync(Type type,
                                                object value,
                                                Stream writeStream,
                                                HttpContent content,
                                                TransportContext transportContext)
        {
            return base.WriteToStreamAsync(value.GetType(), value, writeStream, content, transportContext);
        }

        private Type GetCommandType(string commandType)
        {
            var type = Type.GetType(commandType);
            if (type == null)
            {
                type = Type.GetType(string.Format(CommandTypeTemplate,
                                                  commandType));
            }
            return type;
        }

        public override async Task<object> ReadFromStreamAsync(Type type,
                                                               Stream readStream,
                                                               HttpContent content,
                                                               IFormatterLogger formatterLogger)
        {
            var commandType = type;
            if (type.IsAbstract || type.IsInterface)
            {
                var commandContentType =
                    content.Headers.ContentType.Parameters.FirstOrDefault(p => p.Name == "command");
                if (commandContentType != null)
                {
                    commandType = GetCommandType(HttpUtility.UrlDecode(commandContentType.Value));
                }
                else
                {
                    commandType = GetCommandType(HttpContext.Current.Request.Url.Segments.Last());
                }
            }
            var part = await content.ReadAsStringAsync();
            var mediaType = content.Headers.ContentType.MediaType;
            object command = null;
            if (mediaType == "application/x-www-form-urlencoded" || mediaType == "application/command+form")
            {
                command = new FormDataCollection(part).ConvertToObject(commandType);
            }
            if (command == null)
            {
                command = part.ToJsonObject(commandType, useCamelCase: _useCamelCase);
            }
            return command;
        }
    }
}