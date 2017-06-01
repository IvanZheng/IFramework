using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using Kafka.Client.Utils;
using Kafka.Client.ZooKeeperIntegration.Events;
using Kafka.Client.ZooKeeperIntegration.Listeners;
using ZooKeeperNet;

namespace Kafka.Client.ZooKeeperIntegration
{
    public partial class ZooKeeperClient
    {
        /// <summary>
        ///     Represents the method that will handle a ZooKeeper event
        /// </summary>
        /// <param name="args">
        ///     The args.
        /// </param>
        /// <typeparam name="T">
        ///     Type of event data
        /// </typeparam>
        public delegate void ZooKeeperEventHandler<T>(T args)
            where T : ZooKeeperEventArgs;

        public static ILogger Logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(ZooKeeperClient));

        private readonly ConcurrentDictionary<string, ChildChangedEventItem> childChangedHandlers =
            new ConcurrentDictionary<string, ChildChangedEventItem>();

        private readonly ConcurrentDictionary<string, DataChangedEventItem> dataChangedHandlers =
            new ConcurrentDictionary<string, DataChangedEventItem>();

        private readonly object eventLock = new object();

        private readonly ConcurrentQueue<ZooKeeperEventArgs> eventsQueue = new ConcurrentQueue<ZooKeeperEventArgs>();
        private Thread eventWorker;
        private DateTime? idleTime;
        private ZooKeeperEventHandler<ZooKeeperSessionCreatedEventArgs> sessionCreatedHandlers;
        private ZooKeeperEventHandler<ZooKeeperStateChangedEventArgs> stateChangedHandlers;
        private Thread zooKeeperEventWorker;

        /// <summary>
        ///     Gets time (in miliseconds) of event thread iddleness
        /// </summary>
        /// <remarks>
        ///     Used for testing purpose
        /// </remarks>
        public int? IdleTime => idleTime.HasValue
                                    ? Convert.ToInt32((DateTime.Now - idleTime.Value).TotalMilliseconds)
                                    : (int?) null;

        public void Process(WatchedEvent e)
        {
            Logger.DebugFormat("Process called by handler. Received event, e.EventType:{0}  e.State: {1} e.Path :{2} ",
                               e.Type, e.State, e.Path);
            zooKeeperEventWorker = Thread.CurrentThread;
            if (shutdownTriggered)
            {
                Logger.DebugFormat("Shutdown triggered. Ignoring event. Type: {0}, Path: {1}, State: {2} ", e.Type,
                                   e.Path ?? "null", e.State);
                return;
            }

            try
            {
                EnsuresNotDisposed();
                var stateChanged = string.IsNullOrEmpty(e.Path);
                var znodeChanged = !string.IsNullOrEmpty(e.Path);
                var dataChanged =
                    e.Type == EventType.NodeDataChanged
                    || e.Type == EventType.NodeDeleted
                    || e.Type == EventType.NodeCreated
                    || e.Type == EventType.NodeChildrenChanged;

                Logger.DebugFormat("Process called by handler. stateChanged:{0} znodeChanged:{1}  dataChanged:{2} ",
                                   stateChanged, znodeChanged, dataChanged);

                lock (somethingChanged)
                {
                    try
                    {
                        if (stateChanged)
                        {
                            ProcessStateChange(e);
                        }

                        if (dataChanged)
                        {
                            ProcessDataOrChildChange(e);
                        }
                    }
                    finally
                    {
                        if (stateChanged)
                        {
                            lock (stateChangedLock)
                            {
                                Monitor.PulseAll(stateChangedLock);
                            }

                            if (e.State == KeeperState.Expired)
                            {
                                lock (znodeChangedLock)
                                {
                                    Monitor.PulseAll(znodeChangedLock);
                                }

                                foreach (var path in childChangedHandlers.Keys)
                                {
                                    Enqueue(new ZooKeeperChildChangedEventArgs(path));
                                }

                                foreach (var path in dataChangedHandlers.Keys)
                                {
                                    Enqueue(new ZooKeeperDataChangedEventArgs(path));
                                }
                            }
                        }

                        if (znodeChanged)
                        {
                            lock (znodeChangedLock)
                            {
                                Monitor.PulseAll(znodeChangedLock);
                            }
                        }
                    }

                    Monitor.PulseAll(somethingChanged);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error occurred while processing event: " + ex.FormatException());
            }
        }

        /// <summary>
        ///     Subscribes listeners on ZooKeeper state changes events
        /// </summary>
        /// <param name="listener">
        ///     The listener.
        /// </param>
        public void Subscribe(IZooKeeperStateListener listener)
        {
            Guard.NotNull(listener, "listener");

            EnsuresNotDisposed();
            StateChanged += listener.HandleStateChanged;
            SessionCreated += listener.HandleSessionCreated;
            Logger.Debug("Subscribed state changes handler " + listener.GetType().Name);
        }

        /// <summary>
        ///     Un-subscribes listeners on ZooKeeper state changes events
        /// </summary>
        /// <param name="listener">
        ///     The listener.
        /// </param>
        public void Unsubscribe(IZooKeeperStateListener listener)
        {
            Guard.NotNull(listener, "listener");

            EnsuresNotDisposed();
            StateChanged -= listener.HandleStateChanged;
            SessionCreated -= listener.HandleSessionCreated;
            Logger.Debug("Unsubscribed state changes handler " + listener.GetType().Name);
        }

        /// <summary>
        ///     Subscribes listeners on ZooKeeper child changes under given path
        /// </summary>
        /// <param name="path">
        ///     The parent path.
        /// </param>
        /// <param name="listener">
        ///     The listener.
        /// </param>
        public void Subscribe(string path, IZooKeeperChildListener listener)
        {
            Guard.NotNullNorEmpty(path, "path");
            Guard.NotNull(listener, "listener");

            EnsuresNotDisposed();
            childChangedHandlers.AddOrUpdate(
                                             path,
                                             new ChildChangedEventItem(listener.HandleChildChange),
                                             (key, oldValue) =>
                                             {
                                                 oldValue.ChildChanged += listener.HandleChildChange;
                                                 return oldValue;
                                             });
            WatchForChilds(path);
            Logger.Debug("Subscribed child changes handler " + listener.GetType().Name + " for path: " + path);
        }

        /// <summary>
        ///     Un-subscribes listeners on ZooKeeper child changes under given path
        /// </summary>
        /// <param name="path">
        ///     The parent path.
        /// </param>
        /// <param name="listener">
        ///     The listener.
        /// </param>
        public void Unsubscribe(string path, IZooKeeperChildListener listener)
        {
            Guard.NotNullNorEmpty(path, "path");
            Guard.NotNull(listener, "listener");

            EnsuresNotDisposed();
            childChangedHandlers.AddOrUpdate(
                                             path,
                                             new ChildChangedEventItem(),
                                             (key, oldValue) =>
                                             {
                                                 oldValue.ChildChanged -= listener.HandleChildChange;
                                                 return oldValue;
                                             });
            Logger.Debug("Unsubscribed child changes handler " + listener.GetType().Name + " for path: " + path);
        }

        /// <summary>
        ///     Subscribes listeners on ZooKeeper data changes under given path
        /// </summary>
        /// <param name="path">
        ///     The parent path.
        /// </param>
        /// <param name="listener">
        ///     The listener.
        /// </param>
        public void Subscribe(string path, IZooKeeperDataListener listener)
        {
            Guard.NotNullNorEmpty(path, "path");
            Guard.NotNull(listener, "listener");

            EnsuresNotDisposed();
            dataChangedHandlers.AddOrUpdate(
                                            path,
                                            new DataChangedEventItem(listener.HandleDataChange, listener.HandleDataDelete),
                                            (key, oldValue) =>
                                            {
                                                oldValue.DataChanged += listener.HandleDataChange;
                                                oldValue.DataDeleted += listener.HandleDataDelete;
                                                return oldValue;
                                            });
            WatchForData(path);
            Logger.Debug("Subscribed data changes handler " + listener.GetType().Name + " for path: " + path);
        }

        /// <summary>
        ///     Un-subscribes listeners on ZooKeeper data changes under given path
        /// </summary>
        /// <param name="path">
        ///     The parent path.
        /// </param>
        /// <param name="listener">
        ///     The listener.
        /// </param>
        public void Unsubscribe(string path, IZooKeeperDataListener listener)
        {
            Guard.NotNullNorEmpty(path, "path");
            Guard.NotNull(listener, "listener");

            EnsuresNotDisposed();
            dataChangedHandlers.AddOrUpdate(
                                            path,
                                            new DataChangedEventItem(),
                                            (key, oldValue) =>
                                            {
                                                oldValue.DataChanged -= listener.HandleDataChange;
                                                oldValue.DataDeleted -= listener.HandleDataDelete;
                                                return oldValue;
                                            });
            Logger.Debug("Unsubscribed data changes handler " + listener.GetType().Name + " for path: " + path);
        }

        /// <summary>
        ///     Un-subscribes all listeners
        /// </summary>
        public void UnsubscribeAll()
        {
            EnsuresNotDisposed();
            lock (eventLock)
            {
                stateChangedHandlers = null;
                sessionCreatedHandlers = null;
                childChangedHandlers.Clear();
                dataChangedHandlers.Clear();
            }

            Logger.Debug("Unsubscribed all handlers");
        }

        /// <summary>
        ///     Installs a child watch for the given path.
        /// </summary>
        /// <param name="path">
        ///     The parent path.
        /// </param>
        /// <returns>
        ///     the current children of the path or null if the znode with the given path doesn't exist
        /// </returns>
        public IEnumerable<string> WatchForChilds(string path)
        {
            Guard.NotNullNorEmpty(path, "path");

            EnsuresNotDisposed();
            if (zooKeeperEventWorker != null && Thread.CurrentThread == zooKeeperEventWorker)
            {
                throw new InvalidOperationException("Must not be done in the zookeeper event thread.");
            }

            return RetryUntilConnected(
                                       () =>
                                       {
                                           Exists(path);
                                           try
                                           {
                                               return GetChildren(path);
                                           }
                                           catch (KeeperException e)
                                           {
                                               if (e.ErrorCode == KeeperException.Code.NONODE)
                                               {
                                                   return null;
                                               }
                                               throw;
                                           }
                                       });
        }

        /// <summary>
        ///     Installs a data watch for the given path.
        /// </summary>
        /// <param name="path">
        ///     The parent path.
        /// </param>
        public void WatchForData(string path)
        {
            Guard.NotNullNorEmpty(path, "path");

            EnsuresNotDisposed();
            RetryUntilConnected(
                                () => Exists(path, true));
        }

        /// <summary>
        ///     Occurs when ZooKeeper connection state changes
        /// </summary>
        public event ZooKeeperEventHandler<ZooKeeperStateChangedEventArgs> StateChanged
        {
            add
            {
                EnsuresNotDisposed();
                lock (eventLock)
                {
                    stateChangedHandlers -= value;
                    stateChangedHandlers += value;
                }
            }
            remove
            {
                EnsuresNotDisposed();
                lock (eventLock)
                {
                    stateChangedHandlers -= value;
                }
            }
        }

        /// <summary>
        ///     Occurs when ZooKeeper session re-creates
        /// </summary>
        public event ZooKeeperEventHandler<ZooKeeperSessionCreatedEventArgs> SessionCreated
        {
            add
            {
                EnsuresNotDisposed();
                lock (eventLock)
                {
                    sessionCreatedHandlers -= value;
                    sessionCreatedHandlers += value;
                }
            }
            remove
            {
                EnsuresNotDisposed();
                lock (eventLock)
                {
                    sessionCreatedHandlers -= value;
                }
            }
        }

        /// <summary>
        ///     Checks whether any data or child listeners are registered
        /// </summary>
        /// <param name="path">
        ///     The path.
        /// </param>
        /// <returns>
        ///     Value indicates whether any data or child listeners are registered
        /// </returns>
        private bool HasListeners(string path)
        {
            ChildChangedEventItem childChanged;
            childChangedHandlers.TryGetValue(path, out childChanged);
            if (childChanged != null && childChanged.Count > 0)
            {
                return true;
            }

            DataChangedEventItem dataChanged;
            dataChangedHandlers.TryGetValue(path, out dataChanged);
            if (dataChanged != null && dataChanged.TotalCount > 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Event thread starting method
        /// </summary>
        private void RunEventWorker()
        {
            Logger.Debug("Starting ZooKeeper watcher event thread");
            try
            {
                PoolEventsQueue();
            }
            catch (ThreadInterruptedException)
            {
                Logger.Debug("Terminate ZooKeeper watcher event thread");
            }
        }

        /// <summary>
        ///     Pools ZooKeeper events form events queue
        /// </summary>
        /// <remarks>
        ///     Thread sleeps if queue is empty
        /// </remarks>
        private void PoolEventsQueue()
        {
            while (true)
            {
                while (!eventsQueue.IsEmpty)
                {
                    Dequeue();
                }

                lock (somethingChanged)
                {
                    Logger.Debug("Awaiting events ...");
                    idleTime = DateTime.Now;
                    Monitor.Wait(somethingChanged);
                    idleTime = null;
                }
            }
        }

        /// <summary>
        ///     Enqueues new event from ZooKeeper in events queue
        /// </summary>
        /// <param name="e">
        ///     The event from ZooKeeper.
        /// </param>
        private void Enqueue(ZooKeeperEventArgs e)
        {
            Logger.Debug("New event queued: " + e);
            eventsQueue.Enqueue(e);
        }

        /// <summary>
        ///     Dequeues event from events queue and invokes subscribed handlers
        /// </summary>
        private void Dequeue()
        {
            try
            {
                ZooKeeperEventArgs e;
                var success = eventsQueue.TryDequeue(out e);
                if (success)
                {
                    if (e != null)
                    {
                        Logger.Debug("Event dequeued: " + e);
                        switch (e.Type)
                        {
                            case ZooKeeperEventTypes.StateChanged:
                                OnStateChanged((ZooKeeperStateChangedEventArgs) e);
                                break;
                            case ZooKeeperEventTypes.SessionCreated:
                                OnSessionCreated((ZooKeeperSessionCreatedEventArgs) e);
                                break;
                            case ZooKeeperEventTypes.ChildChanged:
                                OnChildChanged((ZooKeeperChildChangedEventArgs) e);
                                break;
                            case ZooKeeperEventTypes.DataChanged:
                                OnDataChanged((ZooKeeperDataChangedEventArgs) e);
                                break;
                            default:
                                throw new InvalidOperationException("Not supported event type");
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                Logger.WarnFormat("Error handling event  {0}", exc.FormatException());
            }
        }

        /// <summary>
        ///     Processess ZooKeeper state changes events
        /// </summary>
        /// <param name="e">
        ///     The event data.
        /// </param>
        private void ProcessStateChange(WatchedEvent e)
        {
            Logger.Info("ProcessStateChange==zookeeper state changed (" + e.State + ")");
            lock (stateChangedLock)
            {
                Logger.InfoFormat("Current state:{0} in the lib:{1}", currentState,
                                  connection.GetInternalZKClient().State);
                currentState = e.State;
            }

            if (shutdownTriggered)
            {
                return;
            }

            Enqueue(new ZooKeeperStateChangedEventArgs(e.State));
            if (e.State == KeeperState.Expired)
            {
                while (true)
                {
                    try
                    {
                        Reconnect(connection.Servers, connection.SessionTimeout);
                        Enqueue(ZooKeeperSessionCreatedEventArgs.Empty);
                        break;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Exception occurred while trying to reconnect to ZooKeeper", ex);
                        Thread.Sleep(1000);
                    }
                }
            }
        }

        /// <summary>
        ///     Processess ZooKeeper childs or data changes events
        /// </summary>
        /// <param name="e">
        ///     The event data.
        /// </param>
        private void ProcessDataOrChildChange(WatchedEvent e)
        {
            if (shutdownTriggered)
            {
                return;
            }

            if (e.Type == EventType.NodeChildrenChanged
                || e.Type == EventType.NodeCreated
                || e.Type == EventType.NodeDeleted)
            {
                Enqueue(new ZooKeeperChildChangedEventArgs(e.Path));
            }

            if (e.Type == EventType.NodeDataChanged
                || e.Type == EventType.NodeCreated
                || e.Type == EventType.NodeDeleted)
            {
                Enqueue(new ZooKeeperDataChangedEventArgs(e.Path));
            }
        }

        /// <summary>
        ///     Invokes subscribed handlers for ZooKeeeper state changes event
        /// </summary>
        /// <param name="e">
        ///     The event data
        /// </param>
        private void OnStateChanged(ZooKeeperStateChangedEventArgs e)
        {
            try
            {
                var handlers = stateChangedHandlers;
                if (handlers == null)
                {
                    return;
                }

                foreach (var handler in handlers.GetInvocationList())
                {
                    Logger.Debug(e + " sent to " + handler.Target);
                }

                handlers(e);
            }
            catch (Exception exc)
            {
                Logger.ErrorFormat("Failed to handle state changed event.  {0}", exc.FormatException());
            }
        }

        /// <summary>
        ///     Invokes subscribed handlers for ZooKeeeper session re-creates event
        /// </summary>
        /// <param name="e">
        ///     The event data.
        /// </param>
        private void OnSessionCreated(ZooKeeperSessionCreatedEventArgs e)
        {
            var handlers = sessionCreatedHandlers;
            if (handlers == null)
            {
                return;
            }

            foreach (var handler in handlers.GetInvocationList())
            {
                Logger.Debug(e + " sent to " + handler.Target);
            }

            handlers(e);
        }

        /// <summary>
        ///     Invokes subscribed handlers for ZooKeeeper child changes event
        /// </summary>
        /// <param name="e">
        ///     The event data.
        /// </param>
        private void OnChildChanged(ZooKeeperChildChangedEventArgs e)
        {
            ChildChangedEventItem handlers;
            childChangedHandlers.TryGetValue(e.Path, out handlers);
            if (handlers == null || handlers.Count == 0)
            {
                return;
            }

            Exists(e.Path);
            try
            {
                var children = GetChildren(e.Path);
                e.Children = children;
            }
            catch (KeeperException ex) // KeeperException.NoNodeException)
            {
                if (ex.ErrorCode == KeeperException.Code.NONODE) { }
                else
                {
                    throw;
                }
            }

            handlers.OnChildChanged(e);
        }

        /// <summary>
        ///     Invokes subscribed handlers for ZooKeeeper data changes event
        /// </summary>
        /// <param name="e">
        ///     The event data.
        /// </param>
        private void OnDataChanged(ZooKeeperDataChangedEventArgs e)
        {
            DataChangedEventItem handlers;
            dataChangedHandlers.TryGetValue(e.Path, out handlers);
            if (handlers == null || handlers.TotalCount == 0)
            {
                return;
            }

            try
            {
                Exists(e.Path, true);
                var data = ReadData<string>(e.Path, null, true);
                e.Data = data;
                handlers.OnDataChanged(e);
            }
            catch (KeeperException ex)
            {
                if (ex.ErrorCode == KeeperException.Code.NONODE)
                {
                    handlers.OnDataDeleted(e);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}