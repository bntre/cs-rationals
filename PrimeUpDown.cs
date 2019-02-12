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
    // NumericUpDown with custom text formatting
    //  Value - prime index - 0, 1, 2, 3, 4,..
    //  Text  - prime       - 2, 3, 5, 7, 11,..
    public partial class PrimeUpDown : CustomUpDown
    {
        protected override decimal TextToValue(string text) {
            int n;
            if (int.TryParse(text, out n) && n >= 2) {
                Rational r = new Rational(n);
                int lastPrimeIndex = r.GetPowerCount() - 1;
                return (decimal)lastPrimeIndex;
            }
            throw new Exception("Invalid prime: " + text);
        }

        protected override string ValueToText(decimal value) {
            int primeIndex = (int)value;
            return Rationals.Utils.GetPrime(primeIndex).ToString();
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
}
