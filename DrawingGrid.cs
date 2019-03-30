﻿using System;
using System.Collections.Generic;
//using System.Linq;

namespace Rationals.Drawing
{
    using Torec.Drawing;
    using Color = System.Drawing.Color;


    public class GridDrawer
    {
        // Base settings
        private Rational[] _subgroup;
        private int _dimensionCount;
        private int _minPrimeIndex; // smallest prime index. used for narrowing intervals
        private int _maxPrimeIndex; // largest prime index. used for basis size
        // Depend on base
        private Rational[] _narrowPrimes;
        private RationalColors _colors;

        // Generation
        private IHarmonicity _harmonicity;
        private int _rationalCountLimit;
        private Rational _distanceLimit;
        private Item[] _items;
        private Bands<Item> _bands;

        // slope & basis
        private float _octaveWidth; // octave width in user units
        private Point[] _basis; // basis vectors for all primes upto _topPrimeIndex

        // bounds and point radius factor
        private Point[] _bounds;
        private float _pointRadius = _defaultPointRadius;
        private const float _defaultPointRadius = 0.05f;

        // Stick commas
        private Rational[] _stickCommas;
        private float _stickMeasure; // 0..1
        //
        private Point[] _commaSpanBasis; // cached pos per sticking comma
        private List<CommaSpan> _commaSpans; // unique spans
        [System.Diagnostics.DebuggerDisplay("Span {key.FormatFraction()} {key.FormatMonzo()}")]
        private class CommaSpan {
            public Rational key;
            public Vectors.Matrix basisMatrix;
            public Point keyToPivot; // "span key to span pivot" vector - update with basis
        }

        // update levels
        private bool _updatedBase; // regenerate items
        private bool _updatedBasis;
        private bool _updatedRadiusFactor;
        private bool _updatedBounds;
        private bool _updatedCommas;
        private bool _updatedStickMeasure;

        // Equal division grids
        private EDGrid[] _edGrids;
        private static Color[] _gridColors = GenerateGridColors(10);


        public struct EDGrid { // equal division grid: https://en.xen.wiki/w/Equal-step_tuning
            public Rational baseInterval; // e.g. Octave
            public int stepCount;
            public int[] basis; // 2 step indices
        }

        public struct Settings { //!!! get rid of this structure?
            // primes
            public int limitPrimeIndex; // 0,1,2,..
            public Rational[] subgroup; // e.g. {3, 5, 7} (Bohlen-Pierce), {2, 3, 7/5},.. https://en.xen.wiki/w/Just_intonation_subgroups

            // generating items
            public string harmonicityName; // null for some default
            public int rationalCountLimit; // -1 for unlimited
            public Rational distanceLimit; // default(Rational) for unlimited

            // slope
            public Rational slopeOrigin; // starting point to define slope
            public float slopeChainTurns; // chain turn count to "slope origin" point. set an integer for vertical.

            // stick commas
            public Rational[] stickCommas;
            public float stickMeasure; // 0..1

            // grids
            public EDGrid[] edGrids;

            //
            public static Settings Edo12() {
                var s = new Settings();
                //
                s.limitPrimeIndex = 2; // 5-limit
                //
                s.slopeOrigin = new Rational(3, 2); // 5th
                s.slopeChainTurns = 2;
                //
                s.edGrids = new[] {
                    new EDGrid { stepCount = 12, baseInterval = Rational.Two }
                };
                //
                return s;
            }
        }

        [System.Diagnostics.DebuggerDisplay("{rational} <- {parent.rational}")]
        private class Item {
            // base
            public Rational rational;
            public Item parent;
            public float cents;

            // harmonicity
            public double distance;
            public float harmonicity; // 0..1
            //
            public string id;
            public Color[] colors; // [Point, Line] colors
            // viewport + basis
            public Point posOriginal; // by basis
            public Point pos; // probably shifted to comma span pivot (according to stickMeasure)
            public float radius;
            public bool visible;
            // stick commas
            public CommaSpan commaSpan;
            public int[] spanCoordinates;
            // update levels
            //public int updateBasis;
            //public int updateBounds;

            //
            public static int CompareDistance(Item a, Item b) { return a.distance.CompareTo(b.distance); }
        }

        //
        public GridDrawer() {
        }

        public void SetBase(int limitPrimeIndex, Rational[] subgroup, string harmonicityName) {
            if (subgroup != null) {
                _subgroup = subgroup;
                GetSubgroupPrimeRange(_subgroup, out _minPrimeIndex, out _maxPrimeIndex);
                _dimensionCount = _subgroup.Length;
            } else {
                _minPrimeIndex = 0;
                _maxPrimeIndex = limitPrimeIndex;
                _dimensionCount = limitPrimeIndex + 1;
                _subgroup = null;
            }
            _harmonicity = new HarmonicityNormalizer(
                Rationals.Utils.CreateHarmonicity(harmonicityName)
            );
            // 
            _narrowPrimes = Rational.GetNarrowPrimes(_maxPrimeIndex + 1, _minPrimeIndex);
            _colors = new RationalColors(_maxPrimeIndex + 1);
            //
            _updatedBase = true;
            // 
            UpdateBasis(); // update basis: prime count might change
            _updatedBasis = true;
        }
        public void SetGeneratorLimits(int rationalCountLimit, Rational distanceLimit) {
            _rationalCountLimit = rationalCountLimit;
            _distanceLimit = distanceLimit;
            _updatedBase = true;
        }
        public void SetSlope(Rational slopeOrigin, float slopeChainTurns) {
            if (slopeOrigin.IsDefault()) {
                slopeOrigin = new Rational(3, 2);
            }
            SetSlope(slopeOrigin.ToCents(), slopeChainTurns);
            UpdateBasis();
            _updatedBasis = true;
        }
        public void SetBounds(Point[] bounds) {
            _bounds = bounds;
            _updatedBounds = true;
        }
        public void SetPointRadiusFactor(float pointRadiusFactor) {
            _pointRadius = _defaultPointRadius * pointRadiusFactor;
            _updatedRadiusFactor = true;
        }
        public void SetEDGrids(EDGrid[] edGrids) {
            _edGrids = edGrids;
        }
        public void SetCommas(Rational[] commas) {
            _stickCommas = null;
            if (commas != null && commas.Length > 0) {
                _stickCommas = commas;
            }
            _commaSpans = new List<CommaSpan>(); // reset spans. call UpdateItemCommaSpan to recreate spans
            _updatedCommas = true;
        }
        public void SetStickMeasure(float value) {
            _stickMeasure = value;
            _updatedStickMeasure = true;
        }

        public static void GetSubgroupPrimeRange(Rational[] subgroup, out int minPrimeIndex, out int maxPrimeIndex) {
            var r = new Rational(1);
            for (int i = 0; i < subgroup.Length; ++i) {
                r *= subgroup[i];
            }
            int[] pows = r.GetPrimePowers();
            maxPrimeIndex = Powers.GetLength(pows) - 1;
            minPrimeIndex = 0;
            while (minPrimeIndex <= maxPrimeIndex && pows[minPrimeIndex] == 0) ++minPrimeIndex; // skip heading zeros
        }

        #region Generate items
        private Dictionary<Rational, Item> _generatedItems; // temporal dict for generation

        protected void GenerateItems() {
            _items = null;
            _bands = new Bands<Item>();
            var limits = new RationalGenerator.Limits {
                rationalCount = _rationalCountLimit,
                dimensionCount = _dimensionCount,
                distance = _distanceLimit.IsDefault() ? -1 : _harmonicity.GetDistance(_distanceLimit),
            };
            var generator = new RationalGenerator(_harmonicity, limits, _subgroup);
            _generatedItems = new Dictionary<Rational, Item>();
            generator.Iterate(this.HandleRational);
            var list = new List<Item>(_generatedItems.Values);
            list.Sort(Item.CompareDistance); // !!! do we need to sort?
            _items = list.ToArray();
            _generatedItems = null;
        }

        protected int HandleRational(Rational r, double distance) {
            if (!_generatedItems.ContainsKey(r)) { // probably already added as a parent
                AddItem(r, distance);
            }
            return 1; // always accept
        }

        private Item AddItem(Rational r, double distance = -1)
        {
            // make sure his parent is added
            Item parentItem = null;
            if (r.GetPowerCount() > 1) { // we don't draw lines between octaves
                Rational parent = GetNarrowParent(r);
                if (!parent.IsDefault() && !_generatedItems.TryGetValue(parent, out parentItem)) {
                    parentItem = AddItem(parent);
                    //!!! here we should recheck _generatorLimits.rationalCount
                }
            }
            if (distance < 0) {
                distance = _harmonicity.GetDistance(r);
            }

            Item item = new Item {
                rational = r,
                parent = parentItem,
                cents = (float)r.ToCents(),
                distance = distance,
            };

            bool inBands = _bands.AddItem(item.cents, item);
            if (!inBands) {
                //!!! skip if out of bands?
            }

            // also needed to get item visibility
            item.harmonicity = GetHarmonicity(distance); // 0..1
            item.radius = GetPointRadius(item.harmonicity);

            _generatedItems[r] = item;

            return item;
        }
        #endregion

        private static float GetHarmonicity(double distance) { //!!! might be moved out
            return (float)Math.Exp(-distance * 1.2); // 0..1
        }

        private Rational GetNarrowParent(Rational r) {
            int[] n = r.GetNarrowPowers(_narrowPrimes);
            int lastLevel = Powers.GetLength(n) - 1; // ignoring trailing zeros
            if (lastLevel < 0) return default(Rational); // no levels - the root
            Rational step = _narrowPrimes[lastLevel]; // last level step
            int lastPower = n[lastLevel]; // last level coordinate
            if (lastPower > 0) {
                return r / step;
            } else {
                return r * step;
            }
        }

        private void SetSlope(double slopeCents, float slopeTurns) {
            // Set octave width in user units
            double d = slopeCents / 1200; // 0..1
            _octaveWidth = (float)(slopeTurns / d);
        }

        public double GetCursorCents(float cx, float cy) {
            float w = _octaveWidth * cy;
            float o  = cx - w;
            int i = (int)Math.Round(o); // chain index
            return (cx - i) / _octaveWidth * 1200f;
        }

        private void UpdateBasis() {
            if (_octaveWidth == 0f) {
                _basis = null;
                return;
            }
            // Set basis
            int basisSize = _maxPrimeIndex + 1;
            _basis = new Point[basisSize];
            for (int i = 0; i < basisSize; ++i) {
                _basis[i] = GetPoint(_narrowPrimes[i].ToCents());

                // add some distortion to better see comma structure  -- make configurable !!!
                _basis[i].Y *= (float)Math.Exp(-0.006 * i);
            }
        }

        private Point GetPoint(double cents) {
            double d = cents / 1200; // 0..1
            float y = (float)d;
            float x = (float)d * _octaveWidth;
            x -= (float)Math.Round(x);
            return new Point(x, y);
        }

        private Point GetPoint(Rational r) {
            if (_basis == null) throw new Exception("Basis not set");
            int[] pows = r.GetNarrowPowers(_narrowPrimes);
            var p = new Point(0, 0);
            for (int i = 0; i < pows.Length; ++i) {
                if (pows[i] != 0) {
                    p += _basis[i] * pows[i];
                }
            }
            return p;
        }

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

        public void UpdateItems()
        {
            // Generate

            if (_updatedBase) {
                _updatedBase = false;
                GenerateItems();
            }

            // Update visibility

            if (_items == null) return;
            if (_basis == null) return;

            if (_updatedCommas) {
                // regenerate and reassign spans
                for (int i = 0; i < _items.Length; ++i) {
                    Item item = _items[i];
                    UpdateItemCommaSpan(item);
                }
            }

            if (_updatedBasis || _updatedCommas) {
                UpdateCommaSpanBasis();
                UpdateCommaSpanPivots();
            }

            if (_updatedBasis || _updatedRadiusFactor || _updatedBounds || _updatedCommas || _updatedStickMeasure)
            {
                for (int i = 0; i < _items.Length; ++i) {
                    Item item = _items[i];
                    if (_updatedBasis) {
                        item.pos = item.posOriginal = GetPoint(item.rational);
                    }
                    if (_updatedCommas || _updatedStickMeasure) {
                        item.pos = item.posOriginal;
                        if (_stickCommas != null) {
                            item.pos += (item.commaSpan.keyToPivot - GetCommaSpanPoint(item.spanCoordinates)) * _stickMeasure;
                        }
                    }
                    if (_updatedRadiusFactor) {
                        item.radius = GetPointRadius(item.harmonicity);
                    }
                    if (_updatedBasis || _updatedRadiusFactor || _updatedBounds) {
                        item.visible = IsPointVisible(item.pos.Y, item.radius);
                    }
                }

                _updatedBasis = _updatedRadiusFactor = _updatedBounds = _updatedCommas = _updatedStickMeasure = false;
            }
        }

        private const float _lineWidthFactor = 0.612f;

        private Element _groupLines;
        private Element _groupPoints;
        private Element _groupText;

        private void DrawItem(IImage image, Item item, bool highlight = false)
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
                var hue = _colors.GetRationalHue(item.rational.GetNarrowPowers(_narrowPrimes));
                item.colors = new Color[] {
                    RationalColors.GetColor(hue, Rationals.Utils.Interp(1, 0.4, item.harmonicity)),
                    RationalColors.GetColor(hue, Rationals.Utils.Interp(0.4, 0, item.harmonicity)),
                };
            }

            int i0, i1;
            GetPointVisibleRangeX(item.pos.X, item.radius, out i0, out i1);

            // Point & Text
            if (item.visible)
            {
                for (int i = i0; i <= i1; ++i)
                {
                    Point p = item.pos;
                    p.X += i;

                    string id_i = item.id + "_" + i.ToString();

                    image.Circle(p, item.radius)
                        .Add(_groupPoints, index: -1, id: "c " + id_i)
                        .FillStroke(item.colors[0], highlight ? Color.Red : Color.Empty, _pointRadius * 0.1f);

                    string t = item.rational.FormatFraction("\n");
                    image.Text(p, t, fontSize: item.radius, lineLeading: 0.8f, align: Align.Center, centerHeight: true)
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
                        .FillStroke(item.colors[0], Color.Empty);
                }
            }
        }

        private void DrawCursor(IImage image, double cursorCents) {
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
                    .FillStroke(Color.Empty, Color.Red, _pointRadius * 0.1f);
            }
        }

        public void DrawGrid(IImage image, Rational highlight, double cursorCents)
        {
            if (_items != null) {
                _groupLines  = image.Group().Add(id: "groupLines");
                _groupPoints = image.Group().Add(id: "groupPoints");
                _groupText   = image.Group().Add(id: "groupText");

                for (int i = 0; i < _items.Length; ++i) {
                    Item item = _items[i];
                    if (item.visible || (item.parent != null && item.parent.visible)) {
                        bool h = item.rational.Equals(highlight);
                        DrawItem(image, item, h);
                    }
                }
            }

            if (highlight.IsDefault()) {
                DrawCursor(image, cursorCents);
            }

            if (_edGrids != null) {
                for (int i = 0; i < _edGrids.Length; ++i) {
                    Color color = _gridColors[i % _gridColors.Length];
                    DrawEDGrid(image, _edGrids[i], color);
                }
            }
        }

        public Rational FindNearestRational(Point pos) {
            Item nearest = null;
            if (_items != null) {
                float dist = float.MaxValue;
                for (int i = 0; i < _items.Length; ++i) {
                    Item item = _items[i];
                    if (item.visible) {
                        Point p = item.pos - pos;
                        p.X -= (float)Math.Round(p.X);
                        float d = p.X * p.X + p.Y * p.Y;
                        if (dist > d) {
                            dist = d;
                            nearest = item;
                        }
                    }
                }
            }
            if (nearest == null) return default(Rational);
            return nearest.rational;
        }
        public Rational FindNearestRational(double cents) {
            //!!! here should be the search by band neighbors
            Item nearest = null;
            if (_items != null) {
                float dist = float.MaxValue;
                for (int i = 0; i < _items.Length; ++i) {
                    Item item = _items[i];
                    if (item.visible) {
                        float d = Math.Abs(item.cents - (float)cents);
                        if (dist > d) {
                            dist = d;
                            nearest = item;
                        }
                    }
                }
            }
            if (nearest == null) return default(Rational);
            return nearest.rational;
        }

        public string FormatRationalInfo(Rational r, double cursorCents) {
            var b = new System.Text.StringBuilder();
            if (!r.IsDefault()) {
                b.AppendLine(r.FormatFraction());
                b.AppendLine(r.FormatMonzo() + " " + r.FormatNarrows(_narrowPrimes));
                b.AppendLine("Distance " + _harmonicity.GetDistance(r).ToString());
                b.AppendFormat("{0:F2}c", r.ToCents());
                b.AppendLine();
            }
            b.AppendFormat("cursor: {0:F2}c", cursorCents);
            return b.ToString();
        }

        #region ED grids
        private static Color[] GenerateGridColors(int count) {
            Color[] result = new Color[count];
            for (int i = 0; i < count; ++i) {
                double h = RationalColors.GetRareHue(i);
                h += 0.55; h -= Math.Floor(h); // shift phase to first blue
                result[i] = Utils.HslToRgb(h * 360, 0.5, 0.75);
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

        public void DrawEDGrid(IImage image, EDGrid edGrid, Color color) {
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

            string gridId = String.Format("grid_{0}ed{1}", ed, edGrid.baseInterval.FormatFraction());
            Element group = image.Group().Add(id: gridId, index: -2); // put under groupText
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
                            string id = gridId + String.Format("_{0}_{1}_{2}", j, k, i);
                            if (i == 0) {
                                image.Line(ps[0], ps[1], lineWidth * 3, lineWidth)
                                    .Add(group, id: id)
                                    .FillStroke(color, Color.Empty);
                            } else {
                                image.Line(ps)
                                    .Add(group, id: id)
                                    .FillStroke(Color.Empty, color, lineWidth);
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Stick commas
        private void UpdateItemCommaSpan(Item item) {
            if (_stickCommas == null) {
                item.commaSpan = null;
                return; // _commaSpans list stays empty
            }
            CommaSpan span;
            for (int i = 0; i < _commaSpans.Count; ++i) {
                span = _commaSpans[i];
                int[] coords = span.basisMatrix.FindCoordinates(item.rational);
                if (coords != null) {
                    int c = coords[_stickCommas.Length]; // span key coefficient
                    if (c == 0 || c == 1) { // "c == 0" means we should find the span with key "|0>" - this probably is the one.
                        // span found
                        item.commaSpan = span;
                        item.spanCoordinates = coords; // last coordinate ('c') not used !!!
                        return;
                    }
                }
            }
            // create new span for this key
            Rational[] basis = new Rational[_stickCommas.Length + 1];
            _stickCommas.CopyTo(basis, 0);
            basis[_stickCommas.Length] = item.rational;
            var matrix = new Vectors.Matrix(basis, _maxPrimeIndex + 1);
            matrix.MakeEchelon();
            matrix.ReduceRows();
            span = new CommaSpan { key = item.rational, basisMatrix = matrix };
            _commaSpans.Add(span);
            //
            item.commaSpan = span;
            item.spanCoordinates = new int[_stickCommas.Length]; // zeros
        }
        private void UpdateCommaSpanBasis() {
            _commaSpanBasis = null;
            if (_stickCommas == null) return;
            _commaSpanBasis = new Point[_stickCommas.Length];
            for (int i = 0; i < _stickCommas.Length; ++i) {
                Point b = GetPoint(_stickCommas[i]);
                b.X -= (float)Math.Round(b.X);
                _commaSpanBasis[i] = b;
            }
        }
        private void UpdateCommaSpanPivots() {
            for (int i = 0; i < _commaSpans.Count; ++i) {
                UpdateCommaSpanPivot(_commaSpans[i]);
            }
        }
        private void UpdateCommaSpanPivot(CommaSpan span) {
            Point p = new Point(0, 0);
            // weighted
            float fullWeight = 0;
            for (int i = 0; i < _stickCommas.Length; ++i) {
                for (int j = -5; j <= 5; ++j) {
                    Rational r = span.key * _stickCommas[i].Power(j);
                    double d = _harmonicity.GetDistance(r);
                    float w = GetHarmonicity(d); // 0..1
                    p += (_commaSpanBasis[i] * j) * w;
                    fullWeight += w;
                }
            }
            if (fullWeight != 0) {
                p /= fullWeight;
            }
            span.keyToPivot = p;
        }
        private Point GetCommaSpanPoint(int[] coords) {
            Point p = new Point(0, 0);
            for (int i = 0; i < _commaSpanBasis.Length; ++i) {
                p += _commaSpanBasis[i] * coords[i];
            }
            return p;
        }

        #endregion
    }
}
