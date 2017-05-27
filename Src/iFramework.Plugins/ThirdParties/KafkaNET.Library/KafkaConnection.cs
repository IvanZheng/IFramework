using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using Kafka.Client.Consumers;
using Kafka.Client.Exceptions;
using Kafka.Client.Producers;
using Kafka.Client.Requests;
using Kafka.Client.Responses;
using Kafka.Client.Serialization;
using Kafka.Client.Utils;

namespace Kafka.Client
{
    /// <summary>
    ///     Manages connections to the Kafka.
    /// </summary>
    public class KafkaConnection : IKafkaConnection
    {
        public static ILogger Logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(KafkaConnection));
        private readonly int bufferSize;
        private readonly int networkStreamReadTimeoutMs;
        private readonly int networkStreamWriteTimeoutMs;
        private readonly int port;
        private readonly int receiveTimeoutMs;
        private readonly long reconnectIntervalMs;
        private readonly object sendLock = new object();
        private readonly int sendTimeoutMs;

        private readonly string server;
        private bool connected;

        private volatile bool disposed;
        private int lastActiveTimeMs;
        private KafkaBinaryReader reader;

        private Socket socket;
        private NetworkStream stream;

        /// <summary>
        ///     Initializes a new instance of the KafkaConnection class.
        /// </summary>
        /// <param name="server">The server to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <param name="bufferSize"></param>
        /// <param name="sendTimeoutMs"></param>
        /// <param name="receiveTimeoutMs"></param>
        /// <param name="reconnectIntervalMs"></param>
        public KafkaConnection(string server,
            int port,
            int bufferSize,
            int sendTimeoutMs,
            int receiveTimeoutMs,
            int reconnectIntervalMs,
            int networkStreamReadTimeoutMs = 60 * 1000,
            int networkStreamWriteTimeoutMs = 60 * 1000)
        {
            this.server = server;
            this.port = port;
            this.bufferSize = bufferSize;
            this.sendTimeoutMs = sendTimeoutMs;
            this.receiveTimeoutMs = receiveTimeoutMs;
            this.reconnectIntervalMs = reconnectIntervalMs;
            this.networkStreamReadTimeoutMs = networkStreamReadTimeoutMs;
            this.networkStreamWriteTimeoutMs = networkStreamWriteTimeoutMs;
            Connect();
        }

        private bool Connected
        {
            get => socket != null && socket.Connected && connected &&
                   Environment.TickCount - lastActiveTimeMs <= reconnectIntervalMs;
            set => connected = value;
        }

        /// <summary>
        ///     Writes a producer request to the server.
        /// </summary>
        /// <remarks>
        ///     Write timeout is defaulted to infitite.
        /// </remarks>
        /// <param name="request">The <see cref="ProducerRequest" /> to send to the server.</param>
        public ProducerResponse Send(ProducerRequest request)
        {
            EnsuresNotDisposed();
            Guard.NotNull(request, "request");
            return Handle(request.RequestBuffer.GetBuffer(), new ProducerResponse.Parser(), request.RequiredAcks != 0);
        }

        /// <summary>
        ///     Writes a topic metadata request to the server.
        /// </summary>
        /// <remarks>
        ///     Write timeout is defaulted to infitite.
        /// </remarks>
        /// <param name="request">The <see cref="TopicMetadataRequest" /> to send to the server.</param>
        public IEnumerable<TopicMetadata> Send(TopicMetadataRequest request)
        {
            EnsuresNotDisposed();
            Guard.NotNull(request, "request");
            return Handle(request.RequestBuffer.GetBuffer(), new TopicMetadataRequest.Parser());
        }

        /// <summary>
        ///     Writes a fetch request to the server.
        /// </summary>
        /// <remarks>
        ///     Write timeout is defaulted to infitite.
        /// </remarks>
        /// <param name="request">The <see cref="FetchRequest" /> to send to the server.</param>
        public FetchResponse Send(FetchRequest request)
        {
            EnsuresNotDisposed();
            Guard.NotNull(request, "request");
            return Handle(request.RequestBuffer.GetBuffer(), new FetchResponse.Parser());
        }

        /// <summary>
        ///     Writes a offset request to the server.
        /// </summary>
        /// <remarks>
        ///     Write timeout is defaulted to infitite.
        /// </remarks>
        /// <param name="request">The <see cref="OffsetRequest" /> to send to the server.</param>
        public OffsetResponse Send(OffsetRequest request)
        {
            EnsuresNotDisposed();
            Guard.NotNull(request, "request");
            return Handle(request.RequestBuffer.GetBuffer(), new OffsetResponse.Parser());
        }

        /// <summary>
        ///     Close the connection to the server.
        /// </summary>
        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            if (stream != null)
                CloseConnection();
        }

        /// <summary>
        ///     Ensures that object was not disposed
        /// </summary>
        private void EnsuresNotDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        public override string ToString()
        {
            return string.Format("Server:{0} Port:{1} Disposed:{2}  ", server, port, disposed);
        }

        private void Connect()
        {
            var watch = Stopwatch.StartNew();
            if (socket != null)
                try
                {
                    CloseConnection();
                }
                catch (Exception e)
                {
                    Logger.Error(
                        string.Format("KafkaConnectio.Connect() exception in CloseConnection, duration={0}ms",
                            watch.ElapsedMilliseconds), e);
                }

            socket = null;

            IPAddress targetAddress;
            if (IPAddress.TryParse(server, out targetAddress))
            {
                try
                {
                    var newSocket = new Socket(targetAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
                    {
                        NoDelay = true,
                        ReceiveTimeout = receiveTimeoutMs,
                        SendTimeout = sendTimeoutMs,
                        SendBufferSize = bufferSize,
                        ReceiveBufferSize = bufferSize
                    };

                    var result = newSocket.BeginConnect(targetAddress, port, null, null);
                    // use receiveTimeoutMs as connectionTimeoutMs
                    result.AsyncWaitHandle.WaitOne(receiveTimeoutMs, true);
                    result.AsyncWaitHandle.Close();

                    if (newSocket.Connected)
                        socket = newSocket;
                    else
                        newSocket.Close();
                }
                catch (Exception ex)
                {
                    Logger.Error(
                        string.Format("KafkaConnectio.Connect() failed, duration={0}ms,this={1},targetAddress={2}",
                            watch.ElapsedMilliseconds, this, targetAddress), ex);
                    throw new UnableToConnectToHostException(targetAddress.ToString(), port, ex);
                }
            }
            else
            {
                var addresses =
                    Dns.GetHostAddresses(server)
                        .Where(
                            h =>
                                h.AddressFamily == AddressFamily.InterNetwork ||
                                h.AddressFamily == AddressFamily.InterNetworkV6);

                foreach (var address in addresses)
                    try
                    {
                        var newSocket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
                        {
                            NoDelay = true,
                            ReceiveTimeout = receiveTimeoutMs,
                            SendTimeout = sendTimeoutMs,
                            SendBufferSize = bufferSize,
                            ReceiveBufferSize = bufferSize
                        };

                        var result = newSocket.BeginConnect(address, port, null, null);
                        // use receiveTimeoutMs as connectionTimeoutMs
                        result.AsyncWaitHandle.WaitOne(receiveTimeoutMs, true);
                        result.AsyncWaitHandle.Close();

                        if (!newSocket.Connected)
                        {
                            newSocket.Close();
                            continue;
                        }

                        socket = newSocket;
                        break;
                    }
                    catch (Exception e)
                    {
                        Logger.Error(
                            string.Format("ErrorConnectingToAddress, duration={0}ms,address={1},server={2},port={3}",
                                watch.ElapsedMilliseconds, address, server, port), e);
                        throw new UnableToConnectToHostException(server, port, e);
                    }
            }

            if (socket == null)
            {
                Logger.ErrorFormat("UnableToConnectToHostException, duration={0}ms,server={1},port={2}",
                    watch.ElapsedMilliseconds, server, port);
                throw new UnableToConnectToHostException(server, port);
            }

            Logger.DebugFormat("KafkaConnection.Connect() succeeded, duration={0}ms,server={1},port={2}",
                watch.ElapsedMilliseconds, server, port);

            stream = new NetworkStream(socket, true)
            {
                ReadTimeout = networkStreamReadTimeoutMs,
                WriteTimeout = networkStreamWriteTimeoutMs
            };
            reader = new KafkaBinaryReader(stream);
            Connected = true;
            lastActiveTimeMs = Environment.TickCount;
        }

        private void CloseConnection()
        {
            socket.Shutdown(SocketShutdown.Both);
            socket = null;
            stream.Close();
            stream = null;
        }


        private T Handle<T>(byte[] data, IResponseParser<T> parser, bool shouldParse = true)
        {
            try
            {
                var response = default(T);
                lock (sendLock)
                {
                    Send(data);

                    if (shouldParse)
                        response = parser.ParseFrom(reader);
                    lastActiveTimeMs = Environment.TickCount;
                }
                return response;
            }
            catch (Exception e)
            {
                if (e is IOException || e is SocketException || e is InvalidOperationException)
                    Connected = false;
                throw;
            }
        }

        /// <summary>
        ///     Writes data to the server.
        /// </summary>
        /// <param name="data">The data to write to the server.</param>
        private void Send(byte[] data)
        {
            if (!Connected)
                Connect();

            try
            {
                //// Send the message to the connected TcpServer. 
                stream.Write(data, 0, data.Length);
                stream.Flush();
            }
            catch (Exception e)
            {
                if (e is IOException || e is SocketException || e is InvalidOperationException)
                    Connected = false;
                throw;
            }
        }
    }
}