using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

using Avalonia;
using Avalonia.Collections;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Styling;
using Avalonia.Markup.Xaml;
using Avalonia.CustomControls;

using SD = System.Drawing;
using TD = Torec.Drawing;
using AI = Avalonia.Media.Imaging;
using Rationals.Drawing;

using TextBox = Avalonia.CustomControls.TextBox2;

namespace Rationals.Explorer
{
    class BitmapAdapter { //!!! IDisposable?
        public AI.WriteableBitmap AvaloniaBitmap = null;
        public SD.Bitmap[] SystemBitmaps;

        public BitmapAdapter(int systemBitmapCount) {
            SystemBitmaps = new SD.Bitmap[systemBitmapCount];
        }

        public void DisposeAll() {
            if (AvaloniaBitmap != null) {
                AvaloniaBitmap.Dispose();
                AvaloniaBitmap = null;
            }
            for (int i = 0; i < SystemBitmaps.Length; ++i) { 
                if (SystemBitmaps[i] != null) {
                    SystemBitmaps[i].Dispose();
                    SystemBitmaps[i] = null;
                }
            }
        }

        public static bool EnsureBitmapSize(ref SD.Bitmap systemBitmap, SD.Size size) {
            bool invalid = systemBitmap != null && systemBitmap.Size != size;
            bool create = systemBitmap == null || invalid;
            if (invalid) {
                systemBitmap.Dispose();
                systemBitmap = null;
            }
            if (create) {
                systemBitmap = new SD.Bitmap(
                    size.Width, size.Height,
                    SD.Imaging.PixelFormat.Format32bppArgb
                );
            }
            return create;
        }

        public static bool EnsureBitmapSize(ref AI.WriteableBitmap avaloniaBitmap, PixelSize size) {
            bool invalid = avaloniaBitmap != null && avaloniaBitmap.PixelSize != size;
            bool create = avaloniaBitmap == null || invalid;
            if (invalid) {
                avaloniaBitmap.Dispose();
                avaloniaBitmap = null;
            }
            if (create) {
                avaloniaBitmap = new AI.WriteableBitmap(
                    size,
                    new Vector(1, 1), // DPI scale ?
                    //Avalonia.Platform.PixelFormat.Rgba8888
                    Avalonia.Platform.PixelFormat.Bgra8888 // seems faster for me! // like System.Drawing.Imaging.PixelFormat.Format32bppArgb
                );
            }
            return create;
        }

        /*
        public void Resize(PixelSize size) {
            if (size == Size) return;
            Size = size;

            Dispose();

            if (IsEmpty()) return;

            AvaloniaBitmap = new AI.WriteableBitmap(
                Size,
                new Vector(1, 1), // DPI scale ?
                //Avalonia.Platform.PixelFormat.Rgba8888
                Avalonia.Platform.PixelFormat.Bgra8888 // seems faster for me! // like System.Drawing.Imaging.PixelFormat.Format32bppArgb
            );

            for (int i = 0; i < SystemBitmaps.Length; ++i) {
                SystemBitmaps[i] = new SD.Bitmap(
                    Size.Width, Size.Height,
                    SD.Imaging.PixelFormat.Format32bppArgb
                );
            }
        }
        */

        public bool CopyPixels(int sourceIndex) // copy pixels from SystemBitmap to AvaloniaBitmap. UI thread
        {
            //Debug.WriteLine("CopyPixels begin");

            SD.Bitmap systemBitmap = SystemBitmaps[sourceIndex];
            if (systemBitmap == null) return false;
            if (AvaloniaBitmap == null) return false;

            using (var buf1 = AvaloniaBitmap.Lock())
            {
                long length1 = buf1.Size.Height * buf1.RowBytes;

                var buf0 = systemBitmap.LockBits(
                    new SD.Rectangle(SD.Point.Empty, systemBitmap.Size),
                    SD.Imaging.ImageLockMode.ReadOnly,
                    systemBitmap.PixelFormat
                );

                long length0 = buf0.Height * buf0.Stride;

                if (length1 == length0) {
                    // quick. just copy memory
                    unsafe {
                        System.Buffer.MemoryCopy(
                            buf0.Scan0.ToPointer(), 
                            buf1.Address.ToPointer(), 
                            length1, length0
                        );
                    }
                } else {
                    // slow. copy by line. may occure on resizing
                    int h = Math.Min(buf0.Height, buf1.Size.Height); // in pixels
                    int w = Math.Min(buf0.Stride, buf1.RowBytes); // in bytes
                    unsafe {
                        for (int i = 0; i < h; ++i) {
                            System.Buffer.MemoryCopy(
                                (buf0.Scan0 + buf0.Stride * i).ToPointer(),
                                (buf1.Address + buf1.RowBytes * i).ToPointer(),
                                w, w
                            );
                        }
                    }
                }

                systemBitmap.UnlockBits(buf0);
            }

            //Debug.WriteLine("CopyPixels end");

            return true;
        }
    }

    public partial class MainWindow : Window
    {
        Avalonia.Controls.Image _mainImageControl = null;

        Avalonia.Point _pointerPos;
        Avalonia.Point _pointerPosDrag; // dragging start position

        // Preset settings
        TD.Viewport3 _viewport;
        DrawerSettings _drawerSettings;

        GridDrawer _gridDrawer;

        Avalonia.Controls.ItemsControl   _menuPresetRecent;
        Avalonia.Collections.AvaloniaList<Avalonia.Controls.MenuItem> _menuPresetRecentItems;
        Avalonia.Controls.Control        _menuPresetSave;
        const int _recentPresetMaxCount = 5;

        Avalonia.Controls.Grid _mainGrid;
        Avalonia.Controls.TextBox _textBoxSelectionInfo;

        // We have 3 system bitmaps: 1. for copying bits, 2. for rendering to, 3. to always keep previously rendered
        BitmapAdapter _mainBitmap = new BitmapAdapter(3);
        System.Threading.Thread _threadRender;
        System.Threading.AutoResetEvent _eventRenderDoWork; // UI -> render thread
        object _renderLock = new object(); // locking between UI and render thread
        // following variables used locked
        bool _closingWindow = false;
        TD.Image _mainImage = null; // vector image. drawn in UI thread, rasterized in render thtread
        int _lastRenderedBitmap = -1; // Render thread has rendered to this system bitmap. Allowed for UI thread.
        int _copyingBitmap = -1; // UI thread copies bits from this system bitmap to Avalonia bitmap.

        // Midi
#if USE_MIDI
        private Midi.NAudioMidiOut _midiOut = null;
        private Midi.MidiPlayer _midiPlayer = null;
#endif

#if USE_PERF
        private Rationals.Utils.PerfCounter _perfUpdateItems = new Rationals.Utils.PerfCounter("Update item properties");
        private Rationals.Utils.PerfCounter _perfDrawItems   = new Rationals.Utils.PerfCounter("Items to image elements");
        private Rationals.Utils.PerfCounter _perfRenderImage = new Rationals.Utils.PerfCounter("Render raster image");
        private Rationals.Utils.PerfCounter _perfCopyPixels  = new Rationals.Utils.PerfCounter("Copy image to Avalonia");
#endif

        public MainWindow()
        {
            _viewport = new TD.Viewport3();

            _drawerSettings = DrawerSettings.Reset();

            _gridDrawer = new GridDrawer();

            // Initialize from Xaml
            AvaloniaXamlLoader.Load(this);

            _mainGrid = ExpectControl<Grid>(this, "mainGrid");

            _mainImageControl  = ExpectControl<Image>(this, "mainImage");

            var mainImagePanel = ExpectControl<Control>(this, "mainImagePanel");
            mainImagePanel.GetObservable(Control.BoundsProperty).Subscribe(OnMainImageBoundsChanged);

            _menuPresetSave    = ExpectControl<Control>(this, "menuPresetSave");
            _menuPresetRecent  = ExpectControl<ItemsControl>(this, "menuPresetRecent");
            _menuPresetRecentItems = new AvaloniaList<MenuItem>();

            _textBoxSelectionInfo = ExpectControl<Avalonia.Controls.TextBox>(this, "textBoxSelectionInfo");

            // prepare rendering
            _eventRenderDoWork = new System.Threading.AutoResetEvent(false);
            //
            _threadRender = new System.Threading.Thread(RenderThread);
            _threadRender.Name = "Render";
            _threadRender.Start();
            //var r = this.Renderer.SceneInvalidated


            //
            FindDrawerSettingsControls(this);

            LoadAppSettings();
        }

        static T ExpectControl<T>(IControl parent, string name) where T : class, IControl {
            var result = parent.FindControl<T>(name);
            Debug.Assert(result != null, name + " not found");
            return result;
        }

        /*
        private void LogInfo(string template, params object[] args) {
            Avalonia.Logging.Logger.Information("Mine", this, template, args);
        }
        */

        void mainWindow_Initialized(object sender, EventArgs e) {
            Console.WriteLine(">>>> OnWindowInitialized <<<<");

#if USE_MIDI
            if (NAudio.Midi.MidiOut.NumberOfDevices > 0) {
                _midiOut    = new Midi.NAudioMidiOut(0);
                _midiPlayer = new Midi.MidiPlayer(_midiOut);
                _midiPlayer.StartClock(60 * 4);
                //_midiPlayer.SetInstrument(0, Midi.Enums.Instrument.Clarinet);
            }
#endif
        }

        private int _handledOnWindowClosed = 0; //!!! temporal: OnWindowClosed fired twice
        void mainWindow_Closed(object sender, EventArgs e) {
            if (_handledOnWindowClosed++ > 0) return;

            Console.WriteLine(">>>> OnWindowClosed <<<<");

            // stop render thread
            lock (_renderLock) {
                _closingWindow = true;
            }
            _eventRenderDoWork.Set(); // unblock RenderThread()
            _threadRender.Join();
            _threadRender = null;


            SaveAppSettings();

            _mainBitmap.DisposeAll();

#if USE_MIDI
            _midiPlayer.StopClock();
            _midiPlayer = null;
            _midiOut.Dispose();
            _midiOut = null;
#endif

#if USE_PERF
            Console.WriteLine("Performance counters");
            Console.WriteLine(_perfUpdateItems.GetReport());
            Console.WriteLine(_perfDrawItems  .GetReport());
            Console.WriteLine(_perfRenderImage.GetReport());
            Console.WriteLine(_perfCopyPixels .GetReport());
#endif
        }


        #region Menu
        // Preset
        private async void menuPresetReset_Click(object sender, RoutedEventArgs e) {
            if (await SaveChangedPreset()) { // operation may be cancelled
                ResetPreset();

                // Propagate new settings to form controls & drawer
                OnPresetLoaded();
            }
        }
        private async void menuPresetOpen_Click(object sender, RoutedEventArgs e) {
            if (await SaveChangedPreset()) { // operation may be cancelled
                await OpenPreset();

                // Propagate new settings to form controls & drawer
                OnPresetLoaded();
            }
        }
        private async void menuPresetSave_Click(object sender, RoutedEventArgs e) {
            await SavePreset(withNewName: false);
        }
        private async void menuPresetSaveAs_Click(object sender, RoutedEventArgs e) {
            await SavePreset(withNewName: true);
        }
        private void menuRecentPreset_Click(object sender, RoutedEventArgs e) {
            if (sender is MenuItem menuItem) {
                string presetPath = menuItem.Name;
                LoadPreset(presetPath);

                // Propagate new settings to form controls & drawer
                OnPresetLoaded();
            }
        }

        // Image
        private void menuImageOpenSvg_Click(object sender, RoutedEventArgs e) {
            // Export and open svg in default editor
            var image = new Torec.Drawing.Image(_viewport);
            _gridDrawer.DrawGrid(image);
            image.Show(true);
        }
        private async void menuImageSaveAs_Click(object sender, RoutedEventArgs e) {
            // Save image as png/svg
            var dialog = new SaveFileDialog { Title = "Save Image As" };
            dialog.Filters.AddRange(new [] {
                new FileDialogFilter() { Name = "Svg files", Extensions = { "svg" } },
                new FileDialogFilter() { Name = "Png files", Extensions = { "png" } },
                new FileDialogFilter() { Name = "All files", Extensions = { "*" } }
            });
            string filePath = await dialog.ShowAsync(this);
            if (filePath == null) return; //!!! cancel?
            var image = new Torec.Drawing.Image(_viewport);
            _gridDrawer.DrawGrid(image);
            if (System.IO.Path.GetExtension(filePath).ToLower() == ".svg") {
                image.WriteSvg(filePath);
            } else {
                image.WritePng(filePath, true);
            }
        }
        #endregion

        #region Menu Preset Recent
        protected void SetRecentPresets(string[] recentPresetPaths) {
            _menuPresetRecentItems.Clear();
            foreach (string presetPath in recentPresetPaths) {
                if (!String.IsNullOrEmpty(presetPath)) {
                    var item = CreateMenuRecentPresetItem(presetPath);
                    _menuPresetRecentItems.Add(item);
                }
            }
            UpdateMenuRecentPreset();
        }
        protected void PopRecentPreset(string presetPath, bool updateItems = true) {
            RemoveRecentPreset(presetPath, false);
            var item = CreateMenuRecentPresetItem(presetPath);
            _menuPresetRecentItems.Insert(0, item);
            if (updateItems) UpdateMenuRecentPreset();
        }
        protected void RemoveRecentPreset(string presetPath, bool updateItems = true) {
            var remove = new List<MenuItem>();
            foreach (var item in _menuPresetRecentItems) {
                if (item.Name == presetPath) {
                    item.Click -= menuRecentPreset_Click;
                    remove.Add(item);
                }
            }
            if (remove.Count > 0) _menuPresetRecentItems.RemoveAll(remove);
            //
            if (updateItems) UpdateMenuRecentPreset();
        }
        private MenuItem CreateMenuRecentPresetItem(string presetPath) {
            var item = new MenuItem();
            item.Header = presetPath;
            item.Name = presetPath;
            item.Click += menuRecentPreset_Click;
            return item;
        }
        private void UpdateMenuRecentPreset() {
            _menuPresetRecent.Items = _menuPresetRecentItems;
            _menuPresetRecent.IsVisible = _menuPresetRecentItems.Count > 0;
        }
        #endregion


        #region Application settings
        private static readonly XmlWriterSettings _xmlWriterSettings = new XmlWriterSettings {
            Indent = true,
            OmitXmlDeclaration = true,
        };
        private static readonly string _appSettingsPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RationalsExplorer_Settings.xml"
        );
        //
        private string _currentPresetPath = null;
        private bool _currentPresetChanged = false;
        //
        private void MarkPresetChanged(bool changed = true) {
            _currentPresetChanged = changed;
            //
            bool enableSave = changed && _currentPresetPath != null;
            if (_menuPresetSave.IsEnabled != enableSave) {
                _menuPresetSave.IsEnabled = enableSave;
            }
        }

        // Window Location & Layout
        // Format: "<window state> <X> <Y> <W> <H> <panel width>"
        //   window state: Normal = 0, Minimized = 1, Maximized = 2
        private string FormatWindowLayout() {
            var state = this.WindowState;
            //!!! RestoreBounds not yet implemented in Avalonia
            /*
            bool normal = window.WindowState == WindowState.Normal;
            Point p = normal ? form.Location : form.RestoreBounds.Location;
            Size  s = normal ? form.Size     : form.RestoreBounds.Size;
            */
            PixelPoint p = this.Position;
            Size s = this.ClientSize;
            double panelWidth = _mainGrid.ColumnDefinitions[0].Width.Value;
            return String.Format("{0} {1} {2} {3} {4} {5}",
                (int)state, p.X, p.Y, (int)s.Width, (int)s.Height, (int)panelWidth
            );
        }
        private void SetWindowLayout(string locationValue) {
            if (locationValue == null) return;
            int[] ns = DrawerSettings.ParseIntegers(locationValue);
            if (ns == null || ns.Length != 6) return;
            // propagate
            this.WindowState = (WindowState)ns[0];
            if (this.WindowState == WindowState.Normal) {
                this.WindowStartupLocation = WindowStartupLocation.Manual;
                this.Position = new PixelPoint(ns[1], ns[2]);
                this.ClientSize = new Size(ns[3], ns[4]);
            }
            _mainGrid.ColumnDefinitions[0].Width = new GridLength(ns[5]);
        }

        public void SaveAppSettings() {
            using (XmlWriter w = XmlWriter.Create(_appSettingsPath, _xmlWriterSettings)) {
                w.WriteStartDocument();
                w.WriteStartElement("appSettings");
                // Windows
                w.WriteElementString("windowLayout", FormatWindowLayout());
                // Presets
                if (_menuPresetRecentItems.Count > 0) {
                    //w.WriteStartElement("recentPresets");
                    int counter = 0;
                    foreach (var item in _menuPresetRecentItems) {
                        if (++counter <= _recentPresetMaxCount) {
                            w.WriteElementString("recentPreset", item.Name);
                        }
                    }
                    //w.WriteEndElement();
                }
                if (_currentPresetPath != null) {
                    w.WriteElementString("currentPresetPath", _currentPresetPath);
                }
                w.WriteElementString("currentPresetChanged", (_currentPresetChanged ? 1 : 0).ToString());
                w.WriteStartElement("currentPreset");
                SavePreset(w);
                w.WriteEndElement();
                //
                w.WriteEndElement();
                w.WriteEndDocument();
            }
        }
        protected void LoadAppSettings()
        {
            // Reset preset to default
            ResetPreset();

            // Try to load preset from app settings
            var recentPresets = new List<string>();
            try {
                using (XmlTextReader r = new XmlTextReader(_appSettingsPath)) {
                    while (r.Read()) {
                        if (r.NodeType == XmlNodeType.Element) {
                            switch (r.Name) {
                                case "windowLayout":
                                    SetWindowLayout(r.ReadElementContentAsString());
                                    break;
                                case "recentPreset":
                                    recentPresets.Add(r.ReadElementContentAsString());
                                    break;
                                case "currentPresetPath":
                                    _currentPresetPath = r.ReadElementContentAsString();
                                    break;
                                case "currentPresetChanged":
                                    MarkPresetChanged(r.ReadElementContentAsInt() != 0); // _currentPresetPath already set - so call MarkPresetChanged now
                                    break;
                                case "currentPreset":
                                    LoadPreset(r.ReadSubtree());
                                    break;
                            }
                        }
                    }
                }
            } catch (System.IO.FileNotFoundException) {
                return;
            } catch (XmlException) {
                //!!! log error
                return;
            //} catch (Exception ex) {
            //    Console.Error.WriteLine("LoadAppSettings error: " + ex.Message);
            //    return false;
            }

            // Fill recent presets menu
            SetRecentPresets(recentPresets.ToArray());

            // Propagate new settings to form controls & drawer
            OnPresetLoaded();
        }

        private void OnPresetLoaded() {
            // Preset settings (_drawerSettings and _viewport) were loaded (preset was reset or loaded).
            // Now propagate new settings to form controls & drawer.
            SetSettingsToControls(_drawerSettings);
            UpdateDrawerFully(_drawerSettings, _viewport);
            InvalidateMainImage();
        }

        private void SavePreset(XmlWriter w) {
            // drawer
            w.WriteStartElement("drawer");
            DrawerSettings.Save(_drawerSettings, w);
            w.WriteEndElement();
            // viewport
            w.WriteStartElement("viewport");
            SavePresetViewport(w);
            w.WriteEndElement();
        }
        private void LoadPreset(XmlReader r) {
            // read preset from App Settings or saved preset xml
            while (r.Read()) {
                if (r.NodeType == XmlNodeType.Element) {
                    switch (r.Name) {
                        case "drawer":
                            _drawerSettings = DrawerSettings.Load(r.ReadSubtree());
                            break;
                        case "viewport":
                            LoadPresetViewport(r.ReadSubtree());
                            break;
                    }
                }
            }
        }

        protected void ResetPreset() {
            _drawerSettings = DrawerSettings.Reset();
            ResetPresetViewport();
            //
            _currentPresetPath = null;
            _currentPresetChanged = false;
        }

        private static readonly FileDialogFilter[] _fileDialogFilters = new[] {
            new FileDialogFilter() { Name = "Xml files", Extensions = {"xml"} }
        };
        private async Task OpenPreset() {
            var dialog = new OpenFileDialog { Title = "Open Preset" };
            dialog.Filters.AddRange(_fileDialogFilters);
            string[] result = await dialog.ShowAsync(this); // await Open Dialog
            if (result != null && result.Length > 0) {
                string presetPath = result[0];
                LoadPreset(presetPath);
            }
        }
        private async Task SavePreset(bool withNewName) {
            string presetPath = null;
            if (_currentPresetPath != null && !withNewName) {
                presetPath = _currentPresetPath;
            } else {
                var dialog = new SaveFileDialog { Title = "Save Preset As" };
                dialog.Filters.AddRange(_fileDialogFilters);
                if (_currentPresetPath != null) {
                    dialog.Directory = System.IO.Path.GetDirectoryName(_currentPresetPath);
                    dialog.InitialFileName = System.IO.Path.GetFileName(_currentPresetPath);
                }
                presetPath = await dialog.ShowAsync(this);
            }
            if (presetPath != null) {
                if (SavePreset(presetPath)) {
                    _currentPresetPath = presetPath;
                    PopRecentPreset(_currentPresetPath);
                    MarkPresetChanged(false);
                }
            }
        }

        private async Task<bool> SaveChangedPreset() {
            if (!_currentPresetChanged) return true; // continue
            string message = (_currentPresetPath ?? "Unnamed") + " preset has unsaved changes.\r\nSave preset?";
            var result = await MessageBox.Show(this, message, "Rationals Explorer", MessageBox.MessageBoxButtons.YesNoCancel);
            if (result == MessageBox.MessageBoxResult.Cancel) return false; // cancel current operation
            if (result == MessageBox.MessageBoxResult.Yes) {
                await SavePreset(withNewName: false);
            }
            return true; // continue
        }

        private bool SavePreset(string presetPath) {
            using (XmlWriter w = XmlWriter.Create(presetPath, _xmlWriterSettings)) {
                w.WriteStartDocument();
                w.WriteStartElement("preset");
                SavePreset(w);
                w.WriteEndElement();
                w.WriteEndDocument();
            }
            return true;
        }
        private void LoadPreset(string presetPath) {
            bool presetLoaded = false;
            try {
                using (XmlTextReader r = new XmlTextReader(presetPath)) {
                    while (r.Read()) {
                        if (r.NodeType == XmlNodeType.Element && r.Name == "preset") {
                            LoadPreset(r);
                            presetLoaded = true;
                        }
                    }
                }
            } catch (Exception ex) {
                string message = "Can't open preset '" + presetPath + "':\r\n" + ex.Message;
                MessageBox.Show(this, message, "Rationals Explorer", MessageBox.MessageBoxButtons.Ok);
                presetLoaded = false;
            }

            // update presets menu at once
            if (presetLoaded) {
                _currentPresetPath = presetPath;
                PopRecentPreset(_currentPresetPath);
                MarkPresetChanged(false);
            } else {
                // invalid preset path - remove from "recent" list
                RemoveRecentPreset(presetPath);
            }
        }
        private void SavePresetViewport(XmlWriter w) {
            var scale  = _viewport.GetScaleSaved();
            var center = _viewport.GetUserCenter();
            w.WriteElementString("scaleX",  scale .X.ToString());
            w.WriteElementString("scaleY",  scale .Y.ToString());
            w.WriteElementString("centerX", center.X.ToString());
            w.WriteElementString("centerY", center.Y.ToString());
        }
        private void LoadPresetViewport(XmlReader r) {
            var scale  = new TD.Point(1f, 1f);
            var center = new TD.Point(0f, 0f);
            while (r.Read()) {
                if (r.NodeType == XmlNodeType.Element) {
                    switch (r.Name) {
                        case "scaleX":  scale .X = r.ReadElementContentAsFloat(); break;
                        case "scaleY":  scale .Y = r.ReadElementContentAsFloat(); break;
                        case "centerX": center.X = r.ReadElementContentAsFloat(); break;
                        case "centerY": center.Y = r.ReadElementContentAsFloat(); break;
                    }
                }
            }
            // keep initial viewport size, change scale and center only
            _viewport.SetScaleSaved(scale.X, scale.Y);
            _viewport.SetUserCenter(center.X, center.Y);
        }
        private void ResetPresetViewport() {
            _viewport.SetScaleSaved(1f, 1f);
            _viewport.SetUserCenter(0f, 0f);
        }

        #endregion Application settings


        #region Pointer handling

        private static TD.Point ToPoint(Point p) {
            return new TD.Point((float)p.X, (float)p.Y);
        }

        // !!! warning CS0618: 'PointerEventArgs.InputModifiers' is obsolete: 'Use KeyModifiers and PointerPointProperties'
        //  but e.PointerPointProperties is protected, e.GetCurrentPoint is messy
#pragma warning disable CS0618
        private bool IgnorePointerMove(PointerEventArgs e) {
            //
            InputModifiers m = e.InputModifiers;
            return m.HasFlag(InputModifiers.Shift) &&
                !m.HasFlag(InputModifiers.LeftMouseButton | InputModifiers.MiddleMouseButton | InputModifiers.RightMouseButton);
        }
#pragma warning restore CS0618

        private void MainImage_PointerMoved(object sender, PointerEventArgs e) {
            // allow to move cursor out leaving selection/hignlighting unchanged
            if (IgnorePointerMove(e)) return;

            PointerPoint p = e.GetCurrentPoint(_mainImageControl);
            _pointerPos = p.Position;
            //
            if (p.Properties.IsMiddleButtonPressed) {
                TD.Point delta = ToPoint(_pointerPosDrag - _pointerPos);
                _pointerPosDrag = _pointerPos;
                _viewport.MoveOrigin(delta);
                UpdateDrawerBounds();
                MarkPresetChanged();
                InvalidateMainImage();
            } else {
                TD.Point u = _viewport.ToUser(ToPoint(_pointerPos));
                _gridDrawer.SetCursor(u.X, u.Y);
                var mode = e.KeyModifiers.HasFlag(KeyModifiers.Alt)
                    ? GridDrawer.CursorHighlightMode.Cents
                    : GridDrawer.CursorHighlightMode.NearestRational;
                _gridDrawer.SetCursorHighlightMode(mode);
                InvalidateMainImage();
            }
        }

        private void MainImage_PointerLeave(object sender, PointerEventArgs e) {
            // allow to move cursor out leaving selection/hignlighting unchanged
            if (IgnorePointerMove(e)) return;

            // disable highlighting
            //_pointerPos = new Point();
            //_gridDrawer.SetCursor(0, 0);
            _gridDrawer.SetCursorHighlightMode(GridDrawer.CursorHighlightMode.None);
            InvalidateMainImage();
        }

        private void MainImage_PointerPressed(object sender, PointerPressedEventArgs e) {
            PointerPoint p = e.GetCurrentPoint(_mainImageControl);

            if (_pointerPos != p.Position) return;
            // _gridDrawer.SetCursor already called from OnMouseMove

            if (p.Properties.IsLeftButtonPressed)
            {
                // Get tempered note
                Drawing.SomeInterval t = null;
                if (e.KeyModifiers.HasFlag(KeyModifiers.Alt)) { // by cents
                    float c = _gridDrawer.GetCursorCents();
                    t = new Drawing.SomeInterval { cents = c };
                } else { // nearest rational
                    _gridDrawer.UpdateCursorItem();
                    Rational r = _gridDrawer.GetCursorRational();
                    if (!r.IsDefault()) {
                        t = new Drawing.SomeInterval { rational = r };
                    }
                }
                if (t != null) {
                    // Toggle selection
                    if (e.KeyModifiers.HasFlag(KeyModifiers.Control)) {
                        ToggleSelection(t);
                        //!!! invalidate image
                    }
                    // Play note
                    else {
#if USE_MIDI
                        _midiPlayer.NoteOn(0, t.ToCents(), duration: 8f);
#endif
                    }
                }
            }
            else if (p.Properties.IsMiddleButtonPressed) {
                _pointerPosDrag = _pointerPos;
            }
        }

        private void MainImage_PointerWheelChanged(object sender, PointerWheelEventArgs e) {
            bool shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
            bool ctrl  = e.KeyModifiers.HasFlag(KeyModifiers.Control);
            bool alt   = e.KeyModifiers.HasFlag(KeyModifiers.Alt);

            float delta = (float)e.Delta.Y;

            if (shift || ctrl) {
                _viewport.AddScale(delta * 0.1f, straight: ctrl, ToPoint(_pointerPos));
                UpdateDrawerBounds();
            } else if (alt) {
                _drawerSettings.pointRadiusLinear += delta * 0.1f;
                UpdateDrawerPointRadius();
            } else {
                _viewport.MoveOrigin(new TD.Point(0, -delta * 10f));
                UpdateDrawerBounds();
            }

            MarkPresetChanged();
            InvalidateMainImage();
        }

        #endregion

        private void OnMainImageBoundsChanged(Rect bounds) {
            //Console.WriteLine("mainImagePanel bounds -> {0}", bounds);
            if (bounds.IsEmpty) return;

            //Debug.WriteLine("OnMainImageBoundsChanged begin");

            // Update drawer & invalidate
            _viewport.SetImageSize((float)bounds.Width, (float)bounds.Height);
            UpdateDrawerBounds();
            InvalidateMainImage();

            // We have invalidated our image
            //  but we should resize avalonia bitmap (for image control) immediately
            //  because avalonia may redraw scene at any time
            var pixelSize = new PixelSize((int)bounds.Width, (int)bounds.Height);
            BitmapAdapter.EnsureBitmapSize(ref _mainBitmap.AvaloniaBitmap, pixelSize);
            _mainImageControl.Source = _mainBitmap.AvaloniaBitmap; // set new bitmap to image control
            // We also fill new bitmap from last rendered bitmap to avoid "white resize"
            UpdateMainBitmap();

            //Debug.WriteLine("OnMainImageBoundsChanged end");
        }

        private void UpdateDrawerBounds() {
            _gridDrawer.SetBounds(_viewport.GetUserBounds());
        }
        private void UpdateDrawerPointRadius() {
            _gridDrawer.SetPointRadius(_drawerSettings.pointRadiusLinear);
        }

        private void UpdateDrawerFully(DrawerSettings s, TD.Viewport3 v) {
            // viewport
            _gridDrawer.SetBounds(v.GetUserBounds());
            // base
            _gridDrawer.SetBase(s.limitPrimeIndex, s.subgroup, s.narrows);
            _gridDrawer.SetGeneration(s.harmonicityName, s.rationalCountLimit);
            // temperament
            _gridDrawer.SetTemperament(s.temperament);
            _gridDrawer.SetTemperamentMeasure(s.temperamentMeasure);
            // degrees
            //_gridDrawer.SetDegrees(s.stepMinHarmonicity, s.stepSizeMaxCount);
            // slope
            _gridDrawer.SetSlope(s.slopeOrigin, s.slopeChainTurns);
            // view
            _gridDrawer.SetEDGrids(s.edGrids);
            _gridDrawer.SetSelection(s.selection);
            _gridDrawer.SetPointRadius(s.pointRadiusLinear);
        }

        protected void InvalidateMainImage() {
            // Avalonia InvalidateVisual raises no "OnPaint" events (HandlePaint raised on resize only).
            // So we use a render thread:
            RedrawMainImage();
        }

        private void RedrawMainImage() // UI thread
        {
            // Draw items to vector Image elements in UI thread
            TD.Image image = DrawGrid();
            if (image == null) return;

            // Signal render thread to do work
            lock (_renderLock) {
                _mainImage = image;
            }

            //Debug.WriteLine("Render DoWork sent");
            _eventRenderDoWork.Set(); // unblock RenderThread()

            // On render complete we continue in UI thread: UpdateMainBitmap()
        }

        private TD.Image DrawGrid()
        {
            // Skip drawing if viewport not sized yet
            if (_viewport.GetImageSize().IsEmpty()) {
                return null;
            }

            // Create image
            var image = new TD.Image(_viewport);

            // Update drawer items: pos, visibility,.. (according to collected update flags)
#if USE_PERF
            _perfUpdateItems.Start();
#endif
            _gridDrawer.UpdateItems();
            _gridDrawer.UpdateCursorItem();
#if USE_PERF
            _perfUpdateItems.Stop();
#endif

            // Draw items as image elements
#if USE_PERF
            _perfDrawItems.Start();
#endif
            _gridDrawer.DrawGrid(image);
#if USE_PERF
            _perfDrawItems.Stop();
#endif

            return image;
        }

        private void RenderThread() {
            while (true) {
                _eventRenderDoWork.WaitOne();

                //Debug.WriteLine("Render DoWork got");

                // get image and bitmap to render
                TD.Image image; // source vector image
                int bitmapIndex = -1; // destination raster bitmap index
                lock (_renderLock) {
                    if (_closingWindow) return;
                    image = _mainImage;
                    // choose system bitmap to render
                    for (int i = 0; i < 3; ++i) {
                        if (i != _copyingBitmap && i != _lastRenderedBitmap) {
                            bitmapIndex = i;
                            break;
                        }
                    }
                }

                //Debug.WriteLine("Rendering to {0} begin", bitmapIndex);

                // prepare valid size bitmap
                TD.Point imageSize = image.GetSize();
                SD.Size bitmapSize = new SD.Size((int)imageSize.X, (int)imageSize.Y);
                BitmapAdapter.EnsureBitmapSize(
                    ref _mainBitmap.SystemBitmaps[bitmapIndex], 
                    bitmapSize
                );

                // render image to bitmap
#if USE_PERF
                _perfRenderImage.Start();
#endif
                using (var graphics = SD.Graphics.FromImage(
                    _mainBitmap.SystemBitmaps[bitmapIndex]
                )) {
                    graphics.SmoothingMode = SD.Drawing2D.SmoothingMode.AntiAlias;
                    graphics.Clear(SD.Color.White); // smooting makes ugly edges is no background filled
                    image.Draw(graphics);
                }
#if USE_PERF
                _perfRenderImage.Stop();
#endif

                //Debug.WriteLine("Rendering to {0} end", bitmapIndex);

                // Let UI thread to continue
                lock (_renderLock) {
                    _lastRenderedBitmap = bitmapIndex;
                }
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(UpdateMainBitmap);
            }
        }

        private bool UpdateMainBitmap() {
            // Now (in UI thread) we copy bitmap bits to Avalonia bitmap

            // get index of bitmap to copy from
            int bitmapIndex;
            lock (_renderLock) {
                if (_lastRenderedBitmap == -1) return false;
                bitmapIndex = _copyingBitmap = _lastRenderedBitmap;
            }

            //Debug.WriteLine("UpdateMainBitmap from {0} begin", bitmapIndex);

            // copy pixels to avalonia bitmap
#if USE_PERF
            _perfCopyPixels.Start();
#endif
            bool copied = _mainBitmap.CopyPixels(bitmapIndex);
#if USE_PERF
            _perfCopyPixels.Stop();
#endif

            //Debug.WriteLine("UpdateMainBitmap from {0} end", bitmapIndex);

            lock (_renderLock) {
                _copyingBitmap = -1;
            }

            // We have updated avalonia bitmap - now invalidate corresponding control
            if (copied) {
                _mainImageControl.InvalidateVisual();
            }

            return copied;
        }

    }
}
