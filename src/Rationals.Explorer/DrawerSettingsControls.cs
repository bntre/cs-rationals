using System;
using System.Collections.Generic;
//using System.Drawing;
using System.Linq;
using System.Diagnostics;

using Avalonia;
using Avalonia.Controls;
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
        //UpDown upDownDegreeCount;
        UpDown upDownDegreeThreshold;

        // ED grids
        TextBox textBoxEDGrids;

        // selection
        TextBox textBoxSelection;

        #endregion

        private bool _settingInternally = false; // no need to parse control value: e.g. if SetSettingsToControls() in progress

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
            //upDownDegreeCount  = ExpectControl<UpDown>     (parent, "upDownDegreeCount");
            upDownDegreeThreshold = ExpectControl<UpDown>  (parent, "upDownDegreeThreshold");
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

        public void ToggleSelection(SomeInterval t) {
            var s = _drawerSettings.selection ?? new SomeInterval[] { };
            int count = s.Length;
            s = s.Where(i => !i.Equals(t)).ToArray(); // try to remove
            if (s.Length == count) { // otherwise add
                s = s.Concat(new SomeInterval[] { t }).ToArray();
            }
            _drawerSettings.selection = s;

            // Update 'selection' control
            _settingInternally = true;
            textBoxSelection.Text = DS.FormatIntervals(s);
            _settingInternally = false;

            // Update drawer
            _gridDrawer.SetSelection(_drawerSettings.selection);
        }

        static public void SetControlTip(Control control, string tip, string error) {
            if (error != null) {
                control.Classes.Add("error");
                // update tooltip and opened tooltip popup
                if (tip != null) error += "\n" + tip;
                ToolTip.SetTip(control, error);
                if (ToolTip.GetIsOpen(control) || control.IsFocused) {
                    ToolTip.SetIsOpen(control, false);
                    ToolTip.SetIsOpen(control, true);
                }
            } else {
                control.Classes.Remove("error");
                // update tooltip and opened tooltip popup
                ToolTip.SetTip(control, tip);
                if (ToolTip.GetIsOpen(control)) {
                    ToolTip.SetIsOpen(control, false);
                }
            }
        }

        // Set settings to controls
        protected void SetSettingsToControls() {
            DrawerSettings s = _drawerSettings;
            _settingInternally = true;
            // base
            upDownLimit.Value = s.limitPrimeIndex;
            textBoxSubgroup.Text = DS.FormatSubgroup(s.subgroup, s.narrows);
            // temperament
            _temperamentControls.SetTemperament(s.temperament);
            sliderTemperament.Value = (int)Math.Round(s.temperamentMeasure * 100);
            // slope
            textBoxSlopeOrigin.Text = s.slopeOrigin.FormatFraction();
            upDownChainTurns.Value = s.slopeChainTurns;
            // degrees
            //upDownDegreeCount.Value = s.degreeCount;
            upDownDegreeThreshold.Value = s.degreeThreshold;
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

            if (s.temperament != null) {
                UpdateTemperamentRowsAfterValidation(); // validate temperament
            }

            _settingInternally = false;
        }

        // Read settings from controls - used on saving Preset
        protected DrawerSettings GetSettingsFromControls() {
            DrawerSettings s = new DrawerSettings { };

            // subgroup
            if (!String.IsNullOrWhiteSpace(textBoxSubgroup.Text)) {
                string[] subgroupText = DS.SplitSubgroupText(textBoxSubgroup.Text);
                s.subgroup = Rational.ParseRationals(subgroupText[0]);
                s.narrows  = Rational.ParseRationals(subgroupText[1]);
                s.narrows  = NarrowUtils.ValidateNarrows(s.narrows);
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
            //s.degreeCount = (int)upDownDegreeCount.Value;
            s.degreeThreshold = (float)upDownDegreeThreshold.Value;
            // selection
            s.selection = DS.ParseIntervals(textBoxSelection.Text);
            // grids
            s.edGrids = DS.ParseEDGrids(textBoxEDGrids.Text);

            return s;
        }

        private void UpdateDrawerFully() {
            DrawerSettings s = _drawerSettings;
            // subgroup
            _gridDrawer.SetSubgroup(s.limitPrimeIndex, s.subgroup, s.narrows);
            // generation
            _gridDrawer.SetGeneration(s.harmonicityName, s.rationalCountLimit);
            // temperament
            _gridDrawer.SetTemperamentMeasure(s.temperamentMeasure);
            _gridDrawer.SetTemperament(s.temperament);
            // degrees
            _gridDrawer.SetDegrees(s.degreeThreshold);
            // slope
            _gridDrawer.SetSlope(s.slopeOrigin, s.slopeChainTurns);
            // view
            _gridDrawer.SetEDGrids(s.edGrids);
            _gridDrawer.SetSelection(s.selection);
            _gridDrawer.SetPointRadius(s.pointRadiusLinear);
        }

        private void ValidateControlsByDrawer() {
            //!!! GridDrawer currently owns Subgroup and Temperament
            //  so we can't validate by them before we set them there.
            UpdateSubgroupTip();
            UpdateTemperamentRowsAfterValidation();
        }

        #region Base
        private void upDownLimit_ValueChanged(object sender, NumericUpDownValueChangedEventArgs e) {
            if (!_settingInternally) {
                MarkPresetChanged();
                //
                int value = (int)e.NewValue;

                // update current setting
                _drawerSettings.limitPrimeIndex = value;

                // update drawer: subgroup & temperament
                Debug.Assert(_drawerSettings.narrows == null, "Limit UpDown should be disabled");
                _gridDrawer.SetSubgroup(value, _drawerSettings.subgroup, _drawerSettings.narrows);

                if (_drawerSettings.temperament != null) {
                    _gridDrawer.SetTemperament(_drawerSettings.temperament); // GridDrawer also validates its temperament values
                    UpdateTemperamentRowsAfterValidation();
                }

                InvalidateView();
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
                    subgroup = Rational.ParseRationals(textSubgroup[0], ".");
                    if (subgroup == null) {
                        error = "Invalid subgroup format";
                    }
                }
                if (!emptyNarrows) {
                    narrows = Rational.ParseRationals(textSubgroup[1], ".");
                    narrows = NarrowUtils.ValidateNarrows(narrows);
                    if (narrows == null) {
                        error = "Invalid narrows"; //!!! losing subgroup error
                    }
                }
                if (error == null) {
                    // parsed without errors
                    // update current settings
                    _drawerSettings.subgroup = subgroup;
                    _drawerSettings.narrows = narrows;
                    // update drawer subgroup
                    _gridDrawer.SetSubgroup(_drawerSettings.limitPrimeIndex, subgroup, narrows);
                    // revalidate temperament
                    if (_drawerSettings.temperament != null) {
                        _gridDrawer.SetTemperament(_drawerSettings.temperament); // GridDrawer also validates its temperament values
                        UpdateTemperamentRowsAfterValidation();
                    }

                    InvalidateView();
                }
            }
            //
            UpdateSubgroupTip(error);
            //
            upDownLimit.IsEnabled = _drawerSettings.subgroup == null;
        }

        private void UpdateSubgroupTip(string customError = null) {
            string tip   = null;
            string error = null;
            if (_gridDrawer.Subgroup != null) { 
                tip = String.Format("Base: {0}\nNarrows: {1}",
                    _gridDrawer.Subgroup.GetBaseItem(),
                    Rational.FormatRationals(_gridDrawer.Subgroup.GetNarrows())
                );
                error = _gridDrawer.Subgroup.GetError();
            }
            SetControlTip(textBoxSubgroup, tip, customError ?? error);
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
                InvalidateView();
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
                InvalidateView();
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
                        error = "Invalid rational format";
                    } else if (up.Equals(Rational.One)) {
                        error = "No slope for 1/1";
                    }
                }
                if (error == null) {
                    // update current setting
                    _drawerSettings.slopeOrigin = up;
                    // update drawer
                    _gridDrawer.SetSlope(up, _drawerSettings.slopeChainTurns);
                    InvalidateView();
                }
            }
            //
            SetControlTip(textBoxSlopeOrigin, null, error);
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
                InvalidateView();
            }
        }
        #endregion

        #region Degrees
        /*
        private void upDownDegreeCount_ValueChanged(object sender, NumericUpDownValueChangedEventArgs e) {
            if (!_settingInternally) {
                MarkPresetChanged();
                //
                int count = (int)upDownDegreeCount.Value;
                // update current setting
                _drawerSettings.degreeCount = count;
                // update drawer
                _gridDrawer.SetDegrees(count, _drawerSettings.degreeThreshold);
                InvalidateView();
            }
        }
        */
        private void upDownDegreeThreshold_ValueChanged(object sender, NumericUpDownValueChangedEventArgs e) {
            if (!_settingInternally) {
                MarkPresetChanged();
                //
                float threshold = (float)upDownDegreeThreshold.Value;
                // update current setting
                _drawerSettings.degreeThreshold = threshold;
                // update drawer
                _gridDrawer.SetDegrees(threshold);
                InvalidateView();
            }
        }
        #endregion

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
                    InvalidateView(false);
                }
            }
            //
            SetControlTip(textBoxEDGrids, null, error);
        }
#endregion

#region Highlight
        private void textBoxSelection_TextChanged(object sender, RoutedEventArgs e) {
            string error = null;
            if (!_settingInternally) {
                MarkPresetChanged();
                // parse
                SomeInterval[] selection = null;
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
                    InvalidateView();
                }
            }
            //
            SetControlTip(textBoxSelection, null, error);
        }
#endregion

#region Temperament
        private void sliderTemperament_ValueChanged(object sender, RoutedEventArgs e) {
            if (!_settingInternally) {
                MarkPresetChanged();
                //
                float value = (float)sliderTemperament.Value * 0.01f;
                // update current setting
                _drawerSettings.temperamentMeasure = value;
                // update drawer
                _gridDrawer.SetTemperamentMeasure(value);
                InvalidateView();
            }
        }

        private void temperamentGrid_Changed()
        {
            MarkPresetChanged();

            // update current settings
            UpdateTemperamentFromGrid(); // update _drawerSettings.temperament

            // update drawer
            _gridDrawer.SetTemperament(_drawerSettings.temperament); // GridDrawer also validates its temperament values
            
            // update controls
            UpdateTemperamentRowsAfterValidation(); // set errors to _temperamentControls

            InvalidateView();
        }

        private void buttonAdd_Click(object sender, RoutedEventArgs e) {
            var t = new Tempered(); // default values
            _temperamentControls.AddRow(t, focus: true);

            // just mark grid as incomplete
            UpdateTemperamentFromGrid();
            UpdateTemperamentRowsAfterValidation();
        }

        private void UpdateTemperamentFromGrid() {
            _drawerSettings.temperament = _temperamentControls.GetTemperament();
        }

        private void UpdateTemperamentRowsAfterValidation() {
            // _drawerSettings.temperament is updated from grid or loaded from preset
            // _gridDrawer.SetSubgroup(..) and 
            // _gridDrawer.SetTemperament() are already called

            // set error messages to grid rows about dirty user's temperament
            Tempered[] ts = _drawerSettings.temperament;
            if (ts != null) {
                string[] errors = Temperament.GetErrors(ts, _gridDrawer.Subgroup);
                for (int i = 0; i < ts.Length; ++i) {
                    _temperamentControls.SetRationalError(i, errors[i]);
                }
            }

            // hide slider if validated temperament is empty or invalid
            sliderTemperament.IsVisible = _gridDrawer.Temperament.IsSet();
        }

        #endregion

    }
}

