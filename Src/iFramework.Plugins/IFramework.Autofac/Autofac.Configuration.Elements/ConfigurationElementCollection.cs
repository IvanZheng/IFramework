using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;

namespace Autofac.Configuration.Elements
{
    public class ConfigurationElementCollection<TElementType> : ConfigurationElementCollection,
                                                                IEnumerable<TElementType>, IEnumerable where TElementType : ConfigurationElement
    {
        private readonly string _elementName;

        public ConfigurationElementCollection(string elementName)
        {
            _elementName = elementName;
        }

        protected override string ElementName => _elementName;

        public override ConfigurationElementCollectionType CollectionType =>
            ConfigurationElementCollectionType.BasicMap;

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
                {
                    disposable.Dispose();
                }
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
            return Guid.NewGuid();
        }
    }
}