using System;
using System.Collections.Generic;
using System.IO;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using Kafka.Client.Exceptions;
using Kafka.Client.Utils;
using Org.Apache.Zookeeper.Data;
using ZooKeeperNet;

namespace Kafka.Client.ZooKeeperIntegration
{
    /// <summary>
    ///     Abstracts connection with ZooKeeper server
    /// </summary>
    public class ZooKeeperConnection : IZooKeeperConnection
    {
        public const int DefaultSessionTimeout = 30000;
        public static ILogger Logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(ZooKeeperConnection));
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private readonly object shuttingDownLock = new object();

        private readonly object syncLock = new object();
        private volatile ZooKeeper _zkclient;
        private volatile bool disposed;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ZooKeeperConnection" /> class.
        /// </summary>
        /// <param name="servers">
        ///     The list of ZooKeeper servers.
        /// </param>
        public ZooKeeperConnection(string servers)
            : this(servers, DefaultSessionTimeout) { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ZooKeeperConnection" /> class.
        /// </summary>
        /// <param name="servers">
        ///     The list of ZooKeeper servers.
        /// </param>
        /// <param name="sessionTimeout">
        ///     The session timeout.
        /// </param>
        public ZooKeeperConnection(string servers, int sessionTimeout)
        {
            Logger.Info("Enter constructor ...");
            Servers = servers;
            SessionTimeout = sessionTimeout;
            Logger.Info("Quit constructor ...");
        }

        /// <summary>
        ///     Gets the list of ZooKeeper servers.
        /// </summary>
        public string Servers { get; }

        /// <summary>
        ///     Gets the ZooKeeper session timeout
        /// </summary>
        public int SessionTimeout { get; }

        /// <summary>
        ///     Gets ZooKeeper client.
        /// </summary>
        public ZooKeeper GetInternalZKClient()
        {
            return _zkclient;
        }

        /// <summary>
        ///     Gets the ZooKeeper client state
        /// </summary>
        public KeeperState ClientState
        {
            get
            {
                if (_zkclient == null)
                {
                    return KeeperState.Unknown;
                }
                var currentState = _zkclient.State.State;
                if (currentState == ZooKeeper.States.CONNECTED.State)
                {
                    return KeeperState.SyncConnected;
                }
                if (currentState == ZooKeeper.States.CLOSED.State
                    || currentState == ZooKeeper.States.NOT_CONNECTED.State
                    || currentState == ZooKeeper.States.CONNECTING.State)
                {
                    return KeeperState.Disconnected;
                }
                return KeeperState.Unknown;
            }
        }

        /// <summary>
        ///     Connects to ZooKeeper server
        /// </summary>
        /// <param name="watcher">
        ///     The watcher to be installed in ZooKeeper.
        /// </param>
        public void Connect(IWatcher watcher)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            lock (syncLock)
            {
                if (_zkclient != null)
                {
                    throw new InvalidOperationException("ZooKeeper client has already been started");
                }

                try
                {
                    Logger.InfoFormat("Starting ZK client .. with connect handler.. {0}...", watcher.ToString());
                    _zkclient = new ZooKeeper(Servers, new TimeSpan(0, 0, 0, 0, SessionTimeout),
                                              watcher); //new ZkClientState(this.Servers, new TimeSpan(0, 0, 0, 0, this.SessionTimeout), watcher);
                    Logger.InfoFormat("Finish start ZK client .. with connect handler.. {0}...", watcher.ToString());
                }
                catch (IOException exc)
                {
                    throw new ZooKeeperException("Unable to connect to " + Servers, exc);
                }
            }
        }

        /// <summary>
        ///     Deletes znode for a given path
        /// </summary>
        /// <param name="path">
        ///     The given path.
        /// </param>
        public void Delete(string path)
        {
            Guard.NotNullNorEmpty(path, "path");

            EnsuresNotDisposedAndNotNull();
            _zkclient.Delete(path, -1);
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

            EnsuresNotDisposedAndNotNull();
            return _zkclient.Exists(path, true) != null;
        }

        /// <summary>
        ///     Creates znode using given create mode for given path and writes given data to it
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
        public string Create(string path, byte[] data, CreateMode mode)
        {
            Guard.NotNullNorEmpty(path, "path");

            EnsuresNotDisposedAndNotNull();
            var result = _zkclient.Create(path, data, Ids.OPEN_ACL_UNSAFE, mode);
            return result;
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

            EnsuresNotDisposedAndNotNull();
            return _zkclient.GetChildren(path, watch);
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
        /// <returns>
        ///     Data
        /// </returns>
        public byte[] ReadData(string path, Stat stats, bool watch)
        {
            Guard.NotNullNorEmpty(path, "path");

            EnsuresNotDisposedAndNotNull();
            var nodedata = _zkclient.GetData(path, watch, stats);
            return nodedata;
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
        public void WriteData(string path, byte[] data)
        {
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
        /// <param name="version">
        ///     Expected version of data
        /// </param>
        public void WriteData(string path, byte[] data, int version)
        {
            Guard.NotNullNorEmpty(path, "path");

            EnsuresNotDisposedAndNotNull();
            _zkclient.SetData(path, data, version);
        }

        /// <summary>
        ///     Gets time when connetion was created
        /// </summary>
        /// <param name="path">
        ///     The path.
        /// </param>
        /// <returns>
        ///     Connection creation time
        /// </returns>
        public long GetCreateTime(string path)
        {
            Guard.NotNullNorEmpty(path, "path");

            EnsuresNotDisposedAndNotNull();
            var stats = _zkclient.Exists(path, false);
            return stats != null ? ToUnixTimestampMillis(new DateTime(stats.Ctime)) : -1;
        }

        /// <summary>
        ///     Closes underlying ZooKeeper client
        /// </summary>
        public void Dispose()
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
                if (_zkclient != null)
                {
                    Logger.Debug("Closing ZooKeeper client connected to " + Servers);
                    _zkclient.Dispose();
                    _zkclient = null;
                    Logger.Debug("ZooKeeper client connection closed");
                }
            }
            catch (Exception exc)
            {
                Logger.WarnFormat("Ignoring unexpected errors on closing {0}", exc.FormatException());
            }
        }

        public static long ToUnixTimestampMillis(DateTime time)
        {
            var t = DateTime.SpecifyKind(time, DateTimeKind.Utc);
            return (long) (t.ToUniversalTime() - UnixEpoch).TotalMilliseconds;
        }

        /// <summary>
        ///     Ensures object wasn't disposed
        /// </summary>
        private void EnsuresNotDisposedAndNotNull()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
            if (_zkclient == null)
            {
                throw new ApplicationException("internal ZkClient _zkclient is null.");
            }
        }
    }
}