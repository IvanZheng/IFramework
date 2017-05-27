using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;

namespace Autofac.Configuration.Elements
{
    public class NamedConfigurationElementCollection<TElementType> : ConfigurationElementCollection,
        IEnumerable<TElementType>, IEnumerable where TElementType : ConfigurationElement
    {
        private readonly string _elementKey;
        private readonly string _elementName;

        protected NamedConfigurationElementCollection(string elementName, string elementKey)
        {
            if (elementName == null)
                throw new ArgumentNullException("elementName");
            if (elementName.Length == 0)
                throw new ArgumentOutOfRangeException(elementName);
            if (elementKey == null)
                throw new ArgumentNullException("elementKey");
            if (elementKey.Length == 0)
                throw new ArgumentOutOfRangeException(elementKey);
            _elementName = elementName;
            _elementKey = elementKey;
        }

        protected override string ElementName => _elementName;

        public override ConfigurationElementCollectionType CollectionType =>
            ConfigurationElementCollectionType.BasicMap;

        public TElementType this[int index]
        {
            get => BaseGet(index) as TElementType;
            set
            {
                if (BaseGet(index) != null)
                    BaseRemoveAt(index);
                BaseAdd(index, value);
            }
        }

        public new IEnumerator<TElementType> GetEnumerator()
        {
            var enumerator = ((IEnumerable) this).GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    var tElementType = (TElementType) enumerator.Current;
                    yield return tElementType;
                }
            }
            finally
            {
                var disposable = enumerator as IDisposable;
                if (disposable != null)
                    disposable.Dispose();
            }
        }

        protected override bool IsElementName(string elementName)
        {
            return elementName != null && elementName == _elementName;
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return Activator.CreateInstance<TElementType>();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            if (element == null)
                throw new ArgumentNullException("element");
            return (string) element.ElementInformation.Properties[_elementKey].Value;
        }
    }
}