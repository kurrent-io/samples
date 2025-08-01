namespace EventStore.StreamConnectors.RDBMS {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public interface ISqlOptions {
        string Stream { get; set; }
        string Namespace { get; set; }
        string Table { get; set; }
        IEnumerable<ColumnMap> Columns { get; set; }
        // TODO: Better name possible?
        IQueryFormatter QueryFormatter { get; set; }
    }
}
