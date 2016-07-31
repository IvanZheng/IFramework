using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
namespace Autofac.Configuration.Elements
{
	public class NamedConfigurationElementCollection<TElementType> : ConfigurationElementCollection, IEnumerable<TElementType>, IEnumerable where TElementType : ConfigurationElement
	{
		private string _elementName;
		private string _elementKey;
		protected override string ElementName
		{
			get
			{
				return this._elementName;
			}
		}
		public override ConfigurationElementCollectionType CollectionType
		{
			get
			{
				return ConfigurationElementCollectionType.BasicMap;
			}
		}
		public TElementType this[int index]
		{
			get
			{
				return base.BaseGet(index) as TElementType;
			}
			set
			{
				if (base.BaseGet(index) != null)
				{
					base.BaseRemoveAt(index);
				}
				this.BaseAdd(index, value);
			}
		}
		protected NamedConfigurationElementCollection(string elementName, string elementKey)
		{
			if (elementName == null)
			{
				throw new ArgumentNullException("elementName");
			}
			if (elementName.Length == 0)
			{
				throw new ArgumentOutOfRangeException(elementName);
			}
			if (elementKey == null)
			{
				throw new ArgumentNullException("elementKey");
			}
			if (elementKey.Length == 0)
			{
				throw new ArgumentOutOfRangeException(elementKey);
			}
			this._elementName = elementName;
			this._elementKey = elementKey;
		}
		protected override bool IsElementName(string elementName)
		{
			return elementName != null && elementName == this._elementName;
		}
		protected override ConfigurationElement CreateNewElement()
		{
			return Activator.CreateInstance<TElementType>();
		}
		protected override object GetElementKey(ConfigurationElement element)
		{
			if (element == null)
			{
				throw new ArgumentNullException("element");
			}
			return (string)element.ElementInformation.Properties[this._elementKey].Value;
		}
		public new IEnumerator<TElementType> GetEnumerator()
		{
			IEnumerator enumerator = ((IEnumerable)this).GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					TElementType tElementType = (TElementType)((object)enumerator.Current);
					yield return tElementType;
				}
			}
			finally
			{
				IDisposable disposable = enumerator as IDisposable;
				if (disposable != null)
				{
					disposable.Dispose();
				}
			}
			yield break;
		}
	}
}
