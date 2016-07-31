using System;
namespace Autofac.Configuration.Elements
{
	public class FileElementCollection : NamedConfigurationElementCollection<FileElement>
	{
		public FileElementCollection() : base("file", "name")
		{
		}
	}
}
