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
        public int MapHotkey {
            get { return Properties.Settings.Default.mapHotkey; }
            set
            {
                ButtonMap.Content = KeyInterop.KeyFromVirtualKey(value).ToString();
                Properties.Settings.Default.mapHotkey = value;
                Properties.Settings.Default.Save();
            }
        }

        public int ZanaHotkey {
            get { return Properties.Settings.Default.zanaHotkey; }
            set
            {
                ButtonZana.Content = KeyInterop.KeyFromVirtualKey(value).ToString();
                Properties.Settings.Default.zanaHotkey = value;
                Properties.Settings.Default.Save();
            }
        }

        public int CartoHotkey {
            get { return Properties.Settings.Default.cartoHotkey; }
            set
            {
                ButtonCartographer.Content = KeyInterop.KeyFromVirtualKey(value).ToString();
                Properties.Settings.Default.cartoHotkey = value;
                Properties.Settings.Default.Save();
            }
        }

        public int NewHotkey {
            get { return Properties.Settings.Default.newHotkey; }
            set
            {
                ButtonNewMap.Content = KeyInterop.KeyFromVirtualKey(value).ToString();
                Properties.Settings.Default.newHotkey = value;
                Properties.Settings.Default.Save();
            }
        }

        private readonly MainWindow _main;
        //private KeysConverter kConverter;
        public ApplicationSettings()
        {
            InitializeComponent();
            //kConverter = new KeysConverter();
            _main = MainWindow.GetSingleton();
            Icon = _main.Icon;
            ButtonMap.Content = KeyInterop.KeyFromVirtualKey(MapHotkey).ToString();
            ButtonZana.Content = KeyInterop.KeyFromVirtualKey(ZanaHotkey).ToString();
            ButtonCartographer.Content = KeyInterop.KeyFromVirtualKey(CartoHotkey).ToString();
            ButtonNewMap.Content = KeyInterop.KeyFromVirtualKey(NewHotkey).ToString();
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
                        button.Content = KeyInterop.KeyFromVirtualKey(MapHotkey).ToString();
                        break;
                    case ("ButtonZana"):
                        button.Content = KeyInterop.KeyFromVirtualKey(ZanaHotkey).ToString();
                        break;
                    case ("ButtonCarto"):
                        button.Content = KeyInterop.KeyFromVirtualKey(CartoHotkey).ToString();
                        break;
                    case ("ButtonNewMap"):
                        button.Content = KeyInterop.KeyFromVirtualKey(NewHotkey).ToString();
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
            if (key == Key.F9) MessageBox.Show("F9");
            if (key == Key.F10) MessageBox.Show("F10");
            button.Content = key.ToString();
            button.BorderBrush = Brushes.Gray;
            var keyInt = KeyInterop.VirtualKeyFromKey(key);
            switch (button.Name)
            {
                case ("ButtonMap"):
                    MapHotkey = keyInt;
                    break;
                case ("ButtonZana"):
                    ZanaHotkey = keyInt;
                    break;
                case ("ButtonCartographer"):
                    CartoHotkey = keyInt;
                    break;
                case ("ButtonNewMap"):
                    NewHotkey = keyInt;
                    break;
            }

            if (MapHotkey == keyInt && !Equals(button, ButtonMap))
            {
                ButtonMap.Content = string.Empty;
                MapHotkey = KeyInterop.VirtualKeyFromKey(Key.None);
            }
            if (ZanaHotkey == keyInt && !Equals(button, ButtonZana))
            {
                ButtonZana.Content = string.Empty;
                ZanaHotkey = KeyInterop.VirtualKeyFromKey(Key.None);
            }
            if (CartoHotkey == keyInt && !Equals(button, ButtonCartographer))
            {
                ButtonCartographer.Content = string.Empty;
                CartoHotkey = KeyInterop.VirtualKeyFromKey(Key.None);
            }
            if (NewHotkey == keyInt && !Equals(button, ButtonNewMap))
            {
                ButtonNewMap.Content = string.Empty;
                NewHotkey = KeyInterop.VirtualKeyFromKey(Key.None);
            }
        }


        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
            _main.RegisterHotkeys();
            Close();
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Reload();
            Properties.Settings.Default.Save();
            _main.RegisterHotkeys();
            Close();
        }
    }
}
