using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using EventStore.StreamConnectors.MongoDb;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using MongoDB.Driver;

using RTI;
using RTI.Writers.MongoDB;

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
            var connection = EventStoreConnection.Create(Strings.DatabaseConnections.EventStore, connectionSettings, "Writers - MongoDB");
            connection.ConnectAsync().Wait();
            log.LogDebug("Connected to EventStore");
            return connection;
        });
        svc.AddSingleton(sp => {
            var client = new MongoClient(new MongoUrl(Strings.DatabaseConnections.MongoDb));
            return client.GetDatabase("demo");
        });
        svc.AddHostedService(sp => {
            var options = new MongoDbConfigurationOptions {
                Stream = Strings.Streams.InvoiceDocuments,
                Continuous = true,
                CollectionName = Strings.Collections.Invoice
            };
            var processor = new InvoiceStreamProcessor(options, sp.GetRequiredService<IMongoDatabase>(), sp.GetRequiredService<IEventStoreConnection>(), sp.GetRequiredService<ILoggerFactory>());
            return processor;
        });
    })
    .Build();

host.Run();
