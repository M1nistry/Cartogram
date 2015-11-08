using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Cartogram.SQL;

namespace Cartogram
{
    /// <summary>
    /// Interaction logic for ErrorLog.xaml
    /// </summary>
    public partial class ErrorLog : Window
    {
        public ErrorLog()
        {
            InitializeComponent();

            var errorCollection = Sqlite.GetErrors();
            GridErrors.DataContext = errorCollection;
        }

        private void MenuError_OnClick(object sender, RoutedEventArgs e)
        {
            var error = (Error)GridErrors.SelectedItem;
            var sb = new StringBuilder();
            sb.AppendLine($"Message: {error.Message}");
            sb.AppendLine("------");
            sb.AppendLine($"Stacktrace: {error.StackTrace}");
            sb.AppendLine("------");
            sb.AppendLine($"InnerException: {error.InnerException}");
            sb.AppendLine("------");
            sb.AppendLine($"Version: {error.Version}");
            sb.AppendLine($"Environment: {error.Environment}");
            sb.AppendLine($"Method: {error.Method}");
            Clipboard.SetText(sb.ToString());
        }
    }

    public class Error
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public string InnerException { get; set; }
        public string StackTrace { get; set; }
        public DateTime Time { get; set; }
        public string Version { get; set; }
        public string Environment { get; set; }
        public string Method { get; set; }
    }
}
