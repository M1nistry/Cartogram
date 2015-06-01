using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Cartogram.SQL;

using System.Drawing;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using Cartogram.JSON;
using Cartogram.Properties;
using Tesseract;
using Brushes = System.Windows.Media.Brushes;

namespace Cartogram
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        #region DLLs

        [DllImport("User32.dll")]
        protected static extern int
        SetClipboardViewer(int hWndNewViewer);

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern bool
        ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("User32.dll")]
        private static extern bool RegisterHotKey([In] IntPtr hWnd, [In] int id, [In] uint fsModifiers, [In] uint vk);

        [DllImport("User32.dll")]
        private static extern bool UnregisterHotKey([In] IntPtr hWnd, [In] int id);

        private HwndSource _source;

        #endregion

        IntPtr _nextClipboardViewer;
        internal Sqlite _sql;
        private static MainWindow _main;

        private readonly DispatcherTimer _mapTimer;
        internal Map CurrentMap;
        private int _timerTicks;
        private string _state;
        private IntPtr _handle;

        public MainWindow()
        {
            InitializeComponent();

            _sql = new Sqlite();
            _main = this;
            if (_sql.ExperienceCount() != 100) PopulateExperience();

            RefreshGrids();
            RefreshDrops(0);
            PopulateLeagues();
            ScrollViewer.SetCanContentScroll(GridDrops, false);
            _mapTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1),
                IsEnabled = false
            };
            ComboLeague.Text = Settings.Default.SelectedLeague;
            
            _mapTimer.Tick += _mapTimer_Elapsed;
            
            ExtendedStatusStrip.ButtonExpand.Click += ExpandStatus;
            _state = "WAITING";
            ExtendedStatusStrip.AddStatus("Welcome back, Exile!");
            LabelShowToolip.Content = "Hello World!";
            ComboBoxName.DataContext = _sql.CharactersList();
            ComboBoxName.Text = Settings.Default.CharacterName;
        }

        public static MainWindow GetSingleton()
        {
            return _main;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var source = PresentationSource.FromVisual(this) as HwndSource;
            if (source != null) _handle = source.Handle;
            _nextClipboardViewer = (IntPtr)SetClipboardViewer((int)source.Handle);
            source.AddHook(WndProc);
            RegisterHotkeys();
        }

        protected override void OnClosed(EventArgs e)
        {
            _source.RemoveHook(WndProc);
            _source = null;
            base.OnClosed(e);
            UnregisterHotkeys();
        }

        internal void RegisterHotkeys()
        {
            try
            {
                UnregisterHotkeys();
                RegisterHotKey(_handle, 0, 0, Convert.ToUInt32(Settings.Default.mapHotkey));
                RegisterHotKey(_handle, 1, 0, Convert.ToUInt32(Settings.Default.zanaHotkey));
                RegisterHotKey(_handle, 2, 0, Convert.ToUInt32(Settings.Default.cartoHotkey));
            }
            catch (Exception Cuex)
            {
                System.Windows.MessageBox.Show(@"Failed to register hotkeys, please close the application and try again",
                    @"Failed registering hotkeys", MessageBoxButton.OK, MessageBoxImage.Error);
                ExtendedStatusStrip.AddStatus(@"Failed registering hotkeys, please restart application.");
            }
        }

        internal void UnregisterHotkeys()
        {
            var helper = new WindowInteropHelper(this);
            UnregisterHotKey(helper.Handle, 0);
            UnregisterHotKey(helper.Handle, 1);
            UnregisterHotKey(helper.Handle, 2);
        }

        private void PopulateLeagues()
        {
            var jObject = JsonHandler.ParseJsonObject("http://api.exiletools.com/ladder?listleagues=1");
            var leagueList = new List<League>();
            foreach (var entry in jObject)
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

        private void UpdateInformation()
        {
            LabelMapsRunValue.Content = _sql.CountMapsToday();
        }

        private void PopulateExperience()
        {
            var lines = File.ReadAllLines(Directory.GetCurrentDirectory() + @"\Resources\Experience.csv").Select(a => a.Split(','));
            var listExp = new List<Experience>();
            foreach (var line in lines)
            {
                int level, expGoal;
                long experience;
                var exp = new Experience
                {
                    Level = int.TryParse(line[0], out level) ? level : 0,
                    CurrentExperience = long.TryParse(line[1], out experience) ? experience : 0,
                    NextLevelExperience = int.TryParse(line[2], out expGoal) ? expGoal : 0
                };
                listExp.Add(exp);
            }
            _sql.AddExperience(listExp);
        }

        private void RefreshGrids()
        {
            var mapTable = _sql.MapDataTable();
            GridMaps.DataContext = mapTable.DefaultView;
            SortDataGrid(GridMaps, 0, ListSortDirection.Descending);
        }

        private void RefreshDrops(int rowId)
        {
            var dropTable = _sql.DropDataTable(rowId);
            GridDrops.DataContext = dropTable.DefaultView;
        }


        #region WndProc

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_DRAWCLIPBOARD = 0x308;
            const int WM_CHANGECBCHAIN = 0x030D;
            const int WM_HOTKEY = 0x0312;

            switch (msg)
            {
                case WM_DRAWCLIPBOARD:
                    if (CheckClipboard())
                    {
                        if (ComboLeague.Text == string.Empty && Visibility == Visibility.Visible)
                        {
                            System.Windows.MessageBox.Show(@"Please verify that you have selected a league and try again.", @"Error!", MessageBoxButton.OK, MessageBoxImage.Hand);
                            return IntPtr.Zero;
                        }
                        var clipboard = System.Windows.Clipboard.GetText(System.Windows.TextDataFormat.Text);
                        switch (_state)
                        {
                            case ("WAITING"):
                                CurrentMap = ParseClipboard();
                                if (CurrentMap == null) break;
                                CurrentMap.ExpBefore = ExpValue();
                                CurrentMap.League = ComboLeague.Text;
                                CurrentMap.Character = ComboBoxName.Text;
                                //if (publicOpt)
                                //{
                                //    _mySqlId = _mySql.AddMap(CurrentMap);
                                //    labelMySqlId.Text = _mySqlId.ToString(CultureInfo.InvariantCulture);
                                //}
                                //CurrentMap.SqlId = _mySqlId;
                                CurrentMap.Id = _sql.AddMap(CurrentMap);
                                if (CurrentMap.Id > 0)
                                {
                                    CanvasInformation.Visibility = Visibility.Hidden;
                                    CanvasCurrentMap.Visibility = Visibility.Visible;
                                    LabelMapValue.Content = CurrentMap.Name;
                                    _timerTicks = 0;
                                    _mapTimer.Start();

                                    CurrentMapBorder.BorderBrush = Brushes.Red;
                                    ExtendedStatusStrip.AddStatus($"Beginning {CurrentMap.Name} map...");
                                    RefreshGrids();
                                    _state = "DROPS";
                                }
                                break;

                            case ("DROPS"):
                                if (CurrentMap.Id <= 0) break;
                                try
                                {
                                    if (clipboard.Contains("Map"))
                                    {
                                        var parsedMap = ParseClipboard();
                                        if (parsedMap == null) break;
                                        _sql.AddDrop(parsedMap, CurrentMap.Id);
                                        //if (publicOpt && _mySqlId > 0) _mySql.AddDrop(parsedMap, _mySqlId);
                                    }
                                    if (clipboard.Contains("Currency"))
                                    {
                                        var parsedCurrency = ParseCurrency();
                                        _sql.AddCurrency(CurrentMap.Id, parsedCurrency);
                                        //if (publicOpt && _mySqlId > 0) _mySql.AddCurrency(_mySqlId, parsedCurrency);
                                    }
                                    if (!clipboard.Contains("Map") && clipboard.Contains("Unique"))
                                    {
                                        var parsedUnique = ParseUnique();
                                        _sql.AddUnique(CurrentMap.Id, parsedUnique);
                                        //if (publicOpt && _mySqlId > 0) _sql.AddUnique(_mySqlId, parsedUnique);
                                    }
                                }
                                catch (Exception)
                                {
                                    //do nothing
                                }
                                System.Windows.Clipboard.SetText("");
                                break;
                            case ("ZANA"):
                                if (CurrentMap.Id <= 0) break;
                                _sql.AddDrop(ParseClipboard(), CurrentMap.Id, 1);
                                //if (publicOpt && _mySqlId > 0) _mySql.AddDrop(ParseClipboard(), _SqLiteId, 1);
                                break;

                            case ("CARTO"):
                                if (CurrentMap.Id <= 0) break;
                                _sql.AddDrop(ParseClipboard(), CurrentMap.Id, 0, 1);
                                //if (publicOpt && _mySqlId > 0) _mySql.AddDrop(ParseClipboard(), _SqLiteId, 0, 1);
                                break;
                        }
                    }
                    SendMessage(_nextClipboardViewer, msg, wParam, lParam);
                    break;

                case WM_CHANGECBCHAIN:
                    if (wParam == _nextClipboardViewer) _nextClipboardViewer = lParam;
                    else SendMessage(_nextClipboardViewer, msg, wParam, lParam);
                    break;

                case WM_HOTKEY:
                    switch (wParam.ToInt32())
                    {
                        case (0):
                            if (_state == "DROPS")
                            {
                                _mapTimer.Stop();
                                CurrentMapBorder.BorderBrush = Brushes.DimGray;
                                var expAfter = ExpValue();
                                _sql.FinishMap(CurrentMap.Id, expAfter);
                                //if (publicOpt && _mySqlId > 0) _mySql.FinishMap(_mySqlId, expAfter);
                                _state = "WAITING";
                                UpdateInformation();
                                var expDiff = expAfter.CurrentExperience - CurrentMap.ExpBefore.CurrentExperience;
                                var expGoal = _sql.ExperienceGoal(CurrentMap.ExpBefore.Level);
                                var percentDiff = (float)expDiff / expGoal;
                                //ExtendedStatusStrip.AddStatus(string.Format("Finished {0} map, gained {1} of experience", CurrentMap.Name, percentDiff));
                            }
                            break;

                        case (1):
                            if (CurrentMap == null) break;
                            if (_state == "ZANA")
                            {
                                _state = "DROPS";
                                ExtendedStatusStrip.AddStatus($"Finished Zana, returning to {CurrentMap.Name} map");
                                break;
                            }
                            ExtendedStatusStrip.AddStatus($"Zana found, press {KeyInterop.KeyFromVirtualKey(Settings.Default.mapHotkey)} to toggle back.");
                            _state = "ZANA";
                            break;

                        case (2):
                            if (_state == "CARTO")
                            {
                                _state = "DROPS";
                                ExtendedStatusStrip.AddStatus($"Finished Cartogram, returning to {KeyInterop.KeyFromVirtualKey(Settings.Default.mapHotkey)} map");
                                break;
                            }
                            ExtendedStatusStrip.AddStatus($"Cartographer found! press {KeyInterop.KeyFromVirtualKey(Settings.Default.mapHotkey)} when drops recorded.");
                            _state = "CARTO";
                            break;

                    }
                    break;
            }
            return IntPtr.Zero;
        }

        #endregion

        #region Custom Methods

        /// <summary>
        /// Checks the clipboard to determine if it contains PoE Related data or not
        /// </summary>
        /// <returns> TRUE if it contains 'Rarity:' on the first line </returns>
        internal bool CheckClipboard()
        {
            var clipboardContents = System.Windows.Clipboard.GetText(System.Windows.TextDataFormat.Text).Replace("\r", "").Split('\n');
            return clipboardContents.Length != -1 && clipboardContents[0].StartsWith("Rarity:");
        }

        /// <summary>
        /// Parses the information gained off the keyboard to construct a Map Object
        /// </summary>
        /// <returns>Map object with details from the clipboard</returns>
        internal Map ParseClipboard()
        {
            var clipboardValue = System.Windows.Clipboard.GetText(System.Windows.TextDataFormat.Text);
            var clipboardContents = clipboardValue.Replace("\r", "").Split('\n');
            Map newMap;
            if (!clipboardValue.Contains("Map") || clipboardValue.Contains("Sacrifice at")) return null;

            if (clipboardContents[0].Contains("Normal") || clipboardContents[0].Contains("Magic"))
            {
                newMap = new Map
                {
                    Rarity = clipboardContents[0].Replace("Rarity: ", ""),
                    Level = int.Parse(clipboardContents[3].Replace("Map Level:", "")),
                    Name = MapName(clipboardContents[1]),
                    Affixes = GetAffixes(clipboardContents),
                };
                if (clipboardValue.Contains("Item Quantity:"))
                {
                    int quantity;
                    if (int.TryParse(clipboardContents[4].Replace("Item Quantity: +", "").Replace("% (augmented)", ""), out quantity))
                        newMap.Quantity = quantity;
                }
                if (clipboardValue.Contains("Quality:"))
                {
                    int quality;
                    if (int.TryParse(clipboardContents[5].Replace("Quality: +", "").Replace("% (augmented)", ""), out quality))
                        newMap.Quality = quality;
                }
                return newMap;
            }

            if (clipboardContents[0].Replace("Rarity: ", "") == "Rare" || clipboardContents[0].Replace("Rarity: ", "") == "Unique")
            {
                var i = 0;
                if (clipboardValue.Contains("Unidentified")) i = 1;

                newMap = new Map
                {
                    Rarity = clipboardContents[0].Replace("Rarity: ", ""),
                    Level = int.Parse(clipboardContents[4 - i].Replace("Map Level:", "")),
                    Affixes = GetAffixes(clipboardContents),
                };
                newMap.Name = newMap.Rarity == "Rare" ? MapName(clipboardContents[2 - i]) : MapName(clipboardContents[1]);

                if (clipboardValue.Contains("Item Quantity:"))
                {
                    int quantity;
                    if (int.TryParse(clipboardContents[5].Replace("Item Quantity: +", "").Replace("% (augmented)", ""), out quantity))
                        newMap.Quantity = quantity;
                }
                if (clipboardValue.Contains("Quality:"))
                {
                    int quality;
                    if (int.TryParse(clipboardContents[6].Replace("Quality: +", "").Replace("% (augmented)", ""), out quality))
                        newMap.Quality = quality;
                }
                return newMap;
            }
            return null;
        }

        /// <summary>
        /// Parses currency off the clipboard into a KVP of stack size and currency name
        /// </summary>
        /// <returns>Stack Count and Name</returns>
        internal KeyValuePair<int, string> ParseCurrency()
        {
            var clipboardContents = System.Windows.Clipboard.GetText(System.Windows.TextDataFormat.Text).Replace("\r", "").Split(new[] { '\n' });
            if (clipboardContents[0] != "Rarity: Currency") return new KeyValuePair<int, string>(-1, "");

            var currency = clipboardContents[1].Replace("Orb", "").Replace("of", "").Trim();
            var size = Regex.Match(clipboardContents[3].Replace("Stack Size: ", ""), @"^.*?(?=/)");

            return new KeyValuePair<int, string>(int.Parse(size.ToString()), currency);
        }

        /// <summary>
        /// Parses the name out of a unique non-map item off the clipboard
        /// </summary>
        /// <returns>Name of unique item</returns>
        internal string ParseUnique()
        {
            var clipboardContents = System.Windows.Clipboard.GetText(System.Windows.TextDataFormat.Text).Replace("\r", "").Split(new[] { '\n' });
            if (clipboardContents[0] != "Rarity: Unique") return "";

            var item = clipboardContents[1];
            return item;
        }

        /// <summary>
        /// Gets the affixes from the clipboard and puts them into a list
        /// </summary>
        /// <param name="clipboardContents">The map item as it appears on the clipboard</param>
        /// <returns>List containing each parameter</returns>
        private static List<string> GetAffixes(string[] clipboardContents)
        {
            var affixes = new List<string>();
            if (clipboardContents.Count(line => line == "--------") == 4 || clipboardContents.Count(line => line == "--------") == 5)
            {
                var lineCount = 0;
                foreach (var line in clipboardContents)
                {
                    if (line == "--------")
                    {
                        lineCount++;
                        continue;
                    }
                    if (lineCount < 3) continue;
                    if (lineCount == 4) break;
                    affixes.Add(line);
                }
            }
            return affixes;
        }

        /// <summary> Returns the map name given the complete name </summary>
        /// <param name="inputLine"> Line containing map name + affixes</param>
        /// <returns> Map Name eg. "Vaal Pyramid" </returns>
        private static string MapName(string inputLine)
        {
            var maps = Maps.MapArray();

            foreach (var x in maps.Where(inputLine.Contains))
            {
                return x;
            }
            return inputLine;
        }

        /// <summary>
        /// Uses OCR to parse the experience from the tooltip. Moves the mouse to the desired position automatically
        /// </summary>
        /// <returns>The string containing the full experience tooltip</returns>
        private string CaptureExp()
        {
            var previousPoint = System.Windows.Forms.Cursor.Position;
            System.Windows.Forms.Cursor.Position = new System.Drawing.Point((Screen.PrimaryScreen.Bounds.Width / 2) - 200, Screen.PrimaryScreen.Bounds.Height);
            System.Windows.Forms.Cursor.Clip = new Rectangle(System.Windows.Forms.Cursor.Position, new System.Drawing.Size(1, 1));
            Thread.Sleep(750);
            var bmpScreenshot = new Bitmap((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight,
                                                           System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var gfxScreenshot = Graphics.FromImage(bmpScreenshot);
            gfxScreenshot.CopyFromScreen(System.Windows.Forms.Cursor.Position.X + 35, System.Windows.Forms.Cursor.Position.Y - 60, 0, 0, new System.Drawing.Size(580, 500), CopyPixelOperation.SourceCopy);
            bmpScreenshot.Save("Screenshot.bmp");

            var image = Pix.LoadFromFile(Directory.GetCurrentDirectory() + "\\Screenshot.bmp");
            string result;
            var tessData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Cartogram";
            using (var engine = new TesseractEngine(tessData, "eng", EngineMode.Default))
            {
                using (var page = engine.Process(image))
                {
                    result = page.GetText();
                }
            }
            File.Delete(Directory.GetCurrentDirectory() + "\\Screenshot.bmp");
            System.Windows.Forms.Cursor.Position = new System.Drawing.Point(previousPoint.X, previousPoint.Y);
            return result;
        }

        /// <summary>
        /// Parses the captured experience into the Experience object
        /// </summary>
        /// <returns>Experience object containing all the details</returns>
        internal Experience ExpValue()
        {
            var exp = CaptureExp();
            var currentPercent = Regex.Match(exp, @"(?<=\().+?(?=\%)");
            var currentLevel = Regex.Match(exp, @"(?<=el ).+?(?=\ )");
            var currentExperience = Regex.Match(exp, @"(?<=p: ).+?(?=\ )");
            var nextLevel = Regex.Match(exp.Replace("\n", ""), @"(?<=l: ).+?(?=$)");
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

        private void _mapTimer_Elapsed(object sender, EventArgs eventArgs)
        {
            _timerTicks++;
            TimerValue.Content = $"{_timerTicks/3600:00}:{(_timerTicks/60)%60:00}:{_timerTicks%60:00}";
        }

        public static void SortDataGrid(System.Windows.Controls.DataGrid dataGrid, int columnIndex = 0, ListSortDirection sortDirection = ListSortDirection.Ascending)
        {
            var column = dataGrid.Columns[columnIndex];
            dataGrid.Items.SortDescriptions.Clear();
            dataGrid.Items.SortDescriptions.Add(new SortDescription(column.SortMemberPath, sortDirection));

            foreach (var col in dataGrid.Columns)
            {
                col.SortDirection = null;
            }
            column.SortDirection = sortDirection;

            dataGrid.Items.Refresh();
        }

        private void ExpandStatus(object sender, EventArgs e)
        {
            ExtendedStatusStrip.Height = 125;
            Canvas.SetTop(ExtendedStatusStrip, -100);
        }

        private void GridMaps_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridMaps.SelectedItems.Count <= 0) return;
            var row = (DataRowView)GridMaps.SelectedItems[0];
            var rowId = row["id"].ToString();
            int id;
            if (int.TryParse(rowId, out id) )RefreshDrops(id);
        }

        private void DataGrid_Documents_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            e.Handled = true;
        }

        private void TextBoxName_LostFocus(object sender, RoutedEventArgs e)
        {
            if (ComboBoxName.Text == string.Empty) return;
            var currentCharacters = _sql.CharactersList();
            if (!currentCharacters.Contains(ComboBoxName.Text))
            {
                _sql.InsertCharacter(ComboBoxName.Text);
                currentCharacters.Add(ComboBoxName.Text);
                ComboBoxName.DataContext = currentCharacters;
            }
            Settings.Default.CharacterName = ComboBoxName.Text;
            Settings.Default.Save();
        }

        private void MenuSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new ApplicationSettings();
            settingsWindow.Show();
        }

        private void GridMaps_DetailsClick(object sender, RoutedEventArgs e)
        {
            var rowStr = ((DataRowView) GridMaps.SelectedItem)?.Row[0];
            int rowId;
            if (!int.TryParse(rowStr.ToString(), out rowId)) return;
            var selectedMap = _sql.GetMap(rowId);
            var mapDetails = new Details(selectedMap);
            mapDetails.ShowDialog();
        }

        private void GridMaps_DeleteClick(object sender, RoutedEventArgs e)
        {
            var rowStr = ((DataRowView)GridMaps.SelectedItem)?.Row.ItemArray[0];
            int id;
            if (int.TryParse(rowStr?.ToString(), out id)) _sql.DeleteMap(id);
            RefreshGrids();
        }

        private void ComboLeague_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.Default.SelectedLeague = ComboLeague.Text;
            Settings.Default.Save();
        }

        private void MainMenu_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void NameRemove_Click(object sender, RoutedEventArgs e)
        {
            if (ComboBoxName.Text == string.Empty) return;
            
            if (_sql.DeleteCharacter(ComboBoxName.Text))
            {
                ExtendedStatusStrip.AddStatus($"Removed {ComboBoxName.Text} from saved names.");
                if (Settings.Default.CharacterName == ComboBoxName.Text)
                    Settings.Default.CharacterName = string.Empty;
                ComboBoxName.Text = string.Empty;
                ComboBoxName.IsDropDownOpen = false;
                ComboBoxName.DataContext = _sql.CharactersList();
            }
            else
            {
                ExtendedStatusStrip.AddStatus($"Failed to remove {ComboBoxName.Text} from saved names.");
            }
        }
    }

}
