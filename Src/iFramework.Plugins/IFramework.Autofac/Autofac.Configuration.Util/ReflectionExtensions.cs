using System;
using System.Reflection;
namespace Autofac.Configuration.Util
{
	internal static class ReflectionExtensions
	{
		public static bool TryGetDeclaringProperty(this ParameterInfo pi, out PropertyInfo prop)
		{
			MethodInfo methodInfo = pi.Member as MethodInfo;
			if (methodInfo != null && methodInfo.IsSpecialName && methodInfo.Name.StartsWith("set_", StringComparison.Ordinal))
			{
				prop = methodInfo.DeclaringType.GetProperty(methodInfo.Name.Substring(4));
				return true;
			}
			prop = null;
			return false;
		}
	}
}
