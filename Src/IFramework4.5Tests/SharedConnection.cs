using System;
using IFramework.Config;
using StackExchange.Redis;

namespace IFramework4._5Tests
{
    public static class SharedConnection
    {
        private static ConnectionMultiplexer _muxer;

        public static ConnectionMultiplexer GetMuxer(string redisConnectionString = null)
        {
            string connectionString = Configuration.GetConnectionString(redisConnectionString ?? "RedisConnectionString");
            if (String.IsNullOrEmpty(connectionString))
                return null;

            if (_muxer == null)
            {
                _muxer = ConnectionMultiplexer.Connect(connectionString);
                _muxer.PreserveAsyncOrder = false;
            }

            return _muxer;
        }
    }
}
