using System;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace IFramework.Infrastructure
{
    internal interface ISnowIdSlotStore
    {
        Task<string> Read(string key);
        Task<bool> Replace(string key, string expected, string value);
    }

    internal sealed class RedisSnowIdSlotStore : ISnowIdSlotStore
    {
        private static readonly TimeSpan Retention = TimeSpan.FromDays(1);
        private readonly IDatabase database;
        private readonly string prefix;

        internal RedisSnowIdSlotStore(IDatabase database, string prefix)
        {
            this.database = database;
            this.prefix = prefix;
        }

        private RedisKey Key(string key) => prefix + ":" + key;

        public async Task<string> Read(string key) =>
            (string)await database.StringGetAsync(Key(key), CommandFlags.DemandMaster).ConfigureAwait(false);

        public async Task<bool> Replace(string key, string expected, string value)
        {
            RedisKey redisKey = Key(key);
            if (expected == null)
                return await database.StringSetAsync(redisKey, value, Retention, when: When.NotExists).ConfigureAwait(false);

            var transaction = database.CreateTransaction();
            transaction.AddCondition(Condition.StringEqual(redisKey, expected));
            var write = transaction.StringSetAsync(redisKey, value, Retention);
            if (!await transaction.ExecuteAsync().ConfigureAwait(false)) return false;
            return await write.ConfigureAwait(false);
        }
    }
}
