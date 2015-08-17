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
        private static MainWindow _main;

        private readonly DispatcherTimer _mapTimer;
        internal Map CurrentMap;
        internal dynamic LeagueObject;
        private int _timerTicks;
        private string _state;
        private IntPtr _handle;

        public MainWindow()
        {
            InitializeComponent();

            //Sqlite = new Sqlite();
            _main = this;
            if (Sqlite.ExperienceCount() != 100) PopulateExperience();
            PopulateMapInformation();

            RefreshGrids();
            RefreshDrops(0);
            ScrollViewer.SetCanContentScroll(GridDrops, false);
            _mapTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1),
                IsEnabled = false
            };
            LoadSettings();
            _mapTimer.Tick += _mapTimer_Elapsed;
            LeagueObject = JsonHandler.ParseJsonObject("http://api.exiletools.com/ladder?listleagues=1");
            ExtendedStatusStrip.ButtonExpand.Click += ExpandStatus;
            _state = "WAITING";
            UpdateInformation();
            ExtendedStatusStrip.AddStatus("Welcome back, Exile!");
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

            UnregisterHotkeys();
            if (_source == null) return;
            _source.RemoveHook(WndProc);
            _source = null;
            base.OnClosed(e);
        }

        /// <summary>
        /// Registers the hotkeys globally. Unregisters any already registered hotkeys first.
        /// </summary>
        internal void RegisterHotkeys()
        {
            try
            {
                UnregisterHotkeys();
                RegisterHotKey(_handle, 0, 0, Convert.ToUInt32(Settings.Default.mapHotkey));
                RegisterHotKey(_handle, 1, 0, Convert.ToUInt32(Settings.Default.zanaHotkey));
                RegisterHotKey(_handle, 2, 0, Convert.ToUInt32(Settings.Default.cartoHotkey));
                RegisterHotKey(_handle, 3, 0, 121);
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

        private void LoadSettings()
        {
            MenuHideQuantity.IsChecked = Settings.Default.HideQuantity;
            GridQuantityColumn.Visibility = MenuHideQuantity.IsChecked ? Visibility.Hidden : Visibility.Visible;

            MenuHideQuality.IsChecked = Settings.Default.HideQuality;
            GridQuantityColumn.Visibility = MenuHideQuality.IsChecked ? Visibility.Hidden : Visibility.Visible;

            MenuHideRarity.IsChecked = Settings.Default.HideRarity;
            GridRarityColumn.Visibility = MenuHideRarity.IsChecked ? Visibility.Hidden : Visibility.Visible;

            MenuHidePacksize.IsChecked = Settings.Default.HidePacksize;
            GridPacksizeColumn.Visibility = MenuHidePacksize.IsChecked ? Visibility.Hidden : Visibility.Visible;

            MenuTopMost.IsChecked = Settings.Default.Topmost;
        }

        private void UpdateInformation()
        {
            LabelMapsRunValue.Content = Sqlite.CountMapsToday();

            CanvasInformation.Visibility = Visibility.Visible;
            CanvasCurrentMap.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Populates the experience breakpoints from the CSV file into the local database.
        /// </summary>
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
            Sqlite.AddExperience(listExp);
        }

        private void PopulateMapInformation()
        {
            var lines = File.ReadAllLines(Directory.GetCurrentDirectory() + @"\Resources\MapInformation.csv")
                    .Select(a => a.Split(','));
            if (Sqlite.InformationCount() == lines.Count()) return;
            var listMapInformation = lines.Select(line => new MapInformation
            {
                Name = line[0],
                Unique = line[1] == "Yes" ? 1 : 0,
                Zone = line[2],
                Boss = line[3],
                BossDetails = line[4],
                Description = line[5]
            }).ToList();
            Sqlite.AddInformation(listMapInformation);
        }

        private void RefreshGrids()
        {
            var mapTable = Sqlite.MapDataTable();
            GridMaps.DataContext = mapTable.DefaultView;
            SortDataGrid(GridMaps, 0, ListSortDirection.Descending);
        }

        private void RefreshDrops(int rowId)
        {
            var dropTable = Sqlite.DropDataTable(rowId);
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
                    if (ParseHandler.CheckClipboard())
                    {
                        var clipboard = System.Windows.Clipboard.GetText(System.Windows.TextDataFormat.Text);
                        switch (_state)
                        {
                            //case ("WAITING"):
                            //    CurrentMap = ParseHandler.ParseClipboard();
                            //    if (CurrentMap == null) break;
                            //    CurrentMap.ExpBefore = ExpValue();
                            //    var newMap = new NewMap();
                            //    newMap.ShowDialog();
                            //    if (newMap.Cancelled) return IntPtr.Zero;
                            //    //if (publicOpt)
                            //    //{
                            //    //    _mySqlId = _mySql.AddMap(CurrentMap);
                            //    //    labelMySqlId.Text = _mySqlId.ToString(CultureInfo.InvariantCulture);
                            //    //}
                            //    //CurrentMap.SqlId = _mySqlId;
                            //    if (CurrentMap.Id > 0)
                            //    {
                            //        CanvasInformation.Visibility = Visibility.Hidden;
                            //        CanvasCurrentMap.Visibility = Visibility.Visible;
                            //        LabelMapValue.Content = CurrentMap.Name;
                            //        _timerTicks = 0;
                            //        _mapTimer.Start();
                            //        CurrentMapBorder.BorderBrush = Brushes.Red;
                            //        ExtendedStatusStrip.AddStatus($"Beginning {CurrentMap.Name} map...");
                            //        RefreshGrids();
                            //        GridMaps.SelectedIndex = 0;
                            //        _state = "DROPS";
                            //    }
                            //    break;

                            case ("DROPS"):
                                if (CurrentMap.Id <= 0) break;
                                try
                                {
                                    if (clipboard.Contains("Map") && !clipboard.Contains("Stack Size:"))
                                    {
                                        var parsedMap = ParseHandler.ParseClipboard();
                                        if (parsedMap == null) break;
                                        Sqlite.AddDrop(parsedMap, CurrentMap.Id);
                                        //if (publicOpt && _mySqlId > 0) _mySql.AddDrop(parsedMap, _mySqlId);
                                    }
                                    if (clipboard.Contains("Currency"))
                                    {
                                        var parsedCurrency = ParseHandler.ParseCurrency();
                                        Sqlite.AddCurrency(CurrentMap.Id, parsedCurrency);
                                        //if (publicOpt && _mySqlId > 0) _mySql.AddCurrency(_mySqlId, parsedCurrency);
                                    }
                                    if (!clipboard.Contains("Map") && clipboard.Contains("Unique"))
                                    {
                                        var parsedUnique = ParseHandler.ParseUnique();
                                        Sqlite.AddUnique(CurrentMap.Id, parsedUnique);
                                        //if (publicOpt && _mySqlId > 0) Sqlite.AddUnique(_mySqlId, parsedUnique);
                                    }
                                    if (clipboard.Contains("Rarity: Normal") && clipboard.Contains("Stack Size:")
                                        && (!clipboard.Split('\n')[1].Contains("Map") || !clipboard.Split('\n')[2].Contains("Map")))
                                    {
                                        var parsedDivcard = ParseHandler.ParseDivination();
                                        Sqlite.AddDivination(CurrentMap.Id, parsedDivcard);
                                    }
                                    RefreshGrids();
                                    GridMaps.SelectedIndex = 0;
                                }
                                catch (Exception)
                                {
                                    //do nothing
                                }
                                System.Windows.Clipboard.SetText("");
                                break;

                            case ("ZANA"):
                                if (CurrentMap.Id <= 0) break;
                                Sqlite.AddDrop(ParseHandler.ParseClipboard(), CurrentMap.Id, 1);
                                //if (publicOpt && _mySqlId > 0) _mySql.AddDrop(ParseClipboard(), SqliteiteId, 1);
                                break;

                            case ("CARTO"):
                                if (CurrentMap.Id <= 0) break;
                                Sqlite.AddDrop(ParseHandler.ParseClipboard(), CurrentMap.Id, 0, 1);
                                //if (publicOpt && _mySqlId > 0) _mySql.AddDrop(ParseClipboard(), SqliteiteId, 0, 1);
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
                                Experience expAfter = null;//ExpValue();
                                Sqlite.FinishMap(CurrentMap.Id, expAfter);
                                ExtendedStatusStrip.AddStatus($"Finished {CurrentMap.Name}, Gained {"0.0%"} experience.");
                                //if (publicOpt && _mySqlId > 0) _mySql.FinishMap(_mySqlId, expAfter);
                                CurrentMap = null;
                                _state = "WAITING";
                                UpdateInformation();
                                //TODO fix for occurance of character leveling in the map
                                //var expDiff = expAfter.CurrentExperience - CurrentMap.ExpBefore.CurrentExperience;
                                //var expGoal = Sqlite.ExperienceGoal(CurrentMap.ExpBefore.Level);
                                //var percentDiff = (float)expDiff / expGoal;
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
                        case (3):
                                var newMap = new NewMap();
                                newMap.ShowDialog();
                                OnSourceInitialized(new RoutedEventArgs());
                                if (newMap.Cancelled || CurrentMap == null || CurrentMap.Id <= 0) break;
                                //if (publicOpt)
                                //{
                                //    _mySqlId = _mySql.AddMap(CurrentMap);
                                //    labelMySqlId.Text = _mySqlId.ToString(CultureInfo.InvariantCulture);
                                //}
                                //CurrentMap.SqlId = _mySqlId;
                                CanvasInformation.Visibility = Visibility.Hidden;
                                CanvasCurrentMap.Visibility = Visibility.Visible;
                                LabelMapValue.Content = CurrentMap.Name;
                                _timerTicks = 0;
                                _mapTimer.Start();
                                CurrentMapBorder.BorderBrush = Brushes.Red;
                                ExtendedStatusStrip.AddStatus($"Beginning {CurrentMap.Name} map...");
                                RefreshGrids();
                                GridMaps.SelectedIndex = 0;
                                _state = "DROPS";
                            break;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        #endregion

        #region Custom Methods


        /// <summary>
        /// Uses OCR to parse the experience from the tooltip. Moves the mouse to the desired position automatically
        /// </summary>
        /// <returns>The string containing the full experience tooltip</returns>
        private string CaptureExp()
        {
            var previousPoint = System.Windows.Forms.Cursor.Position;
            System.Windows.Forms.Cursor.Position = new System.Drawing.Point(
                (Screen.PrimaryScreen.Bounds.Width/2) - 200, Screen.PrimaryScreen.Bounds.Height);
            System.Windows.Forms.Cursor.Clip = new Rectangle(System.Windows.Forms.Cursor.Position,
                new System.Drawing.Size(1, 1));
            Thread.Sleep(750);
            var bmpScreenshot = new Bitmap((int) SystemParameters.PrimaryScreenWidth,
                (int) SystemParameters.PrimaryScreenHeight,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var gfxScreenshot = Graphics.FromImage(bmpScreenshot);
            gfxScreenshot.CopyFromScreen(System.Windows.Forms.Cursor.Position.X + 35,
                System.Windows.Forms.Cursor.Position.Y - 60, 0, 0, new System.Drawing.Size(580, 500),
                CopyPixelOperation.SourceCopy);
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

        private void MenuSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new ApplicationSettings();
            settingsWindow.Show();
        }

        private void GridMaps_DetailsClick(object sender, RoutedEventArgs e)
        {
            var rowStr = ((DataRowView) GridMaps.SelectedItem)?.Row[0];
            int rowId;
            if (!int.TryParse(rowStr?.ToString(), out rowId)) return;
            var selectedMap = Sqlite.GetMap(rowId);
            if (selectedMap == null) return;
            var mapDetails = new Details(selectedMap);
            mapDetails.ShowDialog();
        }

        private void GridMaps_DeleteClick(object sender, RoutedEventArgs e)
        {
            var rowStr = ((DataRowView)GridMaps.SelectedItem)?.Row.ItemArray[0];
            int id;
            if (int.TryParse(rowStr?.ToString(), out id)) Sqlite.DeleteMap(id);
            RefreshGrids();
        }

        private void NewMap_OnClick(object sender, RoutedEventArgs e)
        {
            var newMap = new NewMap();
            newMap.ShowDialog();
            OnSourceInitialized(e);
            if (newMap.Cancelled || CurrentMap == null || CurrentMap.Id <= 0) return;
            //if (publicOpt)
            //{
            //    _mySqlId = _mySql.AddMap(CurrentMap);
            //    labelMySqlId.Text = _mySqlId.ToString(CultureInfo.InvariantCulture);
            //}
            //CurrentMap.SqlId = _mySqlId;
            CanvasInformation.Visibility = Visibility.Hidden;
            CanvasCurrentMap.Visibility = Visibility.Visible;
            LabelMapValue.Content = CurrentMap.Name;
            _timerTicks = 0;
            _mapTimer.Start();
            CurrentMapBorder.BorderBrush = Brushes.Red;
            ExtendedStatusStrip.AddStatus($"Beginning {CurrentMap.Name} map...");
            RefreshGrids();
            GridMaps.SelectedIndex = 0;
            _state = "DROPS";
        }

        private void MenuUnidentified_Click(object sender, RoutedEventArgs e)
        {
            var unidentifiedCalc = new UnidentifiedMap();
            unidentifiedCalc.Show();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MenuItemHideColumns_Click(object sender, RoutedEventArgs e)
        {
            switch(((System.Windows.Controls.MenuItem)e.Source).Header.ToString())
            {
                case ("Quantity"):
                    Settings.Default.HideQuantity = MenuHideQuantity.IsChecked;
                    GridQuantityColumn.Visibility = MenuHideQuantity.IsChecked ? Visibility.Hidden : Visibility.Visible;
                    break;
                case ("Quality"):
                    Settings.Default.HideQuality = MenuHideQuality.IsChecked;
                    GridQualityColumn.Visibility = MenuHideQuality.IsChecked ? Visibility.Hidden : Visibility.Visible;
                    break;

                case ("Rarity"):
                    Settings.Default.HideRarity = MenuHideRarity.IsChecked;
                    GridRarityColumn.Visibility = MenuHideRarity.IsChecked ? Visibility.Hidden : Visibility.Visible;
                    break;

                case ("Pack Size"):
                    Settings.Default.HidePacksize = MenuHidePacksize.IsChecked;
                    GridPacksizeColumn.Visibility = MenuHidePacksize.IsChecked ? Visibility.Hidden : Visibility.Visible;
                    break;
            }
            Settings.Default.Save();
        }

        private void MenuTopMost_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Topmost = MenuTopMost.IsChecked;
            Topmost = MenuTopMost.IsChecked;
            Settings.Default.Save();
        }

    }
}
