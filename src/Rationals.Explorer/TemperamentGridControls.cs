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

using TextBox = Avalonia.CustomControls.TextBox2;
using UpDown  = Avalonia.CustomControls.UpDown;

namespace Rationals.Explorer
{
    class TemperamentGridControls {
        public TemperamentGridControls(Grid grid) {
            _grid = grid;
        }

        protected Grid _grid;

        public event Action Changed; // something changed: some cell value, row order,..

        private void Clear() {
            _settingInternally = true;
            int count = _grid.RowDefinitions.Count;
            for (int i = 0; i < count; ++i) {
                RemoveRowHandlers(i);
            }
            _grid.Children.Clear();
            _grid.RowDefinitions.Clear();
            _settingInternally = false;
        }

        public void SetTemperament(Tempered[] temperament) {
            Clear();
            if (temperament != null) {
                foreach (Tempered t in temperament) {
                    AddRow(t, focus: false);
                }
            }
        }

        public void AddRow(Tempered t, bool focus = false)
        {
            int rowIndex = _grid.RowDefinitions.Count;

            var cs = new RowControls {
                row      = new Rectangle { },
                rect     = new Rectangle { },
                rational = new TextBox { },
                cents    = new UpDown { },
            };

            Grid.SetRow(cs.row,      rowIndex);  Grid.SetColumn(cs.row,      0);  Grid.SetColumnSpan(cs.row, 3);
            Grid.SetRow(cs.rect,     rowIndex);  Grid.SetColumn(cs.rect,     0);  
            Grid.SetRow(cs.rational, rowIndex);  Grid.SetColumn(cs.rational, 1);  
            Grid.SetRow(cs.cents,    rowIndex);  Grid.SetColumn(cs.cents,    2);

            cs.row     .Classes.Add("row");
            cs.rect    .Classes.Add("rect");
            cs.rational.Classes.Add("rational");
            cs.cents   .Classes.Add("cents");

            _settingInternally = true;

            AddRowHandlers(cs);

            _grid.RowDefinitions.Add(new RowDefinition { });
            _grid.Children.AddRange(new Control[] { cs.row, cs.rect, cs.rational, cs.cents });

            cs.rational.Text = t.rational.FormatFraction();
            cs.cents.Value = t.cents;

            if (focus) {
                cs.rational.Focus();
            }

            _settingInternally = false;
        }

        public void DeleteRow(int rowIndex) {
            RemoveRowHandlers(rowIndex);
            Control[] children = _grid.Children.OfType<Control>().ToArray();
            foreach (Control child in children) {
                int i = Grid.GetRow(child);
                if (i == rowIndex) {
                    _grid.Children.Remove(child);
                } else if (i > rowIndex) {
                    Grid.SetRow(child, i - 1);
                }
            }
            _grid.RowDefinitions.RemoveAt(rowIndex);
        }

        public void MoveRow(int sourceIndex, int destIndex) {
            if (sourceIndex == destIndex) return;
            foreach (Control child in _grid.Children) {
                int i = Grid.GetRow(child);
                if (i == sourceIndex) {
                    Grid.SetRow(child, destIndex);
                } else {
                    if (destIndex < sourceIndex) {
                        if (destIndex <= i && i < sourceIndex) {
                            Grid.SetRow(child, i + 1);
                        }
                    } else if (sourceIndex < destIndex) {
                        if (sourceIndex < i && i <= destIndex) {
                            Grid.SetRow(child, i - 1);
                        }
                    }
                }
            }
        }

        public Tempered[] GetTemperament() {
            int count = _grid.RowDefinitions.Count;
            var ts = new Tempered[count];
            for (int i = 0; i < ts.Length; ++i) {
                RowControls cs = GetRowControls(i);
                ts[i] = new Tempered {
                    rational = GetRowRational(cs),
                    cents    = GetRowCents(cs),
                };
            }
            return ts;
        }

        public void SetRationalError(int rowIndex, string error) {
            RowControls cs = GetRowControls(rowIndex);
            MainWindow.SetControlTip(cs.rational, null, error);
        }

        //----------------------------------------------------------------

        private bool _settingInternally = false; // true on new row creation

        private struct RowControls { // temporal object - controls are collected on the fly
            internal Rectangle row;
            internal Rectangle rect;
            internal TextBox rational;
            internal UpDown cents;
        }

        private RowControls GetRowControls(int rowIndex) {
            var cs = new RowControls {};
            foreach (Control c in _grid.Children) {
                if (Grid.GetRow(c) == rowIndex) {
                    if      (c.Classes.Contains("row"))      cs.row      = (Rectangle)c;
                    else if (c.Classes.Contains("rect"))     cs.rect     = (Rectangle)c;
                    else if (c.Classes.Contains("rational")) cs.rational = (TextBox)c;
                    else if (c.Classes.Contains("cents"))    cs.cents    = (UpDown)c;
                }
            }
            return cs;
        }

        private Rational GetRowRational(RowControls cs) {
            return Rational.Parse(cs.rational.Text);
        }

        private float GetRowCents(RowControls cs) {
            return (float)cs.cents.Value;
        }

        #region Handlers

        private void AddRowHandlers(RowControls cs) {
            cs.row.PointerPressed   += OnRowPointerPressed;
            cs.row.KeyDown          += OnRowKeyDown;
            cs.row.AddHandler(DragDrop.DragOverEvent, OnRowDragOver);
            cs.row.AddHandler(DragDrop.DropEvent,     OnRowDrop);
            DragDrop.SetAllowDrop(cs.row, true);
            cs.rational.TextChanged += OnRationalTextChanged;
            cs.rational.LostFocus   += OnRationalLostFocus;
            cs.cents.ValueChanged   += OnCentsChange;
            //cs.cents.GotFocus       += OnCentsGotFocus;
            //cs.cents.LostFocus      += OnCentsLostFocus;
        }
        private void RemoveRowHandlers(int rowIndex) {
            RowControls cs = GetRowControls(rowIndex);
            cs.row.PointerPressed   -= OnRowPointerPressed;
            cs.row.KeyDown          -= OnRowKeyDown;
            cs.row.RemoveHandler(DragDrop.DragOverEvent, OnRowDragOver);
            cs.row.RemoveHandler(DragDrop.DropEvent,     OnRowDrop);
            cs.rational.TextChanged -= OnRationalTextChanged;
            cs.rational.LostFocus   -= OnRationalLostFocus;
            cs.cents.ValueChanged   -= OnCentsChange;
            //cs.cents.GotFocus       -= OnCentsGotFocus;
            //cs.cents.LostFocus      -= OnCentsLostFocus;
            ToolTip.SetIsOpen(cs.rational, false); // hide tooltips if shown
        }

        private void OnRowKeyDown(object sender, Avalonia.Input.KeyEventArgs e) {
            if (sender is Control row && row.Parent == _grid) {
                if (e.Key == Key.Delete) {
                    int rowIndex = Grid.GetRow(row);
                    DeleteRow(rowIndex);
                    Changed?.Invoke();
                }
            }
        }

        private void OnRowPointerPressed(object sender, PointerPressedEventArgs e) {
            if (sender is Control row && row.Parent == _grid) {
                DataObject d = new DataObject();
                d.Set("temperamentRow", Grid.GetRow(row));
                DragDrop.DoDragDrop(e, d, DragDropEffects.Move); // Start dragging. Now handle DragOver and Drop!
            }
        }
        private void OnRowDragOver(object sender, DragEventArgs e) {
            if (e.DragEffects == DragDropEffects.Move && e.Data.Contains("temperamentRow")) {
                Debug.WriteLine("A row {0} dragged over {1}", e.Data.Get("temperamentRow"), e.Source);
                //!!! here is "invalid" cursor (I) when dragging over TextBox.
            } else {
                e.DragEffects = DragDropEffects.None;
            }
        }
        private void OnRowDrop(object sender, DragEventArgs e) {
            if (e.DragEffects == DragDropEffects.Move && e.Data.Contains("temperamentRow")) {
                int sourceIndex = (int)e.Data.Get("temperamentRow");
                int destIndex = Grid.GetRow(e.Source as Control);
                if (sourceIndex != destIndex) {
                    Debug.WriteLine("A row {0} dropped to row {1}", sourceIndex, destIndex);
                    MoveRow(sourceIndex, destIndex);
                    Changed?.Invoke();
                }
            }
        }

        private void OnRationalTextChanged(object sender, RoutedEventArgs e) {
            if (_settingInternally) return; // we should call validate later

            if (sender is TextBox tb)
            {
                // validate rational
                //Rational r = Rational.Parse(tb.Text);
                //SetRationalError(tb, r.IsDefault() ? "Invalid Rational" : null);

                // reset cents text - we will set exact (default) cents value in OnRationalLostFocus
                int rowIndex = Grid.GetRow(tb);
                RowControls cs = GetRowControls(rowIndex);
                cs.cents.Text = "";

                Changed?.Invoke();
            }
        }

        private void OnRationalLostFocus(object sender, RoutedEventArgs e) {
            if (_settingInternally) return; // for any case

            int rowIndex = Grid.GetRow(sender as Control);
            var cs = GetRowControls(rowIndex);

            // set exact cents value. Text=="" after OnRationalTextChanged
            if (cs.cents.Text == "") {
                Rational r = GetRowRational(cs);
                if (!r.IsDefault()) {
                    cs.cents.Value = r.ToCents();
                    Changed?.Invoke();
                }
            }
        }

        private void OnCentsChange(object sender, NumericUpDownValueChangedEventArgs e) {
            if (_settingInternally) return; // we should call validate later
            if (sender is UpDown c) {
                int rowIndex = Grid.GetRow(c);
                Changed?.Invoke();
            }
        }

        //private void OnCentsGotFocus(object sender, GotFocusEventArgs e) {
        //    //Debug.WriteLine("OnCentsGotFocus %s", (sender as Control).Name);
        //}

        //private void OnCentsLostFocus(object sender, RoutedEventArgs e) {
        //    //TemperamentChanged?.Invoke(); //!!! same as OnRationalLostFocus ?
        //}

        #endregion Handlers
    }
}