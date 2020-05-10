using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.Input;
//using Avalonia.Logging;

using Rationals;

namespace Avalonia.CustomControls
{
    public class UpDown : NumericUpDown, IStyleable
    {
        public bool AllowScroll { // scrolling with mouse
            get { return GetValue(AllowScrollProperty); }
            set { SetValue(AllowScrollProperty, value); }
        }
        public double ScrollStep {
            get { return GetValue(ScrollStepProperty); }
            set { SetValue(ScrollStepProperty, value); }
        }

        public static readonly StyledProperty<bool> AllowScrollProperty =
            AvaloniaProperty.Register<UpDown, bool>(nameof(AllowScroll), true);

        public static readonly StyledProperty<double> ScrollStepProperty =
            AvaloniaProperty.Register<UpDown, double>(nameof(ScrollStep), 0.1d);


        // https://stackoverflow.com/questions/51746650/how-to-extend-a-control-in-avalonia/51761372
        // You need to also apply parent's control styles. You can do that by changing the style key:
        Type IStyleable.StyleKey => typeof(NumericUpDown);

        /*
        protected override void OnTemplateApplied(TemplateAppliedEventArgs e) {
            base.OnTemplateApplied(e);

            // We need focus events from TextBox
            TextBox textBox = e.NameScope.Find<TextBox>("PART_TextBox");
            ResetTextBox(textBox);
        }
        */

        public UpDown() : base() {
            AddHandler(InputElement.PointerPressedEvent, UpDown_PointerPressed, RoutingStrategies.Tunnel);
        }

        /*
        #region TextBox Focus handlers
        public event EventHandler<GotFocusEventArgs> TextBoxGotFocus;
        public event EventHandler<RoutedEventArgs> TextBoxLostFocus;
        protected TextBox _textBox = null;
        private void ResetTextBox(TextBox textBox) {
            if (_textBox != null) {
                _textBox.GotFocus  -= TextBox_GotFocus;
                _textBox.LostFocus -= TextBox_LostFocus;
            }
            _textBox = textBox;
            if (_textBox != null) {
                _textBox.GotFocus  += TextBox_GotFocus;
                _textBox.LostFocus += TextBox_LostFocus;
            }
        }
        private void TextBox_GotFocus(object sender, GotFocusEventArgs e) {
            TextBoxGotFocus?.Invoke(sender, e);
        }
        private void TextBox_LostFocus(object sender, RoutedEventArgs e) {
            TextBoxLostFocus?.Invoke(sender, e);
        }
        #endregion
        */

        #region Scrolling with mouse drag

        bool _captured = false;
        Point _startPos;
        double _startValue;

        private void UpDown_PointerPressed(object sender, PointerPressedEventArgs e) {
            if (!AllowScroll) return;
            Debug.WriteLine("UpDown PointerPressed from {0}", e.Source.GetType().Name as object);
            bool fromTextBox = Utils.IsControlUnder(e.Source as IControl, this.TextBox);
            if (fromTextBox) {
                _captured = true;
                _startPos = e.GetPosition(this);
                _startValue = this.Value;
            }
        }

        protected override void OnPointerMoved(PointerEventArgs e) {
            base.OnPointerMoved(e);
            if (!AllowScroll) return;
            if (_captured) {
                Avalonia.Point delta = e.GetPosition(this) - _startPos;
                double value = _startValue - delta.Y * ScrollStep;
                if (ClipValueToMinMax) {
                    value = Avalonia.Utilities.MathUtilities.Clamp(value, Minimum, Maximum);
                }
                this.Value = value;
                Debug.WriteLine("UpDown OnPointerMoved {0} -> {1} -> {2}", delta, value, this.Value);
            }
        }
        protected override void OnPointerReleased(PointerReleasedEventArgs e) {
            base.OnPointerReleased(e);
            if (!AllowScroll) return;
            Debug.WriteLine("UpDown OnPointerReleased");
            _captured = false;
        }

        #endregion
    }

    public abstract class CustomUpDown : UpDown
    {
        // override Avalonia NumericUpDown methods
        override protected double ConvertTextToValueCore(string currentValueText, string text) {
            try {
                return TextToValue(text);
            } catch (Exception ex) {
                throw new System.IO.InvalidDataException("Input string was not in a correct format: " + text, ex);
            }
        }
        override protected string ConvertValueToText() {
            return ValueToText(this.Value);
        }

        abstract protected string ValueToText(double value);
        abstract protected double TextToValue(string text);
    }

    public class PrimeUpDown : CustomUpDown, IStyleable
    {
        Type IStyleable.StyleKey => typeof(NumericUpDown);

        override protected string ValueToText(double value) {
            int primeIndex = (int)value;
            int prime = Rationals.Utils.GetPrime(primeIndex);
            Rational r = new Rational(prime);
            return r.FormatFraction();
        }
        override protected double TextToValue(string text) {
            Rational r = Rational.Parse(text);
            if (r.IsDefault()) throw new Exception("Invalid prime: " + text);
            return (double)r.GetHighPrimeIndex();
        }
    }

    //!!! move outside
    public static class Utils {
        public static bool IsControlUnder(IControl control, IControl parent) {
            if (parent == null) return false;
            if (control == null) return false;
            return control == parent || IsControlUnder(control.Parent, parent);
        }
    }
}
