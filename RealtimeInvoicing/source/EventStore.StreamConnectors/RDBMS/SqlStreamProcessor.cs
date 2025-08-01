namespace EventStore.StreamConnectors.RDBMS {
    using System;
    using System.Data.Common;
    using System.Text.Json;

    using EventStore.ClientAPI;

    using Microsoft.Extensions.Logging;

    using ILogger = Microsoft.Extensions.Logging.ILogger;

    public class SqlStreamProcessor : StreamProcessor {
        private readonly SqlConfigurationOptions _options;
        private readonly DbConnection _connection;
        private DbCommand _dbCommand;

        public SqlStreamProcessor(SqlConfigurationOptions options, DbConnection connection, IEventStoreConnection esConnection, ILoggerFactory loggerFactory) : base(options, esConnection, loggerFactory) {
            Monitor = new StreamProcessorActivationMonitor(this, esConnection, loggerFactory, Backplanes.Direct);
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));

            if (_options.Direction == StreamDirection.FromExternalSource) throw new InvalidOperationException("Cannot process events from external sources.");
        }

        protected override async Task InitializeAsync(CancellationToken token) {
            if (_connection.State != System.Data.ConnectionState.Open) throw new InvalidOperationException("Sql Connection is not open.");
            _dbCommand = _options.QueryFormatter.BuildUpsertCommand(_connection, _options);
            Log.LogDebug("Upsert command built.");

            // create checkpoint table, if it does not exist.
            await _options.QueryFormatter.CreateSchemaIfNotExistsAsync(_connection, _options);
            Log.LogDebug("Schema created.");

            Log.LogInformation("Initialization completed.");
        }

        protected override async Task ProcessAsync(ResolvedEvent e) {
            foreach (DbParameter dbp in _dbCommand.Parameters) {
                dbp.Value = DBNull.Value;
            }

            var doc = JsonDocument.Parse(e.Event.Data);
            var properties = doc.RootElement.EnumerateObject();

            foreach (var p in properties) {
                var colMapping = _options.Columns.SingleOrDefault(c => c.PropertyName == p.Name);
                if (colMapping == null) continue;

                if (_dbCommand.Parameters.Contains(_options.QueryFormatter.Parameter(colMapping.ColumnName))) {
                    var parm = _dbCommand.Parameters[_options.QueryFormatter.Parameter(colMapping.ColumnName)];

                    if (colMapping.DataType == typeof(Guid)) {
                        try {
                            parm.Value = Guid.TryParse(p.Value.ToString(), out Guid val)
                                ? val
                                : DBNull.Value;
                        } catch (Exception ex) {
                            Log.LogCritical(ex, "parsing a value failed.");
                            throw;
                        }

                        continue;
                    }

                    try {
                        parm.Value = Convert.ChangeType(p.Value.ToString(), colMapping.DataType);
                    } catch (Exception ex) {
                        Log.LogCritical(ex, "parsing a value failed.");
                        throw;
                    }
                }
            }

            if (_options.UsesStreamNameAsKey) _dbCommand.Parameters["@Id"].Value = e.Event.EventStreamId;

            await _dbCommand.ExecuteNonQueryAsync();
        }

        protected override Task CleanupAsync() {
            _dbCommand?.Dispose();
            _dbCommand = null;
            return Task.CompletedTask;
        }

        protected override async Task<long?> ResolveLastCheckpointAsync() => await _options.QueryFormatter.ReadLastCheckpointAsync(_connection, _options);

        protected override async Task UpdateCheckpointAsync(long position) => await _options.QueryFormatter.RecordCheckpointAsync(_connection, position, _options);
    }
}
