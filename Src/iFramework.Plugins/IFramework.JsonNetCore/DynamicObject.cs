using System;
using System.Collections.Generic;
using System.Dynamic;
using IFramework.Infrastructure;
using Newtonsoft.Json.Linq;

namespace IFramework.JsonNet
{
    public class DynamicJson : DynamicObject
    {
        private readonly JObject _json;

        public DynamicJson(JObject json)
        {
            _json = json;
        }

        private dynamic ObjectToDynamic(object value)
        {
            object result;
            if (value is JValue jValue)
            {
                result = jValue.Value;
            }
            else if (value is JObject jObject)
            {
                result = new DynamicJson(jObject);
            }
            else if (value is JArray jArray)
            {
                var values = new List<dynamic>();
                jArray.ForEach(v => { values.Add(ObjectToDynamic(v)); });
                result = values;
            }
            else
            {
                result = value;
            }
            return result;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = _json.TryGetValue(binder.Name, out var value) ? ObjectToDynamic(value) : null;
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object val)
        {
            var ret = true;
            try
            {
                var property = _json.Property(binder.Name);
                if (property != null)
                    property.Value = JToken.FromObject(val);
                else
                    _json.Add(binder.Name, JToken.FromObject(val));
            }
            catch (Exception)
            {
                ret = false;
            }
            return ret;
        }
    }
}
