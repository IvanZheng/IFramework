using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using Newtonsoft.Json.Linq;

namespace IFramework.Infrastructure
{
    public class DynamicJson : DynamicObject
    {
        internal Newtonsoft.Json.Linq.JObject _json;
        public DynamicJson(Newtonsoft.Json.Linq.JObject json)
        {
            _json = json;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            bool ret = false;
            JToken value;
            if (_json.TryGetValue(binder.Name, out value))
            {
                result = (value as JValue).Value;
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
            bool ret = true;
            try
            {
                var property = _json.Property(binder.Name);
                if (property != null)
                {
                    property.Value = JToken.FromObject(val);
                }
                else
                {
                    _json.Add(binder.Name, new JObject(val));
                }
            }
            catch (Exception)
            {
                ret = false;
            }
            return ret;
        }
    }
}
