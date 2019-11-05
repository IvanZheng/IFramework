using System;
using System.Collections;
using System.Collections.Concurrent;
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
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using IFramework.Config;
using IFramework.MessageQueue;

namespace IFramework.Infrastructure
{
    public static class Utility
    {
        private static readonly ConcurrentDictionary<string, MethodInfo> MethodInfoDictionary = new ConcurrentDictionary<string, MethodInfo>();
        private const string KBase36Digits = "0123456789abcdefghijklmnopqrstuvwxyz";
        private static readonly uint[] Lookup32 = CreateLookup32();

        public static string GetFullNameWithAssembly(this Type type)
        {
            return $"{type.FullName}, {type.Assembly.GetName().Name}";
        }

        public static bool TryRemoveBeforeKey<TKey, TElement>(this SortedList<TKey, TElement> list, TKey key, out TElement obj)
        {
            var index = list.IndexOfKey(key);
            if (index >= 0)
            {
                obj = list.ElementAtOrDefault(index).Value;
                if (index == list.Count - 1)
                {
                    list.Clear();
                }
                else
                {
                    for (int i = 0; i <= index; i++)
                    {
                        list.RemoveAt(0);
                    }
                }
            }
            else
            {
                obj = default(TElement);
            }
            return index >= 0;
        }


        public static IPAddress[] GetLocalIpAddresses()
        {
            Dns.GetHostName();
            return Dns.GetHostAddresses(Dns.GetHostName());
        }

        public static IPAddress GetLocalIpv4()
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
                result[i] = s[0] + ((uint)s[1] << 16);
            }
            return result;
        }

        public static string ToBase36String(this byte[] bytes,
                                            EndianFormat bytesEndian = EndianFormat.Little,
                                            bool includeProceedingZeros = true)
        {
            var base36NoZeros = new RadixEncoding(KBase36Digits, bytesEndian, includeProceedingZeros);
            return base36NoZeros.Encode(bytes);
        }

        public static byte[] ConvertBase36StringToBytes(string base36String,
                                                        EndianFormat bytesEndian = EndianFormat.Little,
                                                        bool includeProceedingZeros = true)
        {
            var base36NoZeros = new RadixEncoding(KBase36Digits, bytesEndian, includeProceedingZeros);
            var bytes = new List<byte>(base36NoZeros.Decode(base36String));
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
            var lookup32 = Lookup32;
            var result = new char[bytes.Length * 2];
            for (var i = 0; i < bytes.Length; i++)
            {
                var val = lookup32[bytes[i]];
                result[2 * i] = (char)val;
                result[2 * i + 1] = (char)(val >> 16);
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

        private static bool MatchParameters(ParameterInfo[] parameterInfos, object[] args)
        {
            if (parameterInfos.Length != args.Length)
            {
                return false;
            }
            for (int i = 0; i < parameterInfos.Length; i++)
            {
                if (args[i].GetType() != parameterInfos[i].ParameterType)
                {
                    return false;
                }
            }
            return true;
        }

        public static object InvokeMethod(this object obj, string method, object[] args)
        {
            var mi = obj.GetType().GetMethodInfo(method, args);

            if (mi == null)
            {
                throw new NotSupportedException();
            }
            var fastInvoker = FastInvoke.GetMethodInvoker(mi);
            return fastInvoker(obj, args);
        }

        public static object InvokeGenericMethod(this object obj, string method, object[] args, params Type[] genericTypes)
        {
            var mi = obj.GetType().GetMethodInfo(method, args, genericTypes);
            var miConstructed = mi.MakeGenericMethod(genericTypes);
            var fastInvoker = FastInvoke.GetMethodInvoker(miConstructed);
            return fastInvoker(obj, args);
        }

        public static object InvokeStaticMethod(this Type type, string method, object[] args)
        {
            var mi = type.GetMethodInfo(method, args, true);

            if (mi == null)
            {
                throw new NotSupportedException();
            }
            var fastInvoker = FastInvoke.GetMethodInvoker(mi);
            return fastInvoker(null, args);
        }

        public static object InvokeStaticGenericMethod(this Type type, string method, object[] args, params Type[] genericTypes)
        {
            var mi = type.GetMethodInfo(method, args, genericTypes, true);
            var miConstructed = mi.MakeGenericMethod(genericTypes);
            var fastInvoker = FastInvoke.GetMethodInvoker(miConstructed);
            return fastInvoker(null, args);
        }

        public static bool GenericTypeAssignable(Type sourceType, Type targetType)
        {
            if (sourceType == null)
            {
                return true;
            }

            if (sourceType.Name == targetType.Name && sourceType.GenericTypeArguments.Length == targetType.GenericTypeArguments.Length)
            {
                for (int i = 0; i < sourceType.GenericTypeArguments.Length; i++)
                {
                    if (sourceType.GenericTypeArguments[i].Name != targetType.GenericTypeArguments[i].Name)
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        public static MethodInfo GetMethodInfo(this Type type, string method, object[] args, Type[] genericTypes, bool staticMethod = false)
        {
            var keyAttributes = new List<string>(args.Select(a => a?.GetType().MetadataToken.ToString() ?? "null")) { type.MetadataToken.ToString(), method };
            keyAttributes.AddRange(genericTypes.Select(t => t.MetadataToken.ToString()));
            var methodInfoKey = string.Join("", keyAttributes);
            return MethodInfoDictionary.GetOrAdd(methodInfoKey, key =>
            {
                MethodInfo mi = null;
                var methods = type.GetMethods((staticMethod ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var m in methods.Where(m => m.Name == method &&
                                                     m.IsGenericMethod &&
                                                     m.GetParameters().Length == args.Length))
                {
                    var equalParameters = true;
                    for (var i = 0; i < m.GetParameters().Length; i++)
                    {
                        var parameter = m.GetParameters()[i];
                        var parameterType = parameter.ParameterType.IsGenericParameter
                                                ? genericTypes[parameter.ParameterType
                                                                        .GenericParameterPosition]
                                                : parameter.ParameterType;
                        var arg = args[i];
                        if ((arg != null || parameterType.IsValueType) &&
                            !parameterType.IsInstanceOfType(arg) && 
                            !GenericTypeAssignable(arg?.GetType(), parameterType))
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
                return mi;
            });
        }

        public static MethodInfo GetMethodInfo(this Type type, string method, object[] args, bool staticMethod = false)
        {
            //var methodInfoKey = string.Join("", new List<string>(args.Select(a => a?.GetType().FullName ?? null)) {type.FullName, method});
            var keyAttributes = new List<string>(args.Select(a => a?.GetType().MetadataToken.ToString() ?? "null")) { type.MetadataToken.ToString(), method };
            var methodInfoKey = string.Join("", keyAttributes);
            return MethodInfoDictionary.GetOrAdd(methodInfoKey, key =>
            {
                MethodInfo mi = null;
                foreach (var m in type.GetMethods((staticMethod ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.Public | BindingFlags.NonPublic)
                                      .Where(m => m.Name == method &&
                                                  !m.IsGenericMethod &&
                                                  m.GetParameters().Length == args.Length))
                {
                    var equalParameters = true;
                    for (var i = 0; i < m.GetParameters().Length; i++)
                    {
                        var parameterType = m.GetParameters()[i].ParameterType;
                        var arg = args[i];
                        if ((arg != null || parameterType.IsValueType) && !parameterType.IsInstanceOfType(arg))
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
                return mi;
            });
        }
     

        

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

        public static void ForEach<T>(this IEnumerable<T> source,
                                      Action<T> act)
        {
            if (source == null)
            {
                return;
            }
            foreach (var element in source)
            {
                act(element);
            }
        }

        public static async Task ForEachAsync<T>(this IEnumerable<T> source,
                                      Func<T, Task> func)
        {
            if (source == null)
            {
                return;
            }
            foreach (var element in source)
            {
                await func(element).ConfigureAwait(false);
            }
        }

        public static IQueryable<T> GetPageElements<T>(this IQueryable<T> query, int pageIndex, int pageSize)
        {
            return query.Skip(pageIndex * pageSize).Take(pageSize);
        }

    
        public static T GetPropertyValue<T>(this object obj, string name)
        {
            var retValue = default(T);
            object objValue = null;
            try
            {

                var property = obj.GetType()
                                  .GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property != null)
                {
                    objValue = FastInvoke.GetMethodInvoker(property.GetGetMethod(true))(obj, null);
                }


                if (objValue != null)
                {
                    retValue = (T)objValue;
                }
            }
            catch (Exception)
            {
                retValue = default(T);
            }
            return retValue;
        }

        public static object GetPropertyValue(this object obj, string name)
        {
            object objValue = null;
            var property = obj.GetType()
                              .GetProperty(name,
                                           BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null)
            {
                objValue = FastInvoke.GetMethodInvoker(property.GetGetMethod(true))(obj, null);
            }

            return objValue;
        }

        public static void SetValueByKey(this object obj, string name, object value)
        {
            var property = obj.GetType()
                              .GetProperty(name,
                                           BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null)
            {
                FastInvoke.GetMethodInvoker(property.GetSetMethod(true))(obj, new[] { value });
            }
        }
        

        public static T ToEnum<T>(this string val)
        {
            try
            {
                return (T)Enum.Parse(typeof(T), val);
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

        //加密算法

        public static string Md5Encrypt(string pToEncrypt, CipherMode mode = CipherMode.CBC, string key = "12345678")
        {
            var des = new DESCryptoServiceProvider { Mode = mode };
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

        public static string Md5Decrypt(string pToDecrypt, CipherMode mode = CipherMode.CBC, string key = "12345678")
        {
            var des = new DESCryptoServiceProvider { Mode = mode };
            var inputByteArray = new byte[pToDecrypt.Length / 2];
            for (var x = 0; x < pToDecrypt.Length / 2; x++)
            {
                var i = Convert.ToInt32(pToDecrypt.Substring(x * 2, 2), 16);
                inputByteArray[x] = (byte)i;
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

            using (var stream = new MemoryStream())
            {
                var setting = new XmlWriterSettings
                {
                    OmitXmlDeclaration = omitXmlDeclaration,
                    Encoding = encoding ?? Encoding.GetEncoding("utf-8"),
                    Indent = true
                };
                using (var writer = XmlWriter.Create(stream, setting))
                {
                    serializer.Serialize(writer, xmlContent);
                }
                return Regex.Replace(Encoding.GetEncoding("utf-8").GetString(stream.ToArray()), "^[^<]", "");
            }
        }

        public static object DeSerialize<TXmlType>(string xmlString)
        {
            var serializer = new XmlSerializer(typeof(TXmlType));
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