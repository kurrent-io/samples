namespace ExcelConnector {
    using System.ComponentModel;

    using Microsoft.Office.Interop.Excel;

    internal class ExcelColumnFacade : INotifyPropertyChanged {
        private string _name;
        private string _displayName;
        private bool _isVisible;
        private Range _columnReference;

        public string Name {
            get => _name;
            set {
                _name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }

        public string DisplayName {
            get => _displayName;
            set {
                _displayName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
                UpdateHeader();
            }
        }

        public bool IsVisible {
            get => _isVisible;
            set {
                _isVisible = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVisible)));
                UpdateVisibility();
            }
        }

        public Range ColumnReference {
            get => _columnReference;
            set {
                _columnReference = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ColumnReference)));
                UpdateVisibility();
            }
        }

        private void UpdateHeader() {
            if (ColumnReference == null) return;

            ColumnReference.Cells[1].Value2 = DisplayName;
        }

        private void UpdateVisibility() {
            if (ColumnReference == null) return;

            ColumnReference.EntireColumn.Hidden = !IsVisible;
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
