using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.IO;

using System.Xml.Serialization; //?

namespace Rationals.Forms
{
    using GridDrawer = Rationals.Drawing.GridDrawer;

    public partial class ToolsForm : Form
    {
        private GridDrawer.Settings _currentSettings;
        private MainForm _mainForm;
        private GridDrawer _gridDrawer;

        private int _dirtyMaxPrimeIndex; // used to validate commas

        private bool _settingSettings = false; // true if SetSettings() called

        public ToolsForm(MainForm mainForm, GridDrawer gridDrawer) {
            _mainForm = mainForm;
            _gridDrawer = gridDrawer;

            InitializeComponent();
            comboBoxDistance.Items.AddRange(Rationals.Utils.HarmonicityNames); // fill Harmonicity combo

            InitTooltips();

            // Load previous or set default preset settings
            bool presetLoaded = LoadAppSettings();
            if (!presetLoaded) {
                _currentSettings = CreateDefaultSettings();
                SetSettings(_currentSettings);
                _mainForm.ApplyDrawerSettings(_currentSettings);
            }
        }

        public GridDrawer.Settings GetCurrentSettings() {
            return _currentSettings;
        }

        public void ShowInfo(string text) {
            textBoxSelection.Text = text;
        }

        private void buttonApply_Click(object sender, EventArgs e) {
            _currentSettings = GetSettings();
            _mainForm.ApplyDrawerSettings(_currentSettings);
        }

        private static Color ValidColor(bool valid) {
            return valid ? default(Color) : Color.Pink;
        }

        private static GridDrawer.Settings CreateDefaultSettings() {
            GridDrawer.Settings s = GridDrawer.Settings.Edo12();
            // also set default limits
            s.rationalCountLimit = 500;
            s.distanceLimit = new Rational(new[] { 8, -8, 2 });
            return s;
        }

        // Set settings to controls
        private void SetSettings(GridDrawer.Settings s) {
            _settingSettings = true;
            // limit
            upDownLimit.Value = s.limitPrimeIndex;
            // subgroup
            textBoxSubgroup.Text = FormatSubgroup(s.subgroup);
            // update dirty prime limit
            UpdateMaxPrimeIndex(s.limitPrimeIndex, s.subgroup);
            // commas
            textBoxStickCommas.Text = FormatCommas(s.stickCommas);
            trackBarStickCommas.Value = (int)Math.Round(s.stickMeasure * 100);
            // up interval
            textBoxUp.Text = FormatRational(s.slopeOrigin);
            upDownChainTurns.Value = (decimal)s.slopeChainTurns;
            // grids
            textBoxGrids.Text = FormatGrids(s.edGrids);
            // drawing
            comboBoxDistance.SelectedItem = s.harmonicityName ?? Rationals.Utils.HarmonicityNames[0];
            upDownCountLimit.Value = s.rationalCountLimit;
            textBoxDistanceLimit.Text = FormatRational(s.distanceLimit);
            //
            _settingSettings = false;
        }

        // Read settings from controls
        private GridDrawer.Settings GetSettings() {
            var s = new GridDrawer.Settings();
            // subgroup
            string subgroup = textBoxSubgroup.Text;
            if (!String.IsNullOrWhiteSpace(subgroup)) {
                s.subgroup = ParseSubgroup(subgroup);
            }
            // base & limit
            if (s.subgroup == null) {
                s.limitPrimeIndex = (int)upDownLimit.Value;
            }
            // commas
            string commas = textBoxStickCommas.Text;
            s.stickCommas = ParseCommas(commas);
            s.stickMeasure = trackBarStickCommas.Value * 0.01f;
            // up interval
            s.slopeOrigin = Rational.Parse(textBoxUp.Text);
            s.slopeChainTurns = (float)upDownChainTurns.Value;
            // grids
            string grids = textBoxGrids.Text;
            if (!String.IsNullOrWhiteSpace(grids)) {
                s.edGrids = ParseGrids(grids);
            }
            // drawing
            s.harmonicityName = (string)comboBoxDistance.SelectedItem;
            s.rationalCountLimit = (int)upDownCountLimit.Value;
            s.distanceLimit = Rational.Parse(textBoxDistanceLimit.Text);
            //
            return s;
        }

        private static string FormatRational(Rational r) {
            if (r.IsDefault()) return "";
            return r.FormatFraction();
        }

        private void control_ValueChanged(object sender, EventArgs e) {
            if (!_settingSettings) MarkPresetChanged();
        }

        #region ToolTips
        private Dictionary<Control, TooltipText> _tooltips = new Dictionary<Control, TooltipText>();
        private struct TooltipText {
            public string Name;
            public string Text;
        }
        private Control FindControl(string tag, Control control) {
            if (control == null) return null;
            if (control.Tag as string == tag) return control;
            foreach (Control child in control.Controls) {
                Control r = FindControl(tag, child);
                if (r != null) return r;
            }
            return null;
        }
        private void AddTooltipText(string name, string text) {
            Control control = FindControl(name, this);
            if (control == null) {
                //!!! no control with such Tag found
            } else {
                _tooltips[control] = new TooltipText {
                    Name = name,
                    Text = text,
                };
                control.Enter += control_Enter;
                control.Leave += control_Leave;
            }
        }
        private void InitTooltips() {
            //!!! not sure we need these enforced tooltips
            AddTooltipText("Generated item count", "");
            AddTooltipText("Generation distance", "");
            AddTooltipText("Slope origin", "");
            AddTooltipText("Slope turns", "");
            AddTooltipText("Commas", "e.g. '81/80, 128/125'");
            AddTooltipText("ED grid", "");
        }
        private void control_Enter(object sender, EventArgs e) {
            var control = sender as Control;
            if (control == null) return;
            //
            if (!String.IsNullOrEmpty(toolTip.GetToolTip(control))) return; // probably error tooltip shown
            //
            TooltipText t;
            if (_tooltips.TryGetValue(control, out t)) {
                toolTip.Show(t.Text, control, control.Width, 0);
            }
        }
        private void control_Leave(object sender, EventArgs e) {
            var control = sender as Control;
            if (control == null) return;
            //
            toolTip.Hide(control);
        }
        #endregion

        private void upDownLimit_ValueChanged(object sender, EventArgs e) {
            //!!! code smell: to revalidate commas we reparse subgroup and commas text
            textBoxSubgroup_TextChanged(sender, e);
        }

        #region Subgroup
        private static string FormatSubgroup(Rational[] subgroup) {
            if (subgroup == null) return "";
            return String.Join(".", subgroup.Select(r => r.FormatFraction()));
        }
        private static Rational[] ParseSubgroup(string subgroup) {
            if (String.IsNullOrWhiteSpace(subgroup)) return null;
            string[] parts = subgroup.Split('.');
            Rational[] result = new Rational[parts.Length];
            for (int i = 0; i < parts.Length; ++i) {
                result[i] = Rational.Parse(parts[i]);
                if (result[i].IsDefault()) return null;
            }
            return result;
        }
        private void textBoxSubgroup_TextChanged(object sender, EventArgs e) {
            if (!_settingSettings) MarkPresetChanged();
            //
            string subgroupText = textBoxSubgroup.Text;
            bool empty = String.IsNullOrWhiteSpace(subgroupText);
            Rational[] subgroup = ParseSubgroup(subgroupText);
            bool valid = empty || (subgroup != null);
            textBoxSubgroup.BackColor = ValidColor(valid);
            upDownLimit.Enabled = empty || !valid;
            // we must revalidate commas (we reparse them !!!)
            UpdateMaxPrimeIndex((int)upDownLimit.Value, subgroup);
            textBoxStickCommas_TextChanged(sender, e);
        }
        #endregion

        #region Slope
        private void upDownChainTurns_ValueChanged(object sender, EventArgs e) {
            if (!_settingSettings) MarkPresetChanged();
            //
            if (_settingSettings) return;
            // set directly to drawer
            float chainTurns  = (float)upDownChainTurns.Value;
            _gridDrawer.SetSlope(_currentSettings.slopeOrigin, chainTurns);
            _mainForm.Invalidate();
        }
        #endregion

        #region Grids
        private static string FormatGrids(GridDrawer.EDGrid[] edGrids) {
            if (edGrids == null) return "";
            return String.Join("; ", edGrids.Select(g =>
                String.Format("{0}ed{1}{2}",
                    g.stepCount,
                    FindEDBaseLetter(g.baseInterval) ?? g.baseInterval.FormatFraction(),
                    g.basis == null ? "" : String.Format(" {0} {1}", g.basis[0], g.basis[1])
                )
            ));
        }
        private static string FindEDBaseLetter(Rational b) {
            return _edBases
                .Where(i => i.Value.Equals(b))
                .Select(i => i.Key)
                .FirstOrDefault();
        }
        private static Dictionary<string, Rational> _edBases = new Dictionary<string, Rational> {
            { "o", new Rational(2) },  // edo
            { "t", new Rational(3) },  // edt
            { "f", new Rational(3,2) } // edf
        };
        private GridDrawer.EDGrid[] ParseGrids(string grids) {
            if (String.IsNullOrWhiteSpace(grids)) return null;
            string[] parts = grids.ToLower().Split(',',';');
            var result = new GridDrawer.EDGrid[parts.Length];
            for (int i = 0; i < parts.Length; ++i) {
                string[] ps = parts[i].Split(new[]{"ed","-"," "}, StringSplitOptions.RemoveEmptyEntries);
                int pn = ps.Length;
                if (pn != 2 && pn != 4) return null;
                //
                var g = new GridDrawer.EDGrid();
                if (!int.TryParse(ps[0], out g.stepCount)) return null;
                if (g.stepCount <= 0) return null;
                if (!_edBases.TryGetValue(ps[1], out g.baseInterval)) {
                    g.baseInterval = Rational.Parse(ps[1]);
                    if (g.baseInterval.IsDefault()) return null;
                }
                if (pn == 4) {
                    g.basis = new int[2];
                    if (!int.TryParse(ps[2], out g.basis[0])) return null;
                    if (!int.TryParse(ps[3], out g.basis[1])) return null;
                    // validate
                    g.basis[0] = Rationals.Utils.Mod(g.basis[0], g.stepCount);
                    g.basis[1] = Rationals.Utils.Mod(g.basis[1], g.stepCount);
                }
                //
                result[i] = g;
            }
            return result;
        }
        private void textBoxGrids_TextChanged(object sender, EventArgs e) {
            if (!_settingSettings) MarkPresetChanged();
            //
            string grids = textBoxGrids.Text;
            bool empty = String.IsNullOrWhiteSpace(grids);
            bool valid = empty || (ParseGrids(grids) != null);
            textBoxGrids.BackColor = ValidColor(valid);
        }
        #endregion

        #region Stick commas
        private void UpdateMaxPrimeIndex(int limitPrimeIndex, Rational[] subgroup) {
            if (subgroup != null) {
                int temp;
                GridDrawer.GetSubgroupPrimeRange(subgroup, out temp, out _dirtyMaxPrimeIndex);
            } else {
                _dirtyMaxPrimeIndex = limitPrimeIndex;
            }
        }
        private bool AreCommasInRange(Rational[] commas) {
            if (commas == null) return true;
            for (int i = 0; i < commas.Length; ++i) {
                int m = commas[i].GetPowerCount() - 1;
                if (m > _dirtyMaxPrimeIndex) {
                    return false;
                }
            }
            return true;
        }
        private static string FormatCommas(Rational[] commas) {
            if (commas == null) return "";
            return String.Join(", ", commas.Select(r => r.FormatFraction()));
        }
        private Rational[] ParseCommas(string commasText) {
            if (String.IsNullOrWhiteSpace(commasText)) return null;
            string[] parts = commasText.Trim().ToLower().Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            var commas = new Rational[parts.Length];
            for (int i = 0; i < parts.Length; ++i) {
                Rational c = Rational.Parse(parts[i]);
                if (c.IsDefault()) return null;
                commas[i] = c;
            }
            if (!AreCommasInRange(commas)) return null; // validate
            return commas;
        }
        private void textBoxStickCommas_TextChanged(object sender, EventArgs e) {
            if (!_settingSettings) MarkPresetChanged();
            //
            string commasText = textBoxStickCommas.Text;
            string error = null;
            bool empty = String.IsNullOrWhiteSpace(commasText);
            if (!empty) {
                Rational[] commas = ParseCommas(commasText);
                if (commas == null) {
                    error = "Invalid format or out of JI limit";
                }
            }
            textBoxStickCommas.BackColor = ValidColor(error == null);
            trackBarStickCommas.Enabled = !empty;
            toolTip.SetToolTip(textBoxStickCommas, error);
        }
        private void trackBarStickCommas_ValueChanged(object sender, EventArgs e) {
            if (!_settingSettings) MarkPresetChanged();
            //
            if (_settingSettings) return;
            // set directly to drawer
            float value = trackBarStickCommas.Value * 0.01f;
            _gridDrawer.SetStickMeasure(value);
            _mainForm.Invalidate();
        }
        #endregion

        #region Presets
        // Serialization
        private void SavePreset(XmlWriter w) {
            GridDrawer.Settings s = GetSettings();
            w.WriteElementString("limitPrime",         s.subgroup != null ? "" : Rationals.Utils.GetPrime(s.limitPrimeIndex).ToString());
            w.WriteElementString("subgroup",           FormatSubgroup(s.subgroup));
            //
            w.WriteElementString("harmonicityName",    s.harmonicityName);
            w.WriteElementString("rationalCountLimit", s.rationalCountLimit.ToString());
            w.WriteElementString("distanceLimit",      FormatRational(s.distanceLimit));
            //
            w.WriteElementString("slopeOrigin",        FormatRational(s.slopeOrigin));
            w.WriteElementString("slopeChainTurns",    s.slopeChainTurns.ToString());
            w.WriteElementString("stickCommas",        FormatCommas(s.stickCommas));
            w.WriteElementString("stickMeasure",       s.stickMeasure.ToString());
            w.WriteElementString("edGrids",            FormatGrids(s.edGrids));
            //
            w.WriteStartElement("viewport");
            _mainForm.SavePresetViewport(w);
            w.WriteEndElement();
        }
        private void LoadPreset(XmlReader r) {
            var s = new GridDrawer.Settings { };
            while (r.Read()) {
                if (r.NodeType == XmlNodeType.Element) {
                    switch (r.Name) {
                        case "limitPrime": {
                            Rational limitPrime = Rational.Parse(r.ReadElementContentAsString());
                            if (!limitPrime.IsDefault()) s.limitPrimeIndex = limitPrime.GetPowerCount() - 1;
                            UpdateMaxPrimeIndex(s.limitPrimeIndex, s.subgroup);
                            break;
                        }
                        case "subgroup": {
                            s.subgroup = ParseSubgroup(r.ReadElementContentAsString());
                            UpdateMaxPrimeIndex(s.limitPrimeIndex, s.subgroup);
                            break;
                        }
                        //
                        case "harmonicityName":     s.harmonicityName   = r.ReadElementContentAsString();                   break;
                        case "rationalCountLimit":  s.rationalCountLimit= r.ReadElementContentAsInt();                      break;
                        case "distanceLimit":       s.distanceLimit     = Rational.Parse(r.ReadElementContentAsString());   break;
                        //
                        case "slopeOrigin":         s.slopeOrigin       = Rational.Parse(r.ReadElementContentAsString());   break;
                        case "slopeChainTurns":     s.slopeChainTurns   = r.ReadElementContentAsFloat();                    break;
                        case "stickCommas":         s.stickCommas       = ParseCommas(r.ReadElementContentAsString());      break;
                        case "stickMeasure":        s.stickMeasure      = r.ReadElementContentAsFloat();                    break;
                        case "edGrids":             s.edGrids           = ParseGrids(r.ReadElementContentAsString());       break;
                        //
                        case "viewport":            _mainForm.LoadPresetViewport(r); break;
                    }
                }
            }
            _currentSettings = s;
            SetSettings(_currentSettings);
            _mainForm.ApplyDrawerSettings(_currentSettings);
        }
        private void SavePreset(string presetPath) {
            bool saved = false;
            using (XmlWriter w = XmlWriter.Create(presetPath, _xmlWriterSettings)) {
                w.WriteStartDocument();
                w.WriteStartElement("preset");
                SavePreset(w);
                saved = true;
                w.WriteEndElement();
                w.WriteEndDocument();
            }
            if (saved) {
                _currentPresetPath = presetPath;
                MarkPresetChanged(false);
                PopRecentPreset(_currentPresetPath);
            }
        }
        private void LoadPreset(string presetPath) {
            bool loaded = false;
            try {
                using (XmlReader r = XmlReader.Create(presetPath)) {
                    while (r.Read()) {
                        if (r.NodeType == XmlNodeType.Element && r.Name == "preset") {
                            LoadPreset(r);
                            loaded = true;
                        }
                    }
                }
            } catch (Exception ex) {
                string message = "Can't open preset '" + presetPath + "':\r\n" + ex.Message;
                MessageBox.Show(message, Application.ProductName);
                loaded = false;
            }
            if (loaded) {
                _currentPresetPath = presetPath;
                MarkPresetChanged(false);
                PopRecentPreset(_currentPresetPath);
            } else {
                RemoveRecentPreset(presetPath);
            }
        }
        //
        private static readonly XmlWriterSettings _xmlWriterSettings = new XmlWriterSettings {
            Indent = true,
            OmitXmlDeclaration = true,
        };
        private static readonly string _appSettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Application.ProductName + "_Settings.xml" //!!! remove spaces
        );
        private static readonly string _fileDialogFilter = "Xml files (*.xml)|*.xml|All files (*.*)|*.*";
        //
        private string _currentPresetPath = null;
        private bool _currentPresetChanged = false;
        // Application settings
        public void SaveAppSettings() {
            using (XmlWriter w = XmlWriter.Create(_appSettingsPath, _xmlWriterSettings)) {
                w.WriteStartDocument();
                w.WriteStartElement("appSettings");
                // Presets
                w.WriteStartElement("recentPresets");
                int counter = 0;
                foreach (ToolStripItem r in menuRecent.DropDownItems) {
                    if (++counter <= 5) {
                        w.WriteElementString("recentPreset", r.Name);
                    }
                }
                w.WriteEndElement();
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
        private bool LoadAppSettings() {
            bool presetLoaded = false;
            var recentPresets = new List<string>();
            try {
                using (XmlReader r = XmlReader.Create(_appSettingsPath)) {
                    while (r.Read()) {
                        if (r.NodeType == XmlNodeType.Element) {
                            switch (r.Name) {
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
                                    LoadPreset(r);
                                    presetLoaded = true;
                                    break;
                            }
                        }
                    }
                }
            } catch (FileNotFoundException) {
                return false;
            } catch (Exception ex) {
                Console.Error.WriteLine("LoadAppSettings error: " + ex.Message);
                return false;
            }
            // Fill recent presets menu
            menuRecent.DropDownItems.Clear();
            foreach (string presetPath in recentPresets) {
                var item = CreateRecentPresetMenuItem(presetPath);
                menuRecent.DropDownItems.Add(item);
            }
            menuRecent.Visible = menuRecent.DropDownItems.Count > 0;
            return presetLoaded;
        }
        // Dialogs
        public void MarkPresetChanged(bool changed = true) {
            _currentPresetChanged = changed;
            //
            bool enableSave = changed && _currentPresetPath != null;
            if (menuSave.Enabled != enableSave) {
                menuSave.Enabled = enableSave;
            }
        }
        private void ResetPreset() {
            _currentSettings = CreateDefaultSettings();
            _currentPresetPath = null;
            SetSettings(_currentSettings);
            _mainForm.ResetViewport();
            _mainForm.ApplyDrawerSettings(_currentSettings);
            MarkPresetChanged(false);
        }
        private void OpenPreset() {
            string presetPath = null;
            using (var dialog = new OpenFileDialog {
                Title = "Open Preset",
                Filter = _fileDialogFilter,
            }) {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                presetPath = dialog.FileName;
            }
            LoadPreset(presetPath);
        }
        private void SavePreset(bool forceNewName) {
            string presetPath;
            if (_currentPresetPath != null && !forceNewName) {
                presetPath = _currentPresetPath;
            } else {
                using (var dialog = new SaveFileDialog {
                    Title = "Save Preset As",
                    Filter = _fileDialogFilter,
                }) {
                    if (_currentPresetPath != null) {
                        dialog.InitialDirectory = Path.GetDirectoryName(_currentPresetPath);
                        dialog.FileName         = Path.GetFileName(_currentPresetPath);
                    }
                    if (dialog.ShowDialog() != DialogResult.OK) return;
                    presetPath = dialog.FileName;
                }
            }
            SavePreset(presetPath);
        }
        private bool SaveChangedPreset() {
            if (!_currentPresetChanged) return true;
            string message = (_currentPresetPath ?? "Unnamed") + " preset has unsaved changes.\r\nSave preset?";
            var result = MessageBox.Show(message, Application.ProductName, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (result == DialogResult.Cancel) return false; // cancel current operation
            if (result == DialogResult.Yes) {
                SavePreset(false);
            }
            return true;
        }
        // Menu
        private ToolStripItem CreateRecentPresetMenuItem(string presetPath) {
            var item = new ToolStripMenuItem(presetPath); // text
            item.Name = presetPath; // key
            item.Click += menuRecentPreset_Click;
            return item;
        }
        private void PopRecentPreset(string presetPath) {
            RemoveRecentPreset(presetPath);
            var item = CreateRecentPresetMenuItem(presetPath);
            menuRecent.DropDownItems.Insert(0, item);
            menuRecent.Visible = true;
        }
        private void RemoveRecentPreset(string presetPath) {
            var items = menuRecent.DropDownItems.Find(presetPath, false);
            if (items.Length > 0) {
                foreach (var item in items) {
                    item.Click -= menuRecentPreset_Click;
                    menuRecent.DropDownItems.Remove(item);
                }
                menuRecent.Visible = menuRecent.DropDownItems.Count > 0;
            }
        }
        private void menuRecentPreset_Click(object sender, EventArgs e) {
            var item = sender as ToolStripMenuItem;
            if (item != null) {
                LoadPreset(item.Name);
            }
        }
        private void menuReset_Click(object sender, EventArgs e) {
            if (_currentPresetChanged && !SaveChangedPreset()) return; // cancelled
            ResetPreset();
        }
        private void menuSaveAs_Click(object sender, EventArgs e) {
            SavePreset(true);
        }
        private void menuSave_Click(object sender, EventArgs e) {
            SavePreset(false);
        }
        private void menuOpen_Click(object sender, EventArgs e) {
            if (_currentPresetChanged && !SaveChangedPreset()) return; // cancelled
            OpenPreset();
        }
        #endregion
    }
}
