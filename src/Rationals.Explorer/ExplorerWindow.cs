using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.CustomControls;

using Rationals.Drawing;

using SD = System.Drawing;
using TD = Torec.Drawing;


namespace Rationals.Explorer
{
    public partial class MainWindow : Window
    {
        private static string _windowTitlePrefix;

        Image _mainImageControl = null;

        Avalonia.Point _pointerPos;
        Avalonia.Point _pointerPosDrag; // dragging start position

        // Preset settings
        DrawerSettings _drawerSettings;
        TD.Viewport3 _viewport;
        SoundSettings _soundSettings;

        GridDrawer _gridDrawer;

        ItemsControl   _menuPresetRecent;
        Avalonia.Collections.AvaloniaList<MenuItem> _menuPresetRecentItems;
        Control        _menuPresetSave;
        const int _recentPresetMaxCount = 5;

        Grid _mainGrid;
        TextBox _textBoxSelectionInfo;

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

        // Sound
#if USE_MIDI
        private Midi.MidiPlayer _midiPlayer = null;
#endif
#if USE_WAVE
        private Wave.WaveEngine _waveEngine = null;
        private Wave.PartialProvider _partialProvider = null;
#endif
        private struct SoundSettings {
            internal enum EDevice { None = 0, Midi = 1, Wave = 2 } 
            internal enum EOutput { None = 0, Midi = 1, Wave = 2, WavePartialsTempered = 3 } 
            internal EOutput output; // currently selected sound output
            //internal Rational[] partials; // e.g. 1, 2, 3, 4, 5, 6
        }
        private SoundSettings.EOutput[] _availableSoundOutputs = null;
        private SoundSettings.EDevice _activeSoundDevice = SoundSettings.EDevice.None;
        private ComboBox _comboBoxSoundOutput = null;
        //private TextBox2 _textBoxWavePartials = null;

#if USE_PERF
        private Rationals.Utils.PerfCounter _perfUpdateItems = new Rationals.Utils.PerfCounter("Update item properties");
        private Rationals.Utils.PerfCounter _perfDrawItems   = new Rationals.Utils.PerfCounter("Items to image elements");
        private Rationals.Utils.PerfCounter _perfRenderImage = new Rationals.Utils.PerfCounter("Render raster image");
        private Rationals.Utils.PerfCounter _perfCopyPixels  = new Rationals.Utils.PerfCounter("Copy image to Avalonia");
#endif

        private struct SystemSettings {
            internal string drawerFont;
            internal bool drawerDisableAntialiasing;
            internal int wavePartialCount;
        }
        private SystemSettings _systemSettings;

        public MainWindow()
        {
            var assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();
            _windowTitlePrefix = "Rationals Explorer ";
#if DEBUG
            _windowTitlePrefix += assemblyName.Version.ToString();
#else
            _windowTitlePrefix += String.Format("{0}.{1}", assemblyName.Version.Major, assemblyName.Version.Minor);
#endif

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
            _menuPresetRecentItems = new Avalonia.Collections.AvaloniaList<MenuItem>();

            _textBoxSelectionInfo = ExpectControl<TextBox>(this, "textBoxSelectionInfo");

            // prepare rendering
            _eventRenderDoWork = new System.Threading.AutoResetEvent(false);
            //
            _threadRender = new System.Threading.Thread(RenderThread);
            _threadRender.Name = "Render";
            _threadRender.Start();

            //
            FindDrawerSettingsControls(this);
            InitSoundEngines();

            LoadAppSettings();

            // Propagate some settings to Drawer
            _gridDrawer.SetSystemSettings(_systemSettings.drawerFont);
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
            // Called from AvaloniaXamlLoader.Load
            Console.WriteLine(">>>> OnWindowInitialized <<<<");
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

            // Sound
#if USE_MIDI
            if (_midiPlayer != null) {
                _midiPlayer.StopClock();
                _midiPlayer.Dispose();
                _midiPlayer = null;
            }
#endif
#if USE_WAVE
            if (_waveEngine != null) {
                _waveEngine.Stop();
                _waveEngine.Dispose();
                _waveEngine = null;
            }
#endif



#if USE_PERF
            Console.WriteLine("Performance counters");
            Console.WriteLine(_perfUpdateItems.GetReport());
            Console.WriteLine(_perfDrawItems  .GetReport());
            Console.WriteLine(_perfRenderImage.GetReport());
            Console.WriteLine(_perfCopyPixels .GetReport());
#endif
        }

        private void UpdateWindowTitle() {
            string title = _windowTitlePrefix;
            title += " - ";
            if (_currentPresetPath == null) {
                title += "New";
            } else {
                title += System.IO.Path.GetFileName(_currentPresetPath);
            }
            if (_currentPresetChanged) {
                title += "*";
            }
            if (this.Title != title) {
                this.Title = title;
            }
        }

        #region Sound
        private void InitSoundEngines()
        {
            // Init sound
            var availableOutputs = new List<SoundSettings.EOutput>();
            availableOutputs.Add(SoundSettings.EOutput.None);
#if USE_MIDI
            try {
                _midiPlayer = new Midi.MidiPlayer(0); //!!! make device index configurable
                availableOutputs.Add(SoundSettings.EOutput.Midi);
            } catch (Exception ex) {
                Console.Error.WriteLine("Can't initialize Midi: {0}", ex.Message);
            }
#endif
#if USE_WAVE
            try {
                var format = new Wave.WaveFormat {
                    bitsPerSample = 16,
                    sampleRate = 22050,
                    channels = 1,
                };
                _waveEngine = new Wave.WaveEngine(format, bufferLengthMs: 60);
                _partialProvider = new Wave.PartialProvider();
                _waveEngine.SetSampleProvider(_partialProvider);
                availableOutputs.Add(SoundSettings.EOutput.Wave);
                availableOutputs.Add(SoundSettings.EOutput.WavePartialsTempered);
            }
            catch (Exception ex) {
                Console.Error.WriteLine("Can't initialize WaveEngine: {0}", ex.Message);
            }
#endif
            _availableSoundOutputs = availableOutputs.ToArray();

            // Find controls
            _comboBoxSoundOutput = ExpectControl<ComboBox>(this, "comboBoxSoundOutput");
            _comboBoxSoundOutput.Items = _availableSoundOutputs;
            //_textBoxWavePartials = ExpectControl<TextBox2>(this, "textBoxWavePartials");
        }

        private void ResetPresetSoundSettings() {
            _soundSettings.output = SoundSettings.EOutput.Midi; // default
            //_soundSettings.partials = null;
        }
        private void PropagatePresetSoundSettings()
        {
            // validate and activate the device
            if (!_availableSoundOutputs.Contains(_soundSettings.output)) {
                _soundSettings.output = SoundSettings.EOutput.None;
            }
            ActivateSoundDevice(GetSoundDevice(_soundSettings.output));
            
            // propagate to controls
            _settingInternally = true;
            _comboBoxSoundOutput.SelectedItem = _soundSettings.output; // raise comboBoxSoundOutput_SelectionChanged
            //_textBoxWavePartials.Text = Rational.FormatRationals(_soundSettings.partials, ", "); // raise textBoxWavePartials_TextChanged
            _settingInternally = false;
        }

        private static SoundSettings.EDevice GetSoundDevice(SoundSettings.EOutput output) {
            switch (output) {
                case SoundSettings.EOutput.Midi:
                    return SoundSettings.EDevice.Midi;
                case SoundSettings.EOutput.Wave:
                case SoundSettings.EOutput.WavePartialsTempered:
                    return SoundSettings.EDevice.Wave;
            }
            return SoundSettings.EDevice.None;
        }

        private void ActivateSoundDevice(SoundSettings.EDevice device) {
            if (_activeSoundDevice == device) return;

            // Switch active device
            // Stop previous
            switch (_activeSoundDevice) {
#if USE_MIDI
                case SoundSettings.EDevice.Midi:
                    _midiPlayer.StopClock();
                    break;
#endif
#if USE_WAVE
                case SoundSettings.EDevice.Wave:
                    _waveEngine.Stop();
                    break;
#endif
            }

            _activeSoundDevice = device;

            // Start new
            Console.WriteLine("Starting sound device: {0}", _activeSoundDevice);
            switch (_activeSoundDevice) {
#if USE_MIDI
                case SoundSettings.EDevice.Midi:
                    _midiPlayer.StartClock(beatsPerMinute: 60 * 4);
                    break;
#endif
#if USE_WAVE
                case SoundSettings.EDevice.Wave:
                    _waveEngine.Play();
                    break;
#endif
            }
        }

        private void comboBoxSoundOutput_SelectionChanged(object sender, SelectionChangedEventArgs e) {

            if (sender is ComboBox combo) {
                if (combo.SelectedItem is SoundSettings.EOutput output)
                {
                    if (!_settingInternally) {
                        _soundSettings.output = output;
                        ActivateSoundDevice(GetSoundDevice(_soundSettings.output));
                    }

                    // Enable Partials textbox for Wave
                    bool partialsTempered = _soundSettings.output == SoundSettings.EOutput.WavePartialsTempered;
                    //_textBoxWavePartials.IsEnabled = partialsTempered;

                    // Update drawer
                    _gridDrawer.SetPartials(partialsTempered ? new Rational[] { } : null);
                    InvalidateView();
                }
            }
        }

        /*
        private void textBoxWavePartials_TextChanged(object sender, RoutedEventArgs e) {
            if (_settingInternally) return;

            Rational[] partials = Rational.ParseRationals(_textBoxWavePartials.Text, ",");
            //!!! validate by subgroup matrix ?
            _soundSettings.partials = partials;
        }
        */

        private void PlayNote(SomeInterval t)
        {
            if (_soundSettings.output == SoundSettings.EOutput.None) return;

            // get interval cents
            float cents = t.IsRational()
                ? _gridDrawer.Temperament.CalculateMeasuredCents(t.rational)
                : t.cents;

            switch (_soundSettings.output) {
                case SoundSettings.EOutput.Midi:
#if USE_MIDI
                    if (_midiPlayer != null && _midiPlayer.IsClockStarted()) {
                        _midiPlayer.NoteOn(0, cents, duration: 8f); // duration in beats
                    }
#endif
                    break;
                case SoundSettings.EOutput.Wave:
                case SoundSettings.EOutput.WavePartialsTempered:
#if USE_WAVE
                    if (_waveEngine != null && _waveEngine.IsPlaying() && _partialProvider != null)
                    {
                        IList<Rational> partials = null; // _soundSettings.partials; // get partials from settings
                        if (partials == null) {
                            // generate default integer partials
                            partials = new List<Rational>();
                            for (int i = 1; i < 100; ++i) {
                                var r = new Rational(i);
                                if (!_gridDrawer.Subgroup.IsInRange(r)) {
                                    continue; // skip if out of subgroup
                                }
                                partials.Add(r);
                                if (partials.Count == _systemSettings.wavePartialCount) break;
                            }
                        }
                        //
                        bool temper = _soundSettings.output == SoundSettings.EOutput.WavePartialsTempered;
                        foreach (Rational r in partials) {
                            float c = cents;
                            c += temper ? _gridDrawer.Temperament.CalculateMeasuredCents(r)
                                        : (float)r.ToCents();
                            double hz = Wave.PartialProvider.CentsToHz(c);
                            float h = _gridDrawer.GetRationalHarmonicity(r);
                            Debug.Assert(0 <= h && h <= 1f, "Normalized harmonicity expected");
                            float level = (float)(0.1 * Math.Pow(h, 4.5));
                            int duration = (int)(2000 * Math.Pow(h, 1));
                            Debug.WriteLine("Add partial: {0} {1:0.000} -> {2:0.00}c {3:0.00}hz level {4:0.000}", r, h, c, hz, level);
                            _partialProvider.AddPartial(hz, 10, duration, level, -4f);
                        }
                        _partialProvider.FlushPartials();
                    }
#endif
                    break;
            }
        }

        #endregion Sound

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
            // Don't reorder resent preset lines in menu
#if false
            RemoveRecentPreset(presetPath, false);
            var item = CreateMenuRecentPresetItem(presetPath);
            if (updateItems) UpdateMenuRecentPreset();
#else
            bool exists = _menuPresetRecentItems.Any(i => i.Name == presetPath);
            if (!exists) {
                var item = CreateMenuRecentPresetItem(presetPath);
                _menuPresetRecentItems.Insert(0, item);
                if (updateItems) UpdateMenuRecentPreset();
            }
#endif
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
            item.Header = presetPath.Replace('_', '‗'); // U+2017 ‗ DOUBLE LOW LINE - to avoid shortcut-s
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
            //
            UpdateWindowTitle();
        }

        // Window Location & Layout
        // Format: "<window state> <X> <Y> <W> <H> <panel width>"
        //   window state: Normal = 0, Minimized = 1, Maximized = 2
        private static string GetWindowLayoutDescriptiob() {
            return "<window state> <X> <Y> <W> <H> <panel width>";
        }
        private string FormatWindowLayout() {
            var state = this.WindowState;
            //!!! RestoreBounds not yet implemented in Avalonia
            /*
            bool normal = window.WindowState == WindowState.Normal;
            Point p = normal ? form.Location : form.RestoreBounds.Location;
            Size  s = normal ? form.Size     : form.RestoreBounds.Size;
            */
            PixelPoint p = this.Position; // !!! buggy always {0, 0} ?
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
                // System settings
                w.WriteStartElement("systemSettings");
                WriteSystemSettings(w, _systemSettings);
                w.WriteEndElement();
                // Window layout
                w.WriteElementString("windowLayout", FormatWindowLayout());
                w.WriteComment(GetWindowLayoutDescriptiob());
                // Presets
                w.WriteStartElement("presets");
                SavePresetsSettings(w);
                w.WriteEndElement();
                // Current preset
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
            try {
                using (XmlTextReader r = new XmlTextReader(_appSettingsPath)) {
                    while (r.Read()) {
                        if (r.NodeType == XmlNodeType.Element) {
                            switch (r.Name) {
                                case "systemSettings":
                                    _systemSettings = ReadSystemSettings(r.ReadSubtree());
                                    break;
                                case "windowLayout":
                                    SetWindowLayout(r.ReadElementContentAsString());
                                    break;
                                case "presets":
                                    LoadPresetsSettings(r.ReadSubtree());
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

            // Propagate new settings to form controls & drawer
            OnPresetLoaded();
        }

        private void WriteSystemSettings(XmlWriter w, SystemSettings s) {
            w.WriteElementString("drawerFont", s.drawerFont);
            w.WriteElementString("drawerDisableAntialiasing", (s.drawerDisableAntialiasing ? 1 : 0).ToString());
            w.WriteElementString("wavePartialCount", s.wavePartialCount.ToString());
        }

        private SystemSettings ReadSystemSettings(XmlReader r) {
            var s = new SystemSettings {
                wavePartialCount = 10, // default
            };
            while (r.Read()) {
                if (r.NodeType == XmlNodeType.Element) {
                    switch (r.Name) {
                        case "drawerFont":
                            s.drawerFont = r.ReadElementContentAsString();
                            break;
                        case "drawerDisableAntialiasing":
                            s.drawerDisableAntialiasing = r.ReadElementContentAsInt() != 0;
                            break;
                        case "wavePartialCount":
                            s.wavePartialCount = r.ReadElementContentAsInt();
                            break;
                    }
                }
            }
            return s;
        }

        private void WriteSoundSettings(XmlWriter w, SoundSettings s) {
            w.WriteElementString("output", s.output.ToString());
        }
        private SoundSettings ReadSoundSettings(XmlReader r) {
            var s = new SoundSettings { };
            while (r.Read()) {
                if (r.NodeType == XmlNodeType.Element) {
                    switch (r.Name) {
                        case "output":
                            s.output = Enum.Parse<SoundSettings.EOutput>(r.ReadElementContentAsString(), true);
                            break;
                    }
                }
            }
            return s;
        }

        private void OnPresetLoaded() {
            UpdateWindowTitle();

            // Preset settings (viewport, drawer, sound) were loaded (preset was reset or loaded).
            // Now propagate new settings to form controls & services.

            // Sound (_soundSettings)
            PropagatePresetSoundSettings();
            
            // Drawer
            SetSettingsToControls();
            UpdateDrawerBounds();
            UpdateDrawerFully();
            ValidateControlsByDrawer();

            InvalidateView();
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
                InvalidateView(false); // viewport moved only
            } else {
                TD.Point u = _viewport.ToUser(ToPoint(_pointerPos));
                _gridDrawer.SetCursor(u.X, u.Y);
                var mode = e.KeyModifiers.HasFlag(KeyModifiers.Alt)
                    ? GridDrawer.CursorHighlightMode.Cents
                    : GridDrawer.CursorHighlightMode.NearestRational;
                _gridDrawer.SetCursorHighlightMode(mode);
                InvalidateView();
            }
        }

        private void MainImage_PointerLeave(object sender, PointerEventArgs e) {
            // allow to move cursor out leaving selection/hignlighting unchanged
            if (IgnorePointerMove(e)) return;

            // disable highlighting
            //_pointerPos = new Point();
            //_gridDrawer.SetCursor(0, 0);
            _gridDrawer.SetCursorHighlightMode(GridDrawer.CursorHighlightMode.None);
            InvalidateView();
        }

        private void MainImage_PointerPressed(object sender, PointerPressedEventArgs e) {
            PointerPoint p = e.GetCurrentPoint(_mainImageControl);

            if (_pointerPos != p.Position) return;
            // _gridDrawer.SetCursor already called from OnMouseMove

            if (p.Properties.IsLeftButtonPressed)
            {
                // Get tempered note
                SomeInterval t = null;
                if (e.KeyModifiers.HasFlag(KeyModifiers.Alt)) { // by cents
                    float c = _gridDrawer.GetCursorCents();
                    t = new SomeInterval { cents = c };
                } else { // nearest rational
                    _gridDrawer.UpdateCursorItem();
                    Rational r = _gridDrawer.GetCursorRational();
                    if (!r.IsDefault()) {
                        t = new SomeInterval { rational = r };
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
                        PlayNote(t);
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
            InvalidateView(false); // viewport changed only
        }

        #endregion




        private void MainImage_KeyDown(object sender, KeyEventArgs e)
        {
            Keyboard.Coords c;
            if (!Keyboard.KeyCoords.TryGetValue(e.Key, out c)) return;

            SomeInterval t = _gridDrawer.GetKeyboardInterval(c.x, c.y, 0);
            if (t != null) {
                Debug.WriteLine("KeyDown {0} {1} -> {2}", c.x, c.y, t.ToString());
                PlayNote(t);
            }
        }

        private void OnMainImageBoundsChanged(Rect bounds) {
            //Console.WriteLine("mainImagePanel bounds -> {0}", bounds);
            if (bounds.IsEmpty) return;

            //Debug.WriteLine("OnMainImageBoundsChanged begin");

            // Update drawer & invalidate
            _viewport.SetImageSize((float)bounds.Width, (float)bounds.Height);
            UpdateDrawerBounds();
            InvalidateView(false);

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

        protected void InvalidateView(bool updateSelectionInfo = true) {
            // Avalonia InvalidateVisual raises no "OnPaint" events (HandlePaint raised on resize only).
            // So we use a render thread:

            //RedrawMainImage();
            // temperament measure slider move is slow
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(RedrawMainImage);

            if (updateSelectionInfo) {
                _textBoxSelectionInfo.Text = _gridDrawer.FormatSelectionInfo();
            }
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
                /* !!! slow stuff

                Render raster image           :    9071899 ticks / count    29 =     312824                     // smooth
                    Render raster image           :   12153400 ticks / count    44 =     276213                 // TextRenderingHint.SingleBitPerPixel
                    Render raster image           :    6779342 ticks / count    35 =     193695                 // no text
                    Render raster image           :    8301237 ticks / count    42 =     197648                 // no smooth
                        Render raster image           :    8025534 ticks / count    48 =     167198             // TextRenderingHint.SingleBitPerPixel
                            Render raster image           :    7030453 ticks / count    38 =     185011         // raster font MS serif
                        Render raster image           :    5674569 ticks / count    71 =      79923             // no text
                Render raster image           :   10144730 ticks / count    30 =     338157                     // always Arial 30
                Render raster image           :   12752666 ticks / count    39 =     326991                     // always Arial 30 - created once

                // read this: https://stackoverflow.com/questions/71374/fastest-api-for-rendering-text-in-windows-forms
                // GDI vs. GDI+ Text Rendering Performance https://techcommunity.microsoft.com/t5/windows-blog-archive/gdi-vs-gdi-text-rendering-performance/ba-p/228431

                */

#if USE_PERF
                _perfRenderImage.Start();
#endif
                using (var graphics = SD.Graphics.FromImage(
                    _mainBitmap.SystemBitmaps[bitmapIndex]
                )) {
                    if (_systemSettings.drawerDisableAntialiasing) {
                        graphics.SmoothingMode = SD.Drawing2D.SmoothingMode.None;
                        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixel;
                    } else {
                        graphics.SmoothingMode = SD.Drawing2D.SmoothingMode.AntiAlias; // rendering time *= 1.5
                    }
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

        private void UpdateMainBitmap() {
            // Now (in UI thread) we copy bitmap bits to Avalonia bitmap

            // get index of bitmap to copy from
            int bitmapIndex;
            lock (_renderLock) {
                if (_lastRenderedBitmap == -1) return;
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
        }

    }
}
