using System;
using System.Collections.Generic;
using System.Dynamic;
using Newtonsoft.Json.Linq;

namespace IFramework.Infrastructure
{
    public class DynamicJson : DynamicObject
    {
        internal JObject _json;

        public DynamicJson(JObject json)
        {
            _json = json;
        }

        public string ToJson()
        {
            return _json?.ToString();
        }

        private dynamic ObjectToDynamic(object value)
        {
            object result = null;
            if (value is JValue)
            {
                result = (value as JValue).Value;
            }
            else if (value is JObject)
            {
                result = new DynamicJson(value as JObject);
            }
            else if (value is JArray)
            {
                var values = new List<dynamic>();
                (value as JArray).ForEach(v => { values.Add(ObjectToDynamic(v)); });
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
            var ret = false;
            JToken value;
            if (_json.TryGetValue(binder.Name, out value))
            {
                result = ObjectToDynamic(value);
                ret = true;
            }
            else
            {
                result = null;
            }
            return ret;
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