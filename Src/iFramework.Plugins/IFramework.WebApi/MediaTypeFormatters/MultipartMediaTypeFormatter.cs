using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json;

namespace IFramework.AspNet.MediaTypeFormatters
{
    public class MultipartMediaTypeFormatter : MediaTypeFormatter
    {
        public MultipartMediaTypeFormatter()
        {
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("multipart/form-data"));
        }

        public override bool CanReadType(Type type)
        {
            return true;
        }

        public override bool CanWriteType(Type type)
        {
            return false;
        }

        public override Task<object> ReadFromStreamAsync(Type type,
                                                         Stream readStream,
                                                         HttpContent content,
                                                         IFormatterLogger formatterLogger)
        {
            if (!content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            var parts = content.ReadAsMultipartAsync();
            return Task.Factory.StartNew(() =>
            {
                object data = null;
                var valueCollection = new List<KeyValuePair<string, string>>();
                foreach (var partContent in parts.Result.Contents)
                {
                    if (partContent.Headers.ContentType == null)
                    {
                        var value = partContent.ReadAsStringAsync().Result;
                        var name = partContent.Headers.ContentDisposition.Name;
                        name = name.Substring(1, name.Length - 2);
                        valueCollection.Add(new KeyValuePair<string, string>(name, value));
                    }
                    else if (partContent.Headers.ContentType != null
                             && partContent.Headers.ContentType.MediaType == "application/json")
                    {
                        var partBody = partContent.ReadAsStringAsync().Result;
                        data = JsonConvert.DeserializeObject(partBody, type);
                        break;
                    }
                }
                if (data == null)
                {
                    if (valueCollection.Count > 0)
                    {
                        data = new FormDataCollection(valueCollection).ConvertToObject(type);
                    }
                }
                return data;
            });
        }
    }
}