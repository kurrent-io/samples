namespace RTI.Writers.RDBMS {
    using System.Data.Common;

    using EventStore.ClientAPI;
    using EventStore.StreamConnectors.RDBMS;

    using Microsoft.Extensions.Logging;

    // think service locator pattern here.

    internal class InvoiceHeaderStreamProcessor : SqlStreamProcessor {
        public InvoiceHeaderStreamProcessor(SqlConfigurationOptions options, DbConnection connection, IEventStoreConnection esConnection, ILoggerFactory loggerFactory) : base(options, connection, esConnection, loggerFactory) {
        }
    }

    internal class InvoiceItemsStreamProcessor : SqlStreamProcessor {
        public InvoiceItemsStreamProcessor(SqlConfigurationOptions options, DbConnection connection, IEventStoreConnection esConnection, ILoggerFactory loggerFactory) : base(options, connection, esConnection, loggerFactory) {
        }
    }

    internal class InvoicePaymentsStreamProcessor : SqlStreamProcessor {
        public InvoicePaymentsStreamProcessor(SqlConfigurationOptions options, DbConnection connection, IEventStoreConnection esConnection, ILoggerFactory loggerFactory) : base(options, connection, esConnection, loggerFactory) {
        }
    }
}
