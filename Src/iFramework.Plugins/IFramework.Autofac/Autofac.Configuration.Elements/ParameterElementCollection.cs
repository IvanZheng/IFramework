using Autofac.Configuration.Util;
using Autofac.Core;
using System;
using System.Collections.Generic;
using System.Reflection;
namespace Autofac.Configuration.Elements
{
	public class ParameterElementCollection : NamedConfigurationElementCollection<ParameterElement>
	{
		public ParameterElementCollection() : base("parameter", "name")
		{
		}
		public IEnumerable<Parameter> ToParameters()
		{
			foreach (ParameterElement current in this)
			{
				ParameterElement localParameter = current;
				yield return new ResolvedParameter((ParameterInfo pi, IComponentContext c) => pi.Name == localParameter.Name, (ParameterInfo pi, IComponentContext c) => TypeManipulation.ChangeToCompatibleType(localParameter.CoerceValue(), pi.ParameterType, pi));
			}
			yield break;
		}
	}
}
