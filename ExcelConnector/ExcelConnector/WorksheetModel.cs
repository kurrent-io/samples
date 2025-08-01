namespace ExcelConnector {
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text.Json;

    using EventStore.ClientAPI;

    using Microsoft.Office.Interop.Excel;

    internal class WorksheetModel : IDisposable {
        private readonly IEventStoreConnection _connection;
        private readonly IEnumerable<IStreamParser> _streamParsers;
        private const int ReadPageSize = 500;

        private long _lastEventNumberRead = -1;
        private Dictionary<string, int> _streamIndicies = new();
        private DataSet _inMemoryData = new DataSet();

        public Worksheet Worksheet { get; }
        public string StreamName { get; }
        public WorksheetPropertiesControl Properties { get; }
        public List<ExcelColumnFacade> Columns { get; private set; }


        public WorksheetModel(Worksheet worksheet, string streamName, IEventStoreConnection connection, IEnumerable<IStreamParser> streamParsers) {
            Worksheet = worksheet;
            StreamName = streamName;
            _connection = connection;
            _streamParsers = streamParsers;

            Properties = new WorksheetPropertiesControl(Worksheet);
            Properties.StreamName = streamName;
            Properties.UpdateData += Properties_UpdateData;

            Columns = new List<ExcelColumnFacade>();
        }

        private void Properties_UpdateData(object sender, EventArgs e) => LoadStreamFromLast();

        public void LoadStreamFromLast() {
            // read the stream, then setup the subscription.
            int remaining = int.MaxValue;
            var sliceStart = Properties.Position ?? -1L;
            StreamEventsSlice currentSlice;
            bool isCompleted = false;
            List<ResolvedEvent> events = new();

            do {
                var page = remaining < ReadPageSize ? remaining : ReadPageSize;

                currentSlice = _connection.ReadStreamEventsForwardAsync(
                    StreamName,
                    sliceStart,
                    page,
                    true).GetAwaiter().GetResult();

                if (!(currentSlice is StreamEventsSlice)) {
                    isCompleted = true;
                } else {
                    remaining -= currentSlice.Events.Length;
                    sliceStart = currentSlice.NextEventNumber;
                    events.AddRange(currentSlice.Events.Where(e => e.Event.IsJson));
                }

            } while ((!currentSlice.IsEndOfStream && remaining != 0) || isCompleted);

            _lastEventNumberRead = currentSlice.LastEventNumber;

            var parser = _streamParsers.FirstOrDefault(p => p.CanParseEvents(StreamName));
            if (parser == null) throw new InvalidOperationException($"No parser is available for '{StreamName}'.");

            parser.Parse(ref _inMemoryData, StreamName, events);
            var dt = _inMemoryData.Tables[0];

            var columnDefsJson = Worksheet.GetProperty(nameof(Columns))?.ToString() ?? string.Empty;
            Columns = string.IsNullOrWhiteSpace(columnDefsJson)
                ? new List<ExcelColumnFacade>()
                : JsonSerializer.Deserialize<List<ExcelColumnFacade>>(columnDefsJson);

            // parse datatable columns to extract new column names that are not previously known.  In this case, the column names should be those in the "Name" field.
            for (var x = 0; x < dt.Columns.Count; x++) {
                var column = dt.Columns[x];
                if (Columns.All(cd => cd.Name != column.ColumnName)) {
                    Columns.Add(new ExcelColumnFacade() {
                        Name = column.ColumnName,
                        DisplayName = column.ColumnName,
                        IsVisible = true
                    });
                }
            }

            // get the sub-section of column properties objects that their name does not match the displayname.
            var displayNameUpdates = Columns.Where(cd => cd.Name != cd.DisplayName).ToArray();

            // perform the re-names.
            foreach (var dnu in displayNameUpdates) {
                dt.Columns[dnu.Name].ColumnName = dnu.DisplayName;
            }

            // set the binding source.
            Properties.ColumnPropertiesBinding.DataSource = Columns;

            bool isSingleRow = false;
            if (dt.Rows.Count <= 1) {
                var blankRow = dt.NewRow();
                blankRow[parser.KeyColumnName] = "blank";
                dt.Rows.Add(blankRow);
                isSingleRow = true;
            }

            var xmlSchema = _inMemoryData.GetXmlSchema();
            var xml = _inMemoryData.GetXml();
            var mapName = $"{_inMemoryData.DataSetName}_Map";
            foreach (XmlMap m in Globals.ThisWorkbook.XmlMaps) {
                if (m.Name == mapName) {
                    m.Delete();
                    break;
                }
            }

            Globals.ThisWorkbook.XmlImportXml(xml, out var _, true, Worksheet.Range["A1"]);

            if (isSingleRow) {
                Worksheet.Rows[3].Delete();
            }

            Properties.Position = _lastEventNumberRead;

            var wscEnum = Worksheet.Columns.GetEnumerator();
            while (wscEnum.MoveNext()) {
                Range column = (Range)wscEnum.Current;
                var rangeColumnName = column.Rows[1].Cells[1].Value2;
                var c = Columns.SingleOrDefault(ecf => ecf.DisplayName == rangeColumnName);

                if (c == null) continue;

                c.ColumnReference = column.EntireColumn;
            }
        }

        public void Dispose() {
            Worksheet.SetProperty(nameof(Columns), JsonSerializer.Serialize(Columns));
        }
    }
}
