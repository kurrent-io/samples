using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using EventStore.StreamConnectors.Redis;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using RTI;
using RTI.Writers.Redis;

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
            var connection = EventStoreConnection.Create(Strings.DatabaseConnections.EventStore, connectionSettings, "Writers - Redis");
            connection.ConnectAsync().Wait();
            log.LogDebug("Connected to EventStore");
            return connection;
        });
        svc.AddSingleton(sp => {
            var redis = ConnectionMultiplexer.Connect(Strings.DatabaseConnections.Redis); //maps to redis
            return redis.GetDatabase();
        });
        svc.AddHostedService(sp => {
            var options = new RedisConfigurationOptions {
                Stream = Strings.Streams.InvoiceDocuments,
                Continuous = true,
                KeyPrefix = Strings.Collections.Invoice
            };
            var processor = new InvoiceStreamProcessor(sp.GetRequiredService<IDatabase>(), options, sp.GetRequiredService<IEventStoreConnection>(), sp.GetRequiredService<ILoggerFactory>());
            return processor;
        });
    })
    .Build();

host.Run();