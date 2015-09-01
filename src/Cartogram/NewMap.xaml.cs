using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using Cartogram.JSON;
using Cartogram.Properties;
using Cartogram.SQL;
using MessageBox = System.Windows.MessageBox;

namespace Cartogram
{
    /// <summary>
    /// Interaction logic for NewMap.xaml
    /// </summary>
    public partial class NewMap : Window
    {
        #region DLL Import
        [DllImport("User32.dll")]
        protected static extern int
        SetClipboardViewer(int hWndNewViewer);

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern bool
        ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);
        #endregion

        private HwndSource _source;
        private static MainWindow _main;
        private IntPtr _handle, _nextClipboardViewer;

        public bool Cancelled { get; set; }

        public Map CurrentMap { get; set; }

        public NewMap()
        {
            InitializeComponent();
            _main = MainWindow.GetSingleton();

            PopulateLeagues();
            ComboBoxName.DataContext = Sqlite.CharactersList();

            ComboLeague.Text = Settings.Default.SelectedLeague;
            ComboBoxName.Text = Settings.Default.CharacterName;
            ZanaInt.Content = Settings.Default.ZanaQuantity.ToString();
            ZanaValue.Value = Settings.Default.ZanaQuantity;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var source = PresentationSource.FromVisual(this) as HwndSource;
            if (source != null) _handle = source.Handle;
            _nextClipboardViewer = (IntPtr)SetClipboardViewer((int)source.Handle);
            source.AddHook(WndProc);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_source != null)
            {
                _source.RemoveHook(WndProc);
                _source = null;
                _main._newMap = null;
            }
            base.OnClosed(e);
        }

        private void PopulateLeagues()
        {
            var leagueList = new List<League>();
            foreach (var entry in _main.LeagueObject)
            {
                dynamic values = entry.Value;
                var league = new League
                {
                    Active = values["active"].ToString() == "1",
                    PrettyName = values["prettyName"].ToString(),
                };
                leagueList.Add(league);
            }
            foreach (var league in leagueList.Where(league => league.Active))
            {
                ComboLeague.Items.Add(league.PrettyName);
            }
        }

        private void TextBoxName_LostFocus(object sender, RoutedEventArgs e)
        {
            if (ComboBoxName.Text == string.Empty) return;
            var currentCharacters = Sqlite.CharactersList();
            if (!currentCharacters.Contains(ComboBoxName.Text))
            {
                Sqlite.InsertCharacter(ComboBoxName.Text);
                currentCharacters.Add(ComboBoxName.Text);
                ComboBoxName.DataContext = currentCharacters;
            }
            Settings.Default.CharacterName = ComboBoxName.Text;
            Settings.Default.Save();
        }

        private void ComboLeague_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.Default.SelectedLeague = ComboLeague.Text;
            Settings.Default.Save();
        }

        private void NameRemove_Click(object sender, RoutedEventArgs e)
        {
            if (ComboBoxName.Text == string.Empty) return;

            if (Sqlite.DeleteCharacter(ComboBoxName.Text))
            {
                _main.ExtendedStatusStrip.AddStatus($"Removed {ComboBoxName.Text} from saved names.");
                if (Settings.Default.CharacterName == ComboBoxName.Text)
                    Settings.Default.CharacterName = string.Empty;
                ComboBoxName.Text = string.Empty;
                ComboBoxName.IsDropDownOpen = false;
                ComboBoxName.DataContext = Sqlite.CharactersList();
            }
            else
            {
                _main.ExtendedStatusStrip.AddStatus($"Failed to remove {ComboBoxName.Text} from saved names.");
            }
        }


        private void ZanaValue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ZanaInt.Content = ZanaValue.Value.ToString("0");
        }

        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            if (ComboLeague.Text == string.Empty) MessageBox.Show(@"Please select a league", @"No league selected");
            if (ComboBoxName.Text == string.Empty) MessageBox.Show(@"Please enter your characters name", @"No character selected");
            int zanaQuantity;
            Settings.Default.ZanaQuantity = int.TryParse(ZanaInt.Content.ToString(), out zanaQuantity) ? zanaQuantity : 0;
            Settings.Default.SelectedLeague = ComboLeague.Text;

            //if (publicOpt)
            //{
            //    _mySqlId = _mySql.AddMap(_main.CurrentMap);
            //    labelMySqlId.Text = _mySqlId.ToString(CultureInfo.InvariantCulture);
            //}
            //_main.CurrentMap.SqlId = _mySqlId;

            if (_main.CurrentMap != null || CurrentMap == null) return;


            CurrentMap.Quantity = CurrentMap.Quantity;
            if (ComboZanaMod.SelectionBoxItem != null)
            {
                CurrentMap.ZanaMod = ComboZanaMod.SelectionBoxItem.ToString();
            }
            else
            {
                CurrentMap.Quantity += Settings.Default.ZanaQuantity;
            }
            CurrentMap.OwnMap = radioButtonOwn.IsChecked == true;
            CurrentMap.League = ComboLeague.Text;
            CurrentMap.Character = ComboBoxName.Text;
            CurrentMap.Id = Sqlite.AddMap(CurrentMap);

            if (CurrentMap.Id > 0)
            {
                CurrentMap.StartAt = DateTime.Now;
                _main.CurrentMap = CurrentMap;
                Settings.Default.Save();
                try
                {
                    System.Windows.Forms.Clipboard.SetDataObject(string.Empty, false, 5, 200);
                }
                catch
                {
                    _main.ExtendedStatusStrip.AddStatus("Failed to clear clipboard");
                }
                Close();
            }
            else
            {
                MessageBox.Show(@"Failed inserting Map into database, please try again",
                    @"Failed inserting into database", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            Cancelled = true;
            Close();
        }

        #region WndProc

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_DRAWCLIPBOARD = 0x308;

            switch (msg)
            {
                case WM_DRAWCLIPBOARD:
                    if (ParseHandler.CheckClipboard())
                    {
                        CurrentMap = ParseHandler.ParseClipboard();
                        //CurrentMap.ExpBefore = ExpValue();
                        if (CurrentMap == null) break;
                        PopulateMapInformation();
                        MapInformation.IsExpanded = true;
                        break;
                    }
                    SendMessage(_nextClipboardViewer, msg, wParam, lParam);
                    break;
            }
            return IntPtr.Zero;
        }

        #endregion

        #region Custom Methods

        private void PopulateMapInformation()
        {
            var mapInformation = Sqlite.MapInformation(CurrentMap.Name);
            if (mapInformation == null) return;
            LabelMapValue.Text = mapInformation.Zone;
            LabelBossValue.Text = mapInformation.Boss;
            LabelDescription.Text = mapInformation.BossDetails;
        }

        private string GetExperience()
        {
            Thread.Sleep(500);
            System.Windows.Forms.Cursor.Position = new System.Drawing.Point((Screen.PrimaryScreen.Bounds.Width / 2) - 200, Screen.PrimaryScreen.Bounds.Height);
            SendKeys.SendWait("^c");
            var clipboardContents = System.Windows.Clipboard.GetText();
            if (!clipboardContents.Contains("Current Exp:"))
            {
                MessageBox.Show(@"Failed capturing experience, please try again.", @"Experience Error");
                return string.Empty;
            }
            return clipboardContents;
        }

        /// <summary>
        /// Parses the captured experience into the Experience object
        /// </summary>
        /// <returns>Experience object containing all the details</returns>
        internal Experience ExpValue()
        {
            var exp = GetExperience();
            if (exp == string.Empty) return null;
            var currentPercent = Regex.Match(exp, @"(?<=\().+?(?=\%)");
            var currentLevel = Regex.Match(exp, @"(?<=el ).+?(?=\ )");
            var currentExperience = Regex.Match(exp, @"(?<=p: ).+?(?=\ )");
            var nextLevel = Regex.Match(exp, @"(?<=l: ).+?(?=\n)");
            int level, percent;
            long currentExp, expToLevel;
            var expObj = new Experience
            {
                Level = currentLevel.Success ? int.TryParse(currentLevel.Groups[0].ToString(), out level) ? level : 0 : 0,
                Percentage = currentPercent.Success ? int.TryParse(currentPercent.Groups[0].ToString(), out percent) ? percent : 0 : 0,
                CurrentExperience = currentExperience.Success ? long.TryParse(currentExperience.Groups[0].ToString().Replace(",", ""), out currentExp) ? currentExp : 0 : 0,
                NextLevelExperience = nextLevel.Success ? long.TryParse(nextLevel.Groups[0].ToString().Replace(",", ""), out expToLevel) ? expToLevel : 0 : 0
            };
            return expObj;
        }

        #endregion

        private void MapInformation_Expanded(object sender, RoutedEventArgs e)
        {
            var growAnimation = new DoubleAnimation
            {
                From = 140,
                To = 290,
                FillBehavior = FillBehavior.Stop,
                BeginTime = TimeSpan.FromSeconds(0.1),
                Duration = TimeSpan.FromSeconds(0.2)
            };
            var growForm = new Storyboard()
            {
                Name = "ExpandForm"
            };
            growForm.Children.Add(growAnimation);
            Storyboard.SetTarget(growAnimation, this);
            Storyboard.SetTargetProperty(growAnimation, new PropertyPath(HeightProperty));
            growForm.Begin(this, true);
        }

        private void MapInformation_Collapsed(object sender, RoutedEventArgs e)
        {
            var shrinkAnimation = new DoubleAnimation
            {
                From = 290,
                To = 140,
                FillBehavior = FillBehavior.Stop,
                BeginTime = TimeSpan.FromSeconds(0.1),
                Duration = TimeSpan.FromSeconds(0.2)
            };
            var growForm = new Storyboard
            {
                Name = "ShrinkForm"
            };
            growForm.Children.Add(shrinkAnimation);
            Storyboard.SetTarget(shrinkAnimation, this);
            Storyboard.SetTargetProperty(shrinkAnimation, new PropertyPath(HeightProperty));
            growForm.Begin(this, true);
        }

        private void LabelDescription_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            
        }

        private void LabelDescription_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            
        }
    }
}
