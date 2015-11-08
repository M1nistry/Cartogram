using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Cartogram.Properties;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace Cartogram
{
    /// <summary>
    /// Interaction logic for Overlay.xaml
    /// </summary>
    public partial class Overlay : Window
    {
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        public bool IndicatorImage
        {
            set
            {
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                timer.Tick += (sender, args) =>
                {
                    ImageTick.Source = null;
                    timer.Stop();
                };
                switch (value)
                {
                    case (true):
                        ImageTick.Source = new BitmapImage(new Uri("/Cartogram;component/Resources/check56_green.png", UriKind.Relative));
                        timer.Start();
                        break;
                    case (false):
                        ImageTick.Source = new BitmapImage(new Uri("/Cartogram;component/Resources/cross93_red.png", UriKind.Relative));
                        timer.Start();
                        break;
                }
            }
        }

        public Overlay()
        {
            InitializeComponent();
            var color = new SolidColorBrush(Colors.White) { Opacity = Settings.Default.OverlayOpacity };
            Background = color;
            if (Settings.Default.OverlayLeft <= 0.0 && Settings.Default.OverlayTop <= 0.0)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            else
            {
                Left = Settings.Default.OverlayLeft;
                Top = Settings.Default.OverlayTop;
            }
            if (!Settings.Default.LockOverlay) ResizeMode = ResizeMode.CanResizeWithGrip;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            if (Settings.Default.LockOverlay) WindowsServices.SetWindowExTransparent(hwnd);
        }

        private void ButtonNotification_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PopupWindow.IsOpen = true;
            }
        }

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            ReleaseCapture();
            SendMessage(new WindowInteropHelper(this).Handle, 0xA1, 0x2, 0);
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            Settings.Default.OverlayLeft = Left;
            Settings.Default.OverlayTop = Top;
            Settings.Default.Save();
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0 && Settings.Default.OverlayOpacity >= 0.9) return;
            if (e.Delta > 0 && Settings.Default.OverlayOpacity <= 0.9)
            {
                var opacity = Settings.Default.OverlayOpacity + 0.1;
                var color = new SolidColorBrush(Colors.White) { Opacity = opacity };
                Background = color;
                Settings.Default.OverlayOpacity = opacity;
            }
            else if (Settings.Default.OverlayOpacity > 0.2)
            {
                var opacity = Settings.Default.OverlayOpacity - 0.1;
                var color = new SolidColorBrush(Colors.White) {Opacity = opacity};
                Background = color;
                Settings.Default.OverlayOpacity = opacity;
            }
            Settings.Default.Save();
        }

        private void LabelCurrentMap_MouseDown(object sender, MouseButtonEventArgs e)
        {
            IndicatorImage = true;
        }

        private void LabelMapDrops_MouseDown(object sender, MouseButtonEventArgs e)
        {
            IndicatorImage = false;
        }
    }
}
