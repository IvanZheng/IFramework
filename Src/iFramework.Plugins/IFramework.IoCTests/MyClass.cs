using IFramework.IoC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOP.AutofacTests
{

    public class YourClass
    {
        public IContainer Container { get; set; }
        public YourClass(IContainer container)
        {
            Container = container;
        }
    }

    public class MyClass
    {
        public YourClass YourClass { get; set; }
        public IContainer Container { get; set; }
        public MyClass(YourClass yourClass, IContainer container)
        {
            YourClass = yourClass;
            Container = container;
        }
    }
}
