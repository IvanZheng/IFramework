using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IFramework.Infrastructure;

namespace IFramework.Domain
{
#if NET5_0_OR_GREATER
    public abstract record ValueObject
    #else
    public abstract class ValueObject
    #endif
    {
        #if !NET5_0_OR_GREATER
        public static bool operator !=(ValueObject a, ValueObject b)
        {
            return NotEqualOperator(a, b);
        }

        public static bool operator ==(ValueObject a, ValueObject b)
        {
            return EqualOperator(a, b);
        }
        #endif
        /// <summary>
        /// return if Value Object is null due to EFCore complex type doesn't support optional value.
        /// </summary>
        /// <returns></returns>
        public abstract bool IsNull();  

        /// <summary>
        ///     Helper function for implementing overloaded equality operator.
        /// </summary>
        /// <param name="left">Left-hand side object.</param>
        /// <param name="right">Right-hand side object.</param>
        /// <returns></returns>
        protected static bool EqualOperator(ValueObject left, ValueObject right)
        {
            if (ReferenceEquals(left, null) ^ ReferenceEquals(right, null))
            {
                return false;
            }
            return ReferenceEquals(left, null) || left.Equals(right);
        }

        /// <summary>
        ///     Helper function for implementing overloaded inequality operator.
        /// </summary>
        /// <param name="left">Left-hand side object.</param>
        /// <param name="right">Right-hand side object.</param>
        /// <returns></returns>
        protected static bool NotEqualOperator(ValueObject left, ValueObject right)
        {
            return !EqualOperator(left, right);
        }

        /// <summary>
        ///     To be overridden in inheriting clesses for providing a collection of atomic values of
        ///     this Value Object.
        /// </summary>
        /// <returns>Collection of atomic values.</returns>
        protected virtual IEnumerable<object> GetAtomicValues()
        {
            return GetType().GetProperties().Where(p => !p.GetMethod.IsStatic).Select(p => p.GetValue(this, null));
        }
        

        #if !NET5_0_OR_GREATER 
        /// <summary>
        ///     Compares two Value Objects according to atomic values returned by <see cref="GetAtomicValues" />.
        /// </summary>
        /// <param name="obj">Object to compare to.</param>
        /// <returns>True if objects are considered equal.</returns>
        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
            {
                return false;
            }
            var other = (ValueObject) obj;
            using (var thisValues = GetAtomicValues().GetEnumerator())
            using (var otherValues = other.GetAtomicValues().GetEnumerator())
            {
                while (thisValues.MoveNext() && otherValues.MoveNext())
                {
                    if (ReferenceEquals(thisValues.Current, null) ^ ReferenceEquals(otherValues.Current, null))
                    {
                        return false;
                    }
                    if (thisValues.Current != null && !thisValues.Current.Equals(otherValues.Current))
                    {
                        return false;
                    }
                }
                return !thisValues.MoveNext() && !otherValues.MoveNext();
            }
        }
        #endif
        /// <summary>
        ///     Returns hashcode value calculated according to a collection of atomic values
        ///     returned by <see cref="GetAtomicValues" />.
        /// </summary>
        /// <returns>Hashcode value.</returns>
        public override int GetHashCode()
        {
            return GetAtomicValues()
                .Select(x => x != null ? x.GetHashCode() : 0)
                .Aggregate((x, y) => x ^ y);
        }
        
    }
    #if !NET5_0_OR_GREATER
    public abstract class ValueObject<T> : ValueObject where T: ValueObject
    #else
    public abstract record ValueObject<T> : ValueObject where T: ValueObject
    #endif
    {
        public static T Empty => Activator.CreateInstance<T>();
        public T CloneWith(object values = null)
        {
            // 获取类型
            var type = typeof(T);

            // 获取所有属性
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // 创建新的参数数组
            var parameters = new object[properties.Length];

            // 填充参数数组
            for (int i = 0; i < properties.Length; i++)
            {
                if (values?.HasProperty(properties[i].Name) ?? false)
                {
                    parameters[i] = values.GetPropertyValue(properties[i].Name);
                }
                else
                {
                    parameters[i] = properties[i].GetValue(this);
                }
            }

            // 使用反射创建新的实例
            return (T)Activator.CreateInstance(type, parameters);
        }
    }
}