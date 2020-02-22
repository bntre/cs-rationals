using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;

namespace Rationals.Explorer
{
    using GridDrawer = Rationals.Drawing.GridDrawer;

    public struct DrawerSettings {
        // primes
        public int limitPrimeIndex; // 0,1,2,..
        // or
        public Rational[] subgroup; // e.g. {3, 5, 7} (Bohlen-Pierce), {2, 3, 7/5},.. https://en.xen.wiki/w/Just_intonation_subgroups
        public Rational[] narrows; // "narrow" prime tips for _narrowPrimes

        // generating items
        public string harmonicityName; // null for some default
        public int rationalCountLimit; // -1 for unlimited

        // slope
        public Rational slopeOrigin; // starting point to define slope
        public float slopeChainTurns; // chain turn count to "slope origin" point. set an integer for vertical.

        // temperament
        public Rational.Tempered[] temperament; // user's unfiltered temperament, may contain invalid rationals
        public float temperamentMeasure; // 0..1

        // degrees
        //public float stepMinHarmonicity;
        //public int stepSizeMaxCount; // e.g. 2 for kind of MOS

        // grids
        public GridDrawer.EDGrid[] edGrids;

        // selection
        public Drawing.SomeInterval[] selection;

        //!!! here ?
        public float pointRadiusLinear;

        // default settings
        public static DrawerSettings Edo12() {
            var s = new DrawerSettings();
            //
            s.limitPrimeIndex = 2; // 5-limit
            //
            s.slopeOrigin = new Rational(3, 2); // 5th
            s.slopeChainTurns = 2;
            //
            s.temperament = new[] {
#if DEBUG
                new Rational.Tempered { rational = new Rational(81, 80), cents = 0 },
#endif
            };
            //
            s.edGrids = new[] {
                new GridDrawer.EDGrid { stepCount = 12, baseInterval = Rational.Two }
            };
            //
            return s;
        }


        public static string FormatRational(Rational r) {
            if (r.IsDefault()) return "";
            return r.FormatFraction();
        }

        #region Base
        private static string JoinRationals(Rational[] rs, string separator = ".") {
            if (rs == null) return "";
            return String.Join(separator, rs.Select(r => r.FormatFraction()));
        }
        public static string FormatSubgroup(Rational[] subgroup, Rational[] narrows) {
            string result = "";
            if (subgroup != null) {
                result += JoinRationals(subgroup, ".");
            }
            if (narrows != null) {
                if (result != "") result += " ";
                result += "(" + JoinRationals(narrows, ".") + ")";
            }
            return result;
        }
        public static int[] ParseIntegers(string text, char separator = ' ') {
            if (String.IsNullOrWhiteSpace(text)) return null;
            string[] parts = text.Split(new[]{ separator }, StringSplitOptions.RemoveEmptyEntries);
            int[] result = new int[parts.Length];
            for (int i = 0; i < parts.Length; ++i) {
                if (!int.TryParse(parts[i], out result[i])) {
                    return null; // null if invalid
                }
            }
            return result;
        }
        public static Rational[] ParseRationals(string text, char separator = '.') {
            if (String.IsNullOrWhiteSpace(text)) return null;
            string[] parts = text.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
            Rational[] result = new Rational[parts.Length];
            for (int i = 0; i < parts.Length; ++i) {
                result[i] = Rational.Parse(parts[i]);
                if (result[i].IsDefault()) return null; // null if invalid
            }
            return result;
        }
        public static string[] SplitSubgroupText(string subgroupText) { // 2.3.7/5 (7/5)
            var result = new string[] { null, null };
            if (String.IsNullOrWhiteSpace(subgroupText)) return result;
            string[] parts = subgroupText.Split('(', ')');
            if (!String.IsNullOrWhiteSpace(parts[0])) {
                result[0] = parts[0];
            }
            if (parts.Length > 1 && !String.IsNullOrWhiteSpace(parts[1])) {
                result[1] = parts[1];
            }
            return result;
        }
        #endregion

        #region ED Grids
        public static string FormatEDGrids(GridDrawer.EDGrid[] edGrids) {
            if (edGrids == null) return "";
            return String.Join("; ", edGrids.Select(g =>
                String.Format("{0}ed{1}{2}",
                    g.stepCount,
                    FindEDBaseLetter(g.baseInterval) ?? g.baseInterval.FormatFraction(),
                    g.basis == null ? "" : String.Format(" {0} {1}", g.basis[0], g.basis[1])
                )
            ));
        }
        private static string FindEDBaseLetter(Rational b) {
            return _edBases
                .Where(i => i.Value.Equals(b))
                .Select(i => i.Key)
                .FirstOrDefault();
        }
        private static Dictionary<string, Rational> _edBases = new Dictionary<string, Rational> {
            { "o", new Rational(2) },  // edo
            { "t", new Rational(3) },  // edt
            { "f", new Rational(3,2) } // edf
        };
        public static GridDrawer.EDGrid[] ParseEDGrids(string grids) {
            if (String.IsNullOrWhiteSpace(grids)) return null;
            string[] parts = grids.ToLower().Split(',', ';');
            var result = new GridDrawer.EDGrid[parts.Length];
            for (int i = 0; i < parts.Length; ++i) {
                string[] ps = parts[i].Split(new[] { "ed", "-", " " }, StringSplitOptions.RemoveEmptyEntries);
                int pn = ps.Length;
                if (pn != 2 && pn != 4) return null;
                //
                var g = new GridDrawer.EDGrid();
                if (!int.TryParse(ps[0], out g.stepCount)) return null;
                if (g.stepCount <= 0) return null;
                if (!_edBases.TryGetValue(ps[1], out g.baseInterval)) {
                    g.baseInterval = Rational.Parse(ps[1]);
                    if (g.baseInterval.IsDefault()) return null;
                }
                if (pn == 4) {
                    g.basis = new int[2];
                    if (!int.TryParse(ps[2], out g.basis[0])) return null;
                    if (!int.TryParse(ps[3], out g.basis[1])) return null;
                    // validate
                    g.basis[0] = Rationals.Utils.Mod(g.basis[0], g.stepCount);
                    g.basis[1] = Rationals.Utils.Mod(g.basis[1], g.stepCount);
                }
                //
                result[i] = g;
            }
            return result;
        }
        #endregion ED Grids

        #region Highlight
        public static string FormatIntervals(Drawing.SomeInterval[] ts) {
            if (ts == null) return "";
            return String.Join(", ", ts.Select(t => t.ToString()));
        }
        public static Drawing.SomeInterval[] ParseIntervals(string textIntervals) {
            if (String.IsNullOrWhiteSpace(textIntervals)) return null;
            string[] parts = textIntervals.Trim().ToLower().Split(";, ".ToArray(), StringSplitOptions.RemoveEmptyEntries);
            var intervals = new Drawing.SomeInterval[parts.Length];
            for (int i = 0; i < parts.Length; ++i) {
                var t = Drawing.SomeInterval.Parse(parts[i]);
                if (t == null) return null; // invalid format
                intervals[i] = t;
            }
            return intervals;
        }
        #endregion Highlight

        #region Presets
        public static DrawerSettings Reset() {
            var s = DrawerSettings.Edo12();

            // Common settings
            s.rationalCountLimit = 500;
            s.pointRadiusLinear = 0f;

            return s;
        }

        // Serialization
        public static void Save(DrawerSettings s, XmlWriter w) {
            //
            w.WriteElementString("limitPrime", s.subgroup != null ? "" : Rationals.Utils.GetPrime(s.limitPrimeIndex).ToString());
            w.WriteElementString("subgroup", JoinRationals(s.subgroup, "."));
            w.WriteElementString("narrows", JoinRationals(s.narrows, "."));
            //
            w.WriteElementString("harmonicityName", s.harmonicityName);
            w.WriteElementString("rationalCountLimit", s.rationalCountLimit.ToString());
            //
            w.WriteElementString("slopeOrigin", FormatRational(s.slopeOrigin));
            w.WriteElementString("slopeChainTurns", s.slopeChainTurns.ToString());
            //
            //w.WriteElementString("minimalStep", s.stepMinHarmonicity.ToString());
            //w.WriteElementString("stepSizeCountLimit", s.stepSizeMaxCount.ToString());
            //
            w.WriteElementString("selection", FormatIntervals(s.selection));
            if (s.temperament != null) {
                foreach (Rational.Tempered t in s.temperament) {
                    w.WriteStartElement("temper");
                    w.WriteAttributeString("rational", t.rational.FormatFraction());
                    w.WriteAttributeString("cents", t.cents.ToString());
                    w.WriteEndElement();
                }
            }
            w.WriteElementString("temperamentMeasure", s.temperamentMeasure.ToString());
            w.WriteElementString("edGrids", FormatEDGrids(s.edGrids));
            w.WriteElementString("pointRadius", s.pointRadiusLinear.ToString());
        }

        public static DrawerSettings Load(XmlReader r) {
            var s = new DrawerSettings { };
            var ts = new List<Rational.Tempered>();
            while (r.Read()) {
                if (r.NodeType == XmlNodeType.Element) {
                    switch (r.Name) {
                        case "limitPrime": {
                            Rational limitPrime = Rational.Parse(r.ReadElementContentAsString());
                            if (!limitPrime.IsDefault()) {
                                s.limitPrimeIndex = limitPrime.GetPowerCount() - 1;
                            }
                            break;
                        }
                        case "subgroup": {
                            s.subgroup = ParseRationals(r.ReadElementContentAsString());
                            break;
                        }
                        case "narrows": {
                            s.narrows = ParseRationals(r.ReadElementContentAsString());
                            s.narrows = Rational.ValidateNarrows(s.narrows);
                            break;
                        }
                        //
                        case "harmonicityName":     s.harmonicityName    = r.ReadElementContentAsString();   break;
                        case "rationalCountLimit":  s.rationalCountLimit = r.ReadElementContentAsInt();      break;
                        //
                        case "slopeOrigin":         s.slopeOrigin        = Rational.Parse(r.ReadElementContentAsString()); break;
                        case "slopeChainTurns":     s.slopeChainTurns    = r.ReadElementContentAsFloat();    break;
                        //
                        //case "minimalStep":         s.stepMinHarmonicity = r.ReadElementContentAsFloat();    break;
                        //case "stepSizeCountLimit":  s.stepSizeMaxCount   = r.ReadElementContentAsInt();      break;
                        //
                        case "selection":           s.selection = ParseIntervals(r.ReadElementContentAsString()); break;
                        case "temper": {
                            var t = new Rational.Tempered { };
                            t.rational = Rational.Parse(r.GetAttribute("rational"));
                            float.TryParse(r.GetAttribute("cents"), out t.cents);
                            ts.Add(t);
                            break;
                        }
                        case "temperamentMeasure":  s.temperamentMeasure = r.ReadElementContentAsFloat();    break;
                        case "edGrids":             s.edGrids = ParseEDGrids(r.ReadElementContentAsString());break;
                        case "pointRadius":         s.pointRadiusLinear  = r.ReadElementContentAsFloat();    break;
                    }
                }
            }
            if (ts.Count > 0) s.temperament = ts.ToArray();
            return s;
        }
        #endregion Presets

    }
}

