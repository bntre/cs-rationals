using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Rationals.Forms
{
    using GridDrawer = Rationals.Drawing.GridDrawer;

    public partial class ToolsForm : Form
    {
        private GridDrawer.Settings _currentSettings;
        private MainForm _mainForm;

        public ToolsForm(MainForm mainForm) {
            _mainForm = mainForm;
            InitializeComponent();

            // fill Harmonicity combo
            comboBoxDistance.Items.AddRange(Rationals.Utils.HarmonicityNames);

            // Set defaults
            _currentSettings = GridDrawer.Settings.Edo12();
            // set default limits 
            _currentSettings.rationalCountLimit = 100;
            _currentSettings.distanceLimit = new Rational(new[] { 8, -8, 2 });

            SetSettings(_currentSettings);
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

        private void SetSettings(GridDrawer.Settings s) {
            // limit
            upDownLimit.Value = s.limitPrimeIndex;
            // subgroup
            if (s.subgroup != null) {
                textBoxSubgroup.Text = FormatSubgroup(s.subgroup);
            }
            // up interval
            textBoxUp.Text = s.slopeOrigin.IsDefault() ? "" : s.slopeOrigin.FormatFraction();
            upDownChainTurns.Value = (decimal)s.slopeChainTurns;
            // grids
            if (s.edGrids != null) {
                textBoxGrids.Text = FormatGrids(s.edGrids);
            }
            // drawing
            comboBoxDistance.SelectedItem = s.harmonicityName ?? Rationals.Utils.HarmonicityNames[0];
            upDownCountLimit.Value = s.rationalCountLimit;
            textBoxDistanceLimit.Text = s.distanceLimit.IsDefault() ? "" : s.distanceLimit.FormatFraction();
        }

        private GridDrawer.Settings GetSettings() {
            var s = new GridDrawer.Settings();
            // subgroup
            string subgroup = textBoxSubgroup.Text;
            if (!String.IsNullOrWhiteSpace(subgroup)) {
                s.subgroup = ParseSubgroupNumbers(subgroup);
            }
            // base & limit
            if (s.subgroup == null) {
                s.limitPrimeIndex = (int)upDownLimit.Value;
            }
            // up interval
            s.slopeOrigin = Rational.Parse(textBoxUp.Text);
            s.slopeChainTurns = (double)upDownChainTurns.Value;
            // stick commas
            //s.stickCommas = 
            s.stickMeasure = trackBarStickCommas.Value * 0.01f;
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

        #region Subgroup
        private static string FormatSubgroup(Rational[] subgroup) {
            return String.Join(".", subgroup.Select(r => r.FormatFraction()));
        }
        private static Rational[] ParseSubgroupNumbers(string subgroup) {
            string[] parts = subgroup.Split('.');
            Rational[] result = new Rational[parts.Length];
            for (int i = 0; i < parts.Length; ++i) {
                result[i] = Rational.Parse(parts[i]);
                if (result[i].IsDefault()) return null;
            }
            return result;
        }
        private static int[] GetSubgroupPrimeIndices(int[] primes) {
            // Multiply all numbers - so no need to except non-primes
            Rational r = new Rational(1);
            for (int i = 0; i < primes.Length; ++i) {
                r *= primes[i];
            }
            var primeIndices = new List<int>();
            int[] pows = r.GetPrimePowers();
            for (int i = 0; i < pows.Length; ++i) {
                if (pows[i] != 0) primeIndices.Add(i);
            }
            return primeIndices.ToArray();
        }
        private void textBoxSubgroup_TextChanged(object sender, EventArgs e) {
            string subgroup = textBoxSubgroup.Text;
            bool empty = String.IsNullOrWhiteSpace(subgroup);
            bool valid = empty || (ParseSubgroupNumbers(subgroup) != null);
            textBoxSubgroup.BackColor = ValidColor(valid);
            upDownLimit.Enabled = empty || !valid;
        }
        #endregion

        #region Grids
        private static string FormatGrids(GridDrawer.EDGrid[] edGrids) {
            return String.Join("; ", edGrids.Select(g =>
                String.Format("{0}ed{1}",
                    g.stepCount,
                    FindEDBaseLetter(g.baseInterval) ?? g.baseInterval.FormatFraction()
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
            string[] parts = grids.ToLower().Split(',',';');
            var result = new GridDrawer.EDGrid[parts.Length];
            for (int i = 0; i < parts.Length; ++i) {
                string[] ps = parts[i].Split(new[]{"ed","-"," "}, StringSplitOptions.RemoveEmptyEntries);
                if (ps.Length != 2) return null;
                //
                var g = new GridDrawer.EDGrid();
                if (!int.TryParse(ps[0], out g.stepCount)) return null;
                if (!_edBases.TryGetValue(ps[1], out g.baseInterval)) {
                    g.baseInterval = Rational.Parse(ps[1]);
                    if (g.baseInterval.IsDefault()) return null;
                }
                //
                result[i] = g;
            }
            return result;
        }
        private void textBoxGrids_TextChanged(object sender, EventArgs e) {
            string grids = textBoxGrids.Text;
            bool empty = String.IsNullOrWhiteSpace(grids);
            bool valid = empty || (ParseGrids(grids) != null);
            textBoxGrids.BackColor = ValidColor(valid);
        }
        #endregion

        private void trackBarStickCommas_ValueChanged(object sender, EventArgs e) {
            //!!! temporal
            _currentSettings.stickMeasure = trackBarStickCommas.Value * 0.01f;
            _mainForm.ApplyDrawerSettings(_currentSettings);

        }
    }
}
