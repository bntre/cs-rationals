using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;
using System.IO;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Collections;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Markup.Xaml;
using Avalonia.CustomControls;

using TD = Torec.Drawing;
using Rationals.Drawing;


namespace Rationals.Explorer
{
    class BitmapAdapter { //!!! IDisposable?
        public Avalonia.Media.Imaging.WriteableBitmap AvaloniaBitmap = null;
        public System.Drawing.Bitmap SystemBitmap = null;
        public PixelSize Size = PixelSize.Empty;

        public bool IsEmpty() {
            return Size.Width == 0 || Size.Height == 0;
        }

        public void Resize(PixelSize size) {
            if (size == Size) return;
            Size = size;

            if (AvaloniaBitmap != null) {
                AvaloniaBitmap.Dispose();
                AvaloniaBitmap = null;
            }
            if (SystemBitmap != null) {
                SystemBitmap.Dispose();
                SystemBitmap = null;
            }

            if (IsEmpty()) return;

            AvaloniaBitmap = new Avalonia.Media.Imaging.WriteableBitmap(
                Size,
                new Vector(1, 1), // DPI scale ?
                //Avalonia.Platform.PixelFormat.Rgba8888
                Avalonia.Platform.PixelFormat.Bgra8888 // seems faster for me! // like System.Drawing.Imaging.PixelFormat.Format32bppArgb
            );

            SystemBitmap = new System.Drawing.Bitmap(
                Size.Width,
                Size.Height,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb
            );
        }

        public void CopyPixels() // copy pixels from SystemBitmap to AvaloniaBitmap
        {
            if (AvaloniaBitmap == null || SystemBitmap == null) return;

            using (var buf1 = AvaloniaBitmap.Lock())
            {
                long length1 = buf1.Size.Height * buf1.RowBytes;

                var buf2 = SystemBitmap.LockBits(
                    new System.Drawing.Rectangle(0, 0, Size.Width, Size.Height), 
                    System.Drawing.Imaging.ImageLockMode.ReadOnly,
                    SystemBitmap.PixelFormat
                );

                long length2 = buf2.Height * buf2.Stride;

                if (length1 == length2) {
                    unsafe {
                        System.Buffer.MemoryCopy(
                            buf2.Scan0.ToPointer(), 
                            buf1.Address.ToPointer(), 
                            length1, length2
                        );
                    }
                }

                SystemBitmap.UnlockBits(buf2);
            }
        }
    }

    public class MainWindow : Window
    {
        Avalonia.Controls.Image _mainImageControl = null;
        BitmapAdapter _mainBitmap = new BitmapAdapter();
        Avalonia.Point _pointerPos;
        Avalonia.Point _pointerPosDrag; // dragging start position

        // Preset
        TD.Viewport3 _viewport;
        DrawerSettings _drawerSettings;

        GridDrawer _gridDrawer;

        Avalonia.Controls.ItemsControl   _menuPresetRecent;
        Avalonia.Collections.AvaloniaList<Avalonia.Controls.MenuItem> _menuPresetRecentItems;
        Avalonia.Controls.Control        _menuPresetSave;

        // Midi
#if USE_MIDI
        private NAudio.Midi.MidiOut _midiDevice = null;
        private Midi.MidiPlayer _midiPlayer = null;
#endif

        public MainWindow()
        {
            _viewport = new TD.Viewport3();

            _drawerSettings = DrawerSettings.Reset();

            _gridDrawer = new GridDrawer();

            // Initialize from Xaml
            AvaloniaXamlLoader.Load(this);

            _mainImageControl = this.FindControl<Avalonia.Controls.Image>("mainImage");
            System.Diagnostics.Debug.Assert(_mainImageControl != null, "mainImage not found");

            var mainImagePanel = this.FindControl<Avalonia.Controls.Control>("mainImagePanel");
            System.Diagnostics.Debug.Assert(mainImagePanel != null, "mainImagePanel not found");
            mainImagePanel.GetObservable(Control.BoundsProperty).Subscribe(OnMainImageBoundsChanged);

            _menuPresetRecent = this.FindControl<Avalonia.Controls.ItemsControl>("menuPresetRecent");
            System.Diagnostics.Debug.Assert(_mainImageControl != null, "mainImage not found");
            _menuPresetRecentItems = new AvaloniaList<Avalonia.Controls.MenuItem>();
            _menuPresetSave = this.FindControl<Avalonia.Controls.Control>("menuPresetSave");

            LoadAppSettings();
        }

        /*
        private void LogInfo(string template, params object[] args) {
            Avalonia.Logging.Logger.Information("Mine", this, template, args);
        }
        */

        void OnWindowInitialized(object sender, EventArgs e) {
            Console.WriteLine(">>>> OnWindowInitialized <<<<");

#if USE_MIDI
            if (NAudio.Midi.MidiOut.NumberOfDevices > 0) {
                _midiDevice = new NAudio.Midi.MidiOut(0);
                _midiPlayer = new Midi.MidiPlayer(_midiDevice);
                _midiPlayer.StartClock(60 * 4);
                //_midiPlayer.SetInstrument(0, Midi.Enums.Instrument.Clarinet);
            }
#endif

        }

        private int _handledOnWindowClosed = 0; //!!! temporal: OnWindowClosed fired twice
        void OnWindowClosed(object sender, EventArgs e) {
            if (_handledOnWindowClosed++ > 0) return;

            Console.WriteLine(">>>> OnWindowClosed <<<<");

            SaveAppSettings();

#if USE_MIDI
            _midiPlayer.StopClock();
            _midiPlayer = null;
            _midiDevice.Dispose();
            _midiDevice = null;
#endif
        }

        #region Menu
        // Preset
        private async void OnMenuPresetResetClick(object sender, RoutedEventArgs e) {
            if (await SaveChangedPreset()) { // operation may be cancelled
                ResetPreset();
            }
        }
        private async void OnMenuPresetOpenClick(object sender, RoutedEventArgs e) {
            if (await SaveChangedPreset()) { // operation may be cancelled
                OpenPreset();
            }
        }
        private async void OnMenuPresetSaveClick(object sender, RoutedEventArgs e) {
            await SavePreset(withNewName: false);
        }
        private async void OnMenuPresetSaveAsClick(object sender, RoutedEventArgs e) {
            await SavePreset(withNewName: true);
        }
        private void OnMenuRecentPresetClick(object sender, RoutedEventArgs e) {
            if (sender is MenuItem menuItem) {
                string presetPath = menuItem.Name;
                LoadPreset(presetPath);
            }
        }

        // Image
        private void OnMenuImageOpenSvgClick(object sender, RoutedEventArgs e) {
            Console.WriteLine(">>>> Image Open Svg <<<<");
        }
        private void OnMenuImageSaveAsClick(object sender, RoutedEventArgs e) {
            Console.WriteLine(">>>> Image Save As <<<<");
        }
        #endregion

        #region Menu Preset Recent
        protected void SetRecentPresets(string[] recentPresetPaths, bool updateItems = true) {
            _menuPresetRecentItems.Clear();
            foreach (string presetPath in recentPresetPaths) {
                if (!String.IsNullOrEmpty(presetPath)) {
                    var item = CreateMenuRecentPresetItem(presetPath);
                    _menuPresetRecentItems.Add(item);
                }
            }
            if (updateItems) UpdateMenuRecentPresetItems();
        }
        protected void PopRecentPreset(string presetPath, bool updateItems = true) {
            RemoveRecentPreset(presetPath, false);
            var item = CreateMenuRecentPresetItem(presetPath);
            _menuPresetRecentItems.Insert(0, item);
            if (updateItems) UpdateMenuRecentPresetItems();
        }
        protected void RemoveRecentPreset(string presetPath, bool updateItems = true) {
            var remove = new List<MenuItem>();
            foreach (var item in _menuPresetRecentItems) {
                if (item.Name == presetPath) remove.Add(item);
            }
            if (remove.Count > 0) _menuPresetRecentItems.RemoveAll(remove);
            //
            if (updateItems) UpdateMenuRecentPresetItems();
        }
        private MenuItem CreateMenuRecentPresetItem(string presetPath) {
            var item = new MenuItem();
            item.Header = presetPath;
            item.Name = presetPath;
            item.Click += OnMenuRecentPresetClick;
            return item;
        }
        private void UpdateMenuRecentPresetItems() {
            _menuPresetRecent.Items = _menuPresetRecentItems;
            _menuPresetRecent.IsVisible = _menuPresetRecentItems.Count > 0;
        }
        #endregion


        #region Application settings
        private static readonly XmlWriterSettings _xmlWriterSettings = new XmlWriterSettings {
            Indent = true,
            OmitXmlDeclaration = true,
        };
        private static readonly string _appSettingsPath = Path.Combine(
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

        //
        private string FormatWindowLayout() {
            var state = this.WindowState;
            //!!! RestoreBounds not yet implemented in Avalonia
            /*
            bool normal = window.WindowState == WindowState.Normal;
            Point p = normal ? form.Location : form.RestoreBounds.Location;
            Size  s = normal ? form.Size     : form.RestoreBounds.Size;
            */
            PixelPoint p = this.Position;
            Size s = this.ClientSize; // !!! client?
            return String.Format("{0} {1} {2} {3} {4}",
                (int)state, p.X, p.Y, s.Width, s.Height
            );
        }
        private void SetWindowLayout(string locationValue) {
            if (locationValue == null) return;
            int[] ns = DrawerSettings.ParseIntegers(locationValue); // !!! size values are double
            if (ns == null || ns.Length != 5) return;
            // propagate
            this.WindowState = (WindowState)ns[0];
            if (this.WindowState == WindowState.Normal) {
                this.WindowStartupLocation = WindowStartupLocation.Manual;
                this.Position = new PixelPoint(ns[1], ns[2]);
                this.ClientSize = new Size(ns[3], ns[4]);
            }
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
                        if (++counter <= 5) {
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
        protected void LoadAppSettings() {
            bool presetLoaded = false;
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
                                    presetLoaded = true;
                                    break;
                            }
                        }
                    }
                }
            } catch (FileNotFoundException) {
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

            if (!presetLoaded) {
                ResetPreset();
            }

            // Set loaded drawer settings to drawer
            UpdateDrawerFully();
            //
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
            _currentPresetPath = null;
            _drawerSettings = DrawerSettings.Reset();
            ResetPresetViewport();
            MarkPresetChanged(false);
            //SetSettingsToControls();
            UpdateDrawerFully();
            InvalidateMainImage();
        }

        private static readonly FileDialogFilter[] _fileDialogFilters = new[] {
            new FileDialogFilter() { Name = "Xml files", Extensions = {"xml"} }
        };
        private async void OpenPreset() {
            var dialog = new OpenFileDialog { Title = "Open Preset" };
            dialog.Filters.AddRange(_fileDialogFilters);
            string[] result = await dialog.ShowAsync(this);
            if (result != null && result.Length > 0) { //!!! cancel?
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
                    dialog.InitialDirectory = Path.GetDirectoryName(_currentPresetPath);
                    dialog.InitialFileName = Path.GetFileName(_currentPresetPath);
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
            if (presetLoaded) {
                _currentPresetPath = presetPath;
                PopRecentPreset(_currentPresetPath);
                MarkPresetChanged(false);
                // Set loaded drawer settings to drawer
                UpdateDrawerFully();
                InvalidateMainImage();
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

        private void OnMainImagePointerMoved(object sender, PointerEventArgs e) {
            if (!e.InputModifiers.HasFlag(InputModifiers.RightMouseButton)) { // allow to move cursor out leaving selection/hignlighting
                _pointerPos = e.GetPosition(_mainImageControl);
            }
            if (e.InputModifiers.HasFlag(InputModifiers.MiddleMouseButton)) {
                TD.Point delta = ToPoint(_pointerPosDrag - _pointerPos);
                _pointerPosDrag = _pointerPos;
                _viewport.MoveOrigin(delta);
                UpdateDrawerBounds();
                MarkPresetChanged();
                InvalidateMainImage();
            } else {
                TD.Point u = _viewport.ToUser(ToPoint(_pointerPos));
                _gridDrawer.SetCursor(u.X, u.Y);
                var mode = e.InputModifiers.HasFlag(InputModifiers.Alt)
                    ? GridDrawer.CursorHighlightMode.Cents
                    : GridDrawer.CursorHighlightMode.NearestRational;
                _gridDrawer.SetCursorHighlightMode(mode);
                InvalidateMainImage();
            }
        }

        private void OnMainImagePointerPressed(object sender, PointerPressedEventArgs e) {
            if (_pointerPos != e.GetPosition(_mainImageControl)) return;
            // _gridDrawer.SetCursor already called from OnMouseMove
            if (e.InputModifiers.HasFlag(InputModifiers.LeftMouseButton))
            {
                // Get tempered note
                Drawing.SomeInterval t = null;
                if (e.InputModifiers.HasFlag(InputModifiers.Alt)) { // by cents
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
                    if (e.InputModifiers.HasFlag(InputModifiers.Control)) {
                    /*
                        _toolsForm.ToggleSelection(t); // it calls ApplyDrawerSettings
                    */
                    }
                    // Play note
                    else {
#if USE_MIDI
                        _midiPlayer.NoteOn(0, t.ToCents(), duration: 8f);
#endif
                    }
                }
            }
            else if (e.InputModifiers.HasFlag(InputModifiers.MiddleMouseButton)) {
                _pointerPosDrag = _pointerPos;
            }
        }

        private void OnMainImagePointerWheelChanged(object sender, PointerWheelEventArgs e) {
            bool shift = e.InputModifiers.HasFlag(InputModifiers.Shift);
            bool ctrl  = e.InputModifiers.HasFlag(InputModifiers.Control);
            bool alt   = e.InputModifiers.HasFlag(InputModifiers.Alt);

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

            // update viewport
            _viewport.SetImageSize((float)bounds.Width, (float)bounds.Height);
            UpdateDrawerBounds();

            // update bitmap
            //!!! we need PixelSize. missing some transform?
            var pixelSize = new PixelSize((int)bounds.Width, (int)bounds.Height);
            _mainBitmap.Resize(pixelSize);

            // update image control
            _mainImageControl.Source = _mainBitmap.IsEmpty() ? null : _mainBitmap.AvaloniaBitmap;

            InvalidateMainImage();
        }

        private void UpdateDrawerBounds() {
            _gridDrawer.SetBounds(_viewport.GetUserBounds());
        }
        private void UpdateDrawerPointRadius() {
            _gridDrawer.SetPointRadius(_drawerSettings.pointRadiusLinear);
        }

        private void UpdateDrawerFully() {
            DrawerSettings s = _drawerSettings;
            TD.Viewport3   v = _viewport;
            // viewport
            _gridDrawer.SetBounds(v.GetUserBounds());
            // base
            _gridDrawer.SetBase(s.limitPrimeIndex, s.subgroup, s.narrows);
            _gridDrawer.SetGeneration(s.harmonicityName, s.rationalCountLimit);
            // temperament
            _gridDrawer.SetTemperament(s.temperament);
            _gridDrawer.SetTemperamentMeasure(s.temperamentMeasure);
            // degrees
            _gridDrawer.SetDegrees(s.stepMinHarmonicity, s.stepSizeMaxCount);
            // slope
            _gridDrawer.SetSlope(s.slopeOrigin, s.slopeChainTurns);
            // view
            _gridDrawer.SetEDGrids(s.edGrids);
            _gridDrawer.SetSelection(s.selection);
            _gridDrawer.SetPointRadius(s.pointRadiusLinear);
        }

        protected override void HandlePaint(Rect rect) {
            //!!! not raized on InvalidateVisual ??
            base.HandlePaint(rect);
            Console.WriteLine("HandlePaint {0}", rect.ToString());
        }

        private void InvalidateMainImage() {
            //!!! could we update bitmap in OnPaint?
            UpdateMainBitmap();
            //!!! This InvalidateVisual doesn't raise HandlePaint
            _mainImageControl.InvalidateVisual();
        }

        private void UpdateMainBitmap() {
            if (_mainBitmap.IsEmpty()) return;

            // create grid image
            var image = DrawGrid();

            // render image to system bitmap
            using (var graphics = System.Drawing.Graphics.FromImage(_mainBitmap.SystemBitmap)) {
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                graphics.Clear(System.Drawing.Color.White);
                image.Draw(graphics);
            }

            // copy pixels to avalonia bitmap
            _mainBitmap.CopyPixels();
        }

        private TD.Image DrawGrid()
        {
            // Create image
            var image = new TD.Image(_viewport);


            // Update drawer items (according to collected update flags)
            _gridDrawer.UpdateItems();

            _gridDrawer.UpdateCursorItem();

            // Draw items as image elements
            _gridDrawer.DrawGrid(image);

            return image;
        }
    }
}