# Snowflake 初始化与 Redis 适配器

单独引用 IFramework 时，SnowIdWorker 默认使用进程内生成器。跨进程使用本地模式时，部署方需分配独占的 Id 和 DataCenterId。核心包没有 StackExchange.Redis 依赖。

多实例支付应用引用同版本的 IFramework.Snowflake.Redis。启动时，在首次访问 SnowIdWorker.Instance 或调用 GenerateId 前注册一次。可以复用应用已经持有的 Redis 连接，连接由应用管理。下面的 redis 是 StackExchange.Redis.IConnectionMultiplexer。

```csharp
using IFramework.Infrastructure;

RedisSnowflake.Configure(redis, usePodName: true, podScope: "gench-default");
```

PayCenter 可将这一行放在 Startup.ConfigureServices 的 UseRedis 分支、现有 ConnectionMultiplexer.Connect 调用之后。所有共享支付 ID 空间的实例使用相同的 Redis 逻辑库和键前缀。每个集群及命名空间组合使用不同的 podScope。单 Pod 只运行一个发号进程；框架优先读取 POD_NAME，未设置则使用 HOSTNAME。默认不按 Pod 名绑定，可用于没有稳定主机名的普通进程。原 SnowIdWorker:RedisConnectionName 配置需改为上述显式注册。

Redis 为 1,024 组编号提供原子占用。申请及接管发生在首次初始化，发号在内存中完成。每 10 分钟后台延长持有记录的 24 小时有效期。正常退出后，按 Pod 名绑定的编号保留 24 小时，同名重启可以复用。无 Pod 绑定的编号可立即重新分配。崩溃后无人续期的记录也会过期。此机制适用于新适配器写入的记录，旧版本遗留的永久占用键需要在确认旧进程退役后清理。

Redis 网络中断时，已经启动的进程继续本地发号。断线超过 24 小时可能与新持有者重号；恢复后如果确认已失去所有权，本进程停止发号，需要重启。数据库唯一索引仍应保留。首次从主机名哈希模式升级时，先停止共享编号空间的旧发号实例，再启用新模式。
