using System.Windows;

namespace Cartogram
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class ApplicationSettings : Window
    {
        public ApplicationSettings()
        {
            InitializeComponent();
        }

        private void ZanaValue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ZanaInt.Content = ZanaValue.Value.ToString("0");

        }
    }
}
