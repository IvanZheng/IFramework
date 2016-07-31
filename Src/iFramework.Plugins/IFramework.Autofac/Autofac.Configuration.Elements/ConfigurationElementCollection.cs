using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
namespace Autofac.Configuration.Elements
{
	public class ConfigurationElementCollection<TElementType> : ConfigurationElementCollection, IEnumerable<TElementType>, IEnumerable where TElementType : ConfigurationElement
	{
		private readonly string _elementName;
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
		public ConfigurationElementCollection(string elementName)
		{
			this._elementName = elementName;
		}
		protected override bool IsElementName(string elementName)
		{
			return elementName != null && elementName == this._elementName;
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
