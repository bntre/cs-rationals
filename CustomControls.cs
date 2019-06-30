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
#if DEBUG
using System.Diagnostics;
#endif

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

            if (this.Text != textNew) { // this will avoid an empty text
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

    public class ScrollingCursor {
        private Point _initialCursorPos;
        public bool IsScrolling = false;
        //
        public void StartScrolling(Point initialCursorPos) {
            _initialCursorPos = initialCursorPos;
            IsScrolling = true;
            Cursor.Hide();
        }
        public void StopScrolling() {
            if (!IsScrolling) return;
            IsScrolling = false;
            Cursor.Position = _initialCursorPos;
            Cursor.Show();
        }
        public Point Scroll(Point cursorPos) {
            if (!IsScrolling || cursorPos == _initialCursorPos) {
                return Point.Empty;
            }
            Point d = new Point(
                cursorPos.X - _initialCursorPos.X,
                cursorPos.Y - _initialCursorPos.Y
            );
            Cursor.Position = _initialCursorPos;
            return d;
        }
        //
        public static ScrollingCursor Reference = new ScrollingCursor();
    }

    public class ScrollableUpDown : CustomUpDown // inherit our CustomUpDown to forbid an empty text on "erase + lost focus"
    {
        private ScrollingCursor _cursor = ScrollingCursor.Reference;
        private TextBox _upDownEdit;

        [Category("Scroll")]
        [DefaultValue(1)] //!!! doesn't work?
        public decimal ScrollStep { get; set; }

        /*
        https://referencesource.microsoft.com/#System.Windows.Forms/winforms/Managed/System/WinForms/UpDownBase.cs
        UpDownBase               has two child controls:
            UpDownButtons       
            UpDownEdit
             
        The problem here is to determine when UpDownEdit gets the Mouse Capture.
        We try to handle UpDownBase.MouseDown but it also raised on UpDownButtons.MouseDown.
        So we check "_upDownEdit.ClientRectangle.Contains".
        */

        public ScrollableUpDown() : base() {
            _upDownEdit = Controls[1] as TextBox;
            //
            this.       MouseDown           += new MouseEventHandler(upDown_MouseDown);
            _upDownEdit.MouseCaptureChanged += new EventHandler     (upDownEdit_MouseCaptureChanged);
            _upDownEdit.MouseMove           += new MouseEventHandler(upDownEdit_MouseMove);
        }
        private void upDown_MouseDown(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left && _upDownEdit.ClientRectangle.Contains(e.Location)) {
                Point pos = this.PointToScreen(e.Location);
                _cursor.StartScrolling(pos);
            }
        }
        private void upDownEdit_MouseCaptureChanged(object sender, EventArgs e) {
            if (_cursor.IsScrolling) {
                _cursor.StopScrolling();
            }
        }
        private void upDownEdit_MouseMove(object sender, MouseEventArgs e) {
            if (_cursor.IsScrolling) {
                Point pos = _upDownEdit.PointToScreen(e.Location);
                Point delta = _cursor.Scroll(pos);
                if (delta.Y != 0) {
                    ScrollValue(-delta.Y);
                }
            }
        }
        protected virtual void ScrollValue(int delta) {
            decimal v = this.Value + delta * ScrollStep;
            v = Math.Max(this.Minimum, Math.Min(this.Maximum, v));
            this.Value = v;
        }
    }

    namespace GridView
    {
        // https://docs.microsoft.com/en-us/dotnet/framework/winforms/controls/how-to-host-controls-in-windows-forms-datagridview-cells
        // https://weblogs.asp.net/rweigelt/1647400

        public class NumericEditingControl : DataGridViewTextBoxEditingControl {
            // Allow to scroll numeric cell value with ScrollingCursor and mouse wheel
            private ScrollingCursor _cursor = ScrollingCursor.Reference;
            private NumericColumn OwningColumn { get { return this.EditingControlDataGridView.CurrentCell.OwningColumn as NumericColumn; } }
            protected override void OnMouseDown(MouseEventArgs e) {
                base.OnMouseDown(e);
                if (e.Button == MouseButtons.Left) {
                    Point pos = this.PointToScreen(e.Location);
                    _cursor.StartScrolling(pos);
                }
            }
            protected override void OnMouseCaptureChanged(EventArgs e) {
                base.OnMouseCaptureChanged(e);
                if (_cursor.IsScrolling) {
                    _cursor.StopScrolling();
                }
            }
            protected override void OnMouseMove(MouseEventArgs e) {
                base.OnMouseMove(e);
                if (_cursor.IsScrolling) {
                    Point pos = this.PointToScreen(e.Location);
                    Point posDelta = _cursor.Scroll(pos);
                    if (posDelta.Y != 0) {
                        float delta = -posDelta.Y * OwningColumn.CursorScrollStep;
                        ScrollValue(delta);
                    }
                }
            }
            protected override void OnMouseWheel(MouseEventArgs e) {
                base.OnMouseWheel(e);
                float delta = Math.Sign(e.Delta) * OwningColumn.WheelScrollStep;
                ScrollValue(delta);
            }
            protected virtual void ScrollValue(float delta) {
                TypedGridView gridView = this.EditingControlDataGridView as TypedGridView;
                if (gridView != null) {
                    gridView.ScrollNumericCellValue(delta, OwningColumn.ScrollRoundDigits);
                }
            }
        }

        public class NumericCell : DataGridViewTextBoxCell {
            public override Type EditType { get { return typeof(NumericEditingControl); } }
        }
        public class NumericColumn : DataGridViewColumn {
            [Category("Scroll")] public float WheelScrollStep { get; set; }
            [Category("Scroll")] public float CursorScrollStep { get; set; }
            [Category("Scroll")] public int ScrollRoundDigits { get; set; }
            public NumericColumn() : base(new NumericCell()) {}
            public override object Clone() { // needed for Designer to store custom properties
                var c = (NumericColumn)base.Clone();
                c.WheelScrollStep = WheelScrollStep;
                c.CursorScrollStep = CursorScrollStep;
                c.ScrollRoundDigits = ScrollRoundDigits;
                return c;
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

            public void ScrollNumericCellValue(float delta, int roundDigits = -1) {
                if (IsCurrentCellInEditMode) {
                    DataGridViewCell cell = CurrentCell;
                    var type = GetColumnType(cell.OwningColumn);
                    if (type == ColumnType.Float) {
                        float value = TypedGridView.GetCellTypedValue<float>(cell);
                        value += delta;
                        if (roundDigits >= 0) {
                            value = (float)Math.Round(value, roundDigits);
                        }
                        TypedGridView.SetCellTypedValue(cell, value); // raising CellValueChanged
                        this.RefreshEdit();
                    }
                }
            }

            #region Drag/reorder rows
            private int  _draggedRow = -1;
            private bool _draggingInside = false;
            //
            private int GetRowIndex(int x, int y, bool isClient) {
                Point p = new Point(x, y);
                if (!isClient) {
                    p = PointToClient(p);
                }
                return HitTest(p.X, p.Y).RowIndex;
            }
            protected override void OnMouseDown(MouseEventArgs e) {
                // ready for new dragging
                if (e.Button == MouseButtons.Left) {
                    int i = GetRowIndex(e.X, e.Y, true);
                    if (0 <= i && i < NewRowIndex) {
                        _draggedRow = i;
                        _draggingInside = false; // we will start dragging on mousemove
                    }
                }
                base.OnMouseDown(e);
            }
            protected override void OnMouseMove(MouseEventArgs e) {
                if (e.Button == MouseButtons.Left) {
                    if (!_draggingInside && _draggedRow != -1) {
                        // start new dragging
                        _draggingInside = true;
                        DoDragDrop(1, DragDropEffects.Move);
                    }
                } else {
                    if (_draggedRow != -1) {
                        _draggedRow = -1;
                        _draggingInside = false;
                        DoDragDrop(1, DragDropEffects.None);
                    }
                }
                base.OnMouseMove(e);
            }
            protected override void OnDragEnter(DragEventArgs e) {
                if (_draggedRow != -1) {
                    e.Effect = DragDropEffects.Move;
                    _draggingInside = true;
                }
                base.OnDragEnter(e);
            }
            protected override void OnDragLeave(EventArgs e) {
                _draggingInside = false;
                base.OnDragLeave(e);
            }

            protected override void OnDragDrop(DragEventArgs e) {
                if (_draggedRow != -1) {
                    int i = GetRowIndex(e.X, e.Y, false);
                    if (i != _draggedRow && i < NewRowIndex) {
                        var row = Rows[_draggedRow];
                        Rows.RemoveAt(_draggedRow);
                        Rows.Insert(i, row);
                        // select moved row
                        ClearSelection();
                        row.Selected = true;
                    }
                    _draggedRow = -1;
                    _draggingInside = false;
                }
                base.OnDragDrop(e);
            }
            #endregion
        }

    }
}
