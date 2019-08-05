using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.IO;

namespace Rationals.Forms {
    using GridDrawer = Drawing.GridDrawer;
    using TypedGridView = GridView.TypedGridView;

    public partial class ToolsForm : Form
    {
        private struct DrawerSettings {
            // primes
            public int limitPrimeIndex; // 0,1,2,..
            // or
            public Rational[] subgroup; // e.g. {3, 5, 7} (Bohlen-Pierce), {2, 3, 7/5},.. https://en.xen.wiki/w/Just_intonation_subgroups
            public Rational[] narrows; // "narrow" prime tips for _narrowPrimes

            // generating items
            public string harmonicityName; // null for some default
            public int rationalCountLimit; // -1 for unlimited

            // slope
            public Rational slopeOrigin; // starting point to define slope
            public float slopeChainTurns; // chain turn count to "slope origin" point. set an integer for vertical.

            // temperament
            public Rational.Tempered[] temperament;
            public float temperamentMeasure; // 0..1

            // degrees
            public float stepMinHarmonicity;
            public int stepSizeMaxCount; // e.g. 2 for kind of MOS

            // grids
            public GridDrawer.EDGrid[] edGrids;

            // selection
            public Drawing.SomeInterval[] selection;

            // default settings
            public static DrawerSettings Edo12() {
                var s = new DrawerSettings();
                //
                s.limitPrimeIndex = 2; // 5-limit
                //
                s.slopeOrigin = new Rational(3, 2); // 5th
                s.slopeChainTurns = 2;
                //
                s.temperament = new[] {
#if DEBUG
                    new Rational.Tempered { rational = new Rational(81, 80), cents = 0 },
#endif
                };
                //
                s.edGrids = new[] {
                    new GridDrawer.EDGrid { stepCount = 12, baseInterval = Rational.Two }
                };
                //
                return s;
            }
        }

        private MainForm _mainForm;
        private GridDrawer _gridDrawer;
        private DrawerSettings _currentSettings;

        private bool _settingInternally = false; // no need to parse control value: e.g. if SetSettingsToControls() in progress

        private Vectors.Matrix _subgroupRange = null; // used to validate commas (and selection !!!)

        public ToolsForm(MainForm mainForm, GridDrawer gridDrawer) {
            _mainForm = mainForm;
            _gridDrawer = gridDrawer;

            //!!! use TableLayoutControl
            InitializeComponent();

            InitTooltips();

            comboBoxDistance.Items.AddRange(Rationals.Utils.HarmonicityNames); // fill Harmonicity combo

            gridTemperament.SetColumnType(0, TypedGridView.ColumnType.Rational); // !!! add this type property to custom control
            gridTemperament.SetColumnType(1, TypedGridView.ColumnType.Float);

            upDownMinimalStep.GetHarmonicity = _gridDrawer.GetRationalHarmonicity;
            upDownMinimalStep.FindRational = _gridDrawer.FindRationalByHarmonicity;

            // Load previous or set default preset settings
            bool presetLoaded = LoadAppSettings();
            if (!presetLoaded) {
                ResetSettings();
                SetSettingsToControls();
                UpdateDrawerFully();
                _mainForm.Invalidate();
            }
        }

        private void UpdateDrawerFully() {
            DrawerSettings s = _currentSettings;
            // base
            _gridDrawer.SetBase(s.limitPrimeIndex, s.subgroup, s.narrows);
            _gridDrawer.SetGeneration(s.harmonicityName, s.rationalCountLimit);
            // temperament
            _gridDrawer.SetTemperament(s.temperament);
            _gridDrawer.SetTemperamentMeasure(s.temperamentMeasure);
            //degrees
            _gridDrawer.SetDegrees(s.stepMinHarmonicity, s.stepSizeMaxCount);
            // slope
            _gridDrawer.SetSlope(s.slopeOrigin, s.slopeChainTurns);
            // view
            _gridDrawer.SetEDGrids(s.edGrids);
            _gridDrawer.SetSelection(s.selection);
        }

        public void ShowInfo(string text) {
            textBoxInfo.Text = text;
        }

        public void ToggleSelection(Drawing.SomeInterval t) {
            Drawing.SomeInterval[] s = _currentSettings.selection;
            if (s == null) s = new Drawing.SomeInterval[] { };
            int len = s.Length;
            int i = 0;
            for (i = 0; i < len; ++i) {
                if (s[i].Equals(t)) break;
            }
            if (i < len) {
                Array.Copy(s, i + 1, s, i, len - i - 1);
                Array.Resize(ref s, len - 1);
            } else {
                Array.Resize(ref s, len + 1);
                s[len] = t;
            }
            _currentSettings.selection = s;

            // Update 'selection' control
            _settingInternally = true;
            textBoxSelection.Text = FormatTempered(s);
            _settingInternally = false;

            // Update main drawer
            _gridDrawer.SetSelection(_currentSettings.selection);
            _mainForm.Invalidate();
        }

        private static Color ValidColor(bool valid) {
            return valid ? default(Color) : Color.Pink;
        }

        private void ResetSettings() {
            DrawerSettings s = DrawerSettings.Edo12();
            s.rationalCountLimit = 500; // also set default limits

#if DEBUG
            s.stepMinHarmonicity = _gridDrawer.GetRationalHarmonicity(new Rational(25, 24));
#endif

            _currentSettings = s;
            UpdateSubgroupRange();
        }

        // Set settings to controls
        private void SetSettingsToControls() {
            DrawerSettings s = _currentSettings;
            _settingInternally = true;
            // base
            upDownLimit.Value = s.limitPrimeIndex;
            textBoxSubgroup.Text = FormatSubgroup(s.subgroup, s.narrows);
            // temperament
            SetTemperamentToGrid(s.temperament);
            sliderTemperament.Value = (int)Math.Round(s.temperamentMeasure * 100);
            // slope
            textBoxUp.Text = FormatRational(s.slopeOrigin);
            upDownChainTurns.Value = upDownChainTurns.ClampValue((decimal)s.slopeChainTurns);
            // degrees
            upDownMinimalStep.Value = upDownMinimalStep.ClampValue((decimal)s.stepMinHarmonicity);
            upDownStepSizeCountLimit.Value = upDownStepSizeCountLimit.ClampValue(s.stepSizeMaxCount);
            // selection
            textBoxSelection.Text = FormatTempered(s.selection);
            // grids
            textBoxGrids.Text = FormatGrids(s.edGrids);
            // drawing
            comboBoxDistance.SelectedItem = s.harmonicityName ?? Rationals.Utils.HarmonicityNames[0];
            upDownCountLimit.Value = upDownCountLimit.ClampValue(s.rationalCountLimit);
            //
            _settingInternally = false;

            // validate the whole temperament (out of _settingInternally)
            ValidateGridTemperament();
        }

        // Read settings from controls - used on saving Preset
        private DrawerSettings GetSettingsFromControls() {
            var s = new DrawerSettings();
            // subgroup
            if (!String.IsNullOrWhiteSpace(textBoxSubgroup.Text)) {
                string[] subgroupText = SplitSubgroupText(textBoxSubgroup.Text);
                s.subgroup = ParseRationals(subgroupText[0]);
                s.narrows = ParseRationals(subgroupText[1]);
                s.narrows = Rational.ValidateNarrows(s.narrows);
            }
            // base & prime limit
            if (s.subgroup == null) {
                s.limitPrimeIndex = (int)upDownLimit.Value;
            }
            // generation
            s.harmonicityName = (string)comboBoxDistance.SelectedItem;
            s.rationalCountLimit = (int)upDownCountLimit.Value;
            // temperament
            s.temperament = GetTemperamentFromGrid();
            s.temperamentMeasure = sliderTemperament.Value * 0.01f;
            // slope
            s.slopeOrigin = Rational.Parse(textBoxUp.Text);
            s.slopeChainTurns = (float)upDownChainTurns.Value;
            // degrees
            s.stepMinHarmonicity = (float)upDownMinimalStep.Value;
            s.stepSizeMaxCount = (int)upDownStepSizeCountLimit.Value;
            // selection
            s.selection = ParseTempered(textBoxSelection.Text);
            // grids
            string grids = textBoxGrids.Text;
            if (!String.IsNullOrWhiteSpace(grids)) {
                s.edGrids = ParseGrids(grids);
            }
            //
            return s;
        }

        private static string FormatRational(Rational r) {
            if (r.IsDefault()) return "";
            return r.FormatFraction();
        }

        #region ToolTips
        private Dictionary<Control, TooltipText> _tooltips = new Dictionary<Control, TooltipText>();
        private struct TooltipText {
            public string Tag;
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
        private void AddTooltipText(string tag, string text) {
            Control control = FindControl(tag, this);
            if (control == null) {
                //!!! no control with such Tag found
            } else {
                _tooltips[control] = new TooltipText {
                    Tag = tag,
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
            if (!String.IsNullOrEmpty(toolTip.GetToolTip(control))) return; // probably error tooltip already shown
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

        #region Base
        private void upDownLimit_ValueChanged(object sender, EventArgs e) {
            if (!_settingInternally) {
                MarkPresetChanged();
                //
                int value = (int)upDownLimit.Value;
                // update current setting
                _currentSettings.limitPrimeIndex = value;
                UpdateSubgroupRange();
                // update drawer
                _gridDrawer.SetBase(value, _currentSettings.subgroup, _currentSettings.narrows);
                _mainForm.Invalidate();
            }

            // revalidate commas
            if (_currentSettings.temperament != null) {
                RevalidateTemperation();
            }
        }

        private static string JoinRationals(Rational[] rs, string separator = ".") {
            if (rs == null) return "";
            return String.Join(separator, rs.Select(r => r.FormatFraction()));
        }
        private static string FormatSubgroup(Rational[] subgroup, Rational[] narrows) {
            string result = "";
            if (subgroup != null) {
                result += JoinRationals(subgroup, ".");
            }
            if (narrows != null) {
                if (result != "") result += " ";
                result += "(" + JoinRationals(narrows, ".") + ")";
            }
            return result;
        }
        private static int[] ParseIntegers(string text, char separator = ' ') {
            if (String.IsNullOrWhiteSpace(text)) return null;
            string[] parts = text.Split(new[]{ separator }, StringSplitOptions.RemoveEmptyEntries);
            int[] result = new int[parts.Length];
            for (int i = 0; i < parts.Length; ++i) {
                if (!int.TryParse(parts[i], out result[i])) {
                    return null; // null if invalid
                }
            }
            return result;
        }
        private static Rational[] ParseRationals(string text, char separator = '.') {
            if (String.IsNullOrWhiteSpace(text)) return null;
            string[] parts = text.Split(separator);
            Rational[] result = new Rational[parts.Length];
            for (int i = 0; i < parts.Length; ++i) {
                result[i] = Rational.Parse(parts[i]);
                if (result[i].IsDefault()) return null; // null if invalid
            }
            return result;
        }
        private static string[] SplitSubgroupText(string subgroupText) { // 2.3.7/5 (7/5)
            var result = new string[] { null, null };
            if (String.IsNullOrWhiteSpace(subgroupText)) return result;
            string[] parts = subgroupText.Split('(', ')');
            if (!String.IsNullOrWhiteSpace(parts[0])) {
                result[0] = parts[0];
            }
            if (parts.Length > 1 && !String.IsNullOrWhiteSpace(parts[1])) {
                result[1] = parts[1];
            }
            return result;
        }
        private void textBoxSubgroup_TextChanged(object sender, EventArgs e) {
            string error = null;
            if (!_settingInternally) {
                MarkPresetChanged();
                // parse
                Rational[] subgroup = null;
                Rational[] narrows = null;
                string[] textSubgroup = SplitSubgroupText(textBoxSubgroup.Text);
                bool emptySubgroup = String.IsNullOrWhiteSpace(textSubgroup[0]);
                bool emptyNarrows = String.IsNullOrWhiteSpace(textSubgroup[1]);
                if (!emptySubgroup) {
                    subgroup = ParseRationals(textSubgroup[0], '.');
                    if (subgroup == null) {
                        error = "Invalid subgroup format";
                    }
                }
                if (!emptyNarrows) {
                    narrows = ParseRationals(textSubgroup[1], '.');
                    narrows = Rational.ValidateNarrows(narrows);
                    if (narrows == null) {
                        error = "Invalid narrows"; //!!! losing subgroup error
                    }
                }
                if (error == null) {
                    // update current settings
                    _currentSettings.subgroup = subgroup;
                    _currentSettings.narrows = narrows;
                    UpdateSubgroupRange();
                    // update drawer
                    _gridDrawer.SetBase(_currentSettings.limitPrimeIndex, subgroup, narrows);
                    _mainForm.Invalidate();
                }
            }
            //
            textBoxSubgroup.BackColor = ValidColor(error == null);
            toolTip.SetToolTip(textBoxSubgroup, error);
            //
            upDownLimit.Enabled = _currentSettings.subgroup == null;

            // revalidate temperament
            if (_currentSettings.temperament != null) {
                RevalidateTemperation();
            }
        }
        #endregion

        #region Generation
        private void comboBoxDistance_TextChanged(object sender, EventArgs e) {
            if (!_settingInternally) {
                MarkPresetChanged();
                //
                string harmonicityName = (string)comboBoxDistance.SelectedItem;
                // update current setting
                _currentSettings.harmonicityName = harmonicityName;
                // update drawer
                _gridDrawer.SetGeneration(harmonicityName, _currentSettings.rationalCountLimit);
                _mainForm.Invalidate();
            }
        }
        private void upDownCountLimit_ValueChanged(object sender, EventArgs e) {
            if (!_settingInternally) {
                MarkPresetChanged();
                //
                int rationalCountLimit = (int)upDownCountLimit.Value;
                // update current setting
                _currentSettings.rationalCountLimit = rationalCountLimit;
                // update drawer
                _gridDrawer.SetGeneration(_currentSettings.harmonicityName, rationalCountLimit);
                _mainForm.Invalidate();
            }
        }
        #endregion

        #region Slope
        private void textBoxUp_TextChanged(object sender, EventArgs e) {
            string error = null;
            if (!_settingInternally) {
                MarkPresetChanged();
                // parse
                Rational up = default(Rational);
                string textUp = textBoxUp.Text;
                bool empty = String.IsNullOrWhiteSpace(textUp);
                if (empty) {
                    up = new Rational(3, 2); // default
                } else {
                    up = Rational.Parse(textUp);
                    if (up.IsDefault()) {
                        error = "Invalid format";
                    } else if (up.Equals(Rational.One)) {
                        error = "No slope for 1/1";
                    }
                }
                if (error == null) {
                    // update current setting
                    _currentSettings.slopeOrigin = up;
                    // update drawer
                    _gridDrawer.SetSlope(up, _currentSettings.slopeChainTurns);
                    _mainForm.Invalidate();
                }
            }
            //
            textBoxUp.BackColor = ValidColor(error == null);
            toolTip.SetToolTip(textBoxUp, error);
        }
        private void upDownChainTurns_ValueChanged(object sender, EventArgs e) {
            if (!_settingInternally) {
                MarkPresetChanged();
                //
                float chainTurns = (float)upDownChainTurns.Value;
                // update current setting
                _currentSettings.slopeChainTurns = chainTurns;
                // update drawer
                _gridDrawer.SetSlope(_currentSettings.slopeOrigin, chainTurns);
                _mainForm.Invalidate();
            }
        }
        #endregion

        #region Degrees
        private void upDownMinimalStep_ValueChanged(object sender, EventArgs e) {
            if (!_settingInternally) {
                MarkPresetChanged();
                //
                float minimalStep = (float)upDownMinimalStep.Value;
                // update current setting
                _currentSettings.stepMinHarmonicity = minimalStep;
                // update drawer
                _gridDrawer.SetDegrees(minimalStep, _currentSettings.stepSizeMaxCount);
                _mainForm.Invalidate();
            }
        }
        private void upDownStepSizeCountLimit_ValueChanged(object sender, EventArgs e) {
            if (!_settingInternally) {
                MarkPresetChanged();
                //
                int stepSizeCountLimit = (int)upDownStepSizeCountLimit.Value;
                // update current setting
                _currentSettings.stepSizeMaxCount = stepSizeCountLimit;
                // update drawer
                _gridDrawer.SetDegrees(_currentSettings.stepMinHarmonicity, stepSizeCountLimit);
                _mainForm.Invalidate();
            }
        }
        #endregion

        #region ED Grids
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
            string[] parts = grids.ToLower().Split(',', ';');
            var result = new GridDrawer.EDGrid[parts.Length];
            for (int i = 0; i < parts.Length; ++i) {
                string[] ps = parts[i].Split(new[] { "ed", "-", " " }, StringSplitOptions.RemoveEmptyEntries);
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
            string error = null;
            if (!_settingInternally) {
                MarkPresetChanged();
                // parse
                GridDrawer.EDGrid[] grids = null;
                string textGrids = textBoxGrids.Text;
                bool empty = String.IsNullOrWhiteSpace(textGrids);
                if (!empty) {
                    grids = ParseGrids(textGrids);
                    if (grids == null) {
                        error = "Invalid format";
                    }
                }
                if (error == null) {
                    // update current setting
                    _currentSettings.edGrids = grids;
                    // update drawer
                    _gridDrawer.SetEDGrids(_currentSettings.edGrids);
                    _mainForm.Invalidate();
                }
            }
            //
            textBoxGrids.BackColor = ValidColor(error == null);
            toolTip.SetToolTip(textBoxGrids, error);
        }
        #endregion

        #region Highlight
        private static string FormatTempered(Drawing.SomeInterval[] ts) {
            if (ts == null) return "";
            return String.Join(", ", ts.Select(t => t.ToString()));
        }
        private Drawing.SomeInterval[] ParseTempered(string textTempered) {
            if (String.IsNullOrWhiteSpace(textTempered)) return null;
            string[] parts = textTempered.Trim().ToLower().Split(";, ".ToArray(), StringSplitOptions.RemoveEmptyEntries);
            var tempered = new Drawing.SomeInterval[parts.Length];
            for (int i = 0; i < parts.Length; ++i) {
                var t = Drawing.SomeInterval.Parse(parts[i]);
                if (t == null) return null; // invalid format
                tempered[i] = t;
            }
            return tempered;
        }
        private void textBoxSelection_TextChanged(object sender, EventArgs e) {
            string error = null;
            if (!_settingInternally) {
                MarkPresetChanged();
                // parse
                Drawing.SomeInterval[] selection = null;
                string textSelection = textBoxSelection.Text;
                bool empty = String.IsNullOrWhiteSpace(textSelection);
                if (!empty) {
                    selection = ParseTempered(textSelection);
                    if (selection == null) {
                        error = "Invalid format";
                    }
                }
                if (error == null) {
                    // update current setting
                    _currentSettings.selection = selection;
                    // update drawer
                    _gridDrawer.SetSelection(_currentSettings.selection);
                    _mainForm.Invalidate();
                }
            }
            //
            textBoxSelection.BackColor = ValidColor(error == null);
            toolTip.SetToolTip(textBoxSelection, error);
        }
        #endregion

        #region Commas
        private void UpdateSubgroupRange() {
            DrawerSettings s = _currentSettings;
            Rational[] subgroup;
            if (s.subgroup != null) {
                subgroup = s.subgroup;
            } else {
                int count = s.limitPrimeIndex + 1;
                subgroup = new Rational[count];
                for (int i = 0; i < count; ++i) {
                    subgroup[i] = Rational.Prime(i);
                }
            }
            _subgroupRange = new Vectors.Matrix(subgroup, makeDiagonal: true);
        }
        //!!! we might also check Selection rationals by this subgroup range
        private bool AreRationalsInSubgroupRange(Rational[] rs) {
            if (rs == null) return true;
            for (int i = 0; i < rs.Length; ++i) {
                int[] coords = _subgroupRange.FindCoordinates(rs[i]);
                if (coords == null) return false;
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
            return commas;
        }
        /*
        private void textBoxStickCommas_TextChanged(object sender, EventArgs e) {
            string error = null;
            Rational[] commas = null;
            if (_settingInternally) {
                commas = _currentSettings.stickCommas;
            } else { // edited by user
                MarkPresetChanged();
                // parse
                string commasText = textBoxStickCommas.Text;
                bool empty = String.IsNullOrWhiteSpace(commasText);
                if (!empty) {
                    commas = ParseCommas(commasText);
                    if (commas == null) {
                        error = "Invalid format";
                    }
                }
                if (error == null) {
                    // update current setting
                    _currentSettings.stickCommas = commas; // parced but may be invalid
                    // update drawer
                    _gridDrawer.SetTemperament(commas);
                    _mainForm.Invalidate();
                }
            }
            // revalidate
            if (!AreRationalsInSubgroupRange(commas)) {
                error = "Out of JI range";
            }
            //
            textBoxStickCommas.BackColor = ValidColor(error == null);
            toolTip.SetToolTip(textBoxStickCommas, error);
        }
        */
        private void RevalidateTemperation() { // called when updated Subgroup range
            /*
            string error = null;
            Rational[] commas = _currentSettings.stickCommas;
            if (!AreRationalsInSubgroupRange(commas)) {
                error = "Out of JI range";
            }
            //
            textBoxStickCommas.BackColor = ValidColor(error == null);
            toolTip.SetToolTip(textBoxStickCommas, error);
            */
        }
        private void sliderTemperament_ValueChanged(object sender, EventArgs e) {
            if (!_settingInternally) {
                MarkPresetChanged();
                //
                float value = sliderTemperament.Value * 0.01f;
                // update current setting
                _currentSettings.temperamentMeasure = value;
                // update drawer
                _gridDrawer.SetTemperamentMeasure(value);
                _mainForm.Invalidate();
            }
        }
        #endregion

        #region Presets
        // Serialization
        private void SavePreset(XmlWriter w) {
            DrawerSettings s = GetSettingsFromControls();
            w.WriteElementString("limitPrime", s.subgroup != null ? "" : Rationals.Utils.GetPrime(s.limitPrimeIndex).ToString());
            w.WriteElementString("subgroup", JoinRationals(s.subgroup, "."));
            w.WriteElementString("narrows", JoinRationals(s.narrows, "."));
            //
            w.WriteElementString("harmonicityName", s.harmonicityName);
            w.WriteElementString("rationalCountLimit", s.rationalCountLimit.ToString());
            //
            w.WriteElementString("slopeOrigin", FormatRational(s.slopeOrigin));
            w.WriteElementString("slopeChainTurns", s.slopeChainTurns.ToString());
            //
            w.WriteElementString("minimalStep", s.stepMinHarmonicity.ToString());
            w.WriteElementString("stepSizeCountLimit", s.stepSizeMaxCount.ToString());
            //
            w.WriteElementString("selection", FormatTempered(s.selection));
            if (s.temperament != null) {
                foreach (Rational.Tempered t in s.temperament) {
                    w.WriteStartElement("temper");
                    w.WriteAttributeString("rational", t.rational.FormatFraction());
                    w.WriteAttributeString("cents", t.cents.ToString());
                    w.WriteEndElement();
                }
            }
            w.WriteElementString("temperamentMeasure", s.temperamentMeasure.ToString());
            w.WriteElementString("edGrids", FormatGrids(s.edGrids));
            //
            w.WriteStartElement("viewport");
            _mainForm.SavePresetViewport(w);
            w.WriteEndElement();
        }
        private void LoadPreset(XmlTextReader r) {
            var s = new DrawerSettings { };
            var ts = new List<Rational.Tempered>();
            while (r.Read()) {
                if (r.NodeType == XmlNodeType.Element) {
                    switch (r.Name) {
                        case "limitPrime": {
                                Rational limitPrime = Rational.Parse(r.ReadElementContentAsString());
                                if (!limitPrime.IsDefault()) {
                                    s.limitPrimeIndex = limitPrime.GetPowerCount() - 1;
                                }
                                break;
                            }
                        case "subgroup": {
                                s.subgroup = ParseRationals(r.ReadElementContentAsString());
                                break;
                            }
                        case "narrows": {
                                s.narrows = ParseRationals(r.ReadElementContentAsString());
                                s.narrows = Rational.ValidateNarrows(s.narrows);
                                break;
                            }
                        //
                        case "harmonicityName":     s.harmonicityName       = r.ReadElementContentAsString();   break;
                        case "rationalCountLimit":  s.rationalCountLimit    = r.ReadElementContentAsInt();      break;
                        //
                        case "slopeOrigin":         s.slopeOrigin           = Rational.Parse(r.ReadElementContentAsString()); break;
                        case "slopeChainTurns":     s.slopeChainTurns       = r.ReadElementContentAsFloat();    break;
                        //
                        case "minimalStep":         s.stepMinHarmonicity    = r.ReadElementContentAsFloat();    break;
                        case "stepSizeCountLimit":  s.stepSizeMaxCount      = r.ReadElementContentAsInt();      break;
                        //
                        case "selection":           s.selection = ParseTempered(r.ReadElementContentAsString()); break;
                        case "temper": {
                                var t = new Rational.Tempered { };
                                t.rational = Rational.Parse(r.GetAttribute("rational"));
                                float.TryParse(r.GetAttribute("cents"), out t.cents);
                                ts.Add(t);
                                break;
                            }
                        case "temperamentMeasure":  s.temperamentMeasure    = r.ReadElementContentAsFloat();    break;
                        case "edGrids":             s.edGrids = ParseGrids(r.ReadElementContentAsString());     break;
                        //
                        case "viewport": _mainForm.LoadPresetViewport(r); break;
                    }
                }
            }
            // set settings
            if (ts.Count > 0) s.temperament = ts.ToArray();
            _currentSettings = s;
            UpdateSubgroupRange();
            SetSettingsToControls();
            UpdateDrawerFully();
            _mainForm.Invalidate();
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
                //using (XmlReader r = XmlReader.Create(presetPath)) {
                using (XmlTextReader r = new XmlTextReader(presetPath)) {
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
                // Windows
                w.WriteElementString("mainWindowState", ((int)_mainForm.WindowState).ToString());
                w.WriteElementString("mainWindowLocation", FormatFormLocation(_mainForm));
                w.WriteElementString("toolsWindowLocation", FormatFormLocation(this));
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
                //using (XmlReader r = XmlReader.Create(_appSettingsPath)) {
                using (XmlTextReader r = new XmlTextReader(_appSettingsPath)) {
                    while (r.Read()) {
                        if (r.NodeType == XmlNodeType.Element) {
                            switch (r.Name) {
                                case "mainWindowState":
                                    _mainForm.WindowState = (FormWindowState)r.ReadElementContentAsInt();
                                    break;
                                case "mainWindowLocation":
                                    SetFormLocation(_mainForm, r.ReadElementContentAsString());
                                    break;
                                case "toolsWindowLocation":
                                    SetFormLocation(this, r.ReadElementContentAsString());
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
                                    LoadPreset(r);
                                    presetLoaded = true;
                                    break;
                            }
                        }
                    }
                }
            } catch (FileNotFoundException) {
                return false;
            } catch (XmlException) {
                //!!! log error
                return false;
            //} catch (Exception ex) {
            //    Console.Error.WriteLine("LoadAppSettings error: " + ex.Message);
            //    return false;
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
        private string FormatFormLocation(Form form) {
            bool normal = form.WindowState == FormWindowState.Normal;
            Point p = normal ? form.Location : form.RestoreBounds.Location;
            Size  s = normal ? form.Size     : form.RestoreBounds.Size;
            return String.Format("{0} {1} {2} {3}",
                p.X, p.Y, s.Width, s.Height
            );
        }
        private void SetFormLocation(Form form, string value) {
            if (value == null) return;
            int[] ns = ParseIntegers(value);
            if (ns == null || ns.Length != 4) return;
            // propagate
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(ns[0], ns[1]);
            form.Size     = new Size (ns[2], ns[3]);
        }
        private void ReadToolsFormSettings(string value) {
            if (value == null) return;
            int[] ns = ParseIntegers(value);
            if (ns == null || ns.Length != 4) return;
            // propagate to form
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(ns[0], ns[1]);
            this.Size     = new Size (ns[2], ns[3]);
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
            _currentPresetPath = null;
            MarkPresetChanged(false);
            ResetSettings();
            SetSettingsToControls();
            UpdateDrawerFully();
            _mainForm.ResetViewport();
            _mainForm.Invalidate();
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
                        dialog.FileName = Path.GetFileName(_currentPresetPath);
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

        #region Menu Image
        private void menuImageShow_Click(object sender, EventArgs e) {
            _mainForm.SaveImage();
        }
        private void menuImageSaveAs_Click(object sender, EventArgs e) {
            string filePath = "";
            using (var dialog = new SaveFileDialog {
                Title = "Save Image As",
                Filter = "Svg files|*.svg|Png files|*.png|All files|*.*",
            }) {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                filePath = dialog.FileName;
            }
            _mainForm.SaveImage(filePath);
        }
        #endregion

        #region Temperament

        private void gridTemperation_CellValueChanged(object sender, DataGridViewCellEventArgs e) {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return; // ignore headers
            if (_settingInternally) return; // we should call validate later

            SetDefaultCents(e.RowIndex, e.ColumnIndex == 0);

            OnTemperamentUserChange();
        }

        private void OnTemperamentUserChange()
        {
            MarkPresetChanged();

            Rational.Tempered[] temperament = GetTemperamentFromGrid();

            // update current settings
            _currentSettings.temperament = temperament;

            // update drawer
            _gridDrawer.SetTemperament(temperament);
            _mainForm.Invalidate();

            // validate grid
            ValidateGridTemperament();
        }

        private void SetTemperamentToGrid(Rational.Tempered[] temperament) {
            gridTemperament.Rows.Clear();
            if (temperament == null) return;
            gridTemperament.Rows.Add(temperament.Length); // ??? row added handled?
            for (int i = 0; i < temperament.Length; ++i) {
                DataGridViewRow row = gridTemperament.Rows[i];
                Rational.Tempered t = temperament[i];
                TypedGridView.SetCellTypedValue(row.Cells[0], t.rational);
                TypedGridView.SetCellTypedValue(row.Cells[1], t.cents);
            }
        }
        private Rational.Tempered[] GetTemperamentFromGrid() {
            var temperament = new List<Rational.Tempered>();
            foreach (DataGridViewRow row in gridTemperament.Rows) {
                if (row.IsNewRow) continue;
                var t = new Rational.Tempered { };
                t.rational = TypedGridView.GetCellTypedValue<Rational>(row.Cells[0]);
                t.cents    = TypedGridView.GetCellTypedValue<float   >(row.Cells[1]);
                temperament.Add(t);
            }
            if (temperament.Count == 0) return null;
            return temperament.ToArray();
        }
        private void ValidateGridTemperament() {
            // like in GridDrawer.ValidateTemperament
            var temperament = GetTemperamentFromGrid();
            if (temperament == null) return; // empty temperament grid

            Rational[] indep = new Rational[temperament.Length]; // independent intervals
            int indepSize = 0;

            for (int i = 0; i < temperament.Length; ++i) {
                Rational r = temperament[i].rational;
                DataGridViewRow row = gridTemperament.Rows[i];
                string error = null;
                if (r.IsDefault()) {
                    error = "Invalid rational";
                } else if (_subgroupRange.FindCoordinates(r) == null) {
                    error = "Out of JI range";
                } else {
                    if (indepSize > 0) {
                        var m = new Vectors.Matrix(indep, -1, indepSize, makeDiagonal: true);
                        var coords = m.FindRationalCoordinates(r);
                        if (coords != null) {
                            error = "";
                            for (int j = 0; j < coords.Length; ++j) {
                                if (coords[j].sign != 0) {
                                    error += String.Format(" * {0}^{1}", indep[j].FormatFraction(), coords[j].FormatFraction());
                                }
                            }
                            error = "Dependend: " + error.Substring(2);
                        }
                    }
                    indep[indepSize++] = r;
                }
                row.Cells[0].ErrorText = error;
            }
        }

        private void gridTemperament_UserAddedRow(object sender, DataGridViewRowEventArgs e) {
        }

        private void gridTemperament_UserDeletedRow(object sender, DataGridViewRowEventArgs e) {
            OnTemperamentUserChange();
        }
        private void gridTemperament_DragDrop(object sender, DragEventArgs e) {
            OnTemperamentUserChange();
        }

        private void gridTemperament_CellEndEdit(object sender, DataGridViewCellEventArgs e) {
            //CheckDefaultCents(e.RowIndex);
        }

        private void SetDefaultCents(int rowIndex, bool force) {
            // set pure interval cents by default
            if (_settingInternally) throw new Exception();
            var row = gridTemperament.Rows[rowIndex];
            string textCents = Convert.ToString(row.Cells[1].FormattedValue);
            if (force || String.IsNullOrWhiteSpace(textCents)) {
                Rational r = TypedGridView.GetCellTypedValue<Rational>(row.Cells[0]);
                if (!r.IsDefault()) {
                    TypedGridView.SetCellTypedValue(row.Cells[1], (float)r.ToCents());
                }
            }
        }
        #endregion

    }
}

