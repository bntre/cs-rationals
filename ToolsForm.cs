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

        public void ShowSelection(string Rational) {
            //
        }

        private void buttonApply_Click(object sender, EventArgs e) {
            _currentSettings = GetSettings();
            _mainForm.ApplyDrawerSettings(_currentSettings);
        }

        private static Color ValidColor(bool valid) {
            return valid ? default(Color) : Color.Pink;
        }

        private void SetSettings(GridDrawer.Settings s) {
            // base
            upDownBase.Value = s.basePrimeIndex;
            // limit
            upDownLimit.Value = s.limitPrimeIndex;
            // subgroup
            if (s.subgroup != null) {
                textBoxSubgroup.Text = FormatSubgroup(s.subgroup);
            }
            // up interval
            textBoxUp.Text = s.up.IsDefault() ? "" : s.up.FormatFraction();
            upDownUpTurns.Value = s.upTurns;
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
                s.basePrimeIndex = (int)upDownBase.Value;
                s.limitPrimeIndex = (int)upDownLimit.Value;
            }
            // up interval
            s.up = Rational.Parse(textBoxUp.Text);
            s.upTurns = (int)upDownUpTurns.Value;
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
            upDownLimit.Enabled = upDownBase.Enabled = empty || !valid;
        }
        #endregion

        #region Grids
        private static string FormatGrids(int[][] edGrids) {
            return String.Join("; ", edGrids.Select(g => 
                String.Join(" ", g.Select(n => n.ToString()))
            ));
        }
        private int[][] ParseGrids(string grids) {
            string[] parts = grids.Split(',',';');
            int[][] result = new int[parts.Length][];
            for (int i = 0; i < parts.Length; ++i) {
                result[i] = new int[3];
                string[] ps = parts[i].Split(new[]{' '}, StringSplitOptions.RemoveEmptyEntries);
                if (ps.Length != 3) return null;
                for (int j = 0; j < 3; ++j) {
                    if (!int.TryParse(ps[j], out result[i][j])) return null;
                }
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
    }
}
