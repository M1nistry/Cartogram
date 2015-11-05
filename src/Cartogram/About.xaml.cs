using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Cartogram
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : Window
    {
        public Version GithubVersion { get; set; }

        private readonly MainWindow _main; 
        public About()
        {
            InitializeComponent();
            _main = MainWindow.GetSingleton();
            DataContext = this;
            Icon = _main.Icon;
            TextBlockVersion.Text = _main.Version.ToString();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (GithubVersion == null || GithubVersion <= _main.Version) return;
            CanvasUpdate.Visibility = Visibility.Visible;
            LabelBlockVersion.Content = $"A new version has been found: {GithubVersion}";
            Title = "Update found!";
        }

        private void LabelGithubLink_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Process.Start("https://github.com/M1nistry/Cartogram/releases");
            e.Handled = true;
        }

        private void LabelGithubLink_MouseEnter(object sender, MouseEventArgs e)
        {
            LabelGithubLink.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
        }

        private void LabelGithubLink_MouseLeave(object sender, MouseEventArgs e)
        {
            LabelGithubLink.Foreground = new SolidColorBrush(Color.FromArgb(255, 12, 130, 185));
        }

        private void image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Process.Start("https://github.com/M1nistry/Cartogram");
            e.Handled = true;
        }
    }
}
