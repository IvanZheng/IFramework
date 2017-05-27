using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using Kafka.Client.Requests;
using Kafka.Client.Serialization;
using Kafka.Client.Utils;

namespace Kafka.Client.Cluster
{
    /// <summary>
    ///     Represents Kafka broker
    /// </summary>
    public class Broker : IWritable
    {
        public const byte DefaultPortSize = 4;
        public const byte DefaultBrokerIdSize = 4;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Broker" /> class.
        /// </summary>
        /// <param name="id">
        ///     The broker id.
        /// </param>
        /// <param name="host">
        ///     The broker host.
        /// </param>
        /// <param name="port">
        ///     The broker port.
        /// </param>
        public Broker(int id, string host, int port)
        {
            Id = id;
            Host = host;
            Port = port;
        }

        /// <summary>
        ///     Gets the broker Id.
        /// </summary>
        public int Id { get; }

        /// <summary>
        ///     Gets the broker host.
        /// </summary>
        public string Host { get; }

        /// <summary>
        ///     Gets the broker port.
        /// </summary>
        public int Port { get; }

        public int SizeInBytes => BitWorks.GetShortStringLength(Host, AbstractRequest.DefaultEncoding) +
                                  DefaultPortSize + DefaultBrokerIdSize;

        public void WriteTo(MemoryStream output)
        {
            Guard.NotNull(output, "output");

            using (var writer = new KafkaBinaryWriter(output))
            {
                WriteTo(writer);
            }
        }

        public void WriteTo(KafkaBinaryWriter writer)
        {
            Guard.NotNull(writer, "writer");

            writer.Write(Id);
            writer.WriteShortString(Host, AbstractRequest.DefaultEncoding);
            writer.Write(Port);
        }

        public static Broker CreateBroker(int id, string brokerInfoString)
        {
            if (string.IsNullOrEmpty(brokerInfoString))
                throw new ArgumentException(string.Format("Broker id {0} does not exist", id));

            var ser = new JavaScriptSerializer();
            var result = ser.Deserialize<Dictionary<string, object>>(brokerInfoString);
            var host = result["host"].ToString();
            return new Broker(id, host, int.Parse(result["port"].ToString(), CultureInfo.InvariantCulture));
        }

        public override string ToString()
        {
            var sb = new StringBuilder(1024);
            sb.AppendFormat("Broker.Id:{0},Host:{1},Port:{2}", Id, Host, Port);
            var s = sb.ToString();
            sb.Length = 0;
            return s;
        }

        internal static Broker ParseFrom(KafkaBinaryReader reader)
        {
            var id = reader.ReadInt32();
            var host = BitWorks.ReadShortString(reader, AbstractRequest.DefaultEncoding);
            var port = reader.ReadInt32();
            return new Broker(id, host, port);
        }
    }
}