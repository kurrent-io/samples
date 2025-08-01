namespace EventStore.StreamConnectors.RDBMS {
    using System;

    /// <summary>
    /// Maps a pgsql column to a property on a <see cref="{TEvent}"/>
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    public class ColumnMap {
        public string PropertyName { get; private set; }
        public string ColumnName { get; private set; }
        public bool IsKeyColumn { get; private set; }
        public Type DataType { get;private set; }

        public ColumnMap(string propertyName, string columnName, Type dataType, bool isKeyColumn = false) {
            PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
            ColumnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
            DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
            IsKeyColumn = isKeyColumn;
        }
    }
}
