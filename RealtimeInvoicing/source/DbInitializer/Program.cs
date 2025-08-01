using System.Data.Common;
using System.Net;
using System.Text.Json;

using Confluent.Kafka;

using DbInitializer;

using EventStore.ClientAPI;
using EventStore.ClientAPI.Projections;
using EventStore.ClientAPI.SystemData;
using EventStore.StreamConnectors;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using ReactiveDomain.EventStore;
using ReactiveDomain.Foundation;
using ReactiveDomain.Messaging;

using RTI;
using RTI.ReactiveDomain;

var credentials = new UserCredentials("admin", "changeit");

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(svc => {
        svc.SetMinimumLevel(LogLevel.Debug);
    })
    .ConfigureServices(svc => {
        svc.AddSingleton(sp => {
            var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("EventStore");
            var connectionSettings = ConnectionSettings.Create()
                .SetDefaultUserCredentials(credentials);
            var connection = EventStoreConnection.Create(Strings.DatabaseConnections.EventStore, connectionSettings, "Database Initializer");
            connection.ConnectAsync().Wait();
            log.LogDebug("Connected to EventStore");
            return connection;
        });
        svc.AddTransient<DbConnection>(sp => {
            var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("MsSql");
            var connection = new SqlConnection(Strings.DatabaseConnections.SqlServerAdmin);
            connection.Open();
            log.LogDebug("Connected to SQL Server");
            return connection;
        });
        svc.AddSingleton(sp => new AdminClientBuilder(Strings.DatabaseConnections.Kafka
                .Split(";")
                .Select(x => x.Split('='))
                .ToDictionary(x => x[0].Trim(), x => x[1].Trim()))
            .Build());
    })
    .Build();

var log = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("app");
var esconn = host.Services.GetRequiredService<IEventStoreConnection>();
var conn = new ConfiguredConnection(new EventStoreConnectionWrapper(esconn), new PrefixedCamelCaseStreamNameBuilder(), new JsonMessageSerializer());
var repo = conn.GetCorrelatedRepository();
var msg = MessageBuilder.New(() => new LoadCommand());

var projectionDocument = @"var outputStreamName = 'invoice_documents';
var runCalculations = function(s) {
    s.total = 0;
    s.paymentsTotal = 0;
    s.balanceDue = 0;

    for (var i = 0; i < s.items.length; i++) {
        s.total += s.items[i].subtotal ?? 0;
    }
    for (var i = 0; i < s.payments.length; i++) {
        s.paymentsTotal += s.payments[i].amount ?? 0;
    }

    s.balanceDue = s.total - s.paymentsTotal;
};

fromCategory('invoice')
    .foreachStream()
    .when({
        $init: function() {
            return {
                id: undefined,
                accountId: undefined,
                accountName: undefined,
                paymentTermsId: undefined,
                paymentTermsName: undefined,
                date: undefined,
                status: undefined,
                items: [],
                payments: [],
                total: 0,
                paymentsTotal: 0,
                balanceDue: 0
            }
        },
        Generated: function(s, e) {
            s.id = e.data.Id,
            s.accountId = e.data.AccountId;
            s.accountName = e.data.AccountName;
            s.date = e.data.Date;
            s.status = 'Opened';

            emit(outputStreamName, 'document', s, {});
        },
        PaymentTermsApplied: function(s, e) {
            s.paymentTermsId = e.data.PaymentTermsId;
            s.paymentTermsName = e.data.Description;

            emit(outputStreamName, 'document', s, {});
        },
        ItemAdded: function(s, e) {
            var item = {
                id: e.data.LineItemId,
                itemId: e.data.ItemId,
                sku: e.data.SKU,
                description: e.data.Description,
                cost: e.data.Cost,
                unitPrice: e.data.UnitPrice,
                quantity: e.data.Quantity,
                subtotal: e.data.UnitPrice * e.data.Quantity
            };
            s.items.push(item);
            
            runCalculations(s);

            emit(outputStreamName, 'document', s, {});
        },
        ItemRemoved: function(s, e) {
            for (var i = s.items.length - 1; i >= 0; i--) {
                if (s.items[i].id === e.data.LineItemId) { s.items.splice(i, 1); }
            }

            runCalculations(s);
            
            emit(outputStreamName, 'document', s, {});
        },
        Issued: function(s, e) {
            s.status = 'Issued';
            s.date = e.data.Date;

            emit(outputStreamName, 'document', s, {});
        },
        ReIssued: function(s, e) {
            s.status = 'Re-issued';
            s.date = e.data.Date;

            emit(outputStreamName, 'document', s, {});
        },
        PaymentApplied: function(s, e) {
            var pmt = {
                id: e.data.PaymentId,
                amount: e.data.Amount ?? 0,
                received: e.data.Received
            };
            s.payments.push(pmt);

            runCalculations(s);

            emit(outputStreamName, 'document', s, {});
        },
        PaymentVoided: function(s, e) {
            for(var i = s.payments.length - 1; i >= 0; i--) {
                if (s.payments[i].id === e.data.PaymentId) { s.payments.splice(i, 1); }
            }

            runCalculations(s);

            emit(outputStreamName, 'document', s, {});
        },
        StatusChanged: function(s, e) {
            s.status = e.data.Status;

            emit(outputStreamName, 'document', s, {});
        },
        Closed: function(s, e) {
            s.status = 'Closed';

            emit(outputStreamName, 'document', s, {});
        }
    });";
var projectionRdbms = @"var sendHeader = function(s) {
    emit('invoice_header', 'record', {
        id: s.id,
        accountId: s.accountId,
        accountName: s.accountName,
        paymentTermsId: s.paymentTermsId,
        paymentTermsName: s.paymentTermsName,
        date: s.date,
        status: s.status,
        total: s.total ?? 0,
        paymentsTotal: s.paymentsTotal ?? 0,
        balanceDue: s.balanceDue ?? 0
    }, { });
};
var sendLineItem = function(item) {
    emit('invoice_item', 'record', item, { });
};
var sendPaymentItem = function(pmt) {
    emit('invoice_pmt', 'record', pmt, { });
};
var runCalculations = function(s) {
    s.total = 0;
    s.paymentsTotal = 0;
    s.balanceDue = 0;

    for (var i = 0; i < s.items.length; i++) {
        var item = s.items[i];
        if(!item.removed) s.total += item.subtotal;
    }
    for (var i = 0; i < s.payments.length; i++) {
        var item = s.payments[i];
        if(!item.voided) s.paymentsTotal += item.amount;
    }

    s.balanceDue = s.total - s.paymentsTotal;

    sendHeader(s);
};

fromCategory('invoice')
    .foreachStream()
    .when({
        $init: function() {
            return {
                id: undefined,
                accountId: undefined,
                accountName: undefined,
                paymentTermsId: undefined,
                paymentTermsName: undefined,
                date: undefined,
                status: '',
                items: [],
                payments: [],
                total: 0,
                paymentsTotal: 0,
                balanceDue: 0
            }
        },
        Generated: function(s, e) {
            s.id = e.data.Id,
            s.accountId = e.data.AccountId;
            s.accountName = e.data.AccountName;
            s.date = e.data.Date;
            s.status = 'Opened';

            sendHeader(s);
        },
        PaymentTermsApplied: function(s, e) {
            s.paymentTermsId = e.data.PaymentTermsId;
            s.paymentTermsName = e.data.Description;
            
            sendHeader(s);
        },
        ItemAdded: function(s, e) {
            var item = {
                id: e.data.Id,
                lineItemId: e.data.LineItemId,
                itemId: e.data.ItemId,
                sku: e.data.SKU,
                description: e.data.Description,
                cost: e.data.Cost,
                unitPrice: e.data.UnitPrice,
                quantity: e.data.Quantity,
                subtotal: e.data.UnitPrice * e.data.Quantity,
                removed: false
            };
            s.items.push(item);

            sendLineItem(item);
            
            runCalculations(s);
        },
        ItemRemoved: function(s, e) {
            for (var i = 0; i < s.items.length; i++) {
                var item = s.items[i];
                if (item.lineItemId === e.data.LineItemId) { 
                    item.removed = true;
                    sendLineItem(item);
                }
            }

            runCalculations(s);
        },
        Issued: function(s, e) {
            s.status = 'Issued';
            s.date = e.data.Date;

            sendHeader(s);
        },
        ReIssued: function(s, e) {
            s.status = 'Re-issued';
            s.date = e.data.Date;

            sendHeader(s);
        },
        PaymentApplied: function(s, e) {
            var pmt = {
                id: e.data.Id,
                paymentId: e.data.PaymentId,
                amount: e.data.Amount ?? 0,
                received: e.data.Received,
                voided: false
            };
            s.payments.push(pmt);

            sendPaymentItem(pmt);

            runCalculations(s);
        },
        PaymentVoided: function(s, e) {
            for(var i = 0; i < s.payments.length; i++) {
                var pmt = s.payments[i];
                if (pmt.paymentId === e.data.PaymentId) { 
                    pmt.voided = true;
                    sendPaymentItem(pmt);
                }
            }

            runCalculations(s);
        },
        StatusChanged: function(s, e) {
            s.status = e.data.Status;

            sendHeader(s);
        },
        Closed: function(s, e) {
            s.status = 'Closed';

            sendHeader(s);
        }
    });";

var sqlTables = @"/****** Object:  Table [dbo].[Checkpoints]    Script Date: 3/29/2022 12:21:12 AM ******/
/****** Object:  Table [dbo].[Checkpoints]    Script Date: 3/29/2022 12:21:12 AM ******/
SET ANSI_NULLS ON

SET QUOTED_IDENTIFIER ON

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Checkpoints]') AND type in (N'U'))
DROP TABLE [dbo].[Checkpoints]

CREATE TABLE [dbo].[Checkpoints](
	[StreamName] [varchar](1000) NOT NULL UNIQUE,
	[Position] [bigint] NOT NULL
)

/****** Object:  Table [dbo].[Checkpoints]    Script Date: 3/29/2022 12:21:12 AM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Invoices]') AND type in (N'U'))
DROP TABLE [dbo].[Invoices]

CREATE TABLE [dbo].[Invoices] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY,
    [AccountId] UNIQUEIDENTIFIER NULL,
    [AccountName] [varchar](400) NULL,
    [PaymentTermsId] UNIQUEIDENTIFIER NULL,
    [PaymentTermsName] [varchar](400) NULL,
    [Date] DateTime NULL,
    [Status] [varchar](100) NULL,
    [ItemsTotal] BIGINT,
    [PaymentsTotal] BIGINT,
    [BalanceDue] BIGINT
)

/****** Object:  Table [dbo].[Checkpoints]    Script Date: 3/29/2022 12:21:12 AM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[InvoiceItems]') AND type in (N'U'))
DROP TABLE [dbo].[InvoiceItems]

CREATE TABLE [dbo].[InvoiceItems] (
    [InvoiceId] UNIQUEIDENTIFIER NOT NULL,
    [LineItemId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [ItemId] UNIQUEIDENTIFIER NULL,
	[SKU] [varchar](100) NULL,
    [Description] [varchar](500) NULL,
    [Cost] BIGINT NULL,
    [UnitPrice] BIGINT NULL,
    [QTY] BIGINT NULL,
    [SubTotal] BIGINT NULL,
    [HasBeenRemoved] BIT NULL
)

/****** Object:  Table [dbo].[Checkpoints]    Script Date: 3/29/2022 12:21:12 AM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[InvoicePayments]') AND type in (N'U'))
DROP TABLE [dbo].[InvoicePayments]

CREATE TABLE [dbo].[InvoicePayments] (
    [InvoiceId] UNIQUEIDENTIFIER NOT NULL,
    [PaymentId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [Amount] BIGINT NULL,
    [Received] DATETIME NULL,
    [Voided] BIT NULL
)
";

log.LogInformation("Creating SQL Databases");
using (var cn = host.Services.GetRequiredService<DbConnection>()) {
    var cmd = cn.CreateCommand();
    cmd.CommandType = System.Data.CommandType.Text;

    cmd.CommandText = @"USE MASTER; DROP DATABASE IF EXISTS [demo]; CREATE DATABASE [demo];";
    cmd.ExecuteNonQuery();

    cmd.CommandText = "USE [demo];";
    cmd.ExecuteNonQuery();

    cmd.CommandText = sqlTables;
    cmd.ExecuteNonQuery();
}

log.LogInformation("Creating a few accounts.");
var acct = default(Account);

acct = new Account(Guid.NewGuid(), "Contoso Restaurant Group", msg);
repo.Save(acct);
acct = new Account(Guid.NewGuid(), "Bad Dog Tavern", msg);
repo.Save(acct);
acct = new Account(Guid.NewGuid(), "East Township Tavern", msg);
repo.Save(acct);
acct = new Account(Guid.NewGuid(), "Irish Pub, LLC", msg);
repo.Save(acct);

log.LogInformation("Creating a few items.");
var item = default(Item);

item = new Item(Guid.NewGuid(), "4000", "Apple", 50, 109, msg);
repo.Save(item);
item = new Item(Guid.NewGuid(), "4001", "Banana", 25, 33, msg);
repo.Save(item);
item = new Item(Guid.NewGuid(), "4002", "Pear", 15, 69, msg);
repo.Save(item);
item = new Item(Guid.NewGuid(), "5000", "Chicken Breast", 89, 129, msg);
repo.Save(item);
item = new Item(Guid.NewGuid(), "5001", "London Broil", 229, 499, msg);
repo.Save(item);
item = new Item(Guid.NewGuid(), "5002", "NY Strip Steak", 250, 499, msg);
repo.Save(item);

log.LogInformation("Entering default payment terms");
var pt = default(PaymentTerms);

pt = new PaymentTerms(Guid.NewGuid(), "NET 7", 7, 15, msg);
repo.Save(pt);
pt = new PaymentTerms(Guid.NewGuid(), "NET 10", 10, 15, msg);
repo.Save(pt);
pt = new PaymentTerms(Guid.NewGuid(), "NET 30", 30, 15, msg);
repo.Save(pt);
pt = new PaymentTerms(Guid.NewGuid(), "NET 60", 60, 15, msg);
repo.Save(pt);
pt = new PaymentTerms(Guid.NewGuid(), "NET 90", 90, 15, msg);
repo.Save(pt);

log.LogInformation("Creating projections");
var pm = new ProjectionsManager(
    log: new EventStore.ClientAPI.Common.Log.ConsoleLogger(),
    httpEndPoint: new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2113),
    operationTimeout: TimeSpan.FromMilliseconds(5000));

await pm.CreateContinuousAsync(name: "invoice_documentdb", projectionDocument);
await pm.CreateContinuousAsync(name: "invoice_rdbms", projectionRdbms);

log.LogInformation("Creating persistent subscriptions");
var pss = PersistentSubscriptionSettings
    .Create()
    .StartFromBeginning()
    .PreferRoundRobin()
    .ResolveLinkTos()
    .WithExtraStatistics()
    .Build();
await esconn.CreatePersistentSubscriptionAsync("invoice_pmt", "invoice_pmt_rdbms_group", pss, credentials);
await esconn.CreatePersistentSubscriptionAsync("invoice_item", "invoice_item_rdbms_group", pss, credentials);
await esconn.CreatePersistentSubscriptionAsync("invoice_header", "invoice_header_rdbms_group", pss, credentials);
await esconn.CreatePersistentSubscriptionAsync("invoice_documents", "invoice_documents_redis_group", pss, credentials);
await esconn.CreatePersistentSubscriptionAsync("invoice_documents", "invoice_documents_mongo_group", pss, credentials);

await esconn.AppendToStreamAsync(KnownStreams.StreamProcessor, ExpectedVersion.Any, new[] {
    new EventData(Guid.NewGuid(), nameof(StreamProcessorSignal), true, JsonSerializer.SerializeToUtf8Bytes(new StreamProcessorSignal{ ActiveBackplane = Backplanes.Direct }), Array.Empty<byte>())
});

log.LogInformation("Creating kafka topics");
var knownTopics = new[] {
    Strings.Kafka.Topics.MongoInvoiceDocuments,
    Strings.Kafka.Topics.RedisInvoiceDocuments,
    Strings.Kafka.Topics.RDBMSInvoiceHeaders,
    Strings.Kafka.Topics.RDBMSInvoiceItems,
    Strings.Kafka.Topics.RDBMSInvoicePayments,
};


// ensure topic exists.
using (var client = new AdminClientBuilder(Strings.DatabaseConnections.Kafka
        .Split(';')
        .Select(x => x.Split('='))
        .ToDictionary(x => x[0].Trim(), x => x[1].Trim()))
    .Build()) {

    foreach (var knownTopic in knownTopics) {
        var metaata = client.GetMetadata(TimeSpan.FromSeconds(10));
        var topics = metaata.Topics;
        if (metaata.Topics.All(t => t.Topic != knownTopic)) {
            client.CreateTopicsAsync(new[] { new Confluent.Kafka.Admin.TopicSpecification { Name = knownTopic } }).GetAwaiter().GetResult();
        }
    }
}


log.LogInformation("Database initialized.");