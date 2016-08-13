using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.MessageQueue
{
    public interface ISlidingDoor
    {
        void AddOffset(long offset);
        void RemoveOffset(long offset);
        int MessageCount { get; }
    }
}
