using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace IFramework.Domain
{
    [Serializable]
    public abstract class ValueObject : ICloneable
    {
        public object Clone()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, this);
                stream.Position = 0L;
                return formatter.Deserialize(stream);
            }
        }

        protected static bool EqualOperator(ValueObject left, ValueObject right)
        {
            if (object.ReferenceEquals(left, null) ^ object.ReferenceEquals(right, null))
            {
                return false;
            }
            return (object.ReferenceEquals(left, null) || left.Equals(right));
        }

        public override bool Equals(object obj)
        {
            if ((obj == null) || (obj.GetType() != base.GetType()))
            {
                return false;
            }
            ValueObject obj2 = (ValueObject) obj;
            IEnumerator<object> enumerator = this.GetAtomicValues().GetEnumerator();
            IEnumerator<object> enumerator2 = obj2.GetAtomicValues().GetEnumerator();
            while (enumerator.MoveNext() && enumerator2.MoveNext())
            {
                if (object.ReferenceEquals(enumerator.Current, null) ^ object.ReferenceEquals(enumerator2.Current, null))
                {
                    return false;
                }
                if (!((enumerator.Current == null) || enumerator.Current.Equals(enumerator2.Current)))
                {
                    return false;
                }
            }
            return (!enumerator.MoveNext() && !enumerator2.MoveNext());
        }

        protected virtual IEnumerable<object> GetAtomicValues()
        {
            return base.GetType().GetProperties().Select(p => p.GetValue(this, null));
        }

        public override int GetHashCode()
        {
            return GetAtomicValues()
           .Select(x => x != null ? x.GetHashCode() : 0)
           .Aggregate((x, y) => x ^ y);
        }

        protected static bool NotEqualOperator(ValueObject left, ValueObject right)
        {
            return !EqualOperator(left, right);
        }
    }
}
