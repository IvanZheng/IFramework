using System;
using System.Threading.Tasks;
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
        private readonly IMessageTypeProvider _messageTypeProvider;
        private readonly RedisSnapshotStoreOptions _options;
        private ConnectionMultiplexer _connectionMultiplexer;
        private IDatabase _db;
        private object AsyncState { get; set; }

        public RedisSnapshotStore(IOptions<RedisSnapshotStoreOptions> options, ILogger<RedisSnapshotStore> logger, IMessageTypeProvider messageTypeProvider)
        {
            _logger = logger;
            _messageTypeProvider = messageTypeProvider;
            _options = options.Value;
        }

        public async Task Connect()
        {
            _connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(_options.ConnectionString)
                                                                .ConfigureAwait(false);
            _db = _connectionMultiplexer.GetDatabase(_options.DatabaseName, AsyncState);
        }

        public Task<TAggregateRoot> GetAsync<TAggregateRoot>(string id) where TAggregateRoot : EventSourcingAggregateRoot
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(EventSourcingAggregateRoot ar)
        {
            throw new NotImplementedException();
        }
    }
}