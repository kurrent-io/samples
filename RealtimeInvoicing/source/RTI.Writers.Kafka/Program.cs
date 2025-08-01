using Confluent.Kafka;

using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using EventStore.StreamConnectors.Kafka;
using EventStore.StreamConnectors.RDBMS;
using EventStore.StreamConnectors.RDBMS.QueryFormatters;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using RTI;
using RTI.Writers.Kafka;

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(svc => {
        svc.SetMinimumLevel(LogLevel.Debug);
    })
    .ConfigureServices(svc => {
        svc.AddSingleton(sp => {
            var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("EventStore Connection [Construction]");
            var credentials = new UserCredentials("admin", "changeit");
            var connectionSettings = ConnectionSettings.Create()
                .SetDefaultUserCredentials(credentials);
            var connection = EventStoreConnection.Create(Strings.DatabaseConnections.EventStore, connectionSettings, "Writers - Kafka");
            connection.ConnectAsync().Wait();
            log.LogDebug("Connected to EventStore");
            return connection;
        });
        svc.AddHostedService(sp => {
            var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Kafka Stream Processor [Construction]");
            var options = new KafkaConfigurationOptions {
                Stream = Strings.Streams.InvoiceDocuments,
                Group = Strings.Kafka.Groups.MongoInvoiceDocuments,
                Topic = Strings.Kafka.Topics.MongoInvoiceDocuments,
                ConnectionString = Strings.DatabaseConnections.Kafka
            };
            return new MongoStreamProcessor(options, sp.GetRequiredService<IEventStoreConnection>(), sp.GetRequiredService<ILoggerFactory>());
        });
        
        svc.AddHostedService(sp => {
            var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Kafka Stream Processor [Construction]");
            var options = new KafkaConfigurationOptions {
                Stream = Strings.Streams.InvoiceDocuments,
                Group = Strings.Kafka.Groups.RedisInvoiceDocuments,
                Topic = Strings.Kafka.Topics.RedisInvoiceDocuments,
                ConnectionString = Strings.DatabaseConnections.Kafka
            };
            return new RedisStreamProcessor(options, sp.GetRequiredService<IEventStoreConnection>(), sp.GetRequiredService<ILoggerFactory>());
        });

        /// sql endpoints
        svc.AddHostedService(sp => {
            var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Kafka Stream Processor [Construction]");
            var options = new KafkaSqlConsumerConfigurationOptions {
                Stream = "invoice_header",
                Group = Strings.Kafka.Groups.RDBMSInvoiceHeaders,
                Topic = Strings.Kafka.Topics.RDBMSInvoiceHeaders,
                ConnectionString = Strings.DatabaseConnections.Kafka,
                Table = "Invoices",
                Columns = new ColumnMap[] {
                    new ColumnMap("id", "Id", typeof(Guid), true),
                    new ColumnMap("accountId", "AccountId", typeof(Guid)),
                    new ColumnMap("paymentTermsId", "PaymentTermsId", typeof(Guid)),
                    new ColumnMap("date", "Date", typeof(DateTime)),
                    new ColumnMap("status", "Status", typeof(string)),
                    new ColumnMap("total", "ItemsTotal", typeof(long)),
                    new ColumnMap("paymentsTotal", "PaymentsTotal", typeof(long)),
                    new ColumnMap("balanceDue", "BalanceDue", typeof(long))
                },
                QueryFormatter = new MicrosoftSqlQueryFormatter(),
                UsesStreamNameAsKey = false
            };
            return new InvoiceHeaderSqlStreamProcessor(options, sp.GetRequiredService<IEventStoreConnection>(), sp.GetRequiredService<ILoggerFactory>());
        });

        svc.AddHostedService(sp => {
            var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Kafka Stream Processor [Construction]");
            var options = new KafkaSqlConsumerConfigurationOptions {
                Stream = "invoice_item",
                Group = Strings.Kafka.Groups.RDBMSInvoiceItems,
                Topic = Strings.Kafka.Topics.RDBMSInvoiceItems,
                ConnectionString = Strings.DatabaseConnections.Kafka,
                Table = "InvoiceItems",
                Columns = new ColumnMap[] {
                    new ColumnMap("lineItemId", "LineItemId", typeof(Guid), true),
                    new ColumnMap("id", "InvoiceId", typeof(Guid)),
                    new ColumnMap("itemId", "ItemId", typeof(Guid)),
                    new ColumnMap("sku", "SKU", typeof(string)),
                    new ColumnMap("description", "Description", typeof(string)),
                    new ColumnMap("cost", "Cost", typeof(long)),
                    new ColumnMap("unitPrice", "UnitPrice", typeof(long)),
                    new ColumnMap("quantity", "QTY", typeof(long)),
                    new ColumnMap("subtotal", "SubTotal", typeof(long)),
                    new ColumnMap("removed", "HasBeenRemoved", typeof(bool))
                },
                QueryFormatter = new MicrosoftSqlQueryFormatter(),
                UsesStreamNameAsKey = false
            };
            return new InvoiceItemSqlStreamProcessor(options, sp.GetRequiredService<IEventStoreConnection>(), sp.GetRequiredService<ILoggerFactory>());
        });

        svc.AddHostedService(sp => {
            var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Kafka Stream Processor [Construction]");
            var options = new KafkaSqlConsumerConfigurationOptions {
                Stream = "invoice_pmt",
                Group = Strings.Kafka.Groups.RDBMSInvoicePayments,
                Topic = Strings.Kafka.Topics.RDBMSInvoicePayments,
                ConnectionString = Strings.DatabaseConnections.Kafka,
                Table = "InvoicePayments",
                Columns = new ColumnMap[] {
                    new ColumnMap("paymentId", "PaymentId", typeof(Guid), true),
                    new ColumnMap("id", "InvoiceId", typeof(Guid)),
                    new ColumnMap("amount", "Amount", typeof(long)),
                    new ColumnMap("received", "Received", typeof(DateTime)),
                    new ColumnMap("voided", "Voided", typeof(bool))
                },
                QueryFormatter = new MicrosoftSqlQueryFormatter(),
                UsesStreamNameAsKey = false
            };
            return new InvoicePaymentSqlStreamProcessor(options, sp.GetRequiredService<IEventStoreConnection>(), sp.GetRequiredService<ILoggerFactory>());
        });
    })
    .Build();
host.Run();
