using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Button = System.Windows.Controls.Button;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace Cartogram
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class ApplicationSettings : Window
    {
        private readonly MainWindow _main;
        //private KeysConverter kConverter;
        public ApplicationSettings()
        {
            InitializeComponent();
            //kConverter = new KeysConverter();
            _main = MainWindow.GetSingleton();
            ZanaInt.Content = Properties.Settings.Default.ZanaQuantity.ToString();
            ZanaValue.Value = Properties.Settings.Default.ZanaQuantity;
            ButtonMap.Content = KeyInterop.KeyFromVirtualKey(Properties.Settings.Default.mapHotkey).ToString();
            ButtonZana.Content = KeyInterop.KeyFromVirtualKey(Properties.Settings.Default.zanaHotkey).ToString();
            ButtonCartographer.Content = KeyInterop.KeyFromVirtualKey(Properties.Settings.Default.cartoHotkey);
        }

        private void ZanaValue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ZanaInt.Content = ZanaValue.Value.ToString("0");
        }

        private void ButtonHotkey_Click(object sender, RoutedEventArgs e)
        {
            ToggleButton(((Button)sender));
        }

        private void ButtonHotkey_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            ChangeHotkey(((Button)sender), e.Key);
        }

        private void ToggleButton(ContentControl button)
        {
            if (button.Content.ToString() == string.Empty)
            {
                button.BorderBrush = Brushes.Gray;
                switch (button.Name)
                {
                    case ("ButtonMap"):
                        button.Content = KeyInterop.KeyFromVirtualKey(Properties.Settings.Default.mapHotkey).ToString();
                        break;
                    case ("ButtonZana"):
                        button.Content = KeyInterop.KeyFromVirtualKey(Properties.Settings.Default.zanaHotkey).ToString();
                        break;
                    case ("ButtonCarto"):
                        button.Content = KeyInterop.KeyFromVirtualKey(Properties.Settings.Default.cartoHotkey);
                        break;
                    default:
                        if (button.Content?.ToString() != string.Empty && button.Content?.ToString() != "None") break;
                        button.BorderBrush = Brushes.DeepSkyBlue;
                        button.Content = string.Empty;
                        break;
                }
                
            }
            else if (button.IsFocused)
            {
                button.BorderBrush = Brushes.DeepSkyBlue;
                button.Content = string.Empty;
            }
        }

        private void ChangeHotkey(ContentControl button, Key key)
        {
            button.Content = key.ToString();
            button.BorderBrush = Brushes.Gray;
            switch (button.Name)
            {
                case ("ButtonMap"):
                    Properties.Settings.Default.mapHotkey = KeyInterop.VirtualKeyFromKey(key);
                    break;
                case ("ButtonZana"):
                    Properties.Settings.Default.zanaHotkey = KeyInterop.VirtualKeyFromKey(key);
                    break;
                case ("ButtonCartographer"):
                    Properties.Settings.Default.cartoHotkey = KeyInterop.VirtualKeyFromKey(key);
                    break;
            }

            if (ButtonMap.Content.ToString() == button.Content.ToString() && !Equals(button, ButtonMap))
            {
                ButtonMap.Content = string.Empty;
                Properties.Settings.Default.mapHotkey = KeyInterop.VirtualKeyFromKey(Key.None);
            }
            if (ButtonZana.Content.ToString() == button.Content.ToString() && !Equals(button, ButtonZana))
            {
                ButtonZana.Content = string.Empty;
                Properties.Settings.Default.zanaHotkey = KeyInterop.VirtualKeyFromKey(Key.None);
            }
            if (ButtonCartographer.Content.ToString() == button.Content.ToString() && !Equals(button, ButtonCartographer))
            {
                ButtonCartographer.Content = string.Empty;
                Properties.Settings.Default.cartoHotkey = KeyInterop.VirtualKeyFromKey(Key.None);
            }
        }


        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            int zanaQuantity;
            Properties.Settings.Default.ZanaQuantity = int.TryParse(ZanaInt.Content.ToString(), out zanaQuantity)
                ? zanaQuantity
                : 0;
            Properties.Settings.Default.Save();
            _main.RegisterHotkeys();
            Close();
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
