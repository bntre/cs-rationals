using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SkiaSharp;
using SkiaSharp.Views.Blazor;
using SD = System.Drawing;
using TD = Torec.Drawing;
using RD = Rationals.Drawing;

namespace Rationals.Explorer.Blazor
{

	public static class Utils
	{
        public static float CentsToFactor(float cents) {
            return MathF.Pow(2f, cents / 1200f);
        }
        public static float CentsToHz(float cents) {
            // Like in Rationals.Midi.MidiPlayer (Midi.cs):
            //    0.0 -> C4 (261.626 Hz)
            // 1200.0 -> C5
            return 261.626f * CentsToFactor(cents);
        }
	}


	public partial class ExplorerPage : ComponentBase
	{
		[Inject] private IJSRuntime JS { get; set; } = default!;

		// counter - temp
		private int count = 0;
		private void Increment() => count++;

		bool _sidebarOpen = true;
		private void ToggleSidebar() {
			_sidebarOpen = !_sidebarOpen;
		}


		// Grid Drawer
		TD.Viewport3        _viewport         = new TD.Viewport3();
		RD.GridDrawer       _gridDrawer       = new RD.GridDrawer();
		RD.DrawerSettings   _drawerSettings   = RD.DrawerSettings.Reset();

		private SKCanvasView? skCanvas;

		string currentCursor = "default";

		bool isSpacePressed = false; // Dragging view with Space+LButton
		bool isDragging = false;
		TD.Point lastDraggingPos;

		protected override void OnInitialized()
		{
			// Prepare drawer
			_viewport.SetImageSize(800, 600);
			_gridDrawer.SetBounds(_viewport.GetUserBounds());

			_drawerSettings.pointRadiusLinear = 0.5f;
			_drawerSettings.UpdateDrawer(_gridDrawer);
		}

		private void DrawGrid(TD.Image image) {
			_gridDrawer.UpdateItems();
			_gridDrawer.UpdateCursorItem();
			_gridDrawer.DrawGrid(image);
		}

		void OnPaintSurface(SKPaintSurfaceEventArgs e) // SKCanvasView
		{
			var image = new TD.Image(_viewport);
			DrawGrid(image); // Draw vector items

			SKCanvas canvas = e.Surface.Canvas;
			canvas.Clear(SKColors.White);
			
			// Rasterize
			image.Draw(canvas, true);
		}

		private async void PlayNote(SomeInterval t)
		{
			// get interval cents
			float cents = t.IsRational()
				? _gridDrawer.Temperament.CalculateMeasuredCents(t.rational)
				: t.cents;

			int partialsMaxCount = 10;

			var partials = new List<Rational>();
			for (int i = 1; i < 100; ++i) {
				var r = new Rational(i);
				if (!_gridDrawer.Subgroup.IsInRange(r)) {
					continue; // skip if out of subgroup
				}
				partials.Add(r);
				if (partials.Count == partialsMaxCount) break;
			}

			//bool temper = _soundSettings.output == SoundSettings.EOutput.WavePartialsTempered;
			bool temper = true;

			int partialsCount = partials.Count;
			var freqs    = new float[partialsCount];
			var durs     = new float[partialsCount];
			var levels   = new float[partialsCount];

			for (int i = 0; i < partialsCount; ++i) {
				Rational r = partials[i];
				float c = cents;
				c += temper ? _gridDrawer.Temperament.CalculateMeasuredCents(r) //!!! optimize
							: (float)r.ToCents();
				freqs[i] = Utils.CentsToHz(c);
				float h = _gridDrawer.GetRationalHarmonicity(r);
				//Debug.Assert(0 <= h && h <= 1f, "Normalized harmonicity expected");
				levels[i] = 0.1f * MathF.Pow(h, 4.5f);
				durs[i] = 2f * h;
				//Debug.WriteLine("Add partial: {0} {1:0.000} -> {2:0.00}c {3:0.00}hz level {4:0.000}", r, h, c, hz, level);
			}

			await JS.InvokeVoidAsync("playNote", t.ToString(), partialsCount, freqs, durs, levels);
		}

		private TD.Point GetOffset(MouseEventArgs e) {
			return new TD.Point((float)e.OffsetX, (float)e.OffsetY);
		}

		void HandleKeyDown(KeyboardEventArgs e) {
			if (e.Code == "Space") {
				isSpacePressed = true;
				currentCursor = "grab";
			}
		}

		void HandleKeyUp(KeyboardEventArgs e) {
			if (e.Code == "Space") {
				isSpacePressed = false;
				isDragging = false;
				currentCursor = "default";
			}
		}

		protected void HandleMouseMove(MouseEventArgs e)
		{
			TD.Point pos = GetOffset(e);

			if (isDragging) {
				TD.Point delta = lastDraggingPos - pos;
				lastDraggingPos = pos;
				_viewport.MoveOrigin(delta);
				_gridDrawer.SetBounds(_viewport.GetUserBounds());
				//MarkPresetChanged();
				//!!! update view ?
			}
			else {
				TD.Point u = _viewport.ToUser(pos);
				_gridDrawer.SetCursor(u.X, u.Y);
				var mode = e.AltKey
					? RD.GridDrawer.CursorHighlightMode.Cents
					: RD.GridDrawer.CursorHighlightMode.NearestRational;
				_gridDrawer.SetCursorHighlightMode(mode);
			}

			skCanvas?.Invalidate();
		}

		protected void HandleMouseLeave(MouseEventArgs e)
		{
			_gridDrawer.SetCursorHighlightMode(RD.GridDrawer.CursorHighlightMode.None);
			skCanvas?.Invalidate();
		}

		protected void HandlePointerDown(MouseEventArgs e)
		{
			var pos = GetOffset(e);

			if (e.Button == 1 || (e.Button == 0 && isSpacePressed)) { // MButton or Space+LButton
				isDragging = true;
				lastDraggingPos = pos;
				currentCursor = "grabbing";
			}

			else if (e.Button == 0) { // LButton
				SomeInterval? t = null;
				if (e.AltKey) { // by cents
					float c = _gridDrawer.GetCursorCents();
					t = new SomeInterval { cents = c };
				} else {  // nearest rational
					_gridDrawer.UpdateCursorItem(); //!!! ?
					Rational r = _gridDrawer.GetCursorRational();
					if (!r.IsDefault()) {
						t = new SomeInterval { rational = r };
					}
				}
				if (t != null) {
					// Toggle selection
					if (e.CtrlKey) {
						//ToggleSelection(t);
						//!!! invalidate image
					}
					// Play note
					else {
						PlayNote(t);
					}
				}
			}

			//skCanvas?.Invalidate();
		}

		protected void HandlePointerUp(MouseEventArgs e)
		{
			isDragging = false;
			currentCursor = isSpacePressed ? "grab" : "default";

			/*
			var pos = GetOffset(e);
			
			TD.Point u = _viewport.ToUser(pos);
			_gridDrawer.SetCursor(u.X, u.Y);
			_gridDrawer.UpdateCursorItem();

			skCanvas?.Invalidate();
			*/
		}

		protected void HandleWheel(WheelEventArgs e)
		{
			float delta = (float)e.DeltaY * (e.DeltaMode == 0 ? 1 : (e.DeltaMode == 1 ? 16 : 800));
			delta /= 100f; //!!! single scroll gives me e.DeltaY=100 and e.DeltaMode=0

			var pos = GetOffset(e);

			if (e.ShiftKey || e.CtrlKey)
			{
				_viewport.AddScale(-delta * 0.1f, straight: e.CtrlKey, pointerPos: pos);
				_gridDrawer.SetBounds(_viewport.GetUserBounds());
			}
			else if (e.AltKey)
			{
				_drawerSettings.pointRadiusLinear += delta * 0.1f;
				_gridDrawer.SetPointRadius(_drawerSettings.pointRadiusLinear);
			}
			else
			{
				_viewport.MoveOrigin(new TD.Point(0, -delta * 10f));
				_gridDrawer.SetBounds(_viewport.GetUserBounds());
			}

			skCanvas?.Invalidate();
		}



	}

}