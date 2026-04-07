using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SkiaSharp;
using SkiaSharp.Views.Blazor;
using System.Diagnostics;

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

		private SKCanvasView? skCanvas;

		protected override void OnInitialized()
		{
			InitDrawer();
		}

		protected void InvalidateView() {
			skCanvas?.Invalidate(); // OnPaintSurface() will be called
		}

		void OnPaintSurface(SKPaintSurfaceEventArgs e) {
			//Console.WriteLine("OnPaintSurface {0}x{1}", e.Info.Width, e.Info.Height);
			DrawGridToCanvas(e.Info.Width, e.Info.Height, e.Surface.Canvas);
		}


		#region Play Note
		private async void PlayNote(SomeInterval t)  // Like in Rationals.Explorer.MainWindow.PlayNote(SomeInterval t)
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
			bool temperPartials = true; //!!! always temper?

			int partialsCount = partials.Count;
			var freqs    = new float[partialsCount];
			var durs     = new float[partialsCount];
			var levels   = new float[partialsCount];

			for (int i = 0; i < partialsCount; ++i) {
				Rational r = partials[i];
				float c = cents;
				c += temperPartials ? _gridDrawer.Temperament.CalculateMeasuredCents(r) //!!! optimize
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
		#endregion Play Note

	}

}