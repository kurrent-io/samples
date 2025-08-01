namespace EventStore.StreamConnectors {
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text.Json;
    using System.Text.RegularExpressions;

    using EventStore.ClientAPI;

    public interface IStreamParser {
        string KeyColumnName { get; }
        bool CanParseEvents(string streamName);
        bool Parse(ref DataSet ds, string streamName, IEnumerable<ResolvedEvent> events);
    }

    /// <summary>
    /// Parses instance streams (-), event-type streams ($et-), and category streams ($ce-)
    /// </summary>
    public class StreamParser : IStreamParser {
        protected static Regex rgx = new Regex("[^a-zA-Z0-9 -]");
        public string KeyColumnName => "stream-name";

        /// <summary>
        /// Parses instance streams (-), event-type streams ($et-), and category streams ($ce-)
        /// </summary>
        public StreamParser() { }

        public bool CanParseEvents(string streamName)
            => streamName.StartsWith("$ce-")
            ||
            streamName.StartsWith("$et-")
            ||
            streamName.Contains("-");

        public bool Parse(ref DataSet ds, string streamName, IEnumerable<ResolvedEvent> events) {
            if (ds == null) throw new ArgumentNullException("A DataSet is required.", nameof(ds));
            var cleansedStreamName = rgx.Replace(streamName, "");

            DataTable dt = null;
            DataColumn keyColumn = null;

            // creates a primary key based on the stream name.
            if (ds.Tables.Contains(cleansedStreamName)) {
                dt = ds.Tables[cleansedStreamName];
                keyColumn = dt.Columns[KeyColumnName];
            } else {
                dt = ds.Tables.Add(cleansedStreamName);
                keyColumn = dt.Columns.Add(KeyColumnName);
                dt.PrimaryKey = new[] { keyColumn };
            }

            foreach (var e in events) {
                var doc = JsonDocument.Parse(e.Event.Data);
                foreach (var p in doc.RootElement.EnumerateObject()) {
                    if (!dt.Columns.Contains(p.Name)) dt.Columns.Add(p.Name);
                }

                var row = dt.Rows.Find(e.Event.EventStreamId);
                if (row == null) {
                    row = dt.NewRow();
                    row[KeyColumnName] = e.Event.EventStreamId;
                    dt.Rows.Add(row);
                }

                foreach (var p in doc.RootElement.EnumerateObject()) {
                    row[p.Name] = p.Value.ToString();
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Parses streams with only a name, or an instance id of Guid.Empty().ToString("N")
    /// </summary>
    public class MultiInstanceStreamParser : IStreamParser {
        protected static Regex rgx = new Regex("[^a-zA-Z0-9 -]");
        public string KeyColumnName => "instance-id";

        /// <summary>
        /// Parses streams with only a name, or an instance id of Guid.Empty().ToString("N")
        /// </summary>
        public MultiInstanceStreamParser() { }

        public bool CanParseEvents(string streamName) => !streamName.Contains("-") || streamName.EndsWith($"-{Guid.Empty:N}");


        //Note: Implemented like this, for now, to support this concept within RD until we can adjust the method signature(s) to *NOT* need an instance id.
        public bool Parse(ref DataSet ds, string streamName, IEnumerable<ResolvedEvent> events) {
            if (ds == null) throw new ArgumentNullException("A data set is required.", nameof(ds));
            var cleansedStreamName = rgx.Replace(streamName, "");

            DataTable dt = null;
            DataColumn keyColumn = null;

            // creates a primary key based on the stream name.
            if (ds.Tables.Contains(cleansedStreamName)) {
                dt = ds.Tables[cleansedStreamName];
                keyColumn = dt.Columns[KeyColumnName];
            } else {
                dt = ds.Tables.Add(cleansedStreamName);
                keyColumn = dt.Columns.Add(KeyColumnName);
                dt.PrimaryKey = new[] { keyColumn };
            }

            foreach (var e in events) {
                var doc = JsonDocument.Parse(e.Event.Data);

                string rowKey = string.Empty;
                foreach (var p in doc.RootElement.EnumerateObject()) {
                    if (p.Name.Equals("Id")) { rowKey = p.Value.ToString(); break; }
                }

                if (string.IsNullOrWhiteSpace(rowKey)) continue; // drop events that do not have an Id property (v.next can accept an id name.  for now, let's just assume that all events have an Id property that we care about.

                foreach (var p in doc.RootElement.EnumerateObject()) {
                    if (!dt.Columns.Contains(p.Name) && !p.Name.Equals("Id")) dt.Columns.Add(p.Name);
                }

                var row = dt.Rows.Find(rowKey);
                if (row == null) {
                    row = dt.NewRow();
                    row[KeyColumnName] = rowKey;
                    dt.Rows.Add(row);
                }

                foreach (var p in doc.RootElement.EnumerateObject()) {
                    if (p.Name.Equals("Id")) continue;

                    row[p.Name] = p.Value.ToString();
                }
            }

            return true;
        }
    }
}
