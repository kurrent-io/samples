using EventStore.ClientAPI;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using MockInvoiceGenerator;

using ReactiveDomain.EventStore;
using ReactiveDomain.Foundation;
using ReactiveDomain.Messaging;

using RTI.ReactiveDomain;

using UserCredentials = EventStore.ClientAPI.SystemData.UserCredentials;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(svc => {
        svc.SetMinimumLevel(LogLevel.Debug);
    })
    .ConfigureServices(svc => {
        svc.AddSingleton(sp => {
            var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("EventStore");
            var credentials = new UserCredentials("admin", "changeit");
            var connectionSettings = ConnectionSettings.Create()
                .SetDefaultUserCredentials(credentials);
            var connection = EventStoreConnection.Create("ConnectTo=tcp://localhost:1113;HeartbeatTimeout=300000;HeartbeatInterval=5000", connectionSettings, "Mock Invoice Generator");
            connection.ConnectAsync().Wait();
            log.LogDebug("Connected to EventStore");
            return connection;
        });
        svc.AddSingleton<IConfiguredConnection>(sp => {
            var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger<ConfiguredConnection>();
            var esconn = sp.GetRequiredService<IEventStoreConnection>();
            var wrapped = new EventStoreConnectionWrapper(esconn);
            var snb = new PrefixedCamelCaseStreamNameBuilder();
            var serializer = new JsonMessageSerializer();
            return new ConfiguredConnection(wrapped, snb, serializer);
        });
        svc.AddSingleton<LookupsRm>();
        svc.AddHostedService<Main>();
    })
    .Build();

host.Run();
