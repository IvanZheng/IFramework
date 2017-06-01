using System;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using Kafka.Client.Consumers;
using Kafka.Client.Utils;
using Kafka.Client.ZooKeeperIntegration.Events;
using ZooKeeperNet;

namespace Kafka.Client.ZooKeeperIntegration.Listeners
{
    internal class ZKSessionExpireListener<TData> : IZooKeeperStateListener
    {
        public static ILogger Logger = IoCFactory.Resolve<ILoggerFactory>().Create("ZKSessionExpireListener");

        private readonly string consumerIdString;
        private readonly ZKGroupDirs dirs;
        private readonly ZKRebalancerListener<TData> loadBalancerListener;
        private readonly TopicCount topicCount;
        private readonly ZookeeperConsumerConnector zkConsumerConnector;

        public ZKSessionExpireListener(ZKGroupDirs dirs,
                                       string consumerIdString,
                                       TopicCount topicCount,
                                       ZKRebalancerListener<TData> loadBalancerListener,
                                       ZookeeperConsumerConnector zkConsumerConnector)
        {
            this.consumerIdString = consumerIdString;
            this.loadBalancerListener = loadBalancerListener;
            this.zkConsumerConnector = zkConsumerConnector;
            this.dirs = dirs;
            this.topicCount = topicCount;
        }

        /// <summary>
        ///     Called when the ZooKeeper connection state has changed.
        /// </summary>
        /// <param name="args">
        ///     The <see cref="Kafka.Client.ZooKeeperIntegration.Events.ZooKeeperStateChangedEventArgs" /> instance
        ///     containing the event data.
        /// </param>
        /// <remarks>
        ///     Do nothing, since zkclient will do reconnect for us.
        /// </remarks>
        public void HandleStateChanged(ZooKeeperStateChangedEventArgs args)
        {
            Guard.NotNull(args, "args");
            Guard.Assert<ArgumentException>(() => args.State != KeeperState.Unknown);

            if (args.State != KeeperState.Disconnected)
            {
                Logger.Info("ZK session disconnected; shutting down fetchers and resetting state");

                // Shutdown fetcher threads until we reconnect
                loadBalancerListener.ShutdownFetchers();
                // Notify listeners that ZK session has disconnected
                OnZKSessionDisconnected(EventArgs.Empty);
            }
            else if (args.State == KeeperState.SyncConnected)
            {
                Logger.Info("Performing rebalancing. ZK session has reconnected");

                // Restart fetcher threads via rebalance in case we no longer own a partition
                loadBalancerListener.AsyncRebalance();
            }
        }

        /// <summary>
        ///     Called after the ZooKeeper session has expired and a new session has been created.
        /// </summary>
        /// <param name="args">
        ///     The <see cref="Kafka.Client.ZooKeeperIntegration.Events.ZooKeeperSessionCreatedEventArgs" />
        ///     instance containing the event data.
        /// </param>
        /// <remarks>
        ///     You would have to re-create any ephemeral nodes here.
        ///     Explicitly trigger load balancing for this consumer.
        /// </remarks>
        public void HandleSessionCreated(ZooKeeperSessionCreatedEventArgs args)
        {
            Guard.NotNull(args, "args");

            // Notify listeners that ZK session has expired
            OnZKSessionExpired(EventArgs.Empty);

            Logger.InfoFormat("ZK session expired; release old broker partition ownership; re-register consumer {0}",
                              consumerIdString);
            loadBalancerListener.ResetState();
            zkConsumerConnector.RegisterConsumerInZk(dirs, consumerIdString, topicCount);

            Logger.Info("Performing rebalancing. ZK session has previously expired and a new session has been created");
            loadBalancerListener.AsyncRebalance();
        }

        public event EventHandler ZKSessionDisconnected;
        public event EventHandler ZKSessionExpired;

        protected virtual void OnZKSessionDisconnected(EventArgs args)
        {
            try
            {
                var handler = ZKSessionDisconnected;
                if (handler != null)
                {
                    handler(this, args);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Exception occurred within event handler for ZKSessionDisconnected event: " + ex.Message);
            }
        }

        protected virtual void OnZKSessionExpired(EventArgs args)
        {
            try
            {
                var handler = ZKSessionExpired;
                if (handler != null)
                {
                    handler(this, args);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Exception occurred within event handler for ZKSessionExpired event: " + ex.Message);
            }
        }
    }
}