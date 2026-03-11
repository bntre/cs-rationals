using Microsoft.AspNetCore.Components;

using Torec.Drawing;
using Color = System.Drawing.Color;


namespace Rationals.Explorer.Blazor
{
	public static class Utils
	{
		public static void DrawTest_Pjosik(Image image)
		{
			image.Rectangle(Point.Points(0, 0, 20, 20))
				.Add()
				.FillStroke(ColorUtils.MakeColor(0xFFEEEEEE), Color.Empty);

			image.Rectangle(Point.Points(0, 0, 10, 10))
				.Add()
				.FillStroke(Color.Pink, Color.Empty);

			image.Path(Point.Points(0, 0, 5, 1, 10, 0, 9, 5, 10, 10, 5, 9, 0, 10, 1, 5))
				.Add()
				.FillStroke(Color.Empty, Color.Aqua, 0.5f);

			image.Line(Point.Points(0, 0, 10, 10))
				.Add()
				.FillStroke(Color.Empty, Color.Red, 1);

			image.Line(Point.Points(0, 5, 10, 5))
				.Add()
				.FillStroke(Color.Empty, Color.Red, 0.1f);

			image.Line(new Point(5, 2.5f), new Point(10, 2.5f), 0.5f, 1f)
				.Add()
				.FillStroke(Color.Green, Color.Black, 0.05f);

			image.Circle(new Point(5, 5), 2)
				.Add()
				.FillStroke(Color.Empty, Color.DarkGreen, 0.25f);

			int n = 16;
			for (int i = 0; i <= n; ++i)
			{
				image.Circle(new Point(10f * i / n, 10f), 0.2f)
					.Add()
					.FillStroke(Color.DarkMagenta, Color.Empty);
			}

			image.Text(new Point(5, 5), "Жил\nбыл\nпёсик", fontSize: 5f, lineLeading: 0.7f, align: Image.Align.Center)
				.Add()
				.FillStroke(Color.DarkCyan, Color.Black, 0.05f);

			image.Text(new Point(5, 5), "81\n80\n79", fontSize: 1f, lineLeading: 0.7f, align: Image.Align.Center, centerHeight: true)
				.Add()
				.FillStroke(Color.Black, Color.Empty);
		}
	}


	public partial class ExplorerPage : ComponentBase
	{
		private int count = 0;
		private void Increment() => count++;


		protected string svgMarkup = "";

		protected override void OnInitialized()
		{
			// Call your existing logic here
			svgMarkup = GenerateSvgString();
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
#else
					// Pjosik
					var imageSize = new Point(600, 600);
					var viewport = new Viewport(imageSize.X, imageSize.Y, 0, 20, 0, 20, false);
					var image = new Image(viewport);

					Utils.DrawTest_Pjosik(image);

					image.WriteSvg(xmlWriter);
#endif
				}
				return stringWriter.ToString();
			}
		}
	}

}