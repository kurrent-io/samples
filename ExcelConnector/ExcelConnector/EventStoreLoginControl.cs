namespace ExcelConnector {
    using System;
    using System.Windows.Forms;

    public partial class EventStoreLoginControl : UserControl {
        public EventStoreLoginControl() {
            InitializeComponent();
        }

        private void btnConnect_Click(object sender, EventArgs e) {
            if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtPassword.Text)) {
                MessageBox.Show("Please provide username and/or password.");
                return;
            }
            if (string.IsNullOrWhiteSpace(txtConnString.Text)) {
                MessageBox.Show("A connection string must be provided.");
            }

            Globals.ThisWorkbook.OpenConnection(txtUsername.Text, txtPassword.Text, txtConnString.Text);
        }
    }
}
