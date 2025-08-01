namespace EventStore.StreamConnectors.RDBMS.QueryFormatters {
    using System.Data.Common;
    using System.Linq;

    public class PostgresQueryFormatter : IQueryFormatter {
        public string Column(string columnName) => $"'{columnName}'";

        public string IsNull(string columnName) => $"COALESCE({Parameter(columnName)}, {columnName})";

        public string Parameter(string columnName) => $"@{columnName}";

        public string Table(string tableName, string @namespace = "") {
            var ns = string.IsNullOrEmpty(@namespace)
                ? string.Empty
                : $"\"{@namespace}\".";
            return $"{ns}\"{tableName}\"";
        }

        public DbCommand BuildUpsertCommand(DbConnection connection, ISqlOptions options) {
            var updateAssignments = options.Columns
                .Where(c => !c.IsKeyColumn)
                .Select(c => $"{c.ColumnName} = {IsNull(c.ColumnName)?.ToString()}")
                .Aggregate((s1, s2) => $"{s1}, {s2}");

            var insertColumnDefs = options.Columns
                .Select(c => Column(c.ColumnName))
                .Aggregate((s1, s2) => $"{s1}, {s2}");
            var insertColumnParameters = options.Columns
                .Select(c => Parameter(c.ColumnName))
                .Aggregate((s1, s2) => $"{s1}, {s2}");

            var keyColumnDefs = options.Columns
                .Where(c => c.IsKeyColumn)
                .Select(c => Column(c.ColumnName))
                .Aggregate((s1, s2) => $"{s1}, {s2}");

            var c = connection.CreateCommand();
            c.CommandType = System.Data.CommandType.Text;
            c.CommandText = $@"INSERT INTO {Table(options.Table, options.Namespace)} ({insertColumnDefs})
VALUES({insertColumnParameters})
ON CONFLICT({keyColumnDefs})
DO
    UPDATE SET {updateAssignments};";

            foreach (var col in options.Columns) {
                var p = c.CreateParameter();
                p.ParameterName = Parameter(col.ColumnName);
                c.Parameters.Add(p);
            }

            return c;
        }

        public async Task CreateSchemaIfNotExistsAsync(DbConnection connection, ISqlOptions options) {
            var c = connection.CreateCommand();
            c.CommandType = System.Data.CommandType.Text;
            c.CommandText = @"CREATE TABLE IF NOT EXISTS checkpoints(
    stream_name varchar NOT NULL,
    position BIGSERIAL NOT NULL
PRIMARY KEY(stream_name)";
            await c.ExecuteNonQueryAsync();
        }

        public async Task<long?> ReadLastCheckpointAsync(DbConnection connection, ISqlOptions options) {
            var c = connection.CreateCommand();
            c.CommandType = System.Data.CommandType.Text;
            c.CommandText = "SELECT COALESCE(position, 0) FROM checkpoints WHERE stream_name = @streamName;";

            var p = c.CreateParameter();
            p.ParameterName = "@streamName";
            p.Value = options.Stream;
            c.Parameters.Add(p);

            var result = await c.ExecuteScalarAsync();
            var position = Convert.ToInt64(result);

            return position < 0
                ? null
                : position;
        }

        public async Task RecordCheckpointAsync(DbConnection connection, long position, ISqlOptions options) {
            var c = connection.CreateCommand();
            c.CommandType = System.Data.CommandType.Text;
            c.CommandText = @"INSERT INTO checkpoints (stream_name, position)
VALUES(@streamName, @position)
ON CONFLICT(stream_name)
DO
    UPDATE SET position = @position;";

            var p = c.CreateParameter();
            p.ParameterName = "@streamName";
            p.Value = options.Stream;
            c.Parameters.Add(p);

            p = c.CreateParameter();
            p.ParameterName = "@position";
            p.Value = position;
            c.Parameters.Add(p);

            await c.ExecuteNonQueryAsync();
        }
    }
}
