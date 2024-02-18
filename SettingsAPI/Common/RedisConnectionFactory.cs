using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis.Extensions.Core.Abstractions;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.Core.Models;
using Microsoft.IdentityModel.Protocols;

namespace Cashrewards3API.Common.Context
{
    public class RedisConnectionFactory : IRedisCacheConnectionPoolManager
    {
        private static ConcurrentBag<Lazy<ConnectionMultiplexer>> connections;
        private readonly RedisConfiguration redisConfiguration;

        public RedisConnectionFactory(string connectionString)
        {
            this.redisConfiguration = new RedisConfiguration()
            {
                AbortOnConnectFail = false,
                Hosts = new RedisHost[] {
                    new RedisHost()
                    {
                        Host = connectionString,
                        Port = 6379
                    },
                },
                ConnectTimeout = 5000,
                SyncTimeout = 5000,
                Database = 0,
                ServerEnumerationStrategy = new ServerEnumerationStrategy()
                {
                    Mode = ServerEnumerationStrategy.ModeOptions.All,
                    TargetRole = ServerEnumerationStrategy.TargetRoleOptions.Any,
                    UnreachableServerAction = ServerEnumerationStrategy.UnreachableServerActionOptions.Throw
                },
                PoolSize = 50
            };
            Initialize();
        }

        public IConnectionMultiplexer GetConnection()
        {
            Lazy<ConnectionMultiplexer> response;
            var loadedLazys = connections.Where(lazy => lazy.IsValueCreated);

            if (loadedLazys.Count() == connections.Count)
            {
                response = connections.OrderBy(x => x.Value.GetCounters().TotalOutstanding).First();
            }
            else
            {
                response = connections.First(lazy => !lazy.IsValueCreated);
            }

            return response.Value;
        }

        private void Initialize()
        {
            connections = new ConcurrentBag<Lazy<ConnectionMultiplexer>>();

            for (int i = 0; i < redisConfiguration.PoolSize; i++)
            {
                connections.Add(new Lazy<ConnectionMultiplexer>(() =>
                    ConnectionMultiplexer.Connect(redisConfiguration.ConfigurationOptions)));
            }
        }

        public void Dispose()
        {
            var activeConnections = connections.Where(lazy => lazy.IsValueCreated).ToList();
            activeConnections.ForEach(connection => connection.Value.Dispose());
            Initialize();
        }

        public ConnectionPoolInformation GetConnectionInformations()
        {
            throw new NotImplementedException();
        }
    }
}