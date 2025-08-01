namespace EventStore.StreamConnectors.RDBMS {
    using System.Data.Common;

    public interface IQueryFormatter {
        string Column(string columnName);
        string IsNull(string columnName);
        string Parameter(string columnName);
        string Table(string tableName, string @namespace = "");
        DbCommand BuildUpsertCommand(DbConnection connection, ISqlOptions options);
        Task CreateSchemaIfNotExistsAsync(DbConnection connection, ISqlOptions options);
        Task<long?> ReadLastCheckpointAsync(DbConnection connection, ISqlOptions options);
        Task RecordCheckpointAsync(DbConnection connection, long position, ISqlOptions options);
    }
}
