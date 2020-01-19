using System;
using System.Collections.Generic;
using System.Text;

namespace IFramework.EventStore.Redis
{
    public class RedisSnapshotStoreOptions
    {
        public int DatabaseName { get; set; } = -1;

        public string ConnectionString { get; set; }
    }
}
