namespace EventStore.StreamConnectors.RDBMS.QueryFormatters {
    using System.Data.Common;
    using System.Linq;

    public class MicrosoftSqlQueryFormatter : IQueryFormatter {
        public string Column(string columnName) => $"[{columnName}]";

        public string IsNull(string columnName) => $"ISNull({Parameter(columnName)}, {Column(columnName)})";

        public string Parameter(string columnName) => $"@{columnName}";

        public string Table(string tableName, string @namespace = "") {
            var ns = string.IsNullOrEmpty(@namespace)
                ? string.Empty
                : $"[{@namespace}].";
            return $"{ns}[{tableName}]";
        }

        public DbCommand BuildUpsertCommand(DbConnection connection, ISqlOptions options) {
            var selectColumnDefs = options.Columns
                .Where(c => c.IsKeyColumn)
                .Select(c => $"{Parameter(c.ColumnName)} as {Column(c.ColumnName)}")
                .Aggregate((s1, s2) => $"{s1}, {s2}");

            var matchParameters = options.Columns
                .Where(c => c.IsKeyColumn)
                .Select(c => $"Source.{Column(c.ColumnName)} = Target.{Column(c.ColumnName)}")
                .Aggregate((s1, s2) => $"{s1}, {s2}");

            var updateAssignments = options.Columns
                .Where(c => !c.IsKeyColumn)
                .Select(c => $"{Column(c.ColumnName)} = {IsNull(c.ColumnName)?.ToString()}")
                .Aggregate((s1, s2) => $"{s1}, {s2}");

            var insertColumnDefs = options.Columns
                .Select(c => Column(c.ColumnName))
                .Aggregate((s1, s2) => $"{s1}, {s2}");
            var insertColumnParameters = options.Columns
                .Select(c => Parameter(c.ColumnName))
                .Aggregate((s1, s2) => $"{s1}, {s2}");

            var c = connection.CreateCommand();
            c.CommandType = System.Data.CommandType.Text;
            c.CommandText = $@"MERGE {Table(options.Table, options.Namespace)} as Target 
USING (SELECT {selectColumnDefs}) as Source 
ON {matchParameters}

WHEN MATCHED THEN
    UPDATE SET {updateAssignments}

WHEN NOT MATCHED THEN
    INSERT ({insertColumnDefs})
    VALUES ({insertColumnParameters});";

            foreach (var col in options.Columns) {
                var p = c.CreateParameter();
                p.ParameterName = Parameter(col.ColumnName);
                p.DbType = RDBMSConstants.TypeMap.ContainsKey(col.DataType)
                    ? RDBMSConstants.TypeMap[col.DataType]
                    : System.Data.DbType.Object;
                c.Parameters.Add(p);
            }

            return c;
        }

        public async Task CreateSchemaIfNotExistsAsync(DbConnection connection, ISqlOptions options) {
            using (var c = connection.CreateCommand()) {
                c.CommandType = System.Data.CommandType.Text;
                c.CommandText = @"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Checkpoints' and xtype='U')
BEGIN
CREATE TABLE [dbo].[Checkpoints] (
    StreamName VARCHAR(max) NOT NULL,
    Position BIGINT NOT NULL
)
END";
                await c.ExecuteNonQueryAsync();
            }
        }

        public async Task<long?> ReadLastCheckpointAsync(DbConnection connection, ISqlOptions options) {
            using (var c = connection.CreateCommand()) {
                c.CommandType = System.Data.CommandType.Text;
                c.CommandText = "SELECT ISNULL(position, null) FROM checkpoints WHERE StreamName = @streamName;";

                var p = c.CreateParameter();
                p.ParameterName = "@streamName";
                p.Value = options.Stream;
                c.Parameters.Add(p);

                var result = await c.ExecuteScalarAsync();
                return result == null
                    ? null
                    : Convert.ToInt64(result);
            }
        }

        public async Task RecordCheckpointAsync(DbConnection connection, long position, ISqlOptions options) {
            using (var c = connection.CreateCommand()) {
                c.CommandType = System.Data.CommandType.Text;
                c.CommandText = $@"MERGE [dbo].[Checkpoints] as TARGET
USING (SELECT @streamName as StreamName) as Source
ON Source.StreamName = Target.StreamName

WHEN MATCHED THEN
    UPDATE SET [Position] = @position

WHEN NOT MATCHED THEN
    INSERT ([StreamName], [Position])
    VALUES(@streamName, @position);";

                var p = c.CreateParameter();
                p.ParameterName = "@streamName";
                p.Value = options.Stream;
                c.Parameters.Add(p);

                p = c.CreateParameter();
                p.ParameterName = "@position";
                p.Value = position;
                c.Parameters.Add(p);

                try {
                    await c.ExecuteNonQueryAsync();
                } catch (Exception ex) {
                    throw;
                }
            }
        }
    }
}
