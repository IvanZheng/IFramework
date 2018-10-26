using System.Threading.Tasks;
using IFramework.Message;

namespace IFramework.MessageQueue
{
    public interface ICommitOffsetable
    {
        void CommitOffset(IMessageContext messageContext);
    }
}