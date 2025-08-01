using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using EventStore.StreamConnectors.Kafka;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using RTI;
using RTI.KafkaConsumers.Redis;

using StackExchange.Redis;

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(svc => {
        svc.SetMinimumLevel(LogLevel.Debug);
    })
    .ConfigureServices(svc => {
        svc.AddSingleton(sp => {
            var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("EventStore");
            var credentials = new UserCredentials("admin", "changeit");
            var connectionSettings = ConnectionSettings.Create()
                .SetDefaultUserCredentials(credentials);
            var connection = EventStoreConnection.Create(Strings.DatabaseConnections.EventStore, connectionSettings, "Consumers - Redis");
            connection.ConnectAsync().Wait();
            log.LogDebug("Connected to EventStore");
            return connection;
        });
        svc.AddSingleton(sp => {
            var redis = ConnectionMultiplexer.Connect(Strings.DatabaseConnections.Redis); // maps to redis_kafka
            return redis.GetDatabase();
        });

        svc.AddHostedService(sp => {
            var options = new KafkaRedisConsumerConfigurationOptions {
                Stream = Strings.Streams.InvoiceDocuments,
                Group = Strings.Kafka.Groups.RedisInvoiceDocuments,
                Topic = Strings.Kafka.Topics.RedisInvoiceDocuments,
                ConnectionString = Strings.DatabaseConnections.Kafka,
                KeyPrefix = Strings.Collections.Invoice
            };
            return new RedisConsumer(sp.GetRequiredService<IDatabase>(), options, sp.GetRequiredService<IEventStoreConnection>(), sp.GetRequiredService<ILoggerFactory>());
        });
    })
    .Build();
host.Run();
