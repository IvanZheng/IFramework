using System;
using System.Linq;
using System.Threading.Tasks;
using IFramework.Infrastructure;
using IFramework.Infrastructure.EventSourcing.Domain;
using IFramework.Infrastructure.EventSourcing.Stores;
using IFramework.Message;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace IFramework.EventStore.Redis
{
    public class RedisSnapshotStore : ISnapshotStore
    {
        private readonly ILogger<RedisSnapshotStore> _logger;
        private readonly RedisSnapshotStoreOptions _options;
        private ConnectionMultiplexer _connectionMultiplexer;
        private IDatabase _db;
        private object AsyncState { get; set; }
        private static string FormatId(string id)
        {
            return $"snp:{{{id}}}";
        }

        public RedisSnapshotStore(IOptions<RedisSnapshotStoreOptions> options, ILogger<RedisSnapshotStore> logger)
        {
            _logger = logger;
            _options = options.Value;
        }

        public async Task Connect()
        {
            _connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(_options.ConnectionString)
                                                                .ConfigureAwait(false);
            _db = _connectionMultiplexer.GetDatabase(_options.DatabaseName, AsyncState);
        }

        public async Task<TAggregateRoot> GetAsync<TAggregateRoot>(string id) where TAggregateRoot : IEventSourcingAggregateRoot
        {
            var hashValues = await _db.HashGetAllAsync(FormatId(id))
                                      .ConfigureAwait(false);
            if (hashValues.Length == 0)
            {
                return default;
            }
            var snapshotPayload = new SnapshotPayload(hashValues);
            return snapshotPayload.Payload
                                  .ToJsonObject<TAggregateRoot>(true);
        }

        public async Task UpdateAsync(IEventSourcingAggregateRoot ar)
        {
            var snapshotPayload = new SnapshotPayload(ar);
            await _db.HashSetAsync(FormatId(snapshotPayload.Id), snapshotPayload.ToHashEntries())
                     .ConfigureAwait(false);
        }

        public class SnapshotPayload
        {
            public string Id { get; protected set; }
            public string TypeCode { get; protected set; }
            public string Payload { get; protected set; }
            public int Version { get; protected set; }

            public SnapshotPayload(HashEntry[] hashEntries)
            {
                Id = hashEntries.GetValue<string>(nameof(Id), null);
                Payload = hashEntries.GetValue<string>(nameof(Payload), null);
                Version = hashEntries.GetValue(nameof(Version), 0);
                TypeCode = hashEntries.GetValue<string>(nameof(TypeCode), null);
            }

            public SnapshotPayload(IEventSourcingAggregateRoot aggregateRoot)
            {
                if (aggregateRoot == null)
                {
                    throw new ArgumentNullException(nameof(aggregateRoot));
                }

                Id = aggregateRoot.Id;
                Payload = aggregateRoot.ToJson();
                Version = aggregateRoot.Version;
                TypeCode = aggregateRoot.GetType().GetFullNameWithAssembly();
            }

            public SnapshotPayload(string id, string typeCode, string payload, int version)
            {
                Id = id;
                TypeCode = typeCode;
                Payload = payload;
                Version = version;
            }
        }
    }
}