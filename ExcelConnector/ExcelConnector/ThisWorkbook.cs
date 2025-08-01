namespace ExcelConnector {
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using EventStore.ClientAPI;
    using EventStore.ClientAPI.SystemData;

    using Microsoft.Office.Interop.Excel;

    public partial class ThisWorkbook {
        internal static HashSet<WorksheetModel> WorksheetModels = new();
        private IEventStoreConnection _connection;
        private TasksPane _taskPane = new();
        private EventStoreLoginControl _loginControl = new();
        private IEnumerable<IStreamParser> _streamParsers = new List<IStreamParser> {
            new MultiInstanceStreamParser(),
            new StreamParser()
        };

        internal UserCredentials DefaultUserCredentials { get; private set; }

        private void ThisWorkbook_Startup(object sender, EventArgs e) {
            ActionsPane.Controls.Add(_taskPane);
            ActionsPane.Controls.Add(_loginControl);

            SheetActivate += (e) => {
                var sheet = e as Worksheet;
                foreach (var m in WorksheetModels) {
                    if (m.Worksheet == sheet) {
                        m.Properties.Show();
                    } else {
                        m.Properties.Hide();
                    }
                }
            };

            _loginControl.Show();
            _taskPane.Hide();
        }

        private void ThisWorkbook_Shutdown(object sender, EventArgs e) {
            foreach(var wsm in WorksheetModels) {
                wsm?.Dispose();
            }

            if (_connection != null) {
                _connection.Closed -= onServerClosed;
                _connection.Dispose();
            }
        }

        internal void OpenConnection(string username, string password, string connectionString) {
            _connection?.Dispose();
            DefaultUserCredentials = new UserCredentials(username, password);

            var csb = ConnectionSettings.Create()
                    .SetDefaultUserCredentials(DefaultUserCredentials);
            _connection = EventStoreConnection.Create(connectionString, csb, "ESB Visualizer - Excel Add-in");

            _connection.Connected += onServerConnected;
            _connection.Closed += onServerClosed;

            _connection.ConnectAsync().Wait();
        }

        internal void CloseConnection() { _connection?.Close(); }

        private void onServerConnected(object sender, ClientConnectionEventArgs e) {
            // need to be triggered on the UI thread, as the connection events are on different threads.
            _loginControl.BeginInvoke((System.Action)(() => _loginControl.Hide()));
            _taskPane.BeginInvoke((System.Action)(() => _taskPane.Show()));
        }

        private void onServerClosed(object sender, ClientClosedEventArgs e) {
            // need to be triggered on the UI thread, as the connection events are on different threads.
            _loginControl.BeginInvoke((System.Action)(() => _loginControl.Show()));
            _taskPane.BeginInvoke((System.Action)(() => _taskPane.Hide()));
        }

        /// <summary>
        /// For processing new streams, this will create a new spreadsheet in the workbook, then read the stream
        /// from EventStore.
        /// </summary>
        /// <param name="streamName"></param>
        internal void LoadStream(string streamName) {
            foreach (Worksheet ws in Sheets) {
                if (ws.Name == streamName) {
                    ws.Activate();
                    return;
                }
            }

            var sheet = (Worksheet)Sheets.Add(After: Sheets[Sheets.Count]);
            sheet.Name = streamName.Replace("$", "").Substring(0, streamName.Length > 31 ? 31 : streamName.Length - 1);

            sheet.BeforeDelete += () => {
                var model = WorksheetModels.FirstOrDefault(wsm => wsm.Worksheet == sheet);
                if (model != null) {
                    ActionsPane.Controls.Remove(model.Properties);
                    WorksheetModels.Remove(model);
                }
            };

            var model = new WorksheetModel(sheet, streamName, _connection, _streamParsers);
            model.LoadStreamFromLast();
            WorksheetModels.Add(model);
            ActionsPane.Controls.Add(model.Properties);
        }

        #region VSTO Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InternalStartup() {
            this.Startup += new EventHandler(ThisWorkbook_Startup);
            this.Shutdown += new EventHandler(ThisWorkbook_Shutdown);
        }

        #endregion

    }
}
