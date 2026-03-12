using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SD = System.Drawing;
using SkiaSharp;
using SkiaSharp.Views.Blazor;
using TD = Torec.Drawing;
using RD = Rationals.Drawing;

namespace Rationals.Explorer.Blazor
{

	public static class Utils
	{
	}


	public partial class ExplorerPage : ComponentBase
	{
		// counter - temp
		private int count = 0;
		private void Increment() => count++;


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

			/*
			TD.Point u = _viewport.ToUser(pos);
			_gridDrawer.SetCursor(u.X, u.Y);
			_gridDrawer.UpdateCursorItem();

			skCanvas?.Invalidate();
			*/
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