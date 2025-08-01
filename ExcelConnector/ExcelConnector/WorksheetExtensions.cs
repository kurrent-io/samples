namespace ExcelConnector {

    using Microsoft.Office.Interop.Excel;

    internal static class WorksheetExtensions {
        public static dynamic GetProperty(this Worksheet worksheet, string propertyName) {
            foreach (CustomProperty cp in worksheet.CustomProperties) {
                if (cp.Name == propertyName) return cp.Value;
            }
            return null;
        }

        public static void SetProperty(this Worksheet worksheet, string name, dynamic value) {
            bool found = false;
            foreach (CustomProperty cp in worksheet.CustomProperties) {
                if (cp.Name == name) {
                    found = true;
                    cp.Value = value;
                }
            }
            if (!found) {
                worksheet.CustomProperties.Add(name, value);
            }
            return;
        }
    }
}
