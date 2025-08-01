namespace ExcelConnector {
    using System;
    using System.ComponentModel;
    using System.Windows.Forms;

    using Microsoft.Office.Interop.Excel;

    internal partial class WorksheetPropertiesControl : UserControl {
        private Worksheet _worksheet;

        public string WorksheetName {
            get => gbWorksheetProperties.Text;
            set => gbWorksheetProperties.Text = value;
        }
        public string StreamName {
            get => lbStreamName.Text;
            set => lbStreamName.Text = value;
        }
        public long? Position {
            get => string.IsNullOrWhiteSpace(lbPosition.Text)
                ? -1
                : Convert.ToInt64(lbPosition.Text);
            set => lbPosition.Text = value.ToString();
        }

        public event EventHandler UpdateData;

        public WorksheetPropertiesControl(Worksheet worksheet) {
            _worksheet = worksheet;

            InitializeComponent();

            WorksheetName = _worksheet.GetProperty(nameof(WorksheetName));
            StreamName = _worksheet.GetProperty(nameof(StreamName));
            Position = Convert.ToInt64(_worksheet.GetProperty(nameof(Position)));

            Disposed += (s, e) => {
                SetProperty(nameof(StreamName), StreamName);
                SetProperty(nameof(Position), Position);
                SetProperty(nameof(WorksheetName), WorksheetName);
            };

            btnRefresh.Click += (s, e) => UpdateData?.Invoke(this, EventArgs.Empty);
        }

        void SetProperty(string name, object value) {
            if (IsHandleCreated) {
                BeginInvoke((System.Action)(() => _worksheet.SetProperty(name, value)));
            } else {
                _worksheet.SetProperty(name, value);
            }
        }

        private void dataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e) {
            var dgw = (DataGridView)sender;
            if (dgw.CurrentCell.ColumnIndex == 0) {
                dgw.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }
    }
}
