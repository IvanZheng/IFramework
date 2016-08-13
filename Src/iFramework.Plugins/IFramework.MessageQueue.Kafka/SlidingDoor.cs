using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IFramework.MessageQueue.MSKafka
{
    public class SlidingDoor
    {
        protected SortedSet<long> _offsets;
        protected long _consumedOffset = -1L;
        protected long _lastOffset = -1L;
        protected long _lastCommittedOffset = -1L;
        object _removeOffsetLock = new object();
        protected long _fullLoadThreshold;
        protected int _waitInterval;
        protected int _partition;
        protected bool _commitPerMessage;
        protected Action<int, long> _commitOffset;
        ILogger _logger;

        public SlidingDoor(Action<int, long> commitOffset, int partition, int fullLoadThreshold, int waitInterval, bool commitPerMessage = false)
        {
            _partition = partition;
            _offsets = new SortedSet<long>();
            _fullLoadThreshold = fullLoadThreshold;
            _waitInterval = waitInterval;
            _commitPerMessage = commitPerMessage;
            _logger = IoCFactory.Resolve<ILoggerFactory>().Create(this.GetType().Name);
        }

        public void AddOffset(long offset)
        {
            lock (_removeOffsetLock)
            {
                _offsets.Add(offset);
                _lastOffset = offset;
            }
        }

        public void RemoveOffset(long  offset)
        {
            if (_commitPerMessage)
            {
                _commitOffset(_partition, offset);
                lock (_removeOffsetLock)
                {
                    _offsets.Remove(offset);
                }
            }
            else
            {
                lock (_removeOffsetLock)
                {
                    if (_offsets.Remove(offset))
                    {
                        if (_offsets.Count > 0)
                        {
                            _consumedOffset = _offsets.First() - 1;
                        }
                        else
                        {
                            _consumedOffset = _lastOffset;
                        }
                    }
                    if (_consumedOffset > _lastCommittedOffset)
                    {
                        _commitOffset(_partition, _consumedOffset);
                        _lastCommittedOffset = _consumedOffset;
                    }
                }
            }
        }
    }
}
