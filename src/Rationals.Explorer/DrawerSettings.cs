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
        // subgroup (limit only or custom subgroup)
        public int limitPrimeIndex; // 0,1,2,..
        public Rational[] subgroup; // e.g. {3, 5, 7} (Bohlen-Pierce), {2, 3, 7/5},.. https://en.xen.wiki/w/Just_intonation_subgroups
        public Rational[] narrows; // custom user narrows

        // generating items
        public string harmonicityName; // null for some default
        public int rationalCountLimit; // -1 for unlimited

        // slope
        public Rational slopeOrigin; // starting point to define slope
        public float slopeChainTurns; // chain turn count to "slope origin" point. set an integer for vertical.

        // temperament
        public Tempered[] temperament; // user's unfiltered temperament, may contain invalid rationals
        public float temperamentMeasure; // 0..1

        // degrees
        //public int degreeCount;
        public float degreeThreshold;

        // grids
        public GridDrawer.EDGrid[] edGrids;

        // selection
        public SomeInterval[] selection;

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
#if DEBUG
            s.temperament = new[] {
                new Tempered { rational = new Rational(81, 80), cents = 0 },
            };
#endif
            //
            s.edGrids = new[] {
                new GridDrawer.EDGrid { stepCount = 12, baseInterval = Rational.Two }
            };
            //
            return s;
        }

        #region Base
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
        #endregion ED Grids

        #region Highlight
        public static string FormatIntervals(SomeInterval[] ts) {
            if (ts == null) return "";
            return String.Join(", ", ts.Select(t => t.ToString()));
        }
        public static SomeInterval[] ParseIntervals(string textIntervals) {
            if (String.IsNullOrWhiteSpace(textIntervals)) return null;
            string[] parts = textIntervals.Trim().ToLower().Split(";,".ToArray(), StringSplitOptions.RemoveEmptyEntries);
            var intervals = new SomeInterval[parts.Length];
            for (int i = 0; i < parts.Length; ++i) {
                var t = SomeInterval.Parse(parts[i]);
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
            w.WriteElementString("subgroup", Rational.FormatRationals(s.subgroup, "."));
            w.WriteElementString("narrows",  Rational.FormatRationals(s.narrows, "."));
            //
            w.WriteElementString("harmonicityName", s.harmonicityName);
            w.WriteElementString("rationalCountLimit", s.rationalCountLimit.ToString());
            //
            w.WriteElementString("slopeOrigin", s.slopeOrigin.FormatFraction());
            w.WriteElementString("slopeChainTurns", s.slopeChainTurns.ToString());
            //
            //w.WriteElementString("degreeCount", s.degreeCount.ToString());
            w.WriteElementString("degreeThreshold", s.degreeThreshold.ToString());
            //
            w.WriteElementString("selection", FormatIntervals(s.selection));
            if (s.temperament != null) {
                foreach (Tempered t in s.temperament) {
                    w.WriteStartElement("temper");
                    w.WriteAttributeString("rational", t.rational.FormatFraction());
                    w.WriteAttributeString("cents", t.cents.ToString());
                    w.WriteEndElement();
                }
            }
            w.WriteElementString("temperamentMeasure", s.temperamentMeasure.ToString());
            w.WriteElementString("edGrids", GridDrawer.EDGrid.Format(s.edGrids));
            w.WriteElementString("pointRadius", s.pointRadiusLinear.ToString());
        }

        public static DrawerSettings Load(XmlReader r) {
            var s = new DrawerSettings { };
            var ts = new List<Tempered>();
            while (r.Read()) {
                if (r.NodeType == XmlNodeType.Element) {
                    switch (r.Name) {
                        case "limitPrime": {
                            Rational limitPrime = Rational.Parse(r.ReadElementContentAsString()); // allow to be a rational
                            if (!limitPrime.IsDefault()) {
                                s.limitPrimeIndex = limitPrime.GetHighPrimeIndex();
                            }
                            break;
                        }
                        case "subgroup": {
                            s.subgroup = Rational.ParseRationals(r.ReadElementContentAsString());
                            break;
                        }
                        case "narrows": {
                            s.narrows = Rational.ParseRationals(r.ReadElementContentAsString());
                            s.narrows = NarrowUtils.ValidateNarrows(s.narrows);
                            break;
                        }
                        //
                        case "harmonicityName":     s.harmonicityName    = r.ReadElementContentAsString();   break;
                        case "rationalCountLimit":  s.rationalCountLimit = r.ReadElementContentAsInt();      break;
                        //
                        case "slopeOrigin":         s.slopeOrigin        = Rational.Parse(r.ReadElementContentAsString()); break;
                        case "slopeChainTurns":     s.slopeChainTurns    = r.ReadElementContentAsFloat();    break;
                        //
                        //case "degreeCount":         s.degreeCount        = r.ReadElementContentAsInt();      break;
                        case "degreeThreshold":     s.degreeThreshold    = r.ReadElementContentAsFloat();    break;
                        //
                        case "selection":           s.selection = ParseIntervals(r.ReadElementContentAsString()); break;
                        case "temper": {
                            var t = new Tempered { };
                            t.rational = Rational.Parse(r.GetAttribute("rational"));
                            float.TryParse(r.GetAttribute("cents"), out t.cents);
                            ts.Add(t);
                            break;
                        }
                        case "temperamentMeasure":  s.temperamentMeasure = r.ReadElementContentAsFloat();    break;
                        case "edGrids":             s.edGrids = GridDrawer.EDGrid.Parse(r.ReadElementContentAsString());break;
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

