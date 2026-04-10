using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using Rationals.Drawing;
using Rationals.Explorer.Blazor.Components;
using SkiaSharp;
using System.Diagnostics;
using static Rationals.Explorer.Blazor.Components.TemperamentGrid;
using RD = Rationals.Drawing;
using SD = System.Drawing;
using TD = Torec.Drawing;


namespace Rationals.Explorer.Blazor
{
	public partial class ExplorerPage
	{
		// Grid Drawer
		TD.Viewport3        _viewport         = new TD.Viewport3();
		RD.GridDrawer       _gridDrawer       = new RD.GridDrawer();
		RD.DrawerSettings   _drawerSettings   = RD.DrawerSettings.Reset(); // buffer settings between the GridDrawer and GUI (or XML preset)

		// Setting update chain:
		//    Razor setting properties -> DrawerSettings -> GridDrawer

		protected void InitDrawer()
		{
			_viewport.SetImageSize(800, 600); // Initial size, will be updated on (first) OnPaintSurface
			_gridDrawer.SetBounds(_viewport.GetUserBounds());

			// 
			_drawerSettings.pointRadiusLinear = 0.1f;

			//!!! test
			_drawerSettings.temperament = [
				new Tempered { rational = new Rational(81,80), cents = 0f },
				new Tempered { rational = new Rational(128,125), cents = 0f },
			];

			_drawerSettings.UpdateDrawer(_gridDrawer);

			SetSettingsToControls();
		}

		void DrawGrid(TD.Image image) {
			_gridDrawer.UpdateItems();
			_gridDrawer.UpdateCursorItem();
			_gridDrawer.DrawGrid(image);
		}

		void DrawGridToCanvas(int w, int h, SKCanvas canvas)
		{
			// Ensure viewport knows the actual canvas pixel size (SK surface provides device pixels)
			var currentSize = _viewport.GetImageSize();
			if ((int)currentSize.X != w || (int)currentSize.Y != h) {
				_viewport.SetImageSize(w, h);
				_gridDrawer.SetBounds(_viewport.GetUserBounds());
			}
			
			var image = new TD.Image(_viewport);
			DrawGrid(image); // Draw vector items

			// Clear canvas
			canvas.Clear(SKColors.White);
			
			// Rasterize
			image.Draw(canvas, true);
		}


#region Drawer Controls

		// Primes
		int         settingJiLimit = 3;
		string?     settingSubgroup = null;
		string[]?   settingSubgroupTip = null;
		string?     settingSubgroupError = null;

		// Generation
		string      settingDistanceFunction = "Barlow";
		int         settingItemCountLimit = 200;

		// Chain slope
		string      settingSlopeReference = "4";
		float       settingSlopeTurns = 7.0f;

		// Temperament
		TemperamentGrid.Temperament settingTemperament = new();

		// ED lattice
		string?     settingEDLattice = null;
		string?     settingEDLatticeError = null;

		// Selection
		string?     settingSelection = null;
		string?     settingSelectionError = null;

		string?     selectionInfo = null;

		bool _settingInternally = false; // no need to parse control value: e.g. if SetSettingsToControls() in progress


#region Utils
		public static IEnumerable<string> JoinTooltip(string? error, string[]? tips) {
			if (error != null) yield return error;
			if (tips != null) {
				foreach (var line in tips) yield return line;
			}
		}
		public static RenderFragment JoinTooltipContent(string? error, string[]? tips) {
			return builder => {
				var html = string.Join("", JoinTooltip(error, tips).Select(line => $"<div>{line}</div>"));
				builder.AddContent(0, (MarkupString)html);
			};
		}

		public static string FormatSubgroup(Rational[] subgroup, Rational[] narrows) {
			string result = "";
			if (subgroup != null) {
				result += Rational.FormatRationals(subgroup, ".");
			}
			if (narrows != null) {
				if (result != "") result += " ";
				result += "(" + Rational.FormatRationals(narrows, ".") + ")";
			}
			return result;
		}

		public static string?[] SplitSubgroupText(string? subgroupText) { // 2.3.7/5 (7/5)
			var result = new string?[] { null, null };
			if (string.IsNullOrWhiteSpace(subgroupText)) return result;
			string[] parts = subgroupText.Split('(', ')');
			if (!string.IsNullOrWhiteSpace(parts[0])) {
				result[0] = parts[0];
			}
			if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1])) {
				result[1] = parts[1];
			}
			return result;
		}
#endregion


		// Set settings to controls
		protected void SetSettingsToControls() {
			// _drawerSettings -> Razor GUI

			DrawerSettings s = _drawerSettings;
			_settingInternally = true;

			// base
			settingJiLimit = s.limitPrimeIndex;
			settingSubgroup = DrawerSettings.FormatSubgroup(s.subgroup, s.narrows);
			// generation
			if (!String.IsNullOrEmpty(s.harmonicityName)) {
				settingDistanceFunction = s.harmonicityName;
			}
			settingItemCountLimit = s.rationalCountLimit;
			// slope
			settingSlopeReference = s.slopeOrigin.FormatFraction();
			settingSlopeTurns = s.slopeChainTurns;
			// temperament
			if (s.temperament != null) {
				settingTemperament.rows = s.temperament.Select(t => new TemperamentRow { rational = t.rational, cents = t.cents }).ToList(); //!!! temp
				settingTemperament.measure = s.temperamentMeasure;
				UpdateTemperamentRowErrors(); // validate temperament
			}
			// ED lattice
			settingEDLattice = GridDrawer.EDGrid.Format(s.edGrids);
			// selection
			settingSelection = DrawerSettings.FormatIntervals(s.selection);

			// info
			UpdateSelectionInfo(); 

			// -- ValidateControlsByDrawer() will be called later

			_settingInternally = false;
		}

		// Read settings from controls - used on saving Preset
		/*
		protected Rationals.Drawing.DrawerSettings GetSettingsFromControls() {
			var s = new Rationals.Drawing.DrawerSettings { };

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
			s.edGrids = GridDrawer.EDGrid.Parse(textBoxEDGrids.Text);

			return s;
		}
		*/

		private void onJiLimitChanged() {
			// like upDownLimit_ValueChanged
			
			if (!_settingInternally) {
				MarkPresetChanged();

				// update current setting
				_drawerSettings.limitPrimeIndex = settingJiLimit;

				// update drawer: subgroup & temperament
				Debug.Assert(_drawerSettings.narrows == null, "Limit UpDown should be disabled");
				_gridDrawer.SetSubgroup(settingJiLimit, _drawerSettings.subgroup, _drawerSettings.narrows);

				if (_drawerSettings.temperament != null) {
					_gridDrawer.SetTemperament(_drawerSettings.temperament); // GridDrawer also validates its temperament values
					UpdateTemperamentRowErrors();
				}

				UpdateSelectionInfo();
				InvalidateCanvas();
			}
		}

		private void onSubgroupChanged() {
			// like textBoxSubgroup_TextChanged

			string? error = null;
			if (!_settingInternally) {
				MarkPresetChanged();
				// parse
				Rational[]? subgroup = null;
				Rational[]? narrows  = null;
				string?[] textSubgroup = SplitSubgroupText(settingSubgroup);
				bool emptySubgroup = string.IsNullOrWhiteSpace(textSubgroup[0]);
				bool emptyNarrows  = string.IsNullOrWhiteSpace(textSubgroup[1]);
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
						UpdateTemperamentRowErrors();
					}

					UpdateSelectionInfo();
					InvalidateCanvas();
				}
			}

			UpdateSubgroupTip(error);
		}

		private void UpdateSubgroupTip(string? customError = null) {
			// settingSubgroupTip <- _gridDrawer.Subgroup tip or error
			settingSubgroupError = customError ?? _gridDrawer.Subgroup?.GetError();
			if (_gridDrawer.Subgroup != null && settingSubgroupError == null) {
				settingSubgroupTip = [
					"Base: " + _gridDrawer.Subgroup.GetBaseItem(), 
					"Narrows: " + Rational.FormatRationals(_gridDrawer.Subgroup.GetNarrowItems(), ". ")
				];
			} else {
				settingSubgroupTip = null;
			}
		}


#region Generation
		void onGenerationChanged() {
			if (!_settingInternally) {
				MarkPresetChanged();
				// update current settings
				_drawerSettings.harmonicityName = settingDistanceFunction;
				_drawerSettings.rationalCountLimit = settingItemCountLimit;
				// update drawer
				_gridDrawer.SetGeneration(settingDistanceFunction, settingItemCountLimit);
				//
				UpdateSelectionInfo();
				InvalidateCanvas();
			}
		}
#endregion


#region Slope
		static string? ValidateRational(string? value, out Rational r) {
			r = Rational.Parse(value);
			return r.IsDefault() ? ("Invalid rational: " + value) : null;
		}
		static string? ValidateSlopeOrigin(string? value, out Rational r) {
			return ValidateRational(value, out r) ?? (r.Equals(Rational.One) ? "No slope for 1/1" : null);
		}

		void onSlopeReferenceChanged() {
			string? error = null;
			if (!_settingInternally) {
				MarkPresetChanged();
				// parse
				error = ValidateSlopeOrigin(settingSlopeReference, out Rational up);
				if (error == null) {
					// update current setting
					_drawerSettings.slopeOrigin = up;
					// update drawer
					_gridDrawer.SetSlope(up, _drawerSettings.slopeChainTurns);
					InvalidateCanvas();
				}
			}
		}
		void onSlopeTurnsChanged() {
			if (!_settingInternally) {
				MarkPresetChanged();
				// update current setting
				_drawerSettings.slopeChainTurns = settingSlopeTurns;
				// update drawer
				_gridDrawer.SetSlope(_drawerSettings.slopeOrigin, settingSlopeTurns);
				InvalidateCanvas();
			}
		}
#endregion

#region Temperament
		private void onTemperamentMeasureChanged() {
			if (!_settingInternally) {
				MarkPresetChanged();
				
				// update current setting
				_drawerSettings.temperamentMeasure = settingTemperament.measure;

				// update drawer
				_gridDrawer.SetTemperamentMeasure(_drawerSettings.temperamentMeasure);

				UpdateSelectionInfo();
				InvalidateCanvas();
			}
		}
		private void onTemperamentChanged() {			
			MarkPresetChanged();

			// update current _drawerSettings.temperament
			_drawerSettings.temperament = settingTemperament.rows
				.Select(t => new Tempered { rational = t.rational, cents = t.cents })
				.ToArray();

			// update drawer
			_gridDrawer.SetTemperament(_drawerSettings.temperament); // GridDrawer also validates its temperament values
			
			// update controls
			UpdateTemperamentRowErrors(); // set errors to GUI

			UpdateSelectionInfo();
			InvalidateCanvas();
		}

		private void UpdateTemperamentRowErrors() {
			// _drawerSettings.temperament is updated from grid or loaded from preset
			// _gridDrawer.SetSubgroup(..) and 
			// _gridDrawer.SetTemperament() are already called

			// set error messages to grid rows about dirty user's temperament
			Tempered[] ts = _drawerSettings.temperament;
			if (ts != null) {
				string[] errors = Temperament.GetErrors(ts, _gridDrawer.Subgroup); // Per-row errors
				if (errors != null) {
					for (int i = 0; i < errors.Length; ++i) {
						settingTemperament.rows[i].error = errors[i];
					}
				}
			}
		}
#endregion

		private void onEDLatticeChanged() {
			settingEDLatticeError = null;
			if (!_settingInternally) {
				MarkPresetChanged();
				// parse
				GridDrawer.EDGrid[]? grids = null;
				bool empty = String.IsNullOrWhiteSpace(settingEDLattice);
				if (!empty) {
					grids = GridDrawer.EDGrid.Parse(settingEDLattice);
					if (grids == null) {
						settingEDLatticeError = "Invalid format";
					}
				}
				if (settingEDLatticeError == null) {
					// update current setting
					_drawerSettings.edGrids = grids;
					// update drawer
					_gridDrawer.SetEDGrids(_drawerSettings.edGrids);

					InvalidateCanvas();
				}
			}
		}

		private void onSelectionChanged() {
			settingSelectionError = null;
			if (!_settingInternally) {
				MarkPresetChanged();
				// parse
				SomeInterval[]? selection = null;
				bool empty = String.IsNullOrWhiteSpace(settingSelection);
				if (!empty) {
					selection = DrawerSettings.ParseIntervals(settingSelection);
					if (selection == null) {
						settingSelectionError = "Invalid format";
					}
				}
				if (settingSelectionError == null) {
					// update current setting
					_drawerSettings.selection = selection;
					// update drawer
					_gridDrawer.SetSelection(_drawerSettings.selection);
					
					UpdateSelectionInfo();
					InvalidateCanvas();
				}
			}
		}

		private void ToggleSelection(SomeInterval t) {
			SomeInterval[] s = _drawerSettings.selection ?? [];
			int count = s.Length;
			s = s.Where(i => !i.Equals(t)).ToArray(); // try to remove
			if (s.Length == count) { // otherwise add
				s = s.Concat([ t ]).ToArray();
			}
			_drawerSettings.selection = s;

			// Update drawer
			_gridDrawer.SetSelection(_drawerSettings.selection);

			// Update 'selection' control
			_settingInternally = true; // Avoid onSelectionChanged() call
			UpdateSelectionInfo();
			_settingInternally = false;

			InvalidateCanvas();
		}

		private void UpdateSelectionInfo() {
			selectionInfo = _gridDrawer.FormatSelectionInfo();
		}

#endregion Drawer Controls

	}

}