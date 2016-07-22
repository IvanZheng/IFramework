using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Message
{
    public interface ISlidingDoor
    {
        void AddOffset(long offset);
        void BlockIfFullLoad();
        void RemoveOffset(long offset);
    }
}
