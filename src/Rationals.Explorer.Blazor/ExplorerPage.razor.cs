using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SkiaSharp;
using SkiaSharp.Views.Blazor;
using RD = Rationals.Drawing;
using SD = System.Drawing;
using TD = Torec.Drawing;

namespace Rationals.Explorer.Blazor
{
	public static class Utils
	{
		public static void DrawTest_Pjosik(TD.Image image)
		{
			image.Rectangle(TD.Point.Points(0, 0, 20, 20))
				.Add()
				.FillStroke(TD.ColorUtils.MakeColor(0xFFEEEEEE), SD.Color.Empty);

			image.Rectangle(TD.Point.Points(0, 0, 10, 10))
				.Add()
				.FillStroke(SD.Color.Pink, SD.Color.Empty);

			image.Path(TD.Point.Points(0, 0, 5, 1, 10, 0, 9, 5, 10, 10, 5, 9, 0, 10, 1, 5))
				.Add()
				.FillStroke(SD.Color.Empty, SD.Color.Aqua, 0.5f);

			image.Line(TD.Point.Points(0, 0, 10, 10))
				.Add()
				.FillStroke(SD.Color.Empty, SD.Color.Red, 1);

			image.Line(TD.Point.Points(0, 5, 10, 5))
				.Add()
				.FillStroke(SD.Color.Empty, SD.Color.Red, 0.1f);

			image.Line(new TD.Point(5, 2.5f), new TD.Point(10, 2.5f), 0.5f, 1f)
				.Add()
				.FillStroke(SD.Color.Green, SD.Color.Black, 0.05f);

			image.Circle(new TD.Point(5, 5), 2)
				.Add()
				.FillStroke(SD.Color.Empty, SD.Color.DarkGreen, 0.25f);

			int n = 16;
			for (int i = 0; i <= n; ++i)
			{
				image.Circle(new TD.Point(10f * i / n, 10f), 0.2f)
					.Add()
					.FillStroke(SD.Color.DarkMagenta, SD.Color.Empty);
			}

			image.Text(new TD.Point(5, 5), "Жил\nбыл\nпёсик", fontSize: 5f, lineLeading: 0.7f, align: TD.Image.Align.Center)
				.Add()
				.FillStroke(SD.Color.DarkCyan, SD.Color.Black, 0.05f);

			image.Text(new TD.Point(5, 5), "81\n80\n79", fontSize: 1f, lineLeading: 0.7f, align: TD.Image.Align.Center, centerHeight: true)
				.Add()
				.FillStroke(SD.Color.Black, SD.Color.Empty);
		}
	}


	public partial class ExplorerPage : ComponentBase
	{
		// counter - temp
		private int count = 0;
		private void Increment() => count++;


		// Grid Drawer
		TD.Viewport3        _viewport         = new TD.Viewport3();
		RD.DrawerSettings   _drawerSettings   = RD.DrawerSettings.Reset();
		RD.GridDrawer       _gridDrawer       = new RD.GridDrawer();

		private SKCanvasView? skCanvas;

		protected string svgMarkup = "";


		protected override void OnInitialized()
		{
			// Prepare drawer
			_viewport.SetImageSize(600, 300);
			_drawerSettings.pointRadiusLinear = 0.5f;


			// Call your existing logic here
			svgMarkup = GenerateSvgString();
		}


		private void UpdateDrawerFully() { //!!! move to DrawerSettings ?
			Rationals.Drawing.DrawerSettings s = _drawerSettings;
			// subgroup
			_gridDrawer.SetSubgroup(s.limitPrimeIndex, s.subgroup, s.narrows);
			// generation
			_gridDrawer.SetGeneration(s.harmonicityName, s.rationalCountLimit);
			// temperament
			_gridDrawer.SetTemperamentMeasure(s.temperamentMeasure);
			_gridDrawer.SetTemperament(s.temperament);
			// degrees
			_gridDrawer.SetDegrees(s.degreeThreshold);
			// slope
			_gridDrawer.SetSlope(s.slopeOrigin, s.slopeChainTurns);
			// view
			_gridDrawer.SetEDGrids(s.edGrids);
			_gridDrawer.SetSelection(s.selection);
			_gridDrawer.SetPointRadius(s.pointRadiusLinear);
		}

		private void DrawGrid(TD.Image image) {
			UpdateDrawerFully();
			_gridDrawer.SetBounds(_viewport.GetUserBounds());
			_gridDrawer.UpdateItems();
			_gridDrawer.UpdateCursorItem();
			_gridDrawer.DrawGrid(image);
		}


		private string GenerateSvgString()
		{
			using (var stringWriter = new System.IO.StringWriter())
			{
				using (var xmlWriter = System.Xml.XmlWriter.Create(stringWriter))
				{
#if False
					xmlWriter.WriteStartElement("svg", "http://www.w3.org/2000/svg");
					xmlWriter.WriteAttributeString("width", "100");
					xmlWriter.WriteAttributeString("height", "100");
					xmlWriter.WriteStartElement("circle");
					xmlWriter.WriteAttributeString("cx", "50");
					xmlWriter.WriteAttributeString("cy", "50");
					xmlWriter.WriteAttributeString("r", "40");
					xmlWriter.WriteAttributeString("stroke", "black");
					xmlWriter.WriteAttributeString("fill", "red");
					xmlWriter.WriteEndElement(); // circle
					xmlWriter.WriteEndElement(); // svg
#elif False
					// Pjosik
					var imageSize = new TD.Point(600, 600);
					var viewport = new TD.Viewport(imageSize.X, imageSize.Y, 0, 20, 0, 20, false);
					var image = new TD.Image(viewport);

					Utils.DrawTest_Pjosik(image);

					image.WriteSvg(xmlWriter);
#else
					var image = new TD.Image(_viewport);
					DrawGrid(image);
					image.WriteSvg(xmlWriter);
#endif
				}
				return stringWriter.ToString();
			}
		}


		// Handle mouse move over svg container
		protected void HandleMouseMove(MouseEventArgs e)
		{
			// Convert to Torec.Drawing.Point (image pixel coordinates) and then to user coordinates
			TD.Point imagePt = new TD.Point((float)e.OffsetX, (float)e.OffsetY);
			TD.Point u = _viewport.ToUser(imagePt);

			// Update drawer cursor and highlight mode
			_gridDrawer.SetCursor(u.X, u.Y);
			var mode = e.AltKey
				? RD.GridDrawer.CursorHighlightMode.Cents
				: RD.GridDrawer.CursorHighlightMode.NearestRational;
			_gridDrawer.SetCursorHighlightMode(mode);

			// regenerate svg markup and request UI update (StateHasChanged)
			svgMarkup = GenerateSvgString();
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



		// ... inside mouse/wheel handlers replace InvokeAsync(StateHasChanged) with:
		protected void HandleCanvasMouseMove(MouseEventArgs e)
		{
			TD.Point imagePt = new TD.Point((float)e.OffsetX, (float)e.OffsetY);
			TD.Point u = _viewport.ToUser(imagePt);

			_gridDrawer.SetCursor(u.X, u.Y);
			var mode = e.AltKey
				? RD.GridDrawer.CursorHighlightMode.Cents
				: RD.GridDrawer.CursorHighlightMode.NearestRational;
			_gridDrawer.SetCursorHighlightMode(mode);

			// request repaint of SKCanvasView
			// prefer calling Invalidate() directly; wrap in InvokeAsync if you're on a non-UI thread
			skCanvas?.Invalidate();
		}

		protected void HandleCanvasMouseLeave(MouseEventArgs e)
		{
			_gridDrawer.SetCursorHighlightMode(RD.GridDrawer.CursorHighlightMode.None);
			skCanvas?.Invalidate();
		}

		protected void HandleCanvasPointerDown(MouseEventArgs e)
		{
			TD.Point imagePt = new TD.Point((float)e.OffsetX, (float)e.OffsetY);
			TD.Point u = _viewport.ToUser(imagePt);

			_gridDrawer.SetCursor(u.X, u.Y);
			_gridDrawer.UpdateCursorItem();

			skCanvas?.Invalidate();
		}

		protected void HandleCanvasWheel(WheelEventArgs e)
		{
			float delta = (float)e.DeltaY;

			if (e.ShiftKey || e.CtrlKey)
			{
				_viewport.AddScale(delta * 0.1f, straight: e.CtrlKey, pointerPos: new TD.Point((float)e.OffsetX, (float)e.OffsetY));
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