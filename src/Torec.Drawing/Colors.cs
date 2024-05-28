using System;
using System.Collections.Generic;
//using System.Linq;

using Color = System.Drawing.Color; // !!! use own color struct ?


namespace Torec.Drawing
{
    public static class ColorUtils
    {
        public static Color MakeColor(uint color) {
            unchecked {
                return Color.FromArgb((int)color);
            }
        }
        public static Color MakeColor(long color) {
            return MakeColor((uint)color);
        }

        #region Color spaces
        // from http://www.java2s.com/Code/CSharp/2D-Graphics/HsvToRgb.htm
        public static Color HsvToColor(double h, double s, double v)
        {
            double h6 = h * 6.0; // 0..6
            int hi = (int)Math.Floor(h6) % 6;
            double f = h6 - Math.Floor(h6);

            double p = v * (1.0 - s);
            double q = v * (1.0 - (f * s));
            double t = v * (1.0 - ((1.0 - f) * s));

            switch (hi) {
                case 0:
                    return FromRgb(v, t, p);
                case 1:
                    return FromRgb(q, v, p);
                case 2:
                    return FromRgb(p, v, t);
                case 3:
                    return FromRgb(p, q, v);
                case 4:
                    return FromRgb(t, p, v);
                case 5:
                    return FromRgb(v, p, q);
                default:
                    return FromRgb(0, 0, 0);
            }
        }
        private static Color FromRgb(double r, double g, double b) {
            return Color.FromArgb(0xFF, (byte)(r * 0xFF), (byte)(g * 0xFF), (byte)(b * 0xFF));
        }

        // from http://csharphelper.com/blog/2016/08/convert-between-rgb-and-hls-color-models-in-c/
        public static Color HslToColor(double h, double s, double l) {
            if (s == 0) return FromRgb(l, l, l);
            //
            double p2 = l <= 0.5 ? l * (1 + s) : s + l * (1 - s);
            double p1 = 2 * l - p2;
            //
            double h360 = h * 360;
            double r = QqhToRgb(p1, p2, h360 + 120);
            double g = QqhToRgb(p1, p2, h360);
            double b = QqhToRgb(p1, p2, h360 - 120);
            //
            return FromRgb(r, g, b);
        }
        private static double QqhToRgb(double q1, double q2, double h360) {
            double h = h360; // 0..360
            if (h > 360) { do { h -= 360; } while (h > 360); }
            else while (h < 0) h += 360;
            //
            if (h <  60) return q1 + (q2 - q1) * h / 60;
            if (h < 180) return q2;
            if (h < 240) return q1 + (q2 - q1) * (240 - h) / 60;
            return q1;
        }
        #endregion

        #region Rare colors
        // Uniform distribution of hue sequence across the color circle
        public static double GetRareHue(int hueIndex) {
            double h = Math.Log(hueIndex * 2 + 1, 2);
            return h - Math.Floor(h); // 0..1
        }
        public static Color GetRareColor(int hueIndex, double saturation, double lightness) {
            double hue = GetRareHue(hueIndex);
            return HslToColor(hue, saturation, lightness);
        }
        #endregion Rare colors

    }

}