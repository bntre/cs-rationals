﻿using System;
//using System.Linq;
//using System.Diagnostics;
using System.Collections.Generic;

namespace Rationals.Drawing
{
    using Torec.Drawing;
    using Color = System.Drawing.Color;
    using Matrix = Vectors.Matrix;

    public class GridDrawer
    {
        // System settings
        //private string _font;

        // Base settings
        private Subgroup _subgroup;
        private RationalColors _colors; // per prime index !!!?

        // Generation
        //private string _harmonicityName;
        private IHarmonicity _harmonicity; // chosen harmonicity also used for sound (playing partials) - move it outside
        private int _rationalCountLimit;
        private Rational _distanceLimit;
        private Item[] _items;
        //private Bands<Item> _bands = null; //!!! not used

        // temperament
        private Temperament _temperament = new Temperament();

        // interval tree
        //private IntervalTree<Item, float> _intervalTree = null;
        //private BaseSubIntervals _baseSubIntervals = null; // used for HarmonicityUpDown (FindRationalByHarmonicity functionality)

        // degrees
        //private int _degreeCount = 0;
        private float _degreeThreshold = 0f;
        private Bands<Item> _degreeBands = null; // used only for approximate sorting items
        private List<Degree> _degrees = null; // all degrees produced by the items
        //private Item[] _baseDegrees = null; // used in DrawDegreeStepLines
        private Rational[] _degreesBase = null; // origins of degrees within base interval; e.g. [One,.. Two) - used in GetKeyboardInterval only 
        private int _degreeTurnSize = 0; // !!!? - used in GetKeyboardInterval only

        // slope & basis
        private float _octaveWidth; // octave width in user units
        // narrow stuff; tempered. per prime index (like Subgroup._narrows); may contain null-s
        private float[] _narrowCents;
        private Point[] _narrowVectors; // 2D basis vectors
        private float _basisDistortion = 0f; // add some distortion to better see comma structure  -- make configurable !!!
        //private float _basisDistortion = -0.01f;

        // bounds and point radius factor
        private Point[] _bounds;
        private float _pointRadius = _defaultPointRadius;
        private const float _defaultPointRadius = 0.05f;

        // Equal division grids
        private EDGrid[] _edGrids;
        private static Color[] _gridColors = GenerateGridColors(10);

        // Selection
        private SomeInterval[] _selection;

        // Highlighting
        private CursorHighlightMode _cursorHighlightMode = CursorHighlightMode.None;
        public enum CursorHighlightMode { None = 0, NearestRational, Cents }
        private Rational[] _partials = null; // highlight partials with horizontal lines. null to disable, empty array to default integer partials.

        // Stuff accessed from owner.
        // This stuff should be held outside and set here by owner !!!
        public Subgroup Subgroup { get { return _subgroup; } } 
        public Temperament Temperament { get { return _temperament; } }


        private enum UpdateFlags {
            None            = 0,
            Items           = 1, // regenerate items
            Basis           = 2, // recreate basis  -- also slope ? !!!
            Degrees         = 4,
            //Slope           = 4,
            RadiusFactor    = 8,
            Bounds          = 16,
        }

        UpdateFlags _updateFlags = UpdateFlags.None;

        public struct EDGrid { // equal division grid: https://en.xen.wiki/w/Equal-step_tuning
            public Rational baseInterval; // e.g. Octave
            public int stepCount;
            public int[] basis; // 2 step indices for graphical grid basis. optional

            #region Format & Parse
            public static string Format(GridDrawer.EDGrid[] edGrids) {
                if (edGrids == null) return "";
                string[] result = new string[edGrids.Length];
                for (int i = 0; i < edGrids.Length; ++i) {
                    var g = edGrids[i];
                    result[i] = String.Format("{0}ed{1}{2}",
                        g.stepCount,
                        FindEDBaseLetter(g.baseInterval) ?? g.baseInterval.FormatFraction(),
                        g.basis == null ? "" : String.Format(" {0} {1}", g.basis[0], g.basis[1])
                    );
                }
                return String.Join("; ", result);
            }
            private static Dictionary<string, Rational> _edBases = new Dictionary<string, Rational> {
                { "o", new Rational(2) },  // edo
                { "t", new Rational(3) },  // edt
                { "f", new Rational(3,2) } // edf
            };
            private static string FindEDBaseLetter(Rational b) {
                foreach (var k in _edBases) {
                    if (b.Equals(k.Value)) return k.Key;
                }
                return null;
            }
            public static EDGrid[] Parse(string grids) {
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
            #endregion Format & Parse
        }

        // Degree is a group of items located at a distance from each other not exceeding a defined threshold.
        // Dergee 
        // check if all needed !!!
        [System.Diagnostics.DebuggerDisplay("{originRational.FormatFraction()}")]
        private class Degree {
            public float origin; // main item cents
            public float begin; // in cents; range of the degree
            public float end;
            public List<Item> items = null; //!!! needed all of them ?
            //
            public Item originItem { get { return items[0]; } }
            //public Rational originRational { get { return originItem.rational; } }
            // degree chain
            //public Degree next = null;
            //public Degree prev = null;
            //
            public static int CompareOrigins(Degree a, Degree b) { return a.origin.CompareTo(b.origin); } // sorting
        }

        [System.Diagnostics.DebuggerDisplay("{DebuggerFormat()}")]
        private class Item {
            // base
            public Rational rational;
            public Item parent;
            // harmonicity
            //public double distance; //!!! make float
            public float harmonicity; // 0..1
            //
            public string id;
            public Color[] colors; // [Point, Line] colors
            // basis
            public float radius;
            public Point pos; // may be tempered
            public float cents; // may be tempered
            //public IntervalTree<Item, float>.Interval interval = null;
            // degree
            public Degree degree;
            // 
            public bool visible;
            
            /*
            // used for IntervalTree<Item, float>
            public static float GetCents(Item a) { return a.cents; }
            public static float GetHarmonicity(Item a) { return a.harmonicity; }
            */

            public static int CompareHarmonicity(Item a, Item b) { return b.harmonicity.CompareTo(a.harmonicity); } // more harmonic first
            //
            private string DebuggerFormat() {
                return String.Format("{0} <- {1}",
                    rational.FormatFraction(),
                    parent == null ? "" : parent.rational.FormatFraction()
                );
            }
        }

        // Cursor
        private float _cursorCents;
        private Item _cursorItem;

        //
        public GridDrawer() {
            InitHighlightColors();
        }
        public void SetSystemSettings(string font) {
            if (!String.IsNullOrWhiteSpace(font)) {
                Image.FontFamily = font;
            }
        }

        public void SetSubgroup(int limitPrimeIndex, Rational[] subgroup, Rational[] customNarrows = null)
        {
            // subgroup
            if (subgroup == null) {
                _subgroup = new Subgroup(limitPrimeIndex);
            } else {
                _subgroup = new Subgroup(subgroup);
            }

            // narrows
            _subgroup.UpdateNarrows(customNarrows);
            _narrowCents = null; // depends on temperament - will be updated with basis

            /*
            // recreate NarrowHarmonicity ?
            NarrowHarmonicity.Narrows = _subgroup.GetNarrows(); //!!! temporal - propagate to NarrowHarmonicity
            if (_harmonicityName == "Narrow") { // _harmonicityName may be not set yet
                _harmonicity = HarmonicityUtils.CreateHarmonicity("Narrow", normalize: true);
            }
            */

            // colors
            _colors = new RationalColors(_subgroup.GetHighPrimeIndex() + 1);

            _updateFlags |= UpdateFlags.Items; // regenerate items
        }
        public void SetGeneration(string harmonicityName, int rationalCountLimit, Rational distanceLimit = default(Rational)) {
            //_harmonicityName = harmonicityName;
            _harmonicity = HarmonicityUtils.CreateHarmonicity(harmonicityName, normalize: true);
            _rationalCountLimit = rationalCountLimit;
            _distanceLimit = distanceLimit;
            _updateFlags |= UpdateFlags.Items; // regenerate items
        }
        public void SetSlope(Rational slopeOrigin, float slopeChainTurns) {
            if (slopeOrigin.IsDefault()) {
                slopeOrigin = new Rational(3, 2);
            }
            SetSlope(slopeOrigin.ToCents(), slopeChainTurns);
            _updateFlags |= UpdateFlags.Basis;
        }
        public void SetBounds(Point[] bounds) {
            _bounds = bounds;
            _updateFlags |= UpdateFlags.Bounds;
        }
        public void SetPointRadius(float pointRadiusLinear) {
            float value = (float)Math.Exp(pointRadiusLinear);
            _pointRadius = _defaultPointRadius * value;
            _updateFlags |= UpdateFlags.RadiusFactor;
        }
        public void SetSelection(SomeInterval[] selection) {
            _selection = selection;
        }
        public void SetEDGrids(EDGrid[] edGrids) {
            _edGrids = edGrids;
        }
        public void SetCursorHighlightMode(CursorHighlightMode mode) {
            _cursorHighlightMode = mode;
        }
        public void SetPartials(Rational[] partials) {
            _partials = partials;
            // no need to update items - just redrawing needed
        }

        public void SetTemperament(Tempered[] tempered) {
            _temperament.SetTemperament(tempered, _subgroup);
            _updateFlags |= UpdateFlags.Basis;
        }
        public void SetTemperamentMeasure(float value) {
            _temperament.SetMeasure(value);
            _updateFlags |= UpdateFlags.Basis;
        }

        public void SetDegrees(float threshold) {
            //_degreeCount = count;
            _degreeThreshold = threshold;
            _updateFlags |= UpdateFlags.Degrees;
        }

        #region Generate items
        private Dictionary<Rational, Item> _collectedItems; // temporal dict for generation

        protected void GenerateItems()
        {
            _cursorItem = null;
            _items = null;
            //_bands = new Bands<Item>();

            //int overage = 0;
            int overage = Math.Max(30, _rationalCountLimit * 1/10); // we generate extra rationals (then drop it) to smooth generated area
            //overage = 0;

            var limits = new RationalGenerator.Limits {
                rationalCount = _rationalCountLimit + overage,
                dimensionCount = _subgroup.GetItems().Length,
                distance = _distanceLimit.IsDefault() ? -1 : _harmonicity.GetDistance(_distanceLimit),
            };

            //Rational[] generatorBasis = _subgroup.GetItems();
            Rational[] generatorBasis = _subgroup.GetNarrowItems();
            // Generation by "narrow" basis is good e.g. for Euclidean harmonicity: 
            //   3/2
            //    1  5/4     - we get both 3/2 and 5/4 at once.
            // In case of "subgroup" we should wait for 5/2 (-> 5/4) which is "less" harmonic

            var generator = new RationalGenerator(_harmonicity, limits, generatorBasis);
            _collectedItems = new Dictionary<Rational, Item>();
            generator.Iterate(this.HandleGeneratedRational);
            
            var items = new List<Item>(_collectedItems.Values);
            items.Sort(Item.CompareHarmonicity);

            // drop the overage (from _collectedItems and items)
            overage = items.Count - _rationalCountLimit;
            if (overage > 0) {
                for (int i = 0; i < overage; ++i) {
                    _collectedItems.Remove(items[_rationalCountLimit + i].rational);
                }
                items.RemoveRange(_rationalCountLimit, overage);
            }

            // link to parents
            for (int i = 0; i < items.Count; ++i) {
                Rational r = items[i].rational;
                Rational p = _subgroup.GetNarrowParent(r);
                if (!p.IsDefault()) {
                    Item parent = null;
                    if (_collectedItems.TryGetValue(p, out parent)) {
                        items[i].parent = parent;
                    } else {
                        // do we need all parents ?
                    }
                }
            }

            _items = items.ToArray();
            _collectedItems = null;
        }

        protected int HandleGeneratedRational(Rational r, double distance) {
            if (!_collectedItems.ContainsKey(r)) { // probably already added as a parent
                AddItem(r, distance);
            }
            return 1; // always accept
        }

        private Item AddItem(Rational r, double distance = -1)
        {
            // A new rational is generated by our _subgroup

            /*
            // Check his parent was added before - or add it now.
            Item parentItem = null;
            Rational parent = _subgroup.GetNarrowParent(r);
            if (!parent.IsDefault()) {
                if (!_collectedItems.TryGetValue(parent, out parentItem)) {
                    //!!! should never happen: we put narrows to generator basis
                    parentItem = AddItem(parent);
                    //!!! here we should recheck _generatorLimits.rationalCount
                }
            }
            */
            if (distance < 0) {
                distance = _harmonicity.GetDistance(r);
            }

            Item item = new Item {
                rational = r,
                //parent = parentItem,
                harmonicity = (float)HarmonicityUtils.GetHarmonicity(distance) // 0..1
            };

            _collectedItems[r] = item;

            return item;
        }

        //!!! harmonicity might be held outside of Drawer
        public float GetRationalHarmonicity(Rational r) {
            if (_harmonicity == null) return 0.0f;
            return (float)_harmonicity.GetHarmonicity(r);
        }
        #endregion

        private void SetSlope(double slopeCents, float slopeTurns) {
            // Set octave width in user units
            double d = slopeCents / 1200.0; // 0..1
            _octaveWidth = (float)(slopeTurns / d);
        }

        public void SetCursor(float cx, float cy) {
            float w = _octaveWidth * cy;
            float o = cx - w;
            int i = (int)Math.Round(o); // chain index
            _cursorCents = (cx - i) / _octaveWidth * 1200f;
        }
        public float GetCursorCents() {
            return _cursorCents;
        }
        public Rational GetCursorRational() {
            if (_cursorItem != null) return _cursorItem.rational;
            return default(Rational);
        }
        public void UpdateCursorItem() {
            _cursorItem = null;
            if (_items != null && _cursorHighlightMode == CursorHighlightMode.NearestRational) {
                //!!! here should be the search by band neighbors
                float dist = float.MaxValue;
                for (int i = 0; i < _items.Length; ++i) {
                    Item item = _items[i];
                    if (item.visible) {
                        float d = Math.Abs(item.cents - _cursorCents);
                        if (dist > d) {
                            dist = d;
                            _cursorItem = item;
                        }
                    }
                }
            }
        }

        private void UpdateBasis() {
            if (_octaveWidth == 0f) {
                _narrowCents   = null;
                _narrowVectors = null;
                return;
            }

            // Set basis
            int basisSize = _subgroup.GetHighPrimeIndex() + 1;
            _narrowCents   = new float[basisSize];
            _narrowVectors = new Point[basisSize];

            for (int i = 0; i < basisSize; ++i) {
                Rational n = _subgroup.GetNarrow(i);
                if (n.IsDefault()) continue;

                float narrowCents = _temperament.CalculateMeasuredCents(n);

                _narrowCents  [i] = narrowCents;
                _narrowVectors[i] = GetPoint(narrowCents);

                // add some distortion to better see comma structure
                if (_basisDistortion != 0) {
                    _narrowVectors[i].Y *= (float)Math.Exp(_basisDistortion * i);
                }
            }

            // also reset interval tree - we will fill it with items by cents
            //_intervalTree = new IntervalTree<Item, float>(Item.GetCents); //!!! do we need it always
            // also reset base sub intervals - we will fill it later
            //_baseSubIntervals = new BaseSubIntervals(_harmonicity);
        }

        private Point GetPoint(double cents, bool round = true) {
            double d = cents / 1200; // 0..1
            float y = (float)d;
            float x = (float)d * _octaveWidth;
            if (round) {
                x -= (float)Math.Round(x);
            }
            return new Point(x, y);
        }

        private void UpdateItemPos(Item item) {
            if (_narrowCents == null || _narrowVectors == null) throw new Exception("Basis not set");

            float c = 0f;
            Point p = new Point(0, 0);

            var pows = _subgroup.GetNarrowPowers(item.rational);
            if (pows == null) {
                throw new Exception("Narrows can't solve the item");
                //System.Diagnostics.Debug.WriteLine("Narrows ({0}) can't solve the item {1}",
                //    Rational.FormatRationals(_subgroup.GetNarrows()), item.rational
                //);
            } else {
                for (int i = 0; i < pows.Length; ++i) {
                    var e = pows[i];
                    if (e != 0) {
                        c += _narrowCents  [i] * e;
                        p += _narrowVectors[i] * e;
                    }
                }
            }
            item.cents = c;
            item.pos = p;
            
            // not used
            //item.interval = _intervalTree.Add(item); //!!! do we need all items in the tree (or "1/1 - 2/1" range only) ?
        }

        private void ResetDegrees() {
            if (_degreeThreshold == 0) {
                _degreeBands = null;
                _degrees     = null;
            } else {
                _degreeBands = new Bands<Item>(_degreeThreshold / 2);
                _degrees     = new List<Degree>();
            }
        }
        private void UpdateItemDegree(Item item)
        {
            if (_degreeThreshold == 0) { // disabled
                item.degree = null;
                return;
            }

            Degree nearest = null;
            float nearestDist = float.MaxValue;

            Item[] neighbors = _degreeBands.GetNeighbors(item.cents, _degreeThreshold);
            var unique = new HashSet<Degree>();
            for (int i = 0; i < neighbors.Length; ++i) {
                Degree d = neighbors[i].degree;
                if (d == null) continue; // should never occur
                if (!unique.Add(d)) continue; // already processed degree

                float dist = Math.Abs(item.cents - d.origin);
                if (dist < nearestDist) {
                    nearest = d;
                    nearestDist = dist;
                }
            }

            if (nearest != null && nearestDist <= _degreeThreshold) { //!!! here we might also check the distanse to the found degree origin item
                // add item to existing degree
                nearest.begin = Math.Min(nearest.begin, item.cents);
                nearest.end   = Math.Max(nearest.end,   item.cents);
                nearest.items.Add(item);
                item.degree = nearest;
            } else {
                Degree degree = new Degree {
                    origin = item.cents,
                    begin  = item.cents,
                    end    = item.cents,
                    items  = new List<Item> { item },
                };
                item.degree = degree;
                _degrees.Add(degree);
            }

            _degreeBands.AddItem(item.cents, item);
        }

        private void ProcessDegrees() {
            if (_degrees == null) return;

            // Sort degrees
            _degrees.Sort(Degree.CompareOrigins);

            // Find the base degree (Rational.One)
            int baseIndex = -1;
            for (int i = 0; i < _degrees.Count; ++i) {
                if (_degrees[i].originItem.rational.Equals(Rational.One)) {
                    baseIndex = i;
                    break;
                }
            }

            Rational baseInterval = _subgroup.GetBaseItem(); // e.g. 2

            _degreesBase = null;
            if (baseIndex != -1) {
                var bs = new List<Rational>();
                for (int i = baseIndex; i < _degrees.Count; ++i) {
                    Rational r = _degrees[i].originItem.rational;
                    if (r.Equals(baseInterval)) {
                        break;
                    } else {
                        bs.Add(r);
                    }
                }
                _degreesBase = bs.ToArray(); // all degrees within base interval found
            }

            _degreeTurnSize = 0;
            if (_degreesBase != null) {
                for (int i = 0; ; ++i) {
                    Rational b = baseInterval.Power(i);
                    for (int d = 0; d < _degreesBase.Length; ++d) {
                        Rational s = b * _degreesBase[d];
                        double x = s.ToCents() / 1200 * _octaveWidth;
                        if (x > 1.0) return; // don't overflow
                        _degreeTurnSize = _degreesBase.Length * i + d;
                    }
                }
            }

        }

        public SomeInterval GetKeyboardInterval(int x, int y, int flags) {
            if (_degreesBase == null || _degreeTurnSize == 0) return default(SomeInterval);

            int i = x + y * _degreeTurnSize;
            int d = Rationals.Utils.Div(i, _degreesBase.Length);
            int m = Rationals.Utils.Mod(i, _degreesBase.Length);

            Rational r = _subgroup.GetBaseItem().Power(d) * _degreesBase[m];

            return new SomeInterval { rational = r };
        }

#if false
#region Base sub intervals
        private struct SubInterval {
            public Rational rational;
            public float harmonicity;
            //
            public static float GetHarmonicity(SubInterval r) { return r.harmonicity; }
        }
        private class BaseSubIntervals : IntervalTree<SubInterval, float> {
            private IHarmonicity _harmonicity;
            private HashSet<Rational> _rationals = new HashSet<Rational>();
            //
            public BaseSubIntervals(IHarmonicity harmonicity) : base(SubInterval.GetHarmonicity) {
                _harmonicity = harmonicity;
            }
            public void AddSubInterval(Rational r) {
                if (_rationals.Add(r)) {
                    float h = GetRationalHarmonicity(r);
                    base.Add(new SubInterval { rational = r, harmonicity = h });
                }
            }
            public bool HandleSubInterval(Item i0, Item i1) { // used for IntervalTree.IterateIntervals
                AddSubInterval(i1.rational / i0.rational);
                return true; // always go deeper
            }
        }

        private IntervalTree<Item, float>.Interval GetBaseInterval() {
            if (_intervalTree == null) return null;
            // enough items in tree ?
            if (_intervalTree.root.right == null) return null;
            Item item0 = _intervalTree.root.item;
            Item item1 = _intervalTree.root.right.item;
            if (item0 == null || item1 == null) return null;
            return _intervalTree.root.right.left;
        }

        private void CollectBaseSubIntervals() {
            _baseSubIntervals = null;

            var baseInterval = GetBaseInterval();
            if (baseInterval == null) return;

            _baseSubIntervals = new BaseSubIntervals(_harmonicity);
            _baseSubIntervals.AddSubInterval(Rational.One); // 1/1 is not a subinterval, so we add it directly (for better HarmonicityUpDown formatting)
            _intervalTree.IterateIntervals(_baseSubIntervals.HandleSubInterval, baseInterval);

#if DEBUG && false
            // trace the trees
            System.Diagnostics.Debug.WriteLine("-------------- cents tree :");
            foreach (var i in _intervalTree.GetLeveledItems(baseInterval)) {
                System.Diagnostics.Debug.WriteLine("{0}{1}\t{2:F2}c", 
                    new String('·', i.level), 
                    i.item.rational.FormatFraction(),
                    i.item.cents
                );
            }
            System.Diagnostics.Debug.WriteLine("-------------- base subintervals by harmonicity:");
            foreach (var i in _baseSubIntervals.GetLeveledItems()) {
                System.Diagnostics.Debug.WriteLine("{0}{1}\t{2:F2}", 
                    new String('·', i.level), 
                    i.item.rational.FormatFraction(),
                    i.item.harmonicity * 100
                );
            }
#endif
        }

        public Rational FindRationalByHarmonicity(float harmonicity, float threshold) { // used for HarmonicityUpDown
            if (_baseSubIntervals == null) return default(Rational);
            //
            SubInterval i0, i1;
            _baseSubIntervals.FindIntervalRange(harmonicity, out i0, out i1);

            float minD = float.MaxValue;
            Rational r = default(Rational);
            if (!i0.rational.IsDefault()) {
                minD = harmonicity - i0.harmonicity;
                r = i0.rational;
            }
            if (!i1.rational.IsDefault()) {
                float d = i1.harmonicity - harmonicity;
                if (minD > d) {
                    minD = d;
                    r = i1.rational;
                }
            }

            if (!r.IsDefault() && minD <= threshold) {
                return r;
            }

            return default(Rational);
        }
#endregion
#endif

#if false // degrees (stuff about unique step count limit)
        private bool IsValidStep(Rational step) {
            if (_stepMinHarmonicity == 0) return true;
            if (_harmonicity == null) return false;
            float h = GetRationalHarmonicity(step);
            //return h > _stepMinHarmonicity; //!!! excluding min ?
            // 16/15: 0.475064933 > 0.4750649 !!!
            return h > _stepMinHarmonicity + 0.0000001f; // excluding
        }

        private void FindBaseDegrees() {
            _baseDegrees = null;

            if (_stepSizeMaxCount == 0) return; // degrees disabled
            if (_intervalTree == null) return;

            var baseInterval = GetBaseInterval();
            if (baseInterval == null) return;

            var nodes = new LinkedList<Item>();
            var baseNode0 = nodes.AddLast(_intervalTree.GetIntervalLeftItem(baseInterval));
            var baseNode1 = nodes.AddLast(_intervalTree.GetIntervalRightItem(baseInterval));
            //
            var knownIntervals = new Dictionary<IntervalTree<Item, float>.Interval, LinkedListNode<Item>>();
            knownIntervals[baseInterval] = baseNode0;
            //
            var baseItems = new List<Item>();
            _intervalTree.GetItems(baseItems, baseInterval);
            baseItems.Sort(Item.CompareHarmonicity);
            for (int i = -1; i < baseItems.Count; ++i)
            {
                if (i >= 0) {
                    var interval = baseItems[i].interval;

                    LinkedListNode<Item> node0;
                    if (!knownIntervals.TryGetValue(interval, out node0)) {
                        continue; // this subtree was skipped due to insufficient harmonicity
                    }

                    Rational r0 = node0.Value.rational;
                    Rational r1 = node0.Next.Value.rational;
                    Rational rN = interval.item.rational; // new
                    bool validStep = IsValidStep(rN / r0) && IsValidStep(r1 / rN); // depends on _stepMinHarmonicity
                    if (!validStep) continue; // skip this item and the whole subtree below

                    // add new node to LinkedList
                    LinkedListNode<Item> nodeN = nodes.AddAfter(node0, interval.item);
                    knownIntervals.Remove(interval);
                    knownIntervals[interval.left] = node0;
                    knownIntervals[interval.right] = nodeN;
                }

                // now check if the whole baseDegrees variant is valid
                Item[] baseDegrees = new Item[nodes.Count];
                nodes.CopyTo(baseDegrees, 0);
                bool validList = AreBaseDegreesValid(baseDegrees); // depends on _stepSizeMaxCount
                if (validList) {
                    _baseDegrees = baseDegrees; // save the last valid variant
                }

#if DEBUG && false
                System.Diagnostics.Debug.WriteLine("-------------- valid: {0}", validList);
                for (int j = 0; j < baseDegrees.Length; ++j) {
                    Rational R0 = baseDegrees[j].rational;
                    System.Diagnostics.Debug.Write(R0.FormatFraction());
                    if (j < baseDegrees.Length - 1) {
                        Rational R1 = baseDegrees[j + 1].rational; // next
                        Rational step = R1 / R0; // step
                        double h = GetRationalHarmonicity(step);
                        System.Diagnostics.Debug.WriteLine("\tstep:{0} ({1:F2})", step.FormatFraction(), h * 100);
                    } else {
                        System.Diagnostics.Debug.WriteLine("");
                    }
                }
#endif
            }
        }
        private bool AreBaseDegreesValid(Item[] items) {
            var uniqueSteps = new List<float>();
            for (int i = 1; i < items.Length; ++i) {
                float step = items[i].cents - items[i-1].cents;
                if (AddUniqueStep(uniqueSteps, step)) {
                    if (uniqueSteps.Count > _stepSizeMaxCount) {
                        return false;
                    }
                }
            }
            return true;
        }
        private bool AddUniqueStep(List<float> uniqueSteps, float step) {
            for (int i = 0; i < uniqueSteps.Count; ++i) {
                if (Math.Abs(uniqueSteps[i] - step) < 0.0001f) return false; // not unique -- !!! make configurable
            }
            uniqueSteps.Add(step);
            return true;
        }

        /*
        private void FilterDegrees() {
            if (_degrees == null || _items == null) return;

            // Link all degrees to the chain (with prev/next links)
            _degrees.Sort(Degree.CompareOrigins);
            Degree prev = null;
            for (int i = 0; i < _degrees.Count; ++i) {
                Degree cur = _degrees[i];
                cur.prev = prev;
                if (prev != null) prev.next = cur;
                prev = cur;
            }

            // Filter the chain by step size count limit
            if (_stepSizeMaxCount == 0) return; // filtering disabled

            var steps = new HashSet<Rational>(); //!!! here should be tempered cents
            Rational maxKnownStep = Rational.One;

            var uniqueDegrees = new HashSet<Degree>();
            var reachedDegrees = new HashSet<Degree>();

            for (int i = 0; i < _items.Length; ++i) {
                Degree cur = _items[i].degree;
                if (!uniqueDegrees.Add(cur)) continue; // already processed
                reachedDegrees.Add(cur);

                Degree d;

                // find best next
                {
                    bool full = steps.Count == _stepSizeMaxCount;
                    var ds = new List<Degree>(); // collecting variants

                    Degree chosen = null;

                    d = cur.next;
                    while (d != null) {

                        if (uniqueDegrees.Contains(d) && d.prev != null) {
                            if (d.prev != cur && d.prev.originItem.harmonicity > cur.originItem.harmonicity) {
                                d = d.next;
                                continue;
                            }
                        }

                        Rational step = d.originRational / cur.originRational;

                        if (full) {
                            // stop ?
                            if (step > maxKnownStep) {
                                chosen = FindBestDegree(ds);
                                break;
                            }
                            // choose ?
                            if (steps.Contains(step)) {
                                if (reachedDegrees.Contains(d)) {
                                    chosen = d;
                                    break;
                                } else {
                                    ds.Add(d);
                                }
                            }
                        } else {
                            // choose ?
                            if (reachedDegrees.Contains(d)) { //!!! prefer reachedDegrees ?
                                chosen = d;
                                break;
                            } else {
                                ds.Add(d);
                            }

                            // stop ?
                            //if (step > maxKnownStep && ds.Count > 0) {
                            if (ds.Count >= _stepSizeMaxCount) {
                                chosen = FindBestDegree(ds);
                                break;
                            }
                        }

                        //
                        d = d.next;
                    }
                    //
                    d = chosen;
                    if (!full && d != null) {
                        Rational step = d.originRational / cur.originRational; //!!! calculated twice
                        if (steps.Add(step)) {
                            if (maxKnownStep < step) {
                                maxKnownStep = step;
                            }
                        }
                    }
                    reachedDegrees.Add(d);
                }
                
                // link
                if (d != null) {
                    if (d.prev != null) {
                        d.prev.next = null;
                    }
                    d.prev = cur;
                }
                if (cur.next != null) {
                    cur.next.prev = null;
                }
                cur.next = d;

                // find valid prev
                {
                    bool full = steps.Count == _stepSizeMaxCount;
                    var ds = new List<Degree>(); // collecting variants

                    Degree chosen = null;

                    d = cur.prev;
                    while (d != null) {

                        if (uniqueDegrees.Contains(d) && d.next != null) {
                            if (d.next != cur && d.next.originItem.harmonicity > cur.originItem.harmonicity) {
                                d = d.prev;
                                continue;
                            }
                        }

                        Rational step = cur.originRational / d.originRational;

                        if (full) {
                            // stop ?
                            if (step > maxKnownStep) {
                                chosen = FindBestDegree(ds);
                                break;
                            }
                            // choose ?
                            if (steps.Contains(step)) {
                                if (reachedDegrees.Contains(d)) {
                                    chosen = d;
                                    break;
                                } else {
                                    ds.Add(d);
                                }
                            }
                        } else {
                            // choose ?
                            if (reachedDegrees.Contains(d)) { //!!! prefer reachedDegrees ?
                                chosen = d;
                                break;
                            } else {
                                ds.Add(d);
                            }

                             // stop ?
                            if (step > maxKnownStep && ds.Count > 0) {
                                chosen = FindBestDegree(ds);
                                break;
                            }
                       }

                        //
                        d = d.prev;
                    }
                    //
                    d = chosen;
                    if (!full && d != null) {
                        Rational step = cur.originRational / d.originRational; //!!! calculated twice
                        if (steps.Add(step)) {
                            if (maxKnownStep < step) {
                                maxKnownStep = step;
                            }
                        }
                    }
                    reachedDegrees.Add(d);
                }

                // link
                if (d != null) {
                    if (d.next != null) {
                        d.next.prev = null;
                    }
                    d.next = cur;
                }
                if (cur.prev != null) {
                    cur.prev.next = null;
                }
                cur.prev = d;

            }
        }

        private static Degree FindBestDegree(List<Degree> degrees) {
            Degree best = null;
            float bestHarmonicity = 0;
            for (int i = 0; i < degrees.Count; ++i) {
                Degree d = degrees[i];
                float h = d.originItem.harmonicity;
                if (bestHarmonicity < h) {
                    bestHarmonicity = h;
                    best = d;
                }
            }
            return best;
        }
        */

        private static bool CheckStep(Rational step, int stepCountLimit, HashSet<Rational> steps, ref Rational maxStep) {
            if (steps.Count < stepCountLimit) {
                steps.Add(step);
                if (maxStep < step) {
                    maxStep = step;
                }
                return true;
            } else {
                return steps.Contains(step);
            }
        }
#endif

        private float GetPointRadius(float harmonicity) {
            return (float)Rationals.Utils.Interp(_pointRadius * 0.1, _pointRadius, harmonicity);
        }

#region Visibility range
        private static bool IsPointVisible(float pos, float radius, float r0, float r1) {
            float v0 = (pos + radius) - r0;
            float v1 = r1 - (pos - radius);
            //
            return v0 >= 0 && v1 >= 0;
        }
        private static void GetPointVisibleRange(
            float pos, float radius,
            float r0, float r1,
            float period,
            out int i0, out int i1) 
        {
            float v0 = (pos + radius) - r0;
            float v1 = r1 - (pos - radius);
            //
            i0 = -(int)Math.Floor(v0 / period);
            i1 =  (int)Math.Floor(v1 / period);
        }
        private bool IsPointVisible(float posY, float radius) {
            return IsPointVisible(posY, radius, _bounds[0].Y, _bounds[1].Y);
        }
        private void GetPointVisibleRangeX(float posX, float radius, out int i0, out int i1, float period = 1f) {
            GetPointVisibleRange(posX, radius, _bounds[0].X, _bounds[1].X, period, out i0, out i1);
        }
        private void GetPointVisibleRangeY(float posY, float radius, out int i0, out int i1, float period = 1f) {
            GetPointVisibleRange(posY, radius, _bounds[0].Y, _bounds[1].Y, period, out i0, out i1);
        }
#endregion

        private bool IsUpdating(UpdateFlags flags) {
            return (_updateFlags & flags) != 0;
        }

        public void UpdateItems() // Update items according to current _updateFlags - prepare to DrawGrid
        {
            // Generate
            if (IsUpdating(UpdateFlags.Items)) {
                GenerateItems();
            }
            if (_items == null) return;

            // Update basis
            if (IsUpdating(UpdateFlags.Items | UpdateFlags.Basis)) {
                UpdateBasis();
            }
            if (_narrowVectors == null) return;

            // Degrees
            if (IsUpdating(UpdateFlags.Items | UpdateFlags.Basis | UpdateFlags.Degrees)) {
                ResetDegrees();
            }

            // Visibility
            if (IsUpdating(UpdateFlags.Items | UpdateFlags.Basis | UpdateFlags.Degrees | UpdateFlags.RadiusFactor | UpdateFlags.Bounds))
            {
                for (int i = 0; i < _items.Length; ++i) {
                    Item item = _items[i];
                    if (IsUpdating(UpdateFlags.Items | UpdateFlags.Basis)) {
                        UpdateItemPos(item); // update .cents and .pos
                    }
                    if (IsUpdating(UpdateFlags.Items | UpdateFlags.Basis | UpdateFlags.Degrees)) {
                        UpdateItemDegree(item);
                    }
                    if (IsUpdating(UpdateFlags.Items | UpdateFlags.RadiusFactor)) {
                        item.radius = GetPointRadius(item.harmonicity);
                    }
                    if (IsUpdating(UpdateFlags.Items | UpdateFlags.Basis | UpdateFlags.RadiusFactor | UpdateFlags.Bounds)) {
                        item.visible = IsPointVisible(item.pos.Y, item.radius);
                    }
                }
            }

            if (IsUpdating(UpdateFlags.Items | UpdateFlags.Basis | UpdateFlags.Degrees)) {
                //CollectBaseSubIntervals(); // 1-to-base (by cents) items added to _intervalTree - now we can collect sub intervals
                //FilterDegrees();
                //FindBaseDegrees();
                ProcessDegrees(); // update some data when all degrees found
            }

            // reset update flags
            _updateFlags = UpdateFlags.None;
        }

        private const float _lineWidthFactor = 0.612f;

        private Image.Element _groupPartials;
        private Image.Element _groupLines;
        private Image.Element _groupPoints;
        private Image.Element _groupText;

#region Highlight colors
        //!!! move to RationalColors
        private const int _highlightColorCount = 5;
        private Color[] _highlightColors = null; // [color ]
        private void InitHighlightColors() {
            _highlightColors = new Color[_highlightColorCount];
            var hue = new RationalColors.HueSaturation { hue = 0.3f, saturation = 0.5f };
            for (int i = 0; i < _highlightColorCount; ++i) {
                float k = (float)i / (_highlightColorCount - 1);
                _highlightColors[i] = RationalColors.HslToColor(hue, Rationals.Utils.Interp(0.75, 0.3, k));
            }
        }
        private Color GetHighlightColor(int highlightIndex) {
            return _highlightColors[Math.Min(highlightIndex, _highlightColorCount - 1)];
        }
#endregion

        private void DrawItem(Image image, Item item, int highlightIndex = -1)
        {
            if (item.radius == 0) throw new Exception("Invalid item");

            if (item.id == null) { // set id and colors once for new harmonicity
                // id for image elements. actually needed for svg only!!!
                item.id = String.Format("{0} {1} {2}",
                    item.rational.FormatFraction(),
                    item.rational.FormatMonzo(),
                    item.harmonicity
                );
                // also set colors
                var hue = _colors.GetRationalHue(_subgroup.GetNarrowPowers(item.rational));
                item.colors = new Color[2] {
                    RationalColors.HslToColor(hue, Rationals.Utils.Interp(1, 0.4, item.harmonicity)),
                    RationalColors.HslToColor(hue, Rationals.Utils.Interp(0.4, 0, item.harmonicity)),
                };
            }

            int i0, i1;
            GetPointVisibleRangeX(item.pos.X, item.radius, out i0, out i1);

            // Point & Text
            if (item.visible)
            {
                bool selected = false;
                if (_selection != null) {
                    for (int i = 0; i < _selection.Length; ++i) {
                        SomeInterval t = _selection[i];
                        if (t.IsRational() && item.rational.Equals(t.rational)) {
                            selected = true;
                            break;
                        }
                    }
                }

                if (_partials != null) { // highlight partials
                    bool isPartial = _partials.Length == 0
                        ? item.rational.IsInteger()
                        : Array.IndexOf(_partials, item.rational) != -1;
                    if (isPartial) {
                        var points = new [] {
                            new Point(_bounds[0].X, item.pos.Y),
                            new Point(_bounds[1].X, item.pos.Y)
                        };
                        image.Line(points)
                            .Add(_groupPartials, index: 0)
                            .FillStroke(Color.Empty, Color.LawnGreen,
                                item.radius * _lineWidthFactor * 0.33f
                            );
                    }
                }

                for (int i = i0; i <= i1; ++i)
                {
                    Point p = item.pos;
                    p.X += i;

                    string id_i = item.id + "_" + i.ToString();

                    image.Circle(p, item.radius)
                        .Add(_groupPoints, index: -1, id: "c " + id_i)
                        .FillStroke(
                            //item.colors[0], 
                            highlightIndex == -1 ? 
                                item.colors[0] :
                                GetHighlightColor(highlightIndex),
                            selected ? Color.Red : Color.Empty,
                            selected ? 0.01f : 0f
                        );

                    string t = item.rational.FormatFraction("\n");
                    image.Text(p, t, fontSize: item.radius, lineLeading: 0.8f, align: Image.Align.Center, centerHeight: true)
                        .Add(_groupText, index: -1, id: "t " + id_i)
                        .FillStroke(item.colors[1], Color.Empty);
                }
            }

            // Line to parent
            if (item.parent != null && (item.visible || item.parent.visible))
            {
                int pi0, pi1;
                GetPointVisibleRangeX(item.parent.pos.X, item.parent.radius, out pi0, out pi1);
                pi0 = Math.Min(pi0, i0);
                pi1 = Math.Max(pi1, i1);

                for (int i = pi0; i <= pi1; ++i)
                {
                    Point p = item.pos;
                    Point pp = item.parent.pos;
                    p.X += i;
                    pp.X += i;

                    string id_i = item.id + "_" + i.ToString();

                    image.Line(p, pp, item.radius * _lineWidthFactor, item.parent.radius * _lineWidthFactor)
                        .Add(_groupLines, index: 0, id: "l " + id_i)
                        .FillStroke(
                            //item.colors[0],
                            highlightIndex == -1 ?
                                item.colors[0] :
                                GetHighlightColor(highlightIndex),
                            Color.Empty, 0f
                            //highlight == 0 ? Color.Empty : Color.Red,
                            //highlight == 0 ? 0f : _pointRadius * 0.05f
                        );
                }
            }
        }

        private void DrawCursor(Image image, double cursorCents) {
            Point pos = GetPoint(cursorCents);
            float radius = GetPointRadius(0.1f);
            int i0, i1;
            GetPointVisibleRangeX(pos.X, radius, out i0, out i1);
            for (int i = i0; i <= i1; ++i) {
                Point p = pos;
                p.X += i;
                string id_i = "cursor_" + i.ToString();
                image.Circle(p, radius)
                    .Add(_groupPoints, index: -1, id: "c " + id_i)
                    .FillStroke(GetHighlightColor(0), Color.Empty, _pointRadius * 0.15f);
            }
        }

        private void DrawDegree(Image image, Degree degree)
        {
            Color degreeColor = Color.Yellow;

            // draw degree origin
            if (degree.originItem.visible)
            {
                Item item = degree.originItem;
                float radius = item.radius * 1.2f;

                int i0, i1;
                GetPointVisibleRangeX(item.pos.X, radius, out i0, out i1);

                for (int i = i0; i <= i1; ++i) {
                    Point p = item.pos;
                    p.X += i;

                    image.Circle(p, radius)
                        .Add(_groupPoints, index: 0)
                        .FillStroke(degreeColor, Color.Empty, 0f);
                }
            }

            // draw degree line
            if (degree.begin != degree.end)
            {
                Point P0 = GetPoint(degree.begin, round: false);
                Point P1 = GetPoint(degree.end,   round: false);

                if (IsPointVisible(P0.Y, 0) || IsPointVisible(P1.Y, 0))
                {
                    int i00, i01;
                    int i10, i11;
                    GetPointVisibleRangeX(P0.X, 0, out i00, out i01);
                    GetPointVisibleRangeX(P1.X, 0, out i10, out i11);
                    int i0 = Math.Min(i00, i10);
                    int i1 = Math.Max(i01, i11);

                    float w = degree.originItem.radius * _lineWidthFactor * 0.62f;

                    for (int i = i0; i <= i1; ++i) {
                        Point p0 = P0;  p0.X += i;
                        Point p1 = P1;  p1.X += i;
                        image.Line(new[] { p0, p1 })
                            .Add(_groupLines)
                            .FillStroke(Color.Empty, degreeColor, w);
                    }
                }
            }
        }

#if false
        private void DrawDegreeStepLines(Image image) 
        {
            //for (int d = 0; d < _degrees.Count; ++d) {
            if (_baseDegrees == null) return;
            for (int d = 0; d < _baseDegrees.Length - 1; ++d) {
                /*
                Degree d0 = _degrees[d];
                //if (!d0.present) continue;
                Degree d1 = d0.next;
                if (d1 == null) continue;

                //Item item0 = _degrees[d-1].items[0];
                //Item item1 = _degrees[ d ].items[0];

                Item item0 = d0.items[0];
                Item item1 = d1.items[0];
                */
                Item item0 = _baseDegrees[d];
                Item item1 = _baseDegrees[d+1];

                if (!item0.visible && !item1.visible) continue;

                Point P0 = item0.pos;
                Point P1 = item1.pos;
                //P0.Y += item0.radius * 0.5f;
                //P1.Y -= item1.radius * 0.5f;
                P0.Y += _items[0].radius * 0.5f;
                P1.Y -= _items[0].radius * 0.5f;
                P1.X -= (float)Math.Round(P1.X - P0.X);

                int i00, i01;
                int i10, i11;
                GetPointVisibleRangeX(P0.X, 0, out i00, out i01);
                GetPointVisibleRangeX(P1.X, 0, out i10, out i11);
                int i0 = Math.Min(i00, i10);
                int i1 = Math.Max(i01, i11);

                float w0 = item0.radius * _lineWidthFactor * 0.62f;
                float w1 = item1.radius * _lineWidthFactor * 0.62f;

                for (int i = i0; i <= i1; ++i) {
                    Point p0 = P0; p0.X += i;
                    Point p1 = P1; p1.X += i;
                    image.Line(p0, p1, w0, w1)
                        .Add(_groupLines)
                        .FillStroke(Color.Blue, Color.Empty);
                }
            }
        }
#endif

        public void DrawGrid(Image image)
        {
            if (_items != null) {
                _groupPartials = image.Group().Add(id: "groupPartials");
                _groupLines    = image.Group().Add(id: "groupLines");
                _groupPoints   = image.Group().Add(id: "groupPoints");
                _groupText     = image.Group().Add(id: "groupText");

                // Find cursor parents to highlight
                List<Rational> hs = null;
                if (_cursorHighlightMode.HasFlag(CursorHighlightMode.NearestRational)) { // highlight nearest rational and its parents
                    hs = new List<Rational>();
                    for (Item c = _cursorItem; c != null; c = c.parent) {
                        hs.Add(c.rational);
                    }
                }

                // Draw items
                for (int i = 0; i < _items.Length; ++i) {
                    Item item = _items[i];
                    if (item.visible || (item.parent != null && item.parent.visible)) {
                        int hi = hs != null ? hs.IndexOf(item.rational) : -1;
                        DrawItem(image, item, hi);
                    }
                }

                // Draw comma lines
                if (_degrees != null) {
                    for (int i = 0; i < _degrees.Count; ++i) {
                        DrawDegree(image, _degrees[i]);
                    }
                    //DrawDegreeStepLines(image);
                }
            }

            if (_cursorHighlightMode.HasFlag(CursorHighlightMode.Cents)) { // highlight cursor cents
                DrawCursor(image, _cursorCents);
            }

            if (_edGrids != null) {
                for (int i = 0; i < _edGrids.Length; ++i) {
                    Color color = _gridColors[i % _gridColors.Length];
                    DrawEDGrid(image, _edGrids[i], color);
                }
            }
        }

        public string FormatSelectionInfo() {
            var b = new System.Text.StringBuilder();
            // Highlighted cursor
            b.AppendFormat("Cursor: {0}", Rationals.Utils.FormatCents(_cursorCents));
            b.AppendLine();
            Rational c = default(Rational);
            if (_cursorItem != null) {
                c = _cursorItem.rational;
                if (!c.IsDefault()) {
                    double pureCents = c.ToCents();
                    string name = Library.Find(c);
                    b.AppendFormat("{0} {1} {2} {3:F2}{4}c h:{5:F1}{6}\n", 
                        c.FormatFraction(), 
                        c.FormatMonzo(), 
                        _subgroup.FormatNarrowPowers(c),
                        pureCents,
                        !_temperament.IsSet() ? "" : 
                            (_cursorItem.cents - pureCents).ToString("+0.00;-0.00"),
                        _cursorItem.harmonicity * 100,
                        name != null ? ("\n" + name) : ""
                    );
                    b.AppendLine();
                    //b.AppendFormat("Distance {0:F3}", _harmonicity.GetDistance(c));
                    //b.AppendLine();
                }
            }
            // Selection
            if (_selection != null) {
                for (int i = 0; i < _selection.Length; ++i) {
                    SomeInterval t = _selection[i];
                    if (!c.IsDefault() && t.IsRational()) {
                        Rational ct = c / t.rational;
                        string name = Library.Find(ct);
                        b.AppendFormat("{0} : {1} = {2} ({3:F2}c) h:{4:F1}{5}",
                            c.FormatFraction(),
                            t.rational.FormatFraction(),
                            ct.FormatFraction(),
                            ct.ToCents(),
                            GetRationalHarmonicity(ct) * 100,
                            name != null ? ("\n" + name) : ""
                        );
                    } else {
                        b.Append(t.ToString());
                    }
                    b.AppendLine();
                }
            }
            return b.ToString();
        }

#region ED grids
        private static Color[] GenerateGridColors(int count) {
            Color[] result = new Color[count];
            for (int i = 0; i < count; ++i) {
                double h = ColorUtils.GetRareHue(i);
                h += 0.55; h -= Math.Floor(h); // shift phase to make blue first
                result[i] = ColorUtils.HslToColor(h, 0.5, 0.75);
            }
            return result;
        }

        private static void FindEDGridBasis(Point[] points, out int i1, out int i2) {
            int size = points.Length;
            var dists = new float[size];
            dists[0] = float.MaxValue; // we skip origin point
            // Fint i1
            i1 = 0;
            for (int i = 1; i < size; ++i) {
                Point p = points[i];
                dists[i] = p.X*p.X * 6.0f + p.Y*p.Y; //!!! ugly screw factor hardcoded here to avoid flatten basis
                if (dists[i] < dists[i1]) i1 = i;
            }
            Point p1 = points[i1];
            // Fint i2
            i2 = 0;
            for (int i = 1; i < size; ++i) {
                Point p = points[i];
                if (i == i1 || Math.Abs(p.X/p1.X - p.Y/p1.Y) < 0.0001) continue; // skip collinear vectors
                if (dists[i] < dists[i2]) i2 = i;
            }
        }

        public void DrawEDGrid(Image image, EDGrid edGrid, Color color) {
            if (_octaveWidth == 0) return; // no slope set yet

            int ed = edGrid.stepCount;

            // calculate ED grid points by cents
            Point[] points = new Point[ed];
            double baseCents = edGrid.baseInterval.ToCents();
            Point baseStep = GetPoint(baseCents); // base interval may be out of basis - so ask by cents
            for (int i = 0; i < ed; ++i) {
                double cents = baseCents * i/ed;
                points[i] = GetPoint(cents);
            }
            // choose two basis vectors to draw lines
            int[] basis = edGrid.basis;
            if (basis == null) {
                basis = new int[2];
                FindEDGridBasis(points, out basis[0], out basis[1]);
            }

            Image.Element group = image.Group().Add(index: -2); // put under groupText
            float lineWidth = 0.007f;

            for (int i = 0; i < ed; ++i) {
                for (int b = 0; b < 2; ++b) {
                    Point p0 = points[i];
                    Point p1 = points[i] + points[basis[b]];
                    int j0, j1;
                    GetPointVisibleRangeY(p0.Y, 0f, out j0, out j1, baseStep.Y);
                    for (int j = j0 - 1; j <= j1; ++j) {
                        Point basePoint = baseStep * j; // ..1/4 - 1/2 - 1 - 2 - 4..
                        int k0, k1, ktemp;
                        GetPointVisibleRangeX(basePoint.X + Math.Max(p0.X, p1.X), 0f, out k0, out ktemp);
                        GetPointVisibleRangeX(basePoint.X + Math.Min(p0.X, p1.X), 0f, out ktemp, out k1);
                        for (int k = k0; k <= k1; ++k) {
                            Point shift = new Point(1f, 0) * k;
                            Point[] ps = new[] { basePoint + p0 + shift, basePoint + p1 + shift };
                            if (i == 0) {
                                image.Line(ps[0], ps[1], lineWidth * 3, lineWidth)
                                    .Add(group)
                                    .FillStroke(color, Color.Empty);
                            } else {
                                image.Line(ps)
                                    .Add(group)
                                    .FillStroke(Color.Empty, color, lineWidth);
                            }
                        }
                    }
                }
            }
        }
#endregion

        /*
#region Stick commas
        private void UpdateItemCommaSpan(Item item) {
            if (_validCommaCount == 0) {
                item.commaSpan = null;
                return; // _commaSpans list stays empty
            }
            // find an existing span
            CommaSpan span;
            for (int i = 0; i < _commaSpans.Count; ++i) {
                span = _commaSpans[i];
                int[] coords = span.basisMatrix.FindCoordinates(item.rational);
                if (coords != null) {
                    int c = coords[_validCommaCount]; // span key coefficient
                    if (c == 0 || c == 1) { // "c == 0" means we should find the span with key "|0>" - this probably is the one.
                        // span found
                        item.commaSpan = span;
                        item.spanCoordinates = coords; // last coordinate ('c') not used !!!
                        return;
                    }
                }
            }
            // create new span for this key - seems might be optimized !!!
            Rational[] basis = new Rational[_validCommaCount + 1];
            int v = 0; // valid comma index
            for (int i = 0; i < _commas.Length; ++i) {
                if (!_commas[i].valid) continue;
                basis[v] = _commas[i].comma;
                ++v;
            }
            basis[_validCommaCount] = item.rational;
            var matrix = new Vectors.Matrix(basis, _maxPrimeIndex + 1);
            matrix.MakeEchelon();
            matrix.ReduceRows();
            span = new CommaSpan { key = item.rational, basisMatrix = matrix };
            _commaSpans.Add(span);
            //
            item.commaSpan = span;
            item.spanCoordinates = new int[_validCommaCount]; // zeros (key item)
        }
        private void UpdateCommaPoses() {
            if (_validCommaCount == 0) return;
            for (int i = 0; i < _commas.Length; ++i) {
                if (!_commas[i].valid) continue;
                Point pos = GetPoint(_commas[i].comma);
                pos.X -= (float)Math.Round(pos.X);
                _commas[i].pos = pos;
            }
        }
        private void UpdateCommaSpanPivot(CommaSpan span) { // called for each span
            Point p = new Point(0, 0);
            // weighted
            float fullWeight = 0;
            for (int i = 0; i < _commas.Length; ++i) {
                if (!_commas[i].valid) continue;
                for (int j = -5; j <= 5; ++j) {
                    Rational r = span.key * _commas[i].comma.Power(j);
                    float w = GetRationalHarmonicity(r); // 0..1
                    p += (_commas[i].pos * j) * w;
                    fullWeight += w;
                }
            }
            if (fullWeight != 0) {
                p /= fullWeight;
            }
            span.keyToPivot = p;
        }
        private Point GetCommaSpanPoint(int[] itemSpanCoordinates) {
            Point p = new Point(0, 0);
            int v = 0; // valid comma index
            for (int i = 0; i < _commas.Length; ++i) {
                if (!_commas[i].valid) continue;
                p += _commas[i].pos * itemSpanCoordinates[v];
                ++v;
            }
            return p;
        }
#endregion
        */
    }

    public class RationalColors
    {
        private double[] _primeHues;
        private double[] _hueWeights;

        private double _hueStep = 0.025; // !!! make configurable?

        public RationalColors(int count) {
            //_primeHues = new[] { 0, 0.0, 0.7, 0.4, 0.4, 0.4, 0.4, 0.4 };
            //_hueWeights = new[] { 0, 1.0, 0.3, 0.2, 0.2, 0.2, 0.2, 0.2 };

            // generate hues and weights
            count = Math.Max(count, 2); // we need 5ths hue (for octaves)
            _primeHues  = new double[count];
            _hueWeights = new double[count];
            for (int i = 1; i < count; ++i) { // ignore octave hue
                _primeHues[i] = ColorUtils.GetRareHue(i - 1);
                _hueWeights[i] = 1.0 / i;
            }
        }

        public struct HueSaturation {
            public double hue; // 0..1
            public double saturation; // 0..1
        }

        public HueSaturation GetRationalHue(int[] pows)
        {
            int len = Math.Max(pows.Length, 2); // use 5ths hue for octaves

            double[] hues = new double[len];
            for (int i = 0; i < len; ++i) {
                hues[i] = _primeHues[i] + Powers.SafeAt(pows, i) * _hueStep;
            }

            Point p = new Point(0, 0);
            for (int i = 0; i < len; ++i) {
                double a = hues[i] * Math.PI*2;
                p += new Point((float)Math.Cos(a), (float)Math.Sin(a)) * (float)_hueWeights[i];
            }

            double h = 0;
            if (p.X != 0 || p.Y != 0) {
                h = Math.Atan2(p.Y, p.X) / (Math.PI * 2);
            }
            h -= Math.Floor(h);

            double s = Math.Sqrt(p.X * p.X + p.Y * p.Y);
            s = Math.Min(1, s);

            return new HueSaturation { hue = (float)h, saturation = (float)s };
        }

        public static Color HslToColor(HueSaturation h, double lightness) {
            return ColorUtils.HslToColor(h.hue, h.saturation, lightness);
        }

    }

}
