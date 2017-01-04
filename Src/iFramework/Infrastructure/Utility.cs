using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web;
using System.Xml.Serialization;
using System.Xml;
using Newtonsoft.Json.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.ServiceModel;
using System.Dynamic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;
using System.Globalization;
using Newtonsoft.Json;
using IFramework.Domain;
using System.Web.Hosting;
using System.Numerics;
using System.Net;
using System.Net.Sockets;

namespace IFramework.Infrastructure
{
    public class QueryParameter
    {
        private string name = null;
        private string value = null;

        public QueryParameter(string name, string value)
        {
            this.name = name;
            this.value = value;
        }

        public string Name
        {
            get { return name; }
        }

        public string Value
        {
            get { return value; }
            set { this.value = value; }
        }
    }

    public static class Utility
    {
        private static readonly uint[] _lookup32 = CreateLookup32();
        public static IPAddress GetLocalIPV4()
        {
            return Dns.GetHostEntry(Dns.GetHostName()).AddressList.First<IPAddress>(x => (x.AddressFamily == AddressFamily.InterNetwork));
        }
        private static uint[] CreateLookup32()
        {
            var result = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                string s = i.ToString("X2");
                result[i] = ((uint)s[0]) + ((uint)s[1] << 16);
            }
            return result;
        }
        const string k_base36_digits = "0123456789abcdefghijklmnopqrstuvwxyz";

        public static string ToBase36string(this byte[] bytes, EndianFormat bytesEndian = EndianFormat.Little, bool includeProceedingZeros = true)
        {
            var base36_no_zeros = new RadixEncoding(k_base36_digits, bytesEndian, includeProceedingZeros);
            return base36_no_zeros.Encode(bytes);
        }

        public static byte[] ConvertBase36StringToBytes(string base36string, EndianFormat bytesEndian = EndianFormat.Little, bool includeProceedingZeros = true)
        {
            var base36_no_zeros = new RadixEncoding(k_base36_digits, bytesEndian, includeProceedingZeros);
            var bytes = new List<byte>(base36_no_zeros.Decode(base36string));
            //while (bytes[bytes.Count - 1] == 0)
            //{
            //    bytes.RemoveAt(bytes.Count - 1);
            //}
            return bytes.ToArray();
        }

        public static string ToHexString(this byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }
            var lookup32 = _lookup32;
            var result = new char[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                var val = lookup32[bytes[i]];
                result[2 * i] = (char)val;
                result[2 * i + 1] = (char)(val >> 16);
            }
            return new string(result);
        }

        public static int GetUniqueCode(this string str)
        {
            int uniqueCode = 0;
            if (!string.IsNullOrWhiteSpace(str))
            {
                foreach(var c in str)
                {
                    if (c != 0)
                    {
                        uniqueCode += (c << 5) - c;
                    }
                }
            }
            return uniqueCode;
        }

        public static bool TryDo(Action action)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public static bool TryRemove(this Hashtable hashtable, object key)
        {
            return TryDo(() => hashtable.Remove(key));
        }

        public static bool TryRemove(this IDictionary collection, object key)
        {
            return TryDo(() => collection.Remove(key));
        }
        public static object InvokeGenericMethod(this object obj, Type genericType, string method, object[] args)
        {
            MethodInfo mi = obj.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).First(m => m.Name == method && m.IsGenericMethod);
            MethodInfo miConstructed = mi.MakeGenericMethod(genericType);
            FastInvoke.FastInvokeHandler fastInvoker = FastInvoke.GetMethodInvoker(miConstructed);
            return fastInvoker(obj, args);
        }

        public static object InvokeMethod(this object obj, string method, object[] args)
        {
            MethodInfo mi = null;
            foreach (var m in obj.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (m.Name == method && m.GetParameters().Length == args.Length)
                {
                    bool equalParameters = true;
                    for (int i = 0; i < m.GetParameters().Length; i++)
                    {
                        var type = m.GetParameters()[i];
                        if (!type.ParameterType.IsInstanceOfType(args[i]))
                        {
                            equalParameters = false;
                            break;
                        }
                    }
                    if (equalParameters)
                    {
                        mi = m;
                        break;
                    }
                }
            }
            if (mi == null)
            {
                throw new NotSupportedException();
            }
            FastInvoke.FastInvokeHandler fastInvoker = FastInvoke.GetMethodInvoker(mi);
            return fastInvoker(obj, args);
        }


        //public static TRole ActAs<TRole>(this IAggregateRoot entity)
        //    where TRole : Framework.DCI.IRole
        //{
        //    TRole role = IoCFactory.Resolve<TRole>(new ParameterOverride("actor", entity));
        //    return role;
        //}

        public static TAttribute GetCustomAttribute<TAttribute>(this object obj, bool inherit = true)
        where TAttribute : Attribute
        {
            if (obj is Type)
            {
                var attrs = (obj as Type).GetCustomAttributes(typeof(TAttribute), inherit);
                if (attrs != null)
                {
                    return attrs.FirstOrDefault() as TAttribute;
                }
            }
            else if (obj is FieldInfo)
            {
                var attrs = ((FieldInfo)obj).GetCustomAttributes(typeof(TAttribute), inherit);
                if (attrs != null && attrs.Length > 0)
                {
                    return attrs.FirstOrDefault(attr => attr is TAttribute) as TAttribute;
                }
            }
            else if (obj is PropertyInfo)
            {
                var attrs = ((PropertyInfo)obj).GetCustomAttributes(inherit);
                if (attrs != null && attrs.Length > 0)
                {
                    return attrs.FirstOrDefault(attr => attr is TAttribute) as TAttribute;
                }
            }
            else if (obj is MethodInfo)
            {
                var attrs = (obj as MethodInfo).GetCustomAttributes(inherit);
                if (attrs != null && attrs.Length > 0)
                {
                    return attrs.FirstOrDefault(attr => attr is TAttribute) as TAttribute;
                }
            }
            else if (obj.GetType().IsDefined(typeof(TAttribute), true))
            {
                var attr = Attribute.GetCustomAttribute(obj.GetType(), typeof(TAttribute), inherit) as TAttribute;
                return attr;
            }
            return null;
        }


        public static IEnumerable<T> OrEmptyIfNull<T>(this IEnumerable<T> source)
        {
            return source ?? Enumerable.Empty<T>();
        }

        public static IEnumerable<T> ForEach<T>(
                this IEnumerable<T> source,
                Action<T> act)
        {
            foreach (T element in source.OrEmptyIfNull()) act(element);
            return source;
        }

        public static string GetTimeToString(DateTime datetime, bool isEnglish)
        {
            string lang = isEnglish ? "en-US" : "zh-CN";
            string timetext = string.Empty;
            TimeSpan span = DateTime.Now - datetime;
            if (span.Days > 30)
            {
                timetext = datetime.ToShortDateString();
            }
            else if (span.Days >= 1)
            {
                timetext = string.Format("{0}{1}", span.Days, GetResource("Day", lang));
            }
            else if (span.Hours >= 1)
            {
                timetext = string.Format("{0}{1}", span.Hours, GetResource("Hour", lang));
            }
            else if (span.Minutes >= 1)
            {
                timetext = string.Format("{0}{1}", span.Minutes, GetResource("Minute", lang));
            }
            else if (span.Seconds >= 1)
            {
                timetext = string.Format("{0}{1}", span.Seconds, GetResource("Second", lang));
            }
            else
            {
                timetext = string.Format("1{0}", GetResource("Second", lang));
            }
            return timetext;
        }

        public static IQueryable<T> GetPageElements<T>(this IQueryable<T> query, int pageIndex, int pageSize)
        {
            return query.Skip(pageIndex * pageSize).Take(pageSize);
        }
        internal static string GetUniqueIdentifier(int length)
        {
            try
            {
                int maxSize = length;
                char[] chars = new char[62];
                string a;
                a = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
                chars = a.ToCharArray();
                int size = maxSize;
                byte[] data = new byte[1];
                RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider();
                crypto.GetNonZeroBytes(data);
                size = maxSize;
                data = new byte[size];
                crypto.GetNonZeroBytes(data);
                StringBuilder result = new StringBuilder(size);
                foreach (byte b in data)
                {
                    result.Append(chars[b % (chars.Length - 1)]);
                }
                // Unique identifiers cannot begin with 0-9
                if (result[0] >= '0' && result[0] <= '9')
                {
                    return GetUniqueIdentifier(length);
                }
                return result.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("GENERATE_UID_FAIL", ex);
            }
        }

        public static T GetValueByKey<T>(this object obj, string name)
        {
            T retValue = default(T);
            object objValue = null;
            try
            {
                if (obj is Newtonsoft.Json.Linq.JObject)
                {
                    var jObject = obj as Newtonsoft.Json.Linq.JObject;
                    var property = jObject.Property(name);
                    if (property != null)
                    {
                        var value = property.Value as Newtonsoft.Json.Linq.JValue;
                        if (value != null)
                        {
                            objValue = value.Value;
                        }
                    }
                }
                else
                {
                    var property = obj.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (property != null)
                    {
                        objValue = FastInvoke.GetMethodInvoker(property.GetGetMethod(true))(obj, null);
                        //Func<T> PGet = Delegate.CreateDelegate(typeof(Func<T>), obj, property.GetGetMethod(true)) as Func<T>;
                        //objValue = PGet();
                        // property.GetValue(obj, null);
                    }
                }

                if (objValue != null)
                {
                    retValue = (T)objValue;
                }
            }
            catch (System.Exception)
            {
                retValue = default(T);
            }
            return retValue;
        }

        public static object GetValueByKey(this object obj, string name)
        {
            object objValue = null;
            if (obj is Newtonsoft.Json.Linq.JObject)
            {
                var jObject = obj as Newtonsoft.Json.Linq.JObject;
                var property = jObject.Property(name);
                if (property != null)
                {
                    var value = property.Value as Newtonsoft.Json.Linq.JValue;
                    if (value != null)
                    {
                        objValue = value.Value;
                    }
                }
            }
            else
            {
                var property = obj.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property != null)
                {
                    objValue = FastInvoke.GetMethodInvoker(property.GetGetMethod(true))(obj, null);
                    // objValue = property.GetValue(obj, null);
                }
            }
            return objValue;
        }

        public static void SetValueByKey(this object obj, string name, object value)
        {
            if (obj is DynamicJson)
            {
                obj = (obj as DynamicJson)._json;
            }
            if (obj is Newtonsoft.Json.Linq.JObject)
            {
                var jObject = obj as Newtonsoft.Json.Linq.JObject;
                var property = jObject.Property(name);
                if (property != null)
                {
                    property.Value = JToken.FromObject(value);
                }
                else
                {
                    jObject.Add(name, JToken.FromObject(value));
                }
            }
            else
            {
                var property = obj.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property != null)
                {
                    FastInvoke.GetMethodInvoker(property.GetSetMethod(true))(obj, new object[] { value });
                }
            }
        }

        public static T ToEnum<T>(this string val)
        {
            return ParseEnum<T>(val);
        }

        public static T ParseEnum<T>(string val)
        {
            try
            {
                return (T)Enum.Parse(typeof(T), val);
            }
            catch (System.Exception)
            {
                return default(T);
            }
        }

        public static LambdaExpression GetLambdaExpression(Type type, string propertyName)
        {
            ParameterExpression param = Expression.Parameter(type);
            PropertyInfo property = type.GetProperty(propertyName);
            Expression propertyAccessExpression = Expression.MakeMemberAccess(param, property);
            var le = Expression.Lambda(propertyAccessExpression, param);
            return le;
        }

        public static IQueryable<TEntity> GetOrderByQueryable<TEntity>(IQueryable<TEntity> query, LambdaExpression orderByExpression, bool asc)
            where TEntity : class
        {
            var orderBy = asc ? "OrderBy" : "OrderByDescending";
            MethodCallExpression orderByCallExpression =
                        Expression.Call(typeof(Queryable),
                        orderBy,
                        new Type[] { typeof(TEntity),
                        orderByExpression.Body.Type},
                        query.Expression,
                        orderByExpression);
            return query.Provider.CreateQuery<TEntity>(orderByCallExpression);
        }


        public static List<QueryParameter> GetQueryParameters(string parameters)
        {
            if (parameters.StartsWith("?"))
            {
                parameters = parameters.Remove(0, 1);
            }

            List<QueryParameter> result = new List<QueryParameter>();

            if (!string.IsNullOrEmpty(parameters))
            {
                string[] p = parameters.Split('&');
                foreach (string s in p)
                {
                    if (!string.IsNullOrEmpty(s))
                    {
                        if (s.IndexOf('=') > -1)
                        {
                            string[] temp = s.Split('=');
                            result.Add(new QueryParameter(temp[0], temp[1]));
                        }
                        else
                        {
                            result.Add(new QueryParameter(s, string.Empty));
                        }
                    }
                }
            }

            return result;
        }

        public static string NormalizeRequestParameters(IList<QueryParameter> parameters)
        {
            StringBuilder sb = new StringBuilder();
            QueryParameter p = null;
            for (int i = 0; i < parameters.Count; i++)
            {
                p = parameters[i];
                sb.AppendFormat("{0}={1}", p.Name, p.Value);

                if (i < parameters.Count - 1)
                {
                    sb.Append("&");
                }
            }

            return sb.ToString();
        }

        //加密算法

        public static string MD5Encrypt(string pToEncrypt, CipherMode mode = CipherMode.CBC, string key = "IVANIVAN")
        {
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();
            des.Mode = mode;
            byte[] inputByteArray = Encoding.Default.GetBytes(pToEncrypt);
            des.Key = ASCIIEncoding.ASCII.GetBytes(key);
            des.IV = ASCIIEncoding.ASCII.GetBytes(key);
            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();
            StringBuilder ret = new StringBuilder();
            foreach (byte b in ms.ToArray())
            {
                ret.AppendFormat("{0:X2}", b);
            }
            ret.ToString();
            return ret.ToString();

        }

        public static string MD5Decrypt(string pToDecrypt, CipherMode mode = CipherMode.CBC, string key = "IVANIVAN")
        {
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();
            des.Mode = mode;
            byte[] inputByteArray = new byte[pToDecrypt.Length / 2];
            for (int x = 0; x < pToDecrypt.Length / 2; x++)
            {
                int i = (Convert.ToInt32(pToDecrypt.Substring(x * 2, 2), 16));
                inputByteArray[x] = (byte)i;
            }
            des.Key = ASCIIEncoding.ASCII.GetBytes(key);
            des.IV = ASCIIEncoding.ASCII.GetBytes(key);

            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();
            StringBuilder ret = new StringBuilder();
            return System.Text.Encoding.ASCII.GetString(ms.ToArray());

        }

        public static Exception GetRescureInnerException(this Exception ex)
        {
            var innerEx = ex;
            while (innerEx.InnerException != null)
            {
                innerEx = innerEx.InnerException;
            }
            return innerEx;
        }

        public static bool IsGuid(string id)
        {
            bool flag = true;
            try
            {
                new Guid(id.Trim());
            }
            catch (Exception)
            {
                flag = false;
            }
            return flag;
        }

        public static string GetLocalResource(string path, string key, string lang)
        {
            object resource = string.Empty;
            if (!string.IsNullOrEmpty(lang))
            {
                resource = HttpContext.GetLocalResourceObject(path, key, new System.Globalization.CultureInfo(lang));
            }
            else
            {
                resource = HttpContext.GetLocalResourceObject(path, key);
            }
            if (resource != null)
            {
                return resource.ToString();
            }
            return string.Empty;
        }

        public static string GetLocalResource(string path, string key)
        {
            return GetLocalResource(path, key, string.Empty);
        }

        public static string GetResource(string key, string lang)
        {
            object resource = string.Empty;
            if (!string.IsNullOrEmpty(lang))
            {
                resource = HttpContext.GetGlobalResourceObject("GlobalResource", key, new System.Globalization.CultureInfo(lang));
            }
            else
            {
                resource = HttpContext.GetGlobalResourceObject("GlobalResource", key);
            }
            if (resource != null)
            {
                return resource.ToString();
            }
            return string.Empty;
        }


        public static string GetResource(string key)
        {
            return GetResource(key, string.Empty);
        }

        public static string StyledSheetEncode(string s)
        {
            s = s.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\"", "\\\"").Replace("\r\n", "\\n").Replace("\n\r", "\\n").Replace("\r", "\\n").Replace("\n", "\\n");
            s = s.Replace("/", "\\/");
            return s;
        }

        public static string GetMd5Hash(string input)
        {
            MD5 md5Hasher = MD5.Create();
            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }

        static public string Serialize(object xmlContent, bool omitXmlDeclaration = false, Encoding encoding = null)
        {
            System.Xml.Serialization.XmlSerializer serializer = new XmlSerializer(xmlContent.GetType());
            //StringBuilder builder = new System.Text.StringBuilder();
            //StringWriter writer = new StringWriterWithEncoding(Encoding.UTF8);
            //new System.IO.StringWriter(builder);
            //System.Xml.XmlTextWriter writer = new System.Xml.XmlTextWriter(@"c:\test.xml", System.Text.Encoding.UTF8);
            //serializer.Serialize(writer, xmlContent);
            //return builder.ToString();

            MemoryStream stream = new MemoryStream();
            XmlWriterSettings setting = new XmlWriterSettings();
            setting.OmitXmlDeclaration = omitXmlDeclaration;
            setting.Encoding = encoding ?? Encoding.GetEncoding("utf-8");
            setting.Indent = true;
            using (XmlWriter writer = XmlWriter.Create(stream, setting))
            {
                serializer.Serialize(writer, xmlContent);
            }
            return System.Text.RegularExpressions.Regex.Replace(Encoding.GetEncoding("utf-8").GetString(stream.ToArray()), "^[^<]", "");
        }

        static public object DeSerialize<XmlType>(string xmlString)
        {

            System.Xml.Serialization.XmlSerializer serializer = new XmlSerializer(typeof(XmlType));
            StringBuilder builder = new StringBuilder(xmlString);
            System.IO.StringReader reader = new System.IO.StringReader(builder.ToString());
            try
            {
                return serializer.Deserialize(reader);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static Func<TObject, TProperty> GetFieldValueExp<TObject, TProperty>(string fieldName)
        {
            var paramExpr = Expression.Parameter(typeof(TObject));
            var propOrFieldVisit = Expression.PropertyOrField(paramExpr, fieldName);
            var lambda = Expression.Lambda<Func<TObject, TProperty>>(propOrFieldVisit, paramExpr);
            return lambda.Compile();
        }

        /// <summary> 
        /// 序列化 
        /// </summary> 
        /// <param name="data">要序列化的对象</param> 
        /// <returns>返回存放序列化后的数据缓冲区</returns> 
        public static byte[] ToBytes(this object data)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            MemoryStream rems = new MemoryStream();
            formatter.Serialize(rems, data);
            return rems.GetBuffer();
        }

        /// <summary> 
        /// 反序列化 
        /// </summary> 
        /// <param name="data">数据缓冲区</param> 
        /// <returns>对象</returns> 
        public static object ToObject(this byte[] data)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            MemoryStream rems = new MemoryStream(data);
            data = null;
            return formatter.Deserialize(rems);
        }




        public static TValueObject Clone<TValueObject>(this TValueObject valueObject,
                                                       Action<TValueObject> initAction)
            where  TValueObject : ValueObject, new()
        {
            var clonedObject = valueObject.Clone() as TValueObject;
            initAction(clonedObject);
            return clonedObject;
        }

        public static string ResolveVirtualPath(string path)
        {
            if (string.IsNullOrEmpty(HttpRuntime.AppDomainAppVirtualPath))
            {
                return Path.Combine("/", path).Replace('\\', '/').Replace("//", "/");
            }
            return Path.Combine(HttpRuntime.AppDomainAppVirtualPath, path).Replace('\\', '/').Replace("//", "/");
        }

        public static string MapPath(string virtualPath)
        {
            return HostingEnvironment.MapPath(virtualPath);
        }
    }
}
