using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using FormDataCollection = global::System.Net.Http.Formatting.FormDataCollection;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Primitives;

namespace Sample.CommandServiceCore.CommandInputExtension
{
    public static class FormDataExtension
    {
        public static Dictionary<string, StringValues> ToDictionary(this FormDataCollection formDataCollection)
        {
            return new Dictionary<string, StringValues>(formDataCollection.Select(d => KeyValuePair.Create(d.Key, new StringValues(d.Value))));
        }

        public static async Task<object> ConvertToObjectAsync(this IFormCollection formCollection, Type type)
        {
            try
            {
                var binder = new FormCollectionModelBinder();
                var modelBindingContext = new DefaultModelBindingContext
                {
                    ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(type),
                    ValueProvider = new FormValueProvider(BindingSource.Query,
                                                          formCollection,
                                                          CultureInfo.CurrentCulture)
                };
                await binder.BindModelAsync(modelBindingContext);
                return modelBindingContext.Model;
            }
            catch (Exception ex)
            {
                Debug.Write(ex.Message);
                return null;
            }
        }


        public static NameValueCollection ToNameValueCollection<T>(this T dynamicObject,
                                                                   string key = null,
                                                                   NameValueCollection nameValueCollection = null,
                                                                   bool removeEmptyObject = true)
        {
            nameValueCollection = nameValueCollection ?? HttpUtility.ParseQueryString("");
            if (dynamicObject == null)
            {
                return nameValueCollection;
            }
            var objectType = dynamicObject.GetType();
            if (objectType.IsPrimitive || objectType == typeof(string) || objectType == typeof(DateTime))
            {
                var value = dynamicObject.ToString();
                if (!removeEmptyObject || !string.IsNullOrWhiteSpace(value))
                {
                    nameValueCollection.Add(key, value);
                }
                return nameValueCollection;
            }
            var propertyDescriptors = TypeDescriptor.GetProperties(dynamicObject);
            for (var i = 0; i < propertyDescriptors.Count; i++)
            {
                var propertyDescriptor = propertyDescriptors[i];
                var value = propertyDescriptor.GetValue(dynamicObject);
                if (value == null)
                {
                    continue;
                }
                if (propertyDescriptor.PropertyType.IsPrimitive ||
                    propertyDescriptor.PropertyType == typeof(string) ||
                    propertyDescriptor.PropertyType == typeof(DateTime))
                {
                    if (removeEmptyObject && string.IsNullOrWhiteSpace(value.ToString()))
                    {
                        continue;
                    }
                    var formDataKey = string.IsNullOrEmpty(key)
                                          ? $"{propertyDescriptor.Name}"
                                          : $"{key}[{propertyDescriptor.Name}]";

                    nameValueCollection.Add(formDataKey, value.ToString());
                }
                else if (value is IEnumerable)
                {
                    var j = 0;
                    foreach (var val in value as IEnumerable)
                    {
                        var formDataKey = string.IsNullOrEmpty(key)
                                              ? $"{propertyDescriptor.Name}[{j}]"
                                              : $"{key}[{propertyDescriptor.Name}][{j}]";
                        var valType = val.GetType();
                        if (valType.IsPrimitive ||
                            valType == typeof(string) ||
                            valType == typeof(DateTime))
                        {
                            if (!removeEmptyObject || !string.IsNullOrWhiteSpace(val.ToString()))
                            {
                                nameValueCollection.Add(formDataKey, val.ToString());
                            }
                        }
                        else
                        {
                            ToNameValueCollection(val, formDataKey, nameValueCollection, removeEmptyObject);
                        }
                        j++;
                    }
                }
                else
                {
                    var formDataKey = string.IsNullOrEmpty(key)
                                          ? $"{propertyDescriptor.Name}"
                                          : $"{key}[{propertyDescriptor.Name}]";
                    ToNameValueCollection(value, formDataKey, nameValueCollection, removeEmptyObject);
                }
            }
            return nameValueCollection;
        }
    }
}