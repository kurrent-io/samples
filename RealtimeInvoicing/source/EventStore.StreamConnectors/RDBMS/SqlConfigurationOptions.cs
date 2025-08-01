namespace EventStore.StreamConnectors.RDBMS {
    using System.Collections.Generic;
    using System.Data.Common;

    public class SqlConfigurationOptions : StreamConfigurationOptions, ISqlOptions {
        public string Namespace { get; set; } = "dbo";
        public string Table { get; set; }
        public IEnumerable<ColumnMap> Columns { get; set; }
        // TODO: Better name possible?
        public IQueryFormatter QueryFormatter { get; set; }
    }
}
