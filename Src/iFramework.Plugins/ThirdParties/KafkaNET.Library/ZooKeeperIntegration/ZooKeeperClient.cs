using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Kafka.Client.Exceptions;
using Kafka.Client.Utils;
using Org.Apache.Zookeeper.Data;
using ZooKeeperNet;
using ThreadState = System.Threading.ThreadState;

namespace Kafka.Client.ZooKeeperIntegration
{
    /// <summary>
    ///     Abstracts the interaction with zookeeper and allows permanent (not just one time) watches on nodes in ZooKeeper
    /// </summary>
    public partial class ZooKeeperClient : IZooKeeperClient
    {
        private const int DefaultConnectionTimeout = 30000; // 30sec safe default for connecting.
        public const string DefaultConsumersPath = "/consumers";
        public const string DefaultBrokerIdsPath = "/brokers/ids";
        public const string DefaultBrokerTopicsPath = "/brokers/topics";
        private readonly int connectionTimeout;
        private readonly IZooKeeperSerializer serializer;
        private readonly object shuttingDownLock = new object();

        private readonly object somethingChanged = new object();
        private readonly object stateChangedLock = new object();
        private readonly object znodeChangedLock = new object();
        private volatile IZooKeeperConnection connection;
        private KeeperState currentState;
        private volatile bool disposed;
        private bool shutdownTriggered;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ZooKeeperClient" /> class.
        /// </summary>
        /// <param name="connection">
        ///     The connection to ZooKeeper.
        /// </param>
        /// <param name="serializer">
        ///     The given serializer.
        /// </param>
        /// <param name="connectionTimeout">
        ///     The connection timeout (in miliseconds). Default is infinitive.
        /// </param>
        /// <remarks>
        ///     Default serializer is string UTF-8 serializer
        /// </remarks>
        public ZooKeeperClient(
            IZooKeeperConnection connection,
            IZooKeeperSerializer serializer,
            int connectionTimeout = DefaultConnectionTimeout)
        {
            this.serializer = serializer;
            this.connection = connection;
            this.connectionTimeout = connectionTimeout;
        }


        /// <summary>
        ///     Initializes a new instance of the <see cref="ZooKeeperClient" /> class.
        /// </summary>
        /// <param name="servers">
        ///     The list of ZooKeeper servers.
        /// </param>
        /// <param name="sessionTimeout">
        ///     The session timeout (in miliseconds).
        /// </param>
        /// <param name="serializer">
        ///     The given serializer.
        /// </param>
        /// <remarks>
        ///     Default serializer is string UTF-8 serializer.
        ///     It is recommended to use quite large sessions timeouts for ZooKeeper.
        /// </remarks>
        public ZooKeeperClient(string servers, int sessionTimeout, IZooKeeperSerializer serializer)
            : this(new ZooKeeperConnection(servers, sessionTimeout), serializer) { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ZooKeeperClient" /> class.
        /// </summary>
        /// <param name="servers">
        ///     The list of ZooKeeper servers.
        /// </param>
        /// <param name="sessionTimeout">
        ///     The session timeout (in miliseconds).
        /// </param>
        /// <param name="serializer">
        ///     The given serializer.
        /// </param>
        /// <param name="connectionTimeout">
        ///     The connection timeout (in miliseconds).
        /// </param>
        /// <remarks>
        ///     Default serializer is string UTF-8 serializer.
        ///     It is recommended to use quite large sessions timeouts for ZooKeeper.
        /// </remarks>
        public ZooKeeperClient(
            string servers,
            int sessionTimeout,
            IZooKeeperSerializer serializer,
            int connectionTimeout)
            : this(new ZooKeeperConnection(servers, sessionTimeout), serializer, connectionTimeout)
        {
            Logger.Info("Enter constructor ...");
            Logger.Info("Quit constructor ...");
        }

        private ZooKeeperClient() { }
        //public int? IdleTime { get; private set; }

        public ReaderWriterLockSlim SlimLock { get; } = new ReaderWriterLockSlim();

        /// <summary>
        ///     Connects to ZooKeeper server within given time period and installs watcher in ZooKeeper
        /// </summary>
        /// <remarks>
        ///     Also, starts background thread for event handling
        /// </remarks>
        public void Connect()
        {
            EnsuresNotDisposed();
            var started = false;
            try
            {
                Logger.Info("Enter connect ...");
                shutdownTriggered = false;
                eventWorker = new Thread(RunEventWorker) {IsBackground = true};
                eventWorker.Name = "ZooKeeperkWatcher-EventThread-" + eventWorker.ManagedThreadId + "-" +
                                   connection.Servers;
                eventWorker.Start();
                Logger.Info("Will connect ...");
                connection.Connect(this);
                Logger.Info("Finish connect ...");
                Logger.Debug("Awaiting connection to Zookeeper server");
                if (!WaitUntilConnected(connectionTimeout))
                {
                    throw new ZooKeeperException(
                                                 "Unable to connect to zookeeper server within timeout: " + connectionTimeout);
                }
                started = true;
                Logger.Debug("Connection to Zookeeper server established");
            }
            catch (ThreadInterruptedException)
            {
                throw new InvalidOperationException(
                                                    "Not connected with zookeeper server yet. Current state is " + connection.ClientState);
            }
            finally
            {
                if (!started)
                {
                    Disconnect();
                }
            }
        }

        public KeeperState GetClientState()
        {
            return connection == null ? KeeperState.Disconnected : connection.ClientState;
        }

        /// <summary>
        ///     Closes current connection to ZooKeeper
        /// </summary>
        /// <remarks>
        ///     Also, stops background thread
        /// </remarks>
        public void Disconnect()
        {
            Logger.Debug("Closing ZooKeeperClient");
            shutdownTriggered = true;
            eventWorker.Interrupt();
            try
            {
                if (eventWorker.ThreadState != ThreadState.Unstarted)
                {
                    eventWorker.Join(5000);
                }
            }
            catch (ThreadStateException)
            {
                // exception means worker thread was interrupted even before we started waiting.
            }

            if (connection != null)
            {
                connection.Dispose();
            }

            connection = null;
        }

        /// <summary>
        ///     Re-connect to ZooKeeper server when session expired
        /// </summary>
        /// <param name="servers">
        ///     The servers.
        /// </param>
        /// <param name="connectionTimeout">
        ///     The connection timeout.
        /// </param>
        public void Reconnect(string servers, int connectionTimeout)
        {
            EnsuresNotDisposed();
            Logger.Debug("Reconnecting");
            connection.Dispose();
            connection = new ZooKeeperConnection(servers, connectionTimeout);
            connection.Connect(this);
            Logger.Debug("Reconnected");
        }

        /// <summary>
        ///     Waits untill ZooKeeper connection is established
        /// </summary>
        /// <param name="connectionTimeout">
        ///     The connection timeout.
        /// </param>
        /// <returns>
        ///     Status
        /// </returns>
        public bool WaitUntilConnected(int connectionTimeout)
        {
            Guard.Greater(connectionTimeout, 0, "connectionTimeout");

            EnsuresNotDisposed();
            if (eventWorker != null && eventWorker == Thread.CurrentThread)
            {
                throw new InvalidOperationException("Must not be done in the ZooKeeper event thread.");
            }

            Logger.Debug("Waiting for keeper state: " + KeeperState.SyncConnected + " time out: " +
                         connectionTimeout); //.SyncConnected);

            var stillWaiting = true;
            lock (stateChangedLock)
            {
                currentState = connection.ClientState;
                if (currentState == KeeperState.SyncConnected)
                {
                    return true;
                }
                Logger.DebugFormat("Current state:{0} in the lib:{1}", currentState,
                                   connection.GetInternalZKClient().State);

                var stopWatch = Stopwatch.StartNew();
                while (currentState != KeeperState.SyncConnected)
                {
                    if (!stillWaiting)
                    {
                        return false;
                    }

                    stillWaiting = Monitor.Wait(stateChangedLock, connectionTimeout);
                    Logger.DebugFormat("Current state:{0} in the lib:{1}", currentState,
                                       connection.GetInternalZKClient().State);
                    currentState = connection.ClientState;
                    if (currentState == KeeperState.SyncConnected)
                    {
                        return true;
                    }
                    if (stopWatch.Elapsed.TotalMilliseconds > connectionTimeout)
                    {
                        break;
                    }
                    Thread.Sleep(1000);
                }

                Logger.DebugFormat("Current state:{0} in the lib:{1}", currentState,
                                   connection.GetInternalZKClient().State);
                currentState = connection.ClientState;
                if (currentState == KeeperState.SyncConnected)
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        ///     Retries given delegate until connections is established
        /// </summary>
        /// <param name="callback">
        ///     The delegate to invoke.
        /// </param>
        /// <typeparam name="T">
        ///     Type of data returned by delegate
        /// </typeparam>
        /// <returns>
        ///     data returned by delegate
        /// </returns>
        public T RetryUntilConnected<T>(Func<T> callback)
        {
            Guard.NotNull(callback, "callback");

            EnsuresNotDisposed();
            if (zooKeeperEventWorker != null && zooKeeperEventWorker == Thread.CurrentThread)
            {
                throw new InvalidOperationException("Must not be done in the zookeeper event thread");
            }

            var connectionWatch = Stopwatch.StartNew();
            var maxWait = connectionTimeout * 4L;
            while (connectionWatch.ElapsedMilliseconds < maxWait)
            {
                try
                {
                    return callback();
                }
                catch (KeeperException e)
                {
                    if (e.ErrorCode == KeeperException.Code.CONNECTIONLOSS) // KeeperException.ConnectionLossException)
                    {
                        Thread.Yield(); //TODO: is it better than Sleep(1) ?
                        WaitUntilConnected(connection.SessionTimeout);
                    }
                    else if (e.ErrorCode == KeeperException.Code.SESSIONEXPIRED
                    ) //  catch (KeeperException.SessionExpiredException)
                    {
                        Thread.Yield();
                        WaitUntilConnected(connection.SessionTimeout);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            connectionWatch.Stop();

            // exceeded time out, any good callers will handle the exception properly for their situation.
            throw KeeperException.Create(KeeperException.Code
                                                        .OPERATIONTIMEOUT); // KeeperException.OperationTimeoutException();
        }

        /// <summary>
        ///     Retries given delegate until connections is established
        /// </summary>
        /// <param name="callback">
        ///     The delegate to invoke.
        /// </param>
        /// <param name="timeout"></param>
        /// <typeparam name="T">
        ///     Type of data returned by delegate
        /// </typeparam>
        /// <returns>
        ///     data returned by delegate
        /// </returns>
        public T RetryUntilConnected<T>(Func<T> callback, TimeSpan timeout)
        {
            Guard.NotNull(callback, "callback");

            EnsuresNotDisposed();

            var currentTime = DateTime.UtcNow;
            while (true)
            {
                try
                {
                    return callback();
                }
                catch (KeeperException e)
                {
                    if (e.ErrorCode == KeeperException.Code.CONNECTIONLOSS)
                    {
                        if (OperationTimedOut(currentTime, timeout))
                        {
                            throw;
                        }
                        Thread.Yield();
                    }
                    else if (e.ErrorCode == KeeperException.Code.SESSIONEXPIRED)
                    {
                        if (OperationTimedOut(currentTime, timeout))
                        {
                            throw;
                        }
                        Thread.Yield();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            //catch (KeeperException.ConnectionLossException)
            //{
            //    if (OperationTimedOut(currentTime, timeout))
            //    {
            //        throw;
            //    }
            //    Thread.Yield();
            //}
            //catch (KeeperException.SessionExpiredException)
            //{
            //    if (OperationTimedOut(currentTime, timeout))
            //    {
            //        throw;
            //    }
            //    Thread.Yield();
            //}
        }

        /// <summary>
        ///     Checks whether znode for a given path exists
        /// </summary>
        /// <param name="path">
        ///     The given path.
        /// </param>
        /// <returns>
        ///     Result of check
        /// </returns>
        /// <remarks>
        ///     Will reinstall watcher in ZooKeeper if any listener for given path exists
        /// </remarks>
        public bool Exists(string path)
        {
            Guard.NotNullNorEmpty(path, "path");

            EnsuresNotDisposed();
            var hasListeners = HasListeners(path);
            return Exists(path, hasListeners);
        }

        /// <summary>
        ///     Checks whether znode for a given path exists.
        /// </summary>
        /// <param name="path">
        ///     The given path.
        /// </param>
        /// <param name="watch">
        ///     Indicates whether should reinstall watcher in ZooKeeper.
        /// </param>
        /// <returns>
        ///     Result of check
        /// </returns>
        public bool Exists(string path, bool watch)
        {
            Guard.NotNullNorEmpty(path, "path");

            EnsuresNotDisposed();
            return RetryUntilConnected(
                                       () => connection.Exists(path, watch));
        }

        /// <summary>
        ///     Gets all children for a given path
        /// </summary>
        /// <param name="path">
        ///     The given path.
        /// </param>
        /// <returns>
        ///     Children
        /// </returns>
        /// <remarks>
        ///     Will reinstall watcher in ZooKeeper if any listener for given path exists
        /// </remarks>
        public IEnumerable<string> GetChildren(string path)
        {
            Guard.NotNullNorEmpty(path, "path");

            EnsuresNotDisposed();
            var hasListeners = HasListeners(path);
            return GetChildren(path, hasListeners);
        }

        /// <summary>
        ///     Gets all children for a given path
        /// </summary>
        /// <param name="path">
        ///     The given path.
        /// </param>
        /// <param name="watch">
        ///     Indicates whether should reinstall watcher in ZooKeeper.
        /// </param>
        /// <returns>
        ///     Children
        /// </returns>
        public IEnumerable<string> GetChildren(string path, bool watch)
        {
            Guard.NotNullNorEmpty(path, "path");

            EnsuresNotDisposed();
            return RetryUntilConnected(
                                       () => connection.GetChildren(path, watch));
        }

        /// <summary>
        ///     Counts number of children for a given path.
        /// </summary>
        /// <param name="path">
        ///     The given path.
        /// </param>
        /// <returns>
        ///     Number of children
        /// </returns>
        /// <remarks>
        ///     Will reinstall watcher in ZooKeeper if any listener for given path exists.
        ///     Returns 0 if path does not exist
        /// </remarks>
        public int CountChildren(string path)
        {
            Guard.NotNullNorEmpty(path, "path");

            EnsuresNotDisposed();
            try
            {
                return GetChildren(path).Count();
            }
            catch (KeeperException e) // KeeperException.NoNodeException)
            {
                if (e.ErrorCode == KeeperException.Code.NONODE)
                {
                    return 0;
                }
                throw;
            }
        }

        /// <summary>
        ///     Fetches data from a given path in ZooKeeper
        /// </summary>
        /// <param name="path">
        ///     The given path.
        /// </param>
        /// <param name="stats">
        ///     The statistics.
        /// </param>
        /// <param name="watch">
        ///     Indicates whether should reinstall watcher in ZooKeeper.
        /// </param>
        /// <typeparam name="T">
        ///     Expected type of data
        /// </typeparam>
        /// <returns>
        ///     Data
        /// </returns>
        /// <remarks>
        ///     Uses given serializer to deserialize data
        ///     Use null for stats
        /// </remarks>
        public T ReadData<T>(string path, Stat stats, bool watch)
            where T : class
        {
            Guard.NotNullNorEmpty(path, "path");

            EnsuresNotDisposed();
            var bytes = RetryUntilConnected(
                                            () => connection.ReadData(path, stats, watch));
            return serializer.Deserialize(bytes) as T;
        }

        /// <summary>
        ///     Fetches data from a given path in ZooKeeper
        /// </summary>
        /// <param name="path">
        ///     The given path.
        /// </param>
        /// <param name="stats">
        ///     The statistics.
        /// </param>
        /// <typeparam name="T">
        ///     Expected type of data
        /// </typeparam>
        /// <returns>
        ///     Data
        /// </returns>
        /// <remarks>
        ///     Uses given serializer to deserialize data.
        ///     Will reinstall watcher in ZooKeeper if any listener for given path exists.
        ///     Use null for stats
        /// </remarks>
        public T ReadData<T>(string path, Stat stats) where T : class
        {
            Guard.NotNullNorEmpty(path, "path");

            EnsuresNotDisposed();
            var hasListeners = HasListeners(path);
            return ReadData<T>(path, null, hasListeners);
        }

        /// <summary>
        ///     Writes data for a given path
        /// </summary>
        /// <param name="path">
        ///     The given path.
        /// </param>
        /// <param name="data">
        ///     The data to write.
        /// </param>
        public void WriteData(string path, object data)
        {
            Guard.NotNullNorEmpty(path, "path");

            EnsuresNotDisposed();
            WriteData(path, data, -1);
        }

        /// <summary>
        ///     Writes data for a given path
        /// </summary>
        /// <param name="path">
        ///     The given path.
        /// </param>
        /// <param name="data">
        ///     The data to write.
        /// </param>
        /// <param name="expectedVersion">
        ///     Expected version of data
        /// </param>
        /// <remarks>
        ///     Use -1 for expected version
        /// </remarks>
        public void WriteData(string path, object data, int expectedVersion)
        {
            Guard.NotNullNorEmpty(path, "path");

            EnsuresNotDisposed();
            var bytes = serializer.Serialize(data);
            RetryUntilConnected(
                                () =>
                                {
                                    connection.WriteData(path, bytes, expectedVersion);
                                    return null as object;
                                });
        }

        /// <summary>
        ///     Deletes znode for a given path
        /// </summary>
        /// <param name="path">
        ///     The given path.
        /// </param>
        /// <returns>
        ///     Status
        /// </returns>
        public bool Delete(string path)
        {
            Guard.NotNullNorEmpty(path, "path");

            EnsuresNotDisposed();
            return RetryUntilConnected(
                                       () =>
                                       {
                                           try
                                           {
                                               connection.Delete(path);
                                               return true;
                                           }
                                           catch (KeeperException e) // KeeperException.NoNodeException)
                                           {
                                               if (e.ErrorCode == KeeperException.Code.NONODE)
                                               {
                                                   return false;
                                               }
                                               throw;
                                           }
                                       });
        }

        /// <summary>
        ///     Deletes znode and his children for a given path
        /// </summary>
        /// <param name="path">
        ///     The given path.
        /// </param>
        /// <returns>
        ///     Status
        /// </returns>
        public bool DeleteRecursive(string path)
        {
            Guard.NotNullNorEmpty(path, "path");

            EnsuresNotDisposed();
            IEnumerable<string> children;
            try
            {
                children = GetChildren(path, false);
            }
            catch (KeeperException e)
            {
                if (e.ErrorCode == KeeperException.Code.NONODE)
                {
                    return true;
                }
                throw;
            }

            foreach (var child in children)
            {
                if (!DeleteRecursive(path + "/" + child))
                {
                    return false;
                }
            }

            return Delete(path);
        }

        /// <summary>
        ///     Creates persistent znode and all intermediate znodes (if do not exist) for a given path
        /// </summary>
        /// <param name="path">
        ///     The given path.
        /// </param>
        public void MakeSurePersistentPathExists(string path)
        {
            Guard.NotNullNorEmpty(path, "path");

            EnsuresNotDisposed();
            if (!Exists(path))
            {
                CreatePersistent(path, true);
            }
        }

        /// <summary>
        ///     Fetches children for a given path
        /// </summary>
        /// <param name="path">
        ///     The path.
        /// </param>
        /// <returns>
        ///     Children or null, if znode does not exist
        /// </returns>
        public IEnumerable<string> GetChildrenParentMayNotExist(string path)
        {
            Guard.NotNullNorEmpty(path, "path");

            EnsuresNotDisposed();
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
        }

        /// <summary>
        ///     Fetches data from a given path in ZooKeeper
        /// </summary>
        /// <typeparam name="T">
        ///     Expected type of data
        /// </typeparam>
        /// <param name="path">
        ///     The given path.
        /// </param>
        /// <returns>
        ///     Data or null, if znode does not exist
        /// </returns>
        public T ReadData<T>(string path)
            where T : class
        {
            Guard.NotNullNorEmpty(path, "path");

            EnsuresNotDisposed();
            return ReadData<T>(path, false);
        }

        /// <summary>
        ///     Closes connection to ZooKeeper
        /// </summary>
        public void Dispose()
        {
            SlimLock.EnterWriteLock();
            try
            {
                if (disposed)
                {
                    return;
                }

                lock (shuttingDownLock)
                {
                    if (disposed)
                    {
                        return;
                    }

                    disposed = true;
                }

                try
                {
                    Disconnect();
                }
                catch (ThreadInterruptedException) { }
                catch (Exception exc)
                {
                    Logger.Debug("Ignoring unexpected errors on closing ZooKeeperClient", exc);
                }

                Logger.Debug("Closing ZooKeeperClient... done");
            }
            finally
            {
                SlimLock.ExitWriteLock();
            }
        }

        /// <summary>
        ///     Creates a persistent znode for a given path
        /// </summary>
        /// <param name="path">
        ///     The given path.
        /// </param>
        /// <param name="createParents">
        ///     Indicates, whether should create missing intermediate znodes
        /// </param>
        /// <remarks>
        ///     Persistent znodes won't disappear after session close
        /// </remarks>
        public void CreatePersistent(string path, bool createParents)
        {
            Guard.NotNullNorEmpty(path, "path");
            EnsuresNotDisposed();
            try
            {
                Create(path, null, CreateMode.Persistent);
            }

            catch (KeeperException e)
            {
                if (e.ErrorCode == KeeperException.Code.NODEEXISTS)
                {
                    if (!createParents)
                    {
                        throw;
                    }
                }
                else if (e.ErrorCode == KeeperException.Code.NONODE)
                {
                    if (!createParents)
                    {
                        throw;
                    }

                    var parentDir = path.Substring(0, path.LastIndexOf('/'));
                    CreatePersistent(parentDir, true);
                    CreatePersistent(path, true);
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        ///     Creates a persistent znode for a given path
        /// </summary>
        /// <param name="path">
        ///     The given path.
        /// </param>
        /// <remarks>
        ///     Persistent znodes won't disappear after session close
        ///     Doesn't re-create missing intermediate znodes
        /// </remarks>
        public void CreatePersistent(string path)
        {
            Guard.NotNullNorEmpty(path, "path");
            EnsuresNotDisposed();
            CreatePersistent(path, false);
        }

        /// <summary>
        ///     Creates a persistent znode for a given path and writes data into it
        /// </summary>
        /// <param name="path">
        ///     The given path.
        /// </param>
        /// <param name="data">
        ///     The data to write.
        /// </param>
        /// <remarks>
        ///     Persistent znodes won't disappear after session close
        ///     Doesn't re-create missing intermediate znodes
        /// </remarks>
        public void CreatePersistent(string path, object data)
        {
            Guard.NotNullNorEmpty(path, "path");
            EnsuresNotDisposed();
            Create(path, data, CreateMode.Persistent);
        }

        /// <summary>
        ///     Creates a sequential, persistent znode for a given path and writes data into it
        /// </summary>
        /// <param name="path">
        ///     The given path.
        /// </param>
        /// <param name="data">
        ///     The data to write.
        /// </param>
        /// <remarks>
        ///     Persistent znodes won't disappear after session close
        ///     Doesn't re-create missing intermediate znodes
        /// </remarks>
        /// <returns>
        ///     The created znode's path
        /// </returns>
        public string CreatePersistentSequential(string path, object data)
        {
            Guard.NotNullNorEmpty(path, "path");
            EnsuresNotDisposed();
            return Create(path, data, CreateMode.PersistentSequential);
        }

        /// <summary>
        ///     Creates a ephemeral znode for a given path
        /// </summary>
        /// <param name="path">
        ///     The given path.
        /// </param>
        /// <remarks>
        ///     Ephemeral znodes will disappear after session close
        /// </remarks>
        public void CreateEphemeral(string path)
        {
            Guard.NotNullNorEmpty(path, "path");
            EnsuresNotDisposed();
            Create(path, null, CreateMode.Ephemeral);
        }

        /// <summary>
        ///     Creates a ephemeral znode for a given path and writes data into it
        /// </summary>
        /// <param name="path">
        ///     The given path.
        /// </param>
        /// <param name="data">
        ///     The data to write.
        /// </param>
        /// <remarks>
        ///     Ephemeral znodes will disappear after session close
        /// </remarks>
        public void CreateEphemeral(string path, object data)
        {
            Guard.NotNullNorEmpty(path, "path");
            EnsuresNotDisposed();
            Create(path, data, CreateMode.Ephemeral);
        }

        /// <summary>
        ///     Creates a ephemeral, sequential znode for a given path and writes data into it
        /// </summary>
        /// <param name="path">
        ///     The given path.
        /// </param>
        /// <param name="data">
        ///     The data to write.
        /// </param>
        /// <remarks>
        ///     Ephemeral znodes will disappear after session close
        /// </remarks>
        /// <returns>
        ///     Created znode's path
        /// </returns>
        public string CreateEphemeralSequential(string path, object data)
        {
            Guard.NotNullNorEmpty(path, "path");
            EnsuresNotDisposed();
            return Create(path, data, CreateMode.EphemeralSequential);
        }

        /// Fetches data for given path
        /// </summary>
        /// <param name="path">
        ///     The given path.
        /// </param>
        /// <param name="returnNullIfPathNotExists">
        ///     Indicates, whether should return null or throw exception when
        ///     znode doesn't exist
        /// </param>
        /// <typeparam name="T">
        ///     Expected type of data
        /// </typeparam>
        /// <returns>
        ///     Data
        /// </returns>
        public T ReadData<T>(string path, bool returnNullIfPathNotExists)
            where T : class
        {
            Guard.NotNullNorEmpty(path, "path");
            EnsuresNotDisposed();
            try
            {
                return ReadData<T>(path, null);
            }
            catch (KeeperException e)
            {
                if (e.ErrorCode == KeeperException.Code.NONODE)
                {
                    if (!returnNullIfPathNotExists)
                    {
                        throw;
                    }

                    return null;
                }
                throw;
            }
        }

        private bool OperationTimedOut(DateTime currentTime, TimeSpan timeout)
        {
            return DateTime.UtcNow.Subtract(currentTime)
                           .CompareTo(timeout) < 0;
        }

        public long GetCreateTime(string path)
        {
            Guard.NotNullNorEmpty(path, "path");
            EnsuresNotDisposed();
            return RetryUntilConnected(
                                       () => connection.GetCreateTime(path));
        }

        /// <summary>
        ///     Helper method to create znode
        /// </summary>
        /// <param name="path">
        ///     The given path.
        /// </param>
        /// <param name="data">
        ///     The data to write.
        /// </param>
        /// <param name="mode">
        ///     The create mode.
        /// </param>
        /// <returns>
        ///     The created znode's path
        /// </returns>
        private string Create(string path, object data, CreateMode mode)
        {
            if (path == null)
            {
                throw new ArgumentNullException("Path must not be null");
            }

            var bytes = data == null ? null : serializer.Serialize(data);
            return RetryUntilConnected(() =>
                                           connection.Create(path, bytes, mode));
        }

        /// <summary>
        ///     Ensures that object wasn't disposed
        /// </summary>
        private void EnsuresNotDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}