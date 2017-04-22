using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace IFramework.AspNet
{
    public class FormValueProvider : IValueProvider
    {
        private const string BracketExpressionString = @"\[([A-Za-z]+)\]";

        private static readonly Regex BracketExpression = new Regex(BracketExpressionString);

        private static IEnumerable<string> ParseKey(string key)
        {
            // for form values like data[key1][key2], as provided using jQuery $.post, we want
            //   to also ensure that the form data.key1.key2 is in the dictionary to conform to
            //   what ASP.NET MVC expects

            var result = new List<string>
        {
            key
        };

            var str = key;

            while (BracketExpression.IsMatch(str))
            {
                str = BracketExpression.Replace(str, @".$1", 1);

                result.Add(str);
            }

            return result;
        }

        private readonly IValueProvider _valueProvider;


        public FormValueProvider(ControllerContext controllerContext)
            : this(controllerContext.HttpContext.Request.Form)
        {
        }

        public FormValueProvider(NameValueCollection formValues)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string key in formValues)
            {
                var value = formValues.Get(key);

                if (value != null)
                {
                    var keys = ParseKey(key);

                    foreach (var k in keys)
                    {
                        if (k.EndsWith("[]"))
                        {
                            var arrayKey = k.Substring(0, k.Length - 2);
                            var arrayValues = value.Split(',');
                            if (arrayValues.Length > 0)
                            {
                                for (int i = 0; i < arrayValues.Length; i++)
                                {
                                    values[string.Format("{0}[{1}]", arrayKey, i)] = arrayValues[i];
                                }
                            }
                        }
                        else
                        {
                            values[k] = value;
                        }
                    }
                }
            }

            _valueProvider = new DictionaryValueProvider<string>(values, CultureInfo.CurrentCulture);
        }

        public bool ContainsPrefix(string prefix)
        {
            var result = _valueProvider.ContainsPrefix(prefix);

            return result;
        }

        public ValueProviderResult GetValue(string key)
        {
            var result = _valueProvider.GetValue(key);

            return result;
        }
    }
    public static class FormDataUtility
    {
        public static object ConvertToObject(this FormDataCollection formDataCollection, Type type)
        {
            try
            {
                DefaultModelBinder binder = new DefaultModelBinder();
                ModelBindingContext modelBindingContext = new ModelBindingContext()
                {
                    ModelMetadata = ModelMetadataProviders.Current.GetMetadataForType(null, type),
                    ValueProvider = new FormValueProvider(formDataCollection.ReadAsNameValueCollection())
                };
                return binder.BindModel(new ControllerContext(), modelBindingContext);
            }
            catch (Exception ex)
            {
                Debug.Write(ex.Message);
                return null;
            }
        }




        public static NameValueCollection ToNameValueCollection<T>(this T dynamicObject, string key = null, NameValueCollection nameValueCollection = null)
        {
            nameValueCollection = nameValueCollection ?? HttpUtility.ParseQueryString("");
            if (dynamicObject == null)
            {
                return nameValueCollection;
            }
            var objectType = dynamicObject.GetType();
            if (objectType.IsPrimitive || objectType == typeof(string) || objectType == typeof(DateTime))
            {
                nameValueCollection.Add(key, dynamicObject.ToString());
                return nameValueCollection;
            }
            var propertyDescriptors = TypeDescriptor.GetProperties(dynamicObject);
            for (int i = 0; i < propertyDescriptors.Count; i++)
            {
                PropertyDescriptor propertyDescriptor = propertyDescriptors[i];
                var value = propertyDescriptor.GetValue(dynamicObject);
                if (value == null)
                {
                    continue;
                }
                if (propertyDescriptor.PropertyType.IsPrimitive ||
                         propertyDescriptor.PropertyType == typeof(string) ||
                         propertyDescriptor.PropertyType == typeof(DateTime))
                {
                    var formDataKey = string.IsNullOrEmpty(key) ? $"{propertyDescriptor.Name}" :
                                        $"{key}[{propertyDescriptor.Name}]";
                    nameValueCollection.Add(formDataKey, value.ToString());
                }
                else if (value is IEnumerable)
                {
                    int j = 0;
                    foreach (var val in (value as IEnumerable))
                    {
                        var formDataKey = string.IsNullOrEmpty(key) ? $"{propertyDescriptor.Name}[{j}]" :
                                          $"{key}[{propertyDescriptor.Name}][{j}]";
                        var valType = val.GetType();
                        if (valType.IsPrimitive ||
                            valType == typeof(string) ||
                            valType == typeof(DateTime))
                        {
                            nameValueCollection.Add(formDataKey, val.ToString());
                        }
                        else
                        {
                            ToNameValueCollection(val, formDataKey, nameValueCollection);
                        }
                        j++;
                    }
                }
                else
                {
                    var formDataKey = string.IsNullOrEmpty(key) ? $"{propertyDescriptor.Name}" :
                                        $"{key}[{propertyDescriptor.Name}]";
                    ToNameValueCollection(value, formDataKey, nameValueCollection);
                }
            }
            return nameValueCollection;
        }
    }

    public static class WebApiUtility
    {
        public static string GetClientIp(this HttpRequestMessage request)
        {
            // Owin Hosting
            //if (requestMessage.Properties.ContainsKey("MS_OwinContext"))
            //{
            //    return HttpContext.Current != null
            //        ? HttpContext.Current.Request.GetOwinContext().Request.RemoteIpAddress
            //        : null;
            //}
            if (request != null && request.Properties.ContainsKey("MS_HttpContext"))
            {
                return ((HttpContextWrapper)request.Properties["MS_HttpContext"]).Request.UserHostAddress;
            }
            else if (request != null && request.Properties.ContainsKey(RemoteEndpointMessageProperty.Name))
            {
                RemoteEndpointMessageProperty property = (RemoteEndpointMessageProperty)request.Properties[RemoteEndpointMessageProperty.Name];
                return property != null ? property.Address : null;
            }
            else if (HttpContext.Current != null)
            {
                return HttpContext.Current.Request.UserHostAddress;
            }
            else
            {
                return null;
            }
        }
    }
}
