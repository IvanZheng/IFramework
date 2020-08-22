﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IFramework.AspNet;
using IFramework.Command;
using IFramework.Config;
using IFramework.Infrastructure;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
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
            _commandTypeTemplate = Configuration.Instance.Get(CommandTypeTemplate);
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

            var logger = context.HttpContext.RequestServices.GetService<ILogger<CommandInputFormatter>>();

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
                if ((type.IsAbstract || type.IsInterface) && typeof(ICommand).IsAssignableFrom(type))
                {
                    commandType = GetCommandType(request.GetUri().Segments.Last());
                }
                var mediaType = request.ContentType.Split(';').FirstOrDefault();
                object command = null;
                if (mediaType == ApplicationFormUrlEncodedFormMediaType)
                {
                    if (FormUrlEncodedJson.TryParse(request.Form.ToDictionary(f => f.Key, f => f.Value.ToString()), out var jObject))
                    {
                        command = jObject.ToObject(commandType);
                    }
                }
                else
                {
                    var memory = new byte[request.ContentLength??0];
                    await request.Body.ReadAsync(memory);
                    var part = encoding.GetString(memory);
                    command = part.ToJsonObject(commandType, true);
                }
                return command != null ? InputFormatterResult.Success(command) : InputFormatterResult.Failure();
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                return InputFormatterResult.Failure();
            }
        }
    }
}