using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SD = System.Drawing;
using RD = Rationals.Drawing;
using TD = Torec.Drawing;

namespace Rationals.Explorer.Blazor
{
	public partial class ExplorerPage
	{
		// Mouse stuff
		string currentCursor = "default";
		bool isSpacePressed = false; // Dragging view with Space+LButton. !!! it's slow; fixed with AsNonRenderingEventHandler
		bool isDragging = false;
		TD.Point lastDraggingPos;

		private TD.Point GetOffset(MouseEventArgs e) {
			return new TD.Point((float)e.OffsetX, (float)e.OffsetY);
		}

		void HandleKeyDown(KeyboardEventArgs e) {
			if (!e.Repeat) {
				if (e.Code == "Space") {
					isSpacePressed = true;
					currentCursor = "grab";
				}
			}
			//!!! Handle arrow keys for moving?
		}

		void HandleKeyUp(KeyboardEventArgs e) {
			if (e.Code == "Space") {
				isSpacePressed = false;
				isDragging = false;
				currentCursor = "default";
			}
		}

		bool IgnorePointerMove(MouseEventArgs e) {
			// Allow to move pointer out leaving current highlighted item
			return e.ShiftKey && e.Buttons == 0;
		}

		protected void HandleMouseMove(MouseEventArgs e)
		{
			if (IgnorePointerMove(e)) return;

			TD.Point pos = GetOffset(e);

			if (isDragging) {
				TD.Point delta = lastDraggingPos - pos;
				lastDraggingPos = pos;
				_viewport.MoveOrigin(delta);
				_gridDrawer.SetBounds(_viewport.GetUserBounds());
				
				MarkPresetChanged();
				InvalidateCanvas();
			}
			else {
				TD.Point u = _viewport.ToUser(pos);
				_gridDrawer.SetCursor(u.X, u.Y);
				var mode = e.AltKey
					? RD.GridDrawer.CursorHighlightMode.Cents
					: RD.GridDrawer.CursorHighlightMode.NearestRational;
				_gridDrawer.SetCursorHighlightMode(mode);
				
				UpdateSelectionInfo();
				InvalidateCanvas();
			}
		}

		protected void HandleMouseLeave(MouseEventArgs e)
		{
			if (IgnorePointerMove(e)) return; // Allow to move pointer out leaving current highlighted item

			_gridDrawer.SetCursorHighlightMode(RD.GridDrawer.CursorHighlightMode.None);
			InvalidateCanvas();
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
					//_gridDrawer.UpdateCursorItem(); //!!! ?
					Rational r = _gridDrawer.GetCursorRational();
					if (!r.IsDefault()) {
						t = new SomeInterval { rational = r };
					}
				}

				if (t != null) {
					// Toggle selection
					if (e.CtrlKey) {
						ToggleSelection(t);
					}
					// Play note
					else {
						PlayNote(t);
					}
				}
			}
		}

		protected void HandlePointerUp(MouseEventArgs e)
		{
			isDragging = false;
			currentCursor = isSpacePressed ? "grab" : "default";
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

			MarkPresetChanged();
			InvalidateCanvas();
		}
	}

}