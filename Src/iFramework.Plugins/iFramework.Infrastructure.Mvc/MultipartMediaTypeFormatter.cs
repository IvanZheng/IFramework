using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Infrastructure.Mvc
{
    public class MultipartMediaTypeFormatter : MediaTypeFormatter
    {
        public MultipartMediaTypeFormatter()
        {
            this.SupportedMediaTypes.Add(new System.Net.Http.Headers.MediaTypeHeaderValue("multipart/form-data"));
        }
        public override bool CanReadType(Type type)
        {
            return true;
        }

        public override bool CanWriteType(Type type)
        {
            return false;
        }

        public override Task<object> ReadFromStreamAsync(Type type, System.IO.Stream readStream, HttpContent content, System.Net.Http.Formatting.IFormatterLogger formatterLogger)
        {
            if (!content.IsMimeMultipartContent())
            {
                throw new System.Web.Http.HttpResponseException(System.Net.HttpStatusCode.UnsupportedMediaType);
            }

            var parts = content.ReadAsMultipartAsync();
            return Task.Factory.StartNew<object>(() =>
            {
                object data = null;
                Dictionary<string, object> valueCollection = new Dictionary<string, object>();
                foreach (var partContent in parts.Result.Contents)
                {
                    if (partContent.Headers.ContentType == null)
                    {
                        var value = partContent.ReadAsStringAsync().Result;
                        var name = partContent.Headers.ContentDisposition.Name;
                        name = name.Substring(1, name.Length - 2);
                        valueCollection.Add(name, value);
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
                        var valueCollectionJson = valueCollection.ToJson();
                        data = JsonConvert.DeserializeObject(valueCollectionJson, type);
                    }
                }
                return data;
            });

        }
    }
}
