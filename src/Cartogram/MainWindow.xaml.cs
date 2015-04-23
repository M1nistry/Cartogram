using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Cartogram.SQL;

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

        [DllImport("user32.dll")]
        internal static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);


        #endregion

        IntPtr _nextClipboardViewer;
        private Sqlite _sql;

        private Timer _mapTimer;
        internal Map CurrentMap;
        private int _timerTicks;
        private string _state;
        public MainWindow()
        {
            InitializeComponent();

            _sql = new Sqlite();
            RefreshGrids();
            _mapTimer = new Timer
            {
                Interval = 1000,
                Enabled = false,
            };
            _mapTimer.Elapsed += _mapTimer_Elapsed;
            //ExtendedStatusStrip.ButtonExpand.Click += ExpandStatus;

            ExtendedStatusStrip.AddStatus("Welcome back, Exile!"); 

            IntPtr mainWindowPtr = new WindowInteropHelper(this).Handle;
            HwndSource mainWindowSrc = HwndSource.FromHwnd(mainWindowPtr);
            mainWindowSrc.AddHook(WndProc);
        }

        private void RefreshGrids()
        {
            ///TODO: Can we make it a bindable/updateable thing?
            var mapTable = _sql.MapDataTable();
            GridMaps.DataContext = mapTable.DefaultView;
            SortDataGrid(GridMaps, 0, ListSortDirection.Descending);

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
                            MessageBox.Show(@"Please verify that you have selected a league and try again.", @"Error!", MessageBoxButton.OK, MessageBoxImage.Hand);
                            return IntPtr.Zero;
                        }
                        var clipboard = Clipboard.GetText(TextDataFormat.Text);
                        switch (_state)
                        {
                            case ("WAITING"):
                                CurrentMap = ParseClipboard();
                                if (CurrentMap == null) break;
                                CurrentMap.ExpBefore = ExpValue();
                                CurrentMap.League = ComboLeague.Text;
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

                                    ExtendedStatusStrip.AddStatus(string.Format("Beginning {0} map...", CurrentMap.Name));
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
                                Clipboard.SetText("");
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
                                var expAfter = ExpValue();
                                _sql.FinishMap(CurrentMap.Id, expAfter);
                                //if (publicOpt && _mySqlId > 0) _mySql.FinishMap(_mySqlId, expAfter);
                                _state = "WAITING";
                                var expDiff = expAfter.CurrentExperience - CurrentMap.ExpBefore.CurrentExperience;
                                var expGoal = _sql.ExperienceGoal(CurrentMap.ExpBefore.Level);
                                var percentDiff = (float)expDiff / expGoal;
                                //ExtendedStatusStrip.AddStatus(string.Format("Finished {0} map, gained {1} of experience", CurrentMap.Name, percentDiff));
                            }
                            break;

                        case (1):
                            if (_state == "ZANA")
                            {
                                _state = "DROPS";
                                ExtendedStatusStrip.AddStatus(string.Format("Finished Zana, returning to {0} map", CurrentMap.Name));
                                break;
                            }
                            ExtendedStatusStrip.AddStatus(string.Format("Zana found, press {0} again once zana drops have been recorded.", "Hotkey"));
                            _state = "ZANA";
                            break;
                        case (2):
                            if (_state == "CARTO")
                            {
                                _state = "DROPS";
                                ExtendedStatusStrip.AddStatus(string.Format("Finished Cartogram, returning to {0} map", CurrentMap.Name));
                                break;
                            }
                            ExtendedStatusStrip.AddStatus(string.Format("Cartogram found! press {0} again once map drops have been recorded.", "Hotkey"));
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
            var clipboardContents = Clipboard.GetText(TextDataFormat.Text).Replace("\r", "").Split(new[] { '\n' });
            return clipboardContents.Length != -1 && clipboardContents[0].StartsWith("Rarity:");
        }

        /// <summary>
        /// Parses the information gained off the keyboard to construct a Map Object
        /// </summary>
        /// <returns>Map object with details from the clipboard</returns>
        internal Map ParseClipboard()
        {
            var clipboardValue = Clipboard.GetText(TextDataFormat.Text);
            var clipboardContents = clipboardValue.Replace("\r", "").Split(new[] { '\n' });
            Map newMap;
            if (!clipboardValue.Contains("Map")) return null;
            if (clipboardValue.Contains("Sacrifice at")) return null;
            if (clipboardContents[0].Replace("Rarity: ", "") == "Normal" || clipboardContents[0].Replace("Rarity: ", "") == "Magic")
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
                    newMap.Quantity =
                        int.Parse(clipboardContents[4].Replace("Item Quantity: +", "").Replace("% (augmented)", ""));
                }
                if (clipboardValue.Contains("Quality:"))
                {
                    newMap.Quality =
                        int.Parse(clipboardContents[5].Replace("Quality: +", "").Replace("% (augmented)", ""));
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
                    newMap.Quantity =
                        int.Parse(clipboardContents[5].Replace("Item Quantity: +", "").Replace("% (augmented)", ""));
                }
                if (clipboardValue.Contains("Quality:"))
                {
                    newMap.Quality =
                        int.Parse(clipboardContents[6].Replace("Quality: +", "").Replace("% (augmented)", ""));
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
            var clipboardContents = Clipboard.GetText(TextDataFormat.Text).Replace("\r", "").Split(new[] { '\n' });
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
            var clipboardContents = Clipboard.GetText(TextDataFormat.Text).Replace("\r", "").Split(new[] { '\n' });
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
            var maps = new[]
            {
               "Academy", "Crypt","Dried Lake","Dunes","Dungeon","Grotto","Overgrown Ruin", "Tropical Island",
               "Arcade","Arsenal","Cemetery","Mountain Ledge","Sewer","Thicket", "Wharf","Ghetto",
               "Mud Geyser","Reef","Spider Lair","Springs","Vaal Pyramid", "Catacomb", "Overgrown Shrine",
               "Promenade","Shore","Spider Forest","Tunnel","Bog", "Coves", "Graveyard", "Pier",
               "Underground Sea","Arachnid Nest","Colonnade", "Dry Woods", "Strand", "Temple",
               "Jungle Valley", "Torture Chamber", "Waste Pool", "Mine", "Dry Peninsula", "Canyon",
               "Cells", "Dark Forest", "Gorge", "Maze", "Underground River", "Bazaar", "Necropolis",
               "Plateau", "Crematorium", "Precinct", "Shipyard", "Shrine", "Villa", "Palace"
            };

            foreach (var x in maps.Where(inputLine.Contains))
            {
                return x;
            }
            return inputLine;
        }

        #endregion



        private void _mapTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _timerTicks++;
            LabelDuration.Content = string.Format("{0:00}:{1:00}:{2:00}", _timerTicks/3600, (_timerTicks/60)%60, _timerTicks%60);
        }

        public static void SortDataGrid(DataGrid dataGrid, int columnIndex = 0, ListSortDirection sortDirection = ListSortDirection.Ascending)
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
    }
}
