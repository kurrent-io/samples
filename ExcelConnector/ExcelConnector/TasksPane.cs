namespace ExcelConnector {
    using System;
    using System.Windows.Forms;

    partial class TasksPane : UserControl {
        public TasksPane() {
            InitializeComponent();
        }

        private void btnObserve_Click(object sender, EventArgs e) => Globals.ThisWorkbook.LoadStream(txtStreamName.Text);

        private void btnDisconnect_Click(object sender, EventArgs e) => Globals.ThisWorkbook.CloseConnection();
    }
}
