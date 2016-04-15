using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.SingleSignOn.IdentityProvider
{
    public interface ICustomIdentityObject
    {
        string ID { get; }
        string Name { get; }
    }
}
