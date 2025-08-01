namespace EventStore.StreamConnectors.Kafka {
    using System;
    using System.Data.Common;
    using System.Text.Json;
    using System.Threading.Tasks;

    using EventStore.ClientAPI;

    using Microsoft.Extensions.Logging;

    public abstract class KafkaSqlStreamListener : KafkaStreamListener {
        private readonly DbConnection _connection;
        private readonly KafkaSqlConsumerConfigurationOptions _options;
        private DbCommand _dbCommand;
        private JsonSerializerOptions _serializerOptions;

        public KafkaSqlStreamListener(DbConnection connection, KafkaSqlConsumerConfigurationOptions options, IEventStoreConnection esConnection, ILoggerFactory loggerFactory) : base(options, esConnection, loggerFactory) {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));

            _serializerOptions = new JsonSerializerOptions {
                IncludeFields = true,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            _serializerOptions.Converters.Add(new EmptyGuidConverter());
        }

        protected override async Task InitializeAsync() {
            if (_connection.State != System.Data.ConnectionState.Open) throw new InvalidOperationException("Sql Connection is not open.");
            _dbCommand = _options.QueryFormatter.BuildUpsertCommand(_connection, _options);
            Log.LogDebug("Upsert command built.");

            // create checkpoint table, if it does not exist.
            await _options.QueryFormatter.CreateSchemaIfNotExistsAsync(_connection, _options);
            Log.LogDebug("Schema created.");

            Log.LogInformation("Initialization completed.");
        }

        protected override async Task ProcessAsync(ProjectedEvent e) {
            foreach (DbParameter dbp in _dbCommand.Parameters) {
                dbp.Value = DBNull.Value;
            }

            var doc = JsonDocument.Parse(e.Data);
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

            if (_options.UsesStreamNameAsKey) _dbCommand.Parameters["@Id"].Value = e.EventStreamId;

            await _dbCommand.ExecuteNonQueryAsync();
        }

        protected override Task CleanupAsync() {
            _dbCommand?.Dispose();
            _dbCommand = null;
            return Task.CompletedTask;
        }


        protected override Task<long?> ResolveLastCheckpointAsync() => ((KafkaSqlConsumerConfigurationOptions)Options).QueryFormatter.ReadLastCheckpointAsync(_connection, _options);
        protected override Task UpdateCheckpointAsync(long position) => ((KafkaSqlConsumerConfigurationOptions)Options).QueryFormatter.RecordCheckpointAsync(_connection, position, _options);
    }
}
