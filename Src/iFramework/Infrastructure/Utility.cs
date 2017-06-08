using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Hosting;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json.Linq;

namespace IFramework.Infrastructure
{
    public class QueryParameter
    {
        public QueryParameter(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }

        public string Value { get; set; }
    }

    public static class Utility
    {
        private const string k_base36_digits = "0123456789abcdefghijklmnopqrstuvwxyz";
        private static readonly uint[] _lookup32 = CreateLookup32();
        public static IPAddress[] GetLocalIPAddresses()
        {
            Dns.GetHostName();
            return Dns.GetHostAddresses(Dns.GetHostName());
        }

        public static IPAddress GetLocalIPV4()
        {
            return Dns.GetHostEntry(Dns.GetHostName())
                      .AddressList
                      .First(x => x.AddressFamily == AddressFamily.InterNetwork);
        }

        private static uint[] CreateLookup32()
        {
            var result = new uint[256];
            for (var i = 0; i < 256; i++)
            {
                var s = i.ToString("X2");
                result[i] = s[0] + ((uint) s[1] << 16);
            }
            return result;
        }

        public static string ToBase36string(this byte[] bytes,
                                            EndianFormat bytesEndian = EndianFormat.Little,
                                            bool includeProceedingZeros = true)
        {
            var base36_no_zeros = new RadixEncoding(k_base36_digits, bytesEndian, includeProceedingZeros);
            return base36_no_zeros.Encode(bytes);
        }

        public static byte[] ConvertBase36StringToBytes(string base36string,
                                                        EndianFormat bytesEndian = EndianFormat.Little,
                                                        bool includeProceedingZeros = true)
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
            for (var i = 0; i < bytes.Length; i++)
            {
                var val = lookup32[bytes[i]];
                result[2 * i] = (char) val;
                result[2 * i + 1] = (char) (val >> 16);
            }
            return new string(result);
        }

        public static int GetUniqueCode(this string str)
        {
            var uniqueCode = 0;
            if (!string.IsNullOrWhiteSpace(str))
            {
                foreach (var c in str)
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
            var mi = obj.GetType()
                        .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .First(m => m.Name == method && m.IsGenericMethod);
            var miConstructed = mi.MakeGenericMethod(genericType);
            var fastInvoker = FastInvoke.GetMethodInvoker(miConstructed);
            return fastInvoker(obj, args);
        }

        public static object InvokeMethod(this object obj, string method, object[] args)
        {
            MethodInfo mi = null;
            foreach (var m in obj.GetType()
                                 .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (m.Name == method && m.GetParameters().Length == args.Length)
                {
                    var equalParameters = true;
                    for (var i = 0; i < m.GetParameters().Length; i++)
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
            var fastInvoker = FastInvoke.GetMethodInvoker(mi);
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
                var attrs = ((FieldInfo) obj).GetCustomAttributes(typeof(TAttribute), inherit);
                if (attrs != null && attrs.Length > 0)
                {
                    return attrs.FirstOrDefault(attr => attr is TAttribute) as TAttribute;
                }
            }
            else if (obj is PropertyInfo)
            {
                var attrs = ((PropertyInfo) obj).GetCustomAttributes(inherit);
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
            foreach (var element in source.OrEmptyIfNull())
            {
                act(element);
            }
            return source;
        }

        public static string GetTimeToString(DateTime datetime, bool isEnglish)
        {
            var lang = isEnglish ? "en-US" : "zh-CN";
            var timetext = string.Empty;
            var span = DateTime.Now - datetime;
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
                var maxSize = length;
                var chars = new char[62];
                string a;
                a = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
                chars = a.ToCharArray();
                var size = maxSize;
                var data = new byte[1];
                var crypto = new RNGCryptoServiceProvider();
                crypto.GetNonZeroBytes(data);
                size = maxSize;
                data = new byte[size];
                crypto.GetNonZeroBytes(data);
                var result = new StringBuilder(size);
                foreach (var b in data)
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
            var retValue = default(T);
            object objValue = null;
            try
            {
                if (obj is JObject)
                {
                    var jObject = obj as JObject;
                    var property = jObject.Property(name);
                    if (property != null)
                    {
                        var value = property.Value as JValue;
                        if (value != null)
                        {
                            objValue = value.Value;
                        }
                    }
                }
                else
                {
                    var property = obj.GetType()
                                      .GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (property != null)
                    {
                        objValue = FastInvoke.GetMethodInvoker(property.GetGetMethod(true))(obj, null);
                    }
                }

                if (objValue != null)
                {
                    retValue = (T) objValue;
                }
            }
            catch (Exception)
            {
                retValue = default(T);
            }
            return retValue;
        }

        public static object GetValueByKey(this object obj, string name)
        {
            object objValue = null;
            if (obj is JObject)
            {
                var jObject = obj as JObject;
                var property = jObject.Property(name);
                if (property != null)
                {
                    var value = property.Value as JValue;
                    if (value != null)
                    {
                        objValue = value.Value;
                    }
                }
            }
            else
            {
                var property = obj.GetType()
                                  .GetProperty(name,
                                               BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property != null)
                {
                    objValue = FastInvoke.GetMethodInvoker(property.GetGetMethod(true))(obj, null);
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
            if (obj is JObject)
            {
                var jObject = obj as JObject;
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
                var property = obj.GetType()
                                  .GetProperty(name,
                                               BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property != null)
                {
                    FastInvoke.GetMethodInvoker(property.GetSetMethod(true))(obj, new[] {value});
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
                return (T) Enum.Parse(typeof(T), val);
            }
            catch (Exception)
            {
                return default(T);
            }
        }

        public static LambdaExpression GetLambdaExpression(Type type, string propertyName)
        {
            var param = Expression.Parameter(type);
            Expression body = param;
            foreach (var member in propertyName.Split('.'))
            {
                body = Expression.PropertyOrField(body, member);
            }
            return Expression.Lambda(body, param);
        }

        public static LambdaExpression GetLambdaExpression(Type type, Expression expression)
        {
            var propertyName = expression.ToString();
            var index = propertyName.IndexOf('.');
            propertyName = propertyName.Substring(index + 1);
            return GetLambdaExpression(type, propertyName);
        }

        public static IQueryable<TEntity> GetOrderByQueryable<TEntity>(IQueryable<TEntity> query,
                                                                       LambdaExpression orderByExpression,
                                                                       bool asc)
            where TEntity : class
        {
            var orderBy = asc ? "OrderBy" : "OrderByDescending";
            var orderByCallExpression =
                Expression.Call(typeof(Queryable),
                                orderBy,
                                new[]
                                {
                                    typeof(TEntity),
                                    orderByExpression.Body.Type
                                },
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

            var result = new List<QueryParameter>();

            if (!string.IsNullOrEmpty(parameters))
            {
                var p = parameters.Split('&');
                foreach (var s in p)
                {
                    if (!string.IsNullOrEmpty(s))
                    {
                        if (s.IndexOf('=') > -1)
                        {
                            var temp = s.Split('=');
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
            var sb = new StringBuilder();
            QueryParameter p = null;
            for (var i = 0; i < parameters.Count; i++)
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
            var des = new DESCryptoServiceProvider();
            des.Mode = mode;
            var inputByteArray = Encoding.Default.GetBytes(pToEncrypt);
            des.Key = Encoding.ASCII.GetBytes(key);
            des.IV = Encoding.ASCII.GetBytes(key);
            var ms = new MemoryStream();
            var cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();
            var ret = new StringBuilder();
            foreach (var b in ms.ToArray())
            {
                ret.AppendFormat("{0:X2}", b);
            }
            ret.ToString();
            return ret.ToString();
        }

        public static string MD5Decrypt(string pToDecrypt, CipherMode mode = CipherMode.CBC, string key = "IVANIVAN")
        {
            var des = new DESCryptoServiceProvider();
            des.Mode = mode;
            var inputByteArray = new byte[pToDecrypt.Length / 2];
            for (var x = 0; x < pToDecrypt.Length / 2; x++)
            {
                var i = Convert.ToInt32(pToDecrypt.Substring(x * 2, 2), 16);
                inputByteArray[x] = (byte) i;
            }
            des.Key = Encoding.ASCII.GetBytes(key);
            des.IV = Encoding.ASCII.GetBytes(key);

            var ms = new MemoryStream();
            var cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();
            var ret = new StringBuilder();
            return Encoding.ASCII.GetString(ms.ToArray());
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
            var flag = true;
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
                resource = HttpContext.GetLocalResourceObject(path, key, new CultureInfo(lang));
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
                resource = HttpContext.GetGlobalResourceObject("GlobalResource", key, new CultureInfo(lang));
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
            s = s.Replace("\\", "\\\\")
                 .Replace("'", "\\'")
                 .Replace("\"", "\\\"")
                 .Replace("\r\n", "\\n")
                 .Replace("\n\r", "\\n")
                 .Replace("\r", "\\n")
                 .Replace("\n", "\\n");
            s = s.Replace("/", "\\/");
            return s;
        }

        public static string GetMd5Hash(string input)
        {
            var md5Hasher = MD5.Create();
            var data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));
            var sBuilder = new StringBuilder();
            for (var i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }

        public static string Serialize(object xmlContent, bool omitXmlDeclaration = false, Encoding encoding = null)
        {
            var serializer = new XmlSerializer(xmlContent.GetType());
            //StringBuilder builder = new System.Text.StringBuilder();
            //StringWriter writer = new StringWriterWithEncoding(Encoding.UTF8);
            //new System.IO.StringWriter(builder);
            //System.Xml.XmlTextWriter writer = new System.Xml.XmlTextWriter(@"c:\test.xml", System.Text.Encoding.UTF8);
            //serializer.Serialize(writer, xmlContent);
            //return builder.ToString();

            var stream = new MemoryStream();
            var setting = new XmlWriterSettings();
            setting.OmitXmlDeclaration = omitXmlDeclaration;
            setting.Encoding = encoding ?? Encoding.GetEncoding("utf-8");
            setting.Indent = true;
            using (var writer = XmlWriter.Create(stream, setting))
            {
                serializer.Serialize(writer, xmlContent);
            }
            return Regex.Replace(Encoding.GetEncoding("utf-8").GetString(stream.ToArray()), "^[^<]", "");
        }

        public static object DeSerialize<XmlType>(string xmlString)
        {
            var serializer = new XmlSerializer(typeof(XmlType));
            var builder = new StringBuilder(xmlString);
            var reader = new StringReader(builder.ToString());
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
        ///     序列化
        /// </summary>
        /// <param name="data">要序列化的对象</param>
        /// <returns>返回存放序列化后的数据缓冲区</returns>
        public static byte[] ToBytes(this object data)
        {
            var formatter = new BinaryFormatter();
            var rems = new MemoryStream();
            formatter.Serialize(rems, data);
            return rems.GetBuffer();
        }

        /// <summary>
        ///     反序列化
        /// </summary>
        /// <param name="data">数据缓冲区</param>
        /// <returns>对象</returns>
        public static object ToObject(this byte[] data)
        {
            var formatter = new BinaryFormatter();
            var rems = new MemoryStream(data);
            data = null;
            return formatter.Deserialize(rems);
        }


        //public static TValueObject Clone<TValueObject>(this TValueObject valueObject,
        //                                               Action<TValueObject> initAction = null)
        //    where  TValueObject : ValueObject, new()
        //{
        //    var clonedObject = valueObject.Clone() as TValueObject;
        //    initAction(clonedObject);
        //    return clonedObject;
        //}

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

        public static string GetDescription(this object obj)
        {
            if (obj == null)
            {
                return null;
            }
            var fi = obj.GetType().GetField(obj.ToString());
            if (fi != null)
            {
                var arrDesc = fi.GetCustomAttribute<DescriptionAttribute>(false);
                if (arrDesc != null)
                {
                    return arrDesc.Description;
                }
            }
            return null;
        }
    }
}