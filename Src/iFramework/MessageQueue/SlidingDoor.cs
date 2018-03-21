using System;
using System.Collections.Generic;
using System.Linq;
using IFramework.Infrastructure;

namespace IFramework.MessageQueue
{
    public class SlidingDoor : ISlidingDoor
    {
        private readonly object _removeOffsetLock = new object();
        protected Action<MessageOffset> _commitOffset;
        protected bool _commitPerMessage;
        protected long _consumedOffset = -1L;
        protected long _lastCommittedOffset = -1L;
        protected long _lastOffset = -1L;
        protected SortedSet<long> _offsets;
        protected SortedList<long, MessageOffset> _removedMessageOffsets;
        protected int _partition;

        public SlidingDoor(Action<MessageOffset> commitOffset,
                           int partition,
                           bool commitPerMessage = false)
        {
            _commitOffset = commitOffset;
            _partition = partition;
            _offsets = new SortedSet<long>();
            _removedMessageOffsets = new SortedList<long, MessageOffset>();
            _commitPerMessage = commitPerMessage;
        }

        public int MessageCount => _offsets.Count;

        public void AddOffset(long offset)
        {
            if (!_commitPerMessage)
            {
                lock (_removeOffsetLock)
                {
                    _offsets.Add(offset);
                    _lastOffset = offset;
                }
            }
        }

        public void RemoveOffset(MessageOffset messageOffset)
        {
            if (_commitPerMessage)
            {
                _commitOffset(messageOffset);
            }
            else
            {
                lock (_removeOffsetLock)
                {
                    if (_offsets.Remove(messageOffset.Offset))
                    {
                        _removedMessageOffsets.Add(messageOffset.Offset, messageOffset);
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
                        if (_removedMessageOffsets.TryRemoveBeforeKey(_consumedOffset, out var currentMessageOffset))
                        {
                            _commitOffset(currentMessageOffset);
                        }
                        _lastCommittedOffset = _consumedOffset;
                    }
                }
            }
        }
    }
}