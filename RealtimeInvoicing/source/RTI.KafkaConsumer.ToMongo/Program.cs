using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using EventStore.StreamConnectors.Kafka;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using MongoDB.Driver;

using RTI;
using RTI.KafkaConsumer.ToMongo;

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
            var connection = EventStoreConnection.Create(Strings.DatabaseConnections.EventStore, connectionSettings, "Consumers - MongoDB");
            connection.ConnectAsync().Wait();
            log.LogDebug("Connected to EventStore");
            return connection;
        });
        svc.AddSingleton(sp => {
            var client = new MongoClient(new MongoUrl(Strings.DatabaseConnections.MongoDb));
            return client.GetDatabase("demo");
        });
        svc.AddHostedService(sp => {
            var options = new KafkaMongoConsumerConfigurationOptions {
                Stream = Strings.Streams.InvoiceDocuments,
                Group = Strings.Kafka.Groups.MongoInvoiceDocuments,
                Topic = Strings.Kafka.Topics.MongoInvoiceDocuments,
                ConnectionString = Strings.DatabaseConnections.Kafka,
                CollectionName = Strings.Collections.Invoice
            };
            return new MongoDBConsumer(sp.GetRequiredService<IMongoDatabase>(), options, sp.GetRequiredService<IEventStoreConnection>(), sp.GetRequiredService<ILoggerFactory>());
        });
    })
    .Build();
host.Run();
