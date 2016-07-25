using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IFramework.Message.Impl
{
    public class SlidingDoor : ISlidingDoor
    {
        protected SortedSet<long> _offsets;
        protected long _consumedOffset = -1L;
        protected long _lastOffset = -1L;
        protected long _lastCommittedOffset = -1L;
        object _removeOffsetLock = new object();
        protected long _fullLoadThreshold;
        protected Action<long> _CommitOffset;
        protected int _waitInterval;
        protected bool _commitPerMessage;
        ILogger _logger;

        public SlidingDoor(Action<long> commitOffset, int fullLoadThreshold, int waitInterval, bool commitPerMessage = false)
        {
            _offsets = new SortedSet<long>();
            _CommitOffset = commitOffset;
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

        public void BlockIfFullLoad()
        {
            while (_offsets.Count > _fullLoadThreshold)
            {
                Thread.Sleep(_waitInterval);
                _logger.Warn($"working is full load sleep 1000 ms");
            }
        }

        public void RemoveOffset(long offset)
        {
            if (_commitPerMessage)
            {
                _CommitOffset(offset);
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
                        _CommitOffset(_consumedOffset);
                        _lastCommittedOffset = _consumedOffset;
                    }
                }
            }
        }
    }
}
