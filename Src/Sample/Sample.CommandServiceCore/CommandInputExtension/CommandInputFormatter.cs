using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.Infrastructure;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace Sample.CommandServiceCore.CommandInputExtension
{
    public class CommandInputFormatter : TextInputFormatter
    {
        private const string ApplicationCommandMediaType = "application/command";
        private const string ApplicationJsonMediaType = "application/json";
        private const string ApplicationFormUrlEncodedFormMediaType = "application/x-www-form-urlencoded";
        private const string CommandTypeTemplate = nameof(CommandTypeTemplate);
        private readonly string _commandTypeTemplate;

        public CommandInputFormatter()
        {
            _commandTypeTemplate = Configuration.GetAppSetting(CommandTypeTemplate);
            SupportedMediaTypes.Add(new MediaTypeHeaderValue(ApplicationCommandMediaType));
            SupportedMediaTypes.Add(new MediaTypeHeaderValue(ApplicationJsonMediaType));
            SupportedMediaTypes.Add(new MediaTypeHeaderValue(ApplicationFormUrlEncodedFormMediaType));

            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(new UTF8Encoding(false));
            SupportedEncodings.Add(Encoding.GetEncoding("iso-8859-1"));
        }

        private Type GetCommandType(string commandType)
        {
            return Type.GetType(commandType) ?? Type.GetType(string.Format(_commandTypeTemplate,
                                                                           commandType));
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            encoding = encoding ?? SelectCharacterEncoding(context);
            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }
            var request = context.HttpContext.Request;


            try
            {
                var type = context.ModelType;
                var commandType = type;
                if (type.IsAbstract || type.IsInterface)
                {
                    commandType = GetCommandType(request.GetUri().Segments.Last());
                }
                var mediaType = request.ContentType.Split(';').FirstOrDefault();
                object command;
                if (mediaType == ApplicationFormUrlEncodedFormMediaType)
                {
                    command = await request.Form.ConvertToObjectAsync(commandType);
                }
                else
                {
                    using (var streamReader = context.ReaderFactory(request.Body, encoding))
                    {
                        var part = await streamReader.ReadToEndAsync();
                        command = part.ToJsonObject(commandType);
                    }
                }
                return command != null ? InputFormatterResult.Success(command) : InputFormatterResult.Failure();
            }
            catch (Exception)
            {
                return InputFormatterResult.Failure();
            }
        }
    }
}