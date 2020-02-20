using System;
using System.Collections.Generic;
//using System.Drawing;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Diagnostics;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Collections;
using Avalonia.Input;
using Avalonia.Interactivity;

using UpDown      = Avalonia.CustomControls.UpDown;
using PrimeUpDown = Avalonia.CustomControls.PrimeUpDown;
using TextBox     = Avalonia.CustomControls.TextBox2;

namespace Rationals.Explorer
{
    using DS = DrawerSettings;
    using GridDrawer = Rationals.Drawing.GridDrawer;

    public partial class MainWindow
    {
        #region Controls

        // base & prime limit
        PrimeUpDown upDownLimit;
        TextBox textBoxSubgroup;

        // generation
        ComboBox comboBoxDistance;
        UpDown upDownCountLimit;

        // temperament
        Grid gridTemperament;
        Slider sliderTemperament;

        // slope
        TextBox textBoxSlopeOrigin;
        UpDown upDownChainTurns;
        
        // degrees
        //UpDown upDownMinimalStep;
        //UpDown upDownStepSizeCountLimit;

        // ED grids
        TextBox textBoxEDGrids;

        // selection
        TextBox textBoxSelection;

        #endregion

        private bool _settingInternally = false; // no need to parse control value: e.g. if SetSettingsToControls() in progress

        private Vectors.Matrix _subgroupMatrix = null; // used to validate temperament (and selection !!!)
        private TemperamentGridControls _temperamentControls;

        protected void FindDrawerSettingsControls(IControl parent) {
            upDownLimit        = ExpectControl<PrimeUpDown>(parent, "upDownLimit");
            textBoxSubgroup    = ExpectControl<TextBox>    (parent, "textBoxSubgroup");
            comboBoxDistance   = ExpectControl<ComboBox>   (parent, "comboBoxDistance");
            upDownCountLimit   = ExpectControl<UpDown>     (parent, "upDownCountLimit");
            gridTemperament    = ExpectControl<Grid>       (parent, "gridTemperament");
            sliderTemperament  = ExpectControl<Slider>     (parent, "sliderTemperament");
            textBoxSlopeOrigin = ExpectControl<TextBox>    (parent, "textBoxSlopeOrigin");
            upDownChainTurns   = ExpectControl<UpDown>     (parent, "upDownChainTurns");
            //upDownMinimalStep       = ExpectControl<UpDown>(parent, "upDownMinimalStep");
            //upDownStepSizeCountLimit= ExpectControl<UpDown>(parent, "upDownStepSizeCountLimit");
            textBoxEDGrids     = ExpectControl<TextBox>    (parent, "textBoxEDGrids");
            textBoxSelection   = ExpectControl<TextBox>    (parent, "textBoxSelection");

            // Set some initial values
            _settingInternally = true;

            /*
            if (gridTemperament != null) {
                var t1 = new[] { "3/2", "81/80", "6/5" };
                var t2 = new[] { 702f, 0.01f, 333f };
                for (int i = 0; i < 5; ++i) {
                    var t = new Rational.Tempered { rational = Rational.Parse(t1[i % 3]), cents = t2[i % 3] };
                    AddTemperamentRow(t);
                }
            }
            */

            // fill Harmonic distance combo
            comboBoxDistance.Items = Rationals.Utils.HarmonicityNames;
            comboBoxDistance.SelectedIndex = 0;

            _temperamentControls = new TemperamentGridControls(gridTemperament);
            _temperamentControls.Changed += temperamentGrid_Changed;

            _settingInternally = false;
        }

        ///private Vectors.Matrix _subgroupRange = null; // used to validate commas (and selection !!!)

        public void ToggleSelection(Drawing.SomeInterval t) {
            var s = _drawerSettings.selection ?? new Drawing.SomeInterval[] { };
            int count = s.Length;
            s = s.Where(i => !i.Equals(t)).ToArray(); // try to remove
            if (s.Length == count) { // otherwise add
                s = s.Concat(new Drawing.SomeInterval[] { t }).ToArray();
            }
            _drawerSettings.selection = s;

            // Update 'selection' control
            _settingInternally = true;
            textBoxSelection.Text = DS.FormatIntervals(s);
            _settingInternally = false;

            // Update drawer
            _gridDrawer.SetSelection(_drawerSettings.selection);
        }

        /*
         !!! make as class
        private static Avalonia.Color ValidColor(bool valid) {
            return valid ? default(Color) : Color.Pink;
        }
        */

        private void SetValidity(Control control, bool isValid, string tip) {
            //!!!
        }

        /*
        private void ResetSettings() {
            DrawerSettings s = DrawerSettings.Edo12();
            s.rationalCountLimit = 500; // also set default limits

#if DEBUG
            //s.stepMinHarmonicity = _gridDrawer.GetRationalHarmonicity(new Rational(25, 24));
#endif

            _drawerSettings = s;
            UpdateSubgroupMatrix();
        }
        */

        // Set settings to controls
        protected void SetSettingsToControls(DrawerSettings s) {
            _settingInternally = true;
            // base
            upDownLimit.Value = s.limitPrimeIndex;
            textBoxSubgroup.Text = DS.FormatSubgroup(s.subgroup, s.narrows);
            // temperament
            _temperamentControls.SetTemperament(s.temperament);
            sliderTemperament.Value = (int)Math.Round(s.temperamentMeasure * 100);
            // slope
            textBoxSlopeOrigin.Text = DS.FormatRational(s.slopeOrigin);
            upDownChainTurns.Value = s.slopeChainTurns;
            // degrees
            //upDownMinimalStep.Value = s.stepMinHarmonicity;
            //upDownStepSizeCountLimit.Value = s.stepSizeMaxCount;
            // selection
            textBoxSelection.Text = DS.FormatIntervals(s.selection);
            // grids
            textBoxEDGrids.Text = DS.FormatEDGrids(s.edGrids);
            // drawing
            if (!String.IsNullOrEmpty(s.harmonicityName)) {
                comboBoxDistance.SelectedItem = s.harmonicityName;
            }
            upDownCountLimit.Value = s.rationalCountLimit;
            //

            UpdateSubgroupMatrix();
            ValidateGridTemperament(); // validate the whole temperament (out of _settingInternally) -- !!! why out?

            _settingInternally = false;
        }

        // Read settings from controls - used on saving Preset
        protected DrawerSettings GetSettingsFromControls() {
            DrawerSettings s = new DrawerSettings { };

            // subgroup
            if (!String.IsNullOrWhiteSpace(textBoxSubgroup.Text)) {
                string[] subgroupText = DS.SplitSubgroupText(textBoxSubgroup.Text);
                s.subgroup = DS.ParseRationals(subgroupText[0]);
                s.narrows = DS.ParseRationals(subgroupText[1]);
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
            s.temperament = _temperamentControls.GetTemperament();
            s.temperamentMeasure = (float)sliderTemperament.Value * 0.01f;
            // slope
            s.slopeOrigin = Rational.Parse(textBoxSlopeOrigin.Text);
            s.slopeChainTurns = (float)upDownChainTurns.Value;
            // degrees
            //s.stepMinHarmonicity = (float)upDownMinimalStep.Value;
            //s.stepSizeMaxCount = (int)upDownStepSizeCountLimit.Value;
            // selection
            s.selection = DS.ParseIntervals(textBoxSelection.Text);
            // grids
            s.edGrids = DS.ParseEDGrids(textBoxEDGrids.Text);

            return s;
        }

#if false
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
#endif

        #region Base
        private void upDownLimit_ValueChanged(object sender, NumericUpDownValueChangedEventArgs e) {
            if (!_settingInternally) {
                MarkPresetChanged();
                //
                int value = (int)e.NewValue;
                // update current setting
                _drawerSettings.limitPrimeIndex = value;
                UpdateSubgroupMatrix();

                // revalidate temperament
                if (_drawerSettings.temperament != null) {
                    ValidateGridTemperament();
                }

                // update drawer
                _gridDrawer.SetBase(value, _drawerSettings.subgroup, _drawerSettings.narrows);
                InvalidateMainImage();

            }
        }

        private void textBoxSubgroup_TextChanged(object sender, RoutedEventArgs e) {
            string error = null;
            if (!_settingInternally) {
                MarkPresetChanged();
                // parse
                Rational[] subgroup = null;
                Rational[] narrows  = null;
                string[] textSubgroup = DS.SplitSubgroupText(textBoxSubgroup.Text);
                bool emptySubgroup = String.IsNullOrWhiteSpace(textSubgroup[0]);
                bool emptyNarrows  = String.IsNullOrWhiteSpace(textSubgroup[1]);
                if (!emptySubgroup) {
                    subgroup = DS.ParseRationals(textSubgroup[0], '.');
                    if (subgroup == null) {
                        error = "Invalid subgroup format";
                    }
                }
                if (!emptyNarrows) {
                    narrows = DS.ParseRationals(textSubgroup[1], '.');
                    narrows = Rational.ValidateNarrows(narrows);
                    if (narrows == null) {
                        error = "Invalid narrows"; //!!! losing subgroup error
                    }
                }
                if (error == null) {
                    // update current settings
                    _drawerSettings.subgroup = subgroup;
                    _drawerSettings.narrows = narrows;
                    UpdateSubgroupMatrix();
                    // revalidate temperament
                    ValidateGridTemperament();
                    // update drawer
                    _gridDrawer.SetBase(_drawerSettings.limitPrimeIndex, subgroup, narrows);
                    InvalidateMainImage();
                }
            }
            //
            SetValidity(textBoxSubgroup, error == null, error);
            //
            upDownLimit.IsEnabled = _drawerSettings.subgroup == null;
        }
#endregion

#region Generation
        private void comboBoxDistance_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (!_settingInternally) {
                MarkPresetChanged();
                //
                string harmonicityName = (string)comboBoxDistance.SelectedItem;
                // update current setting
                _drawerSettings.harmonicityName = harmonicityName;
                // update drawer
                _gridDrawer.SetGeneration(harmonicityName, _drawerSettings.rationalCountLimit);
                InvalidateMainImage();
            }
        }
        private void upDownCountLimit_ValueChanged(object sender, NumericUpDownValueChangedEventArgs e) {
            if (!_settingInternally) {
                MarkPresetChanged();
                //
                int rationalCountLimit = (int)upDownCountLimit.Value;
                // update current setting
                _drawerSettings.rationalCountLimit = rationalCountLimit;
                // update drawer
                _gridDrawer.SetGeneration(_drawerSettings.harmonicityName, rationalCountLimit);
                InvalidateMainImage();
            }
        }
#endregion

#region Slope
        private void textBoxSlopeOrigin_TextChanged(object sender, RoutedEventArgs e) {
            string error = null;
            if (!_settingInternally) {
                MarkPresetChanged();
                // parse
                Rational up = default(Rational);
                string textUp = textBoxSlopeOrigin.Text;
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
                    _drawerSettings.slopeOrigin = up;
                    // update drawer
                    _gridDrawer.SetSlope(up, _drawerSettings.slopeChainTurns);
                    InvalidateMainImage();
                }
            }
            //
            SetValidity(textBoxSlopeOrigin, error == null, error);
        }
        private void upDownChainTurns_ValueChanged(object sender, NumericUpDownValueChangedEventArgs e) {
            if (!_settingInternally) {
                MarkPresetChanged();
                //
                float chainTurns = (float)upDownChainTurns.Value;
                // update current setting
                _drawerSettings.slopeChainTurns = chainTurns;
                // update drawer
                _gridDrawer.SetSlope(_drawerSettings.slopeOrigin, chainTurns);
                InvalidateMainImage();
            }
        }
        #endregion

#if false
#region Degrees
        private void upDownMinimalStep_ValueChanged(object sender, EventArgs e) {
            if (!_settingInternally) {
                MarkPresetChanged();
                //
                float minimalStep = (float)upDownMinimalStep.Value;
                // update current setting
                _drawerSettings.stepMinHarmonicity = minimalStep;
                // update drawer
                _gridDrawer.SetDegrees(minimalStep, _drawerSettings.stepSizeMaxCount);
                InvalidateMainImage();
            }
        }
        private void upDownStepSizeCountLimit_ValueChanged(object sender, EventArgs e) {
            if (!_settingInternally) {
                MarkPresetChanged();
                //
                int stepSizeCountLimit = (int)upDownStepSizeCountLimit.Value;
                // update current setting
                _drawerSettings.stepSizeMaxCount = stepSizeCountLimit;
                // update drawer
                _gridDrawer.SetDegrees(_drawerSettings.stepMinHarmonicity, stepSizeCountLimit);
                InvalidateMainImage();
            }
        }
        #endregion
#endif

#region ED Grids
        private void textBoxEDGrids_TextChanged(object sender, RoutedEventArgs e) {
            string error = null;
            if (!_settingInternally) {
                MarkPresetChanged();
                // parse
                GridDrawer.EDGrid[] grids = null;
                string textGrids = textBoxEDGrids.Text;
                bool empty = String.IsNullOrWhiteSpace(textGrids);
                if (!empty) {
                    grids = DS.ParseEDGrids(textGrids);
                    if (grids == null) {
                        error = "Invalid format";
                    }
                }
                if (error == null) {
                    // update current setting
                    _drawerSettings.edGrids = grids;
                    // update drawer
                    _gridDrawer.SetEDGrids(_drawerSettings.edGrids);
                    InvalidateMainImage();
                }
            }
            //
            SetValidity(textBoxEDGrids, error == null, error);
        }
#endregion

#region Highlight
        private void textBoxSelection_TextChanged(object sender, RoutedEventArgs e) {
            string error = null;
            if (!_settingInternally) {
                MarkPresetChanged();
                // parse
                Drawing.SomeInterval[] selection = null;
                string textSelection = textBoxSelection.Text;
                bool empty = String.IsNullOrWhiteSpace(textSelection);
                if (!empty) {
                    selection = DS.ParseIntervals(textSelection);
                    if (selection == null) {
                        error = "Invalid format";
                    }
                }
                if (error == null) {
                    // update current setting
                    _drawerSettings.selection = selection;
                    // update drawer
                    _gridDrawer.SetSelection(_drawerSettings.selection);
                    InvalidateMainImage();
                }
            }
            //
            SetValidity(textBoxSelection, error == null, error);
        }
#endregion

#region Temperament
        private void UpdateSubgroupMatrix() {
            DrawerSettings s = _drawerSettings;
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
            _subgroupMatrix = new Vectors.Matrix(subgroup, makeDiagonal: true);
        }
        /*
        //!!! we might also check Selection rationals by this subgroup range
        private bool AreRationalsInSubgroupRange(Rational[] rs) {
            if (rs == null) return true;
            for (int i = 0; i < rs.Length; ++i) {
                int[] coords = _subgroupMatrix.FindCoordinates(rs[i]);
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
        */
        /*
        private void textBoxStickCommas_TextChanged(object sender, EventArgs e) {
            string error = null;
            Rational[] commas = null;
            if (_settingInternally) {
                commas = _drawerSettings.stickCommas;
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
                    _drawerSettings.stickCommas = commas; // parced but may be invalid
                    // update drawer
                    _gridDrawer.SetTemperament(commas);
                    InvalidateMainImage();
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
        private void sliderTemperament_ValueChanged(object sender, RoutedEventArgs e) {
            if (!_settingInternally) {
                MarkPresetChanged();
                //
                float value = (float)sliderTemperament.Value * 0.01f;
                // update current setting
                _drawerSettings.temperamentMeasure = value;
                // update drawer
                _gridDrawer.SetTemperamentMeasure(value);
                InvalidateMainImage();
            }
        }

        private void temperamentGrid_Changed()
        {
            MarkPresetChanged();

            // update current settings
            UpdateTemperamentFromGrid();
            ValidateGridTemperament(); // validate grid

            // update drawer
            _gridDrawer.SetTemperament(_drawerSettings.temperament); // GridDrawer also validates its temperament values
            InvalidateMainImage();
        }

        private void buttonAdd_Click(object sender, RoutedEventArgs e) {
            var t = new Rational.Tempered { }; // default values
            _temperamentControls.AddRow(t, focus: true);

            // just mark grid as incomplete
            UpdateTemperamentFromGrid();
            ValidateGridTemperament();
        }

        private void UpdateTemperamentFromGrid() {
            _drawerSettings.temperament = _temperamentControls.GetTemperament();
        }

        private void ValidateGridTemperament() {
            Rational.Tempered[] ts = _drawerSettings.temperament; // it should be already updated from grid
            if (ts == null) return;

            string[] errors = Vectors.GetTemperamentErrors(ts, _subgroupMatrix);

            for (int i = 0; i < ts.Length; ++i) {
                _temperamentControls.SetRationalError(i, errors[i]);
            }
        }

#endregion

    }
}

