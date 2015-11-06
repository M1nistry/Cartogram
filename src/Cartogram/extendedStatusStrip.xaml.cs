using System;
using System.Windows;
using System.Windows.Controls;

namespace Cartogram
{
    /// <summary>
    /// Interaction logic for ExtendedStatusStrip.xaml
    /// </summary>
    public partial class ExtendedStatusStrip : UserControl
    {
        public ExtendedStatusStrip()
        {
            InitializeComponent();
            StatusValue.Width = Width - (ButtonExpand.Width  + 65);
        }

        public bool Timestamps { get; set; }

        public EventHandler ExpandClickHandler { get; set; }

        public void AddStatus(string message)
        {
            if (message == string.Empty) return;
            var timeStamp = Timestamps ? "[" + DateTime.Now.ToShortTimeString() + "] " : string.Empty;
            ListBoxStatus.Items.Add(timeStamp + message);
            StatusValue.Content = message;
        }

        public void ButtonExpand_Click(object sender, RoutedEventArgs e)
        {
            ButtonExpand.Content = ButtonExpand.Content.ToString() == "↓" ? "↑" : "↓";
            ListBoxStatus.Visibility = ListBoxStatus.Visibility == Visibility.Hidden ? Visibility.Visible : Visibility.Hidden;
            Height = Height <= 25 ? 150 : 25;
            ListBoxStatus.Items.MoveCurrentToLast();
            ListBoxStatus.ScrollIntoView(ListBoxStatus.Items.CurrentItem);
        }
    }
}
