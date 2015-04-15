using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using IFramework.Infrastructure.Logging;
using System.Threading;

namespace IFramework.Infrastructure
{
    public class LockUtility
    {
        protected static readonly ILogger _Logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(LockUtility));

        class LockObject
        {
            volatile int _Counter;
            public int Counter
            {
                get
                {
                    return _Counter;
                }
            }

            internal void Decrement()
            {
                _Counter--;
                //Interlocked.Decrement(ref _Counter);
            }

            internal void Increate()
            {
                _Counter++;
                //Interlocked.Increment(ref _Counter);
            }
        
        }

        /// <summary>
        /// _lockPool 为锁对象池, 所以引用计数大于0的锁对象都会在池中缓存起来
        /// </summary>
        static readonly Hashtable _lockPool = new Hashtable();

        /// <summary>
        /// 释放锁对象, 当锁的引用计数为0时, 从锁对象池移除
        /// </summary>
        /// <param name="key"></param>
        /// <param name="lockObj"></param>
        static void ReleaseLock(object key, LockObject lockObj)
        {
            lock (_lockPool)
            {
                lockObj.Decrement();
                //_Logger.DebugFormat("I am thread {0}:lock counter is {1}", Thread.CurrentThread.ManagedThreadId, lockObj.Counter);
                if (lockObj.Counter == 0)
                {
                    _lockPool.Remove(key);
                }
            }
        }

        /// <summary>
        /// 从锁对象池中获取锁对象, 并且锁对象的引用计数加1.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        static LockObject GetLock(object key)
        {
            lock (_lockPool)
            {
                var lockObj = _lockPool[key] as LockObject;
                if (lockObj == null)
                {
                    lockObj = new LockObject();
                    _lockPool[key] = lockObj;
                }
                lockObj.Increate();
                return lockObj;
            }
        }

        /// <summary>
        /// 用法类似系统lock, 参数key为锁对象的键
        /// </summary>
        /// <param name="key"></param>
        /// <param name="action"></param>
        public static void Lock(object key, Action action)
        {
            var lockObj = GetLock(key);
            try
            {
                lock (lockObj)
                {
                    action();
                }
            }
            finally
            {
                ReleaseLock(key, lockObj);
            }
        }
    }
}
