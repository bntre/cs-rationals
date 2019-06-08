#define USE_LIBRARY

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
#if USE_LIBRARY
    // NumericUpDown with custom text formatting
    //  Value - prime index - 0, 1, 2, 3, 4,..
    //  Text  - prime name  - Octave, Tritave, 5, 7, 11,..
    public partial class NamedPrimeUpDown : PrimeUpDown {
        protected override Rational TextToRational(string text) {
            Rational r = Rationals.Library.Find(text);
            if (!r.IsDefault()) return r;
            return base.TextToRational(text); // parse
        }
        protected override string RationalToText(Rational r) {
            string name = Rationals.Library.Find(r);
            return name ?? base.RationalToText(r);
        }
    }
#endif

    // NumericUpDown with custom text formatting
    //  Value - prime index - 0, 1, 2, 3, 4,..
    //  Text  - prime       - 2, 3, 5, 7, 11,..
    public partial class PrimeUpDown : CustomUpDown
    {
        protected virtual Rational TextToRational(string text) {
            return Rational.Parse(text);
        }
        protected virtual string RationalToText(Rational r) {
            return r.FormatFraction();
        }

        protected override decimal TextToValue(string text) {
            Rational r = TextToRational(text);
            if (r.IsDefault()) throw new Exception("Invalid prime: " + text);
            int lastPrimeIndex = r.GetPowerCount() - 1;
            return (decimal)lastPrimeIndex;
        }
        protected override string ValueToText(decimal value) {
            int primeIndex = (int)value;
            int prime = Rationals.Utils.GetPrime(primeIndex);
            Rational r = new Rational(prime);
            return RationalToText(r);
        }
    }

    // Based on
    //  https://referencesource.microsoft.com/#System.Windows.Forms/winforms/Managed/System/WinForms/UpDownBase.cs
    //  https://referencesource.microsoft.com/#System.Windows.Forms/winforms/Managed/System/WinForms/NumericUpDown.cs

    public partial class CustomUpDown : System.Windows.Forms.NumericUpDown
    {
        protected virtual decimal TextToValue(string text) {
            return Decimal.Parse(text);
        }
        protected virtual string ValueToText(decimal value) {
            return value.ToString();
        }

        private bool _changingText = false; // avoid recursive calls

        protected override void UpdateEditText() {
            if (_changingText) return;
            _changingText = true;

            string textNew = ValueToText(this.Value);

            if (this.Text != textNew) {
                this.ChangingText = true; // internal change
                this.Text = textNew;
                this.ChangingText = false; // actually already set false in Text setter
            }

            _changingText = false;
        }

        private void ParseEditText2() { // base.ParseEditText() is not virtual
            // like base.ParseEditText()
            try {
                this.Value = TextToValue(this.Text);
            } catch {
                // Leave value as it is
            } finally {
                UserEdit = false;
            }
        }

        protected override void ValidateEditText() {
            // like base.ValidateEditText()
            ParseEditText2();
            UpdateEditText();
        }
    }

    public class TypedGridView : DataGridView
    {
        public enum ColumnType {
            Unknown = 0,
            Float,
            Rational,
        }
        private struct CellTypedValue {
            public object value;
        }

        public void SetColumnType(int columnIndex, ColumnType type) {
            Columns[columnIndex].Tag = type;
        }
        private static ColumnType GetColumnType(DataGridViewColumn column) {
            object tag = column.Tag;
            return tag is ColumnType ? (ColumnType)tag : ColumnType.Unknown;
        }
        private static bool CheckColumnType<T>(DataGridViewColumn column) {
            ColumnType type = GetColumnType(column);
            switch (type) {
                case ColumnType.Float:    return typeof(T) == typeof(float);
                case ColumnType.Rational: return typeof(T) == typeof(Rational);
            }
            return false;
        }

        private static void SaveCellTypedValue<T>(DataGridViewCell cell, T value) {
            cell.Tag = new CellTypedValue { value = value };
        }
        /*
        private static string FormatTypedValue<T>(DataGridViewCell cell, T value) {
            ColumnType type = GetColumnType(cell.OwningColumn);
            switch (type) {
                case ColumnType.Float:    return Convert.ToString(value);
                case ColumnType.Rational: return Convert.ToString(value);
            }
            return null;
        }
        */
        public static T GetCellTypedValue<T>(DataGridViewCell cell) {
            if (cell.Tag is CellTypedValue) {
                var v = (CellTypedValue)cell.Tag;
                if (v.value is T) {
                    return (T)v.value;
                }
            }
            return default(T);
        }
        public static bool SetCellTypedValue<T>(DataGridViewCell cell, T value) {
            if (!CheckColumnType<T>(cell.OwningColumn)) return false; // no need to validate - check type only
            SaveCellTypedValue(cell, value);
            //cell.Value = value;
            cell.Value = Convert.ToString(value);
            return true;
        }

        private static bool ValidateCellValue(DataGridViewCell cell, string value) {
            bool valid = false;
            bool empty = String.IsNullOrWhiteSpace(value); // allow empty string for default
            var type = GetColumnType(cell.OwningColumn);
            if (type != ColumnType.Unknown) {
                switch (type) {
                    case ColumnType.Float:
                        float f = 0;
                        valid = empty || float.TryParse(value, out f);
                        if (valid) SaveCellTypedValue(cell, f);
                        break;
                    case ColumnType.Rational:
                        Rational r = Rational.Parse(value);
                        valid = empty || !r.IsDefault();
                        if (valid) SaveCellTypedValue(cell, r);
                        break;
                }
            }
            return valid;
        }

        protected override bool ProcessDialogKey(Keys keyData) {
            // Custom handler to stop moving to next row on Enter
            if (keyData == Keys.Enter && IsCurrentCellInEditMode) {
                DataGridViewCell cell = CurrentCell;
                // Validate and end edit only on valid value
                string value = Convert.ToString(cell.EditedFormattedValue);
                if (ValidateCellValue(cell, value)) {
                    EndEdit(); // CellValidating will not be raised
                    return true;
                } // otherwise ValidateCellValue will be called again from "base.ProcessDialogKey -> OnCellValidating" !!!
            }
            return base.ProcessDialogKey(keyData);
        }

        protected override void OnCellValidating(DataGridViewCellValidatingEventArgs e) {
            DataGridViewCell cell = Rows[e.RowIndex].Cells[e.ColumnIndex];
            string value = Convert.ToString(e.FormattedValue);
            if (!ValidateCellValue(cell, value)) {
                e.Cancel = true;
            }
            base.OnCellValidating(e);
        }

    }
}
