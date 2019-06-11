using System;
using System.Collections.Generic;
//using System.Linq;

namespace Rationals.Drawing
{
    using Torec.Drawing;
    using Color = System.Drawing.Color;

    public class Tempered { //!!! rename: it seems here hould be some union { Rational or float }
        public Rational rational = default(Rational);
        public float centsDelta = 0f;
        //
        public float ToCents() {
            float c = centsDelta;
            if (!rational.IsDefault()) {
                c += (float)rational.ToCents();
            }
            return c;
        }
        public override string ToString() {
            string s = "";
            if (!rational.IsDefault()) s += rational.FormatFraction();
            if (centsDelta != 0)       s += centsDelta.ToString("+0;-#");
            return s;
        }
    }


    public class GridDrawer
    {
        // Base settings
        private Rational[] _subgroup;
        private int _dimensionCount;
        private int _minPrimeIndex; // smallest prime index
        private int _maxPrimeIndex; // largest prime index
        private Vectors.Matrix _subgroupMatrix; // used for comma validation
        // narrow primes
        private Rational[] _narrowPrimes; // for each prime nominator (up to _maxPrimeIndex). may contain an invalid rational e.g. for "2.3.7/5" (narrow for 5 skipped).
        private float[]    _narrowCents; // may be tempered
        // Depend on base
        private RationalColors _colors;

        // Generation
        private IHarmonicity _harmonicity;
        private int _rationalCountLimit;
        private Rational _distanceLimit;
        private Item[] _items;
        private Bands<Item> _bands; //!!! not used

        // temperament
        private Vectors.Matrix _temperamentMatrix     = null; // tempered intervals + primes (so we can solve each narrow prime of basis)
        private float[]        _temperamentPureCents  = null;
        private float[]        _temperamentDeltaCents = null;
        private float          _temperamentMeasure = 0;       // 0..1
        private float[]        _temperamentCents      = null; // pure_cents + delta_cents * measure

        // slope & basis
        private float _octaveWidth; // octave width in user units
        private Point[] _basis; // basis vectors for each narrow prime (upto _maxPrimeIndex)

        // bounds and point radius factor
        private Point[] _bounds;
        private float _pointRadius = _defaultPointRadius;
        private const float _defaultPointRadius = 0.05f;

        // Selection
        private Tempered[] _selection;

        // Equal division grids
        private EDGrid[] _edGrids;
        private static Color[] _gridColors = GenerateGridColors(10);

        private enum UpdateFlags {
            None            = 0,
            Items           = 1, // regenerate items
            Basis           = 2, // recreate basis
            //Slope           = 4,
            RadiusFactor    = 8,
            Bounds          = 16,
        }

        UpdateFlags _updateFlags = UpdateFlags.None;
        /*
        // update levels - make enum flag for this !!!
        private bool _updatedBase = false; // regenerate items
        private bool _updatedTemperament = false; // used? !!!
        private bool _updatedTemperamentMeasure = false;
        private bool _updatedSlope = false;
        //private bool _updatedBasis;
        private bool _updatedRadiusFactor = false;
        private bool _updatedBounds = false;
        */

        public struct EDGrid { // equal division grid: https://en.xen.wiki/w/Equal-step_tuning
            public Rational baseInterval; // e.g. Octave
            public int stepCount;
            public int[] basis; // 2 step indices
        }

        [System.Diagnostics.DebuggerDisplay("{rational} <- {parent.rational}")]
        private class Item {
            // base
            public Rational rational;
            public Item parent;
            public float cents; // may be tempered
            // harmonicity
            public double distance;
            public float harmonicity; // 0..1
            //
            public string id;
            public Color[] colors; // [Point, Line] colors
            // viewport + basis
            public Point pos; // probably shifted to comma span pivot (according to stickMeasure)
            public float radius;
            public bool visible;
            //
            public static int CompareDistance(Item a, Item b) { return a.distance.CompareTo(b.distance); }
        }

        // Cursor
        private float _cursorCents;
        private Item _cursorItem;

        //
        public GridDrawer() {
            InitHighlightColors();
        }

        public void SetBase(int limitPrimeIndex, Rational[] subgroup, Rational[] narrows)
        {
            // subgroup
            if (subgroup == null) {
                _minPrimeIndex = 0;
                _maxPrimeIndex = limitPrimeIndex;
                _dimensionCount = limitPrimeIndex + 1;
                _subgroup = Rational.Primes(_dimensionCount);
            } else {
                _subgroup = subgroup;
                _dimensionCount = subgroup.Length;
                GetSubgroupPrimeRange(_subgroup, out _minPrimeIndex, out _maxPrimeIndex);
            }
            _subgroupMatrix = new Vectors.Matrix(_subgroup, makeDiagonal: true);

            // narrow primes
            //!!! ugly helper here: inserting fractional subgroup items to narrows
            if (narrows == null) narrows = new Rational[] {};
            foreach (Rational r in _subgroup) {
                if (!r.IsInteger()) {
                    var ns = new Rational[1 + narrows.Length];
                    ns[0] = r;
                    narrows.CopyTo(ns, 1);
                    narrows = ns;
                }
            }
            _narrowPrimes = Rational.GetNarrowPrimes(_maxPrimeIndex + 1, _minPrimeIndex, narrows);
            _narrowCents = null; // depends on temperament - will be updated with basis

            // colors
            _colors = new RationalColors(_maxPrimeIndex + 1);

            _updateFlags |= UpdateFlags.Items; // regenerate items
        }
        public void SetGeneration(string harmonicityName, int rationalCountLimit, Rational distanceLimit = default(Rational)) {
            _harmonicity = new HarmonicityNormalizer(
                Rationals.Utils.CreateHarmonicity(harmonicityName)
            );
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
        public void SetPointRadiusFactor(float pointRadiusFactor) {
            _pointRadius = _defaultPointRadius * pointRadiusFactor;
            _updateFlags |= UpdateFlags.RadiusFactor;
        }
        public void SetSelection(Tempered[] selection) {
            _selection = selection;
        }
        public void SetEDGrids(EDGrid[] edGrids) {
            _edGrids = edGrids;
        }
        private Rational.Tempered[] ValidateTemperament(Rational.Tempered[] ts) {
            if (ts == null || ts.Length == 0) return null;

            var result = new List<Rational.Tempered>();

            Rational[] indep = new Rational[ts.Length]; // independent intervals
            int indepSize = 0;

            for (int i = 0; i < ts.Length; ++i) {
                Rational r = ts[i].rational;
                // skip if out of subgroup
                if (_subgroupMatrix.FindCoordinates(r) == null) continue;
                // skip if dependend
                if (indepSize > 0) {
                    var m = new Vectors.Matrix(indep, -1, indepSize, makeDiagonal: true);
                    if (m.FindCoordinates(r) != null) continue;
                }
                indep[indepSize++] = r;
                //
                result.Add(ts[i]);
            }

            return result.ToArray();
        }
        public void SetTemperament(Rational.Tempered[] temperament)
        {
            Rational.Tempered[] ts = ValidateTemperament(temperament); // here in GridGrawer we just ignore invalid tempered intervals
            if (ts == null || ts.Length == 0) {
                _temperamentMatrix     = null;
                _temperamentPureCents  = null;
                _temperamentDeltaCents = null;
                _temperamentCents      = null;
            } else {
                int basisSize = _maxPrimeIndex + 1;
                int matrixSize = ts.Length + basisSize; // we add primes to solve each narrow prime of basis
                Rational[] rs          = new Rational[matrixSize];
                _temperamentPureCents  = new float   [matrixSize];
                _temperamentDeltaCents = new float   [matrixSize];
                for (int i = 0; i < ts.Length; ++i) { // tempered intervals
                    Rational r = ts[i].rational;
                    rs[i] = r;
                    float cents = (float)r.ToCents();
                    _temperamentPureCents [i] = cents;
                    _temperamentDeltaCents[i] = ts[i].cents - cents;
                }
                for (int i = 0; i < basisSize; ++i) { // pure primes
                    int j = ts.Length + i;
                    Rational r = Rational.Prime(i);
                    rs[j] = r;
                    float cents = (float)r.ToCents();
                    _temperamentPureCents [j] = cents;
                    _temperamentDeltaCents[j] = 0f;
                }
                _temperamentMatrix = new Vectors.Matrix(rs, makeDiagonal: true);
                //
                UpdateTemperamentCents();
            }

            _updateFlags |= UpdateFlags.Basis;
        }
        public void SetTemperamentMeasure(float value) {
            _temperamentMeasure = value;
            UpdateTemperamentCents();
            _updateFlags |= UpdateFlags.Basis;
        }
        private void UpdateTemperamentCents() {
            if (_temperamentPureCents == null) {
                _temperamentCents = null;
            } else {
                int matrixSize = _temperamentPureCents.Length;
                _temperamentCents = new float[matrixSize];
                for (int i = 0; i < matrixSize; ++i) {
                    _temperamentCents[i] = _temperamentPureCents[i] + _temperamentDeltaCents[i] * _temperamentMeasure;
                }
            }
        }

        //!!! move to some Utils.Subgroup
        public static void GetSubgroupPrimeRange(Rational[] subgroup, out int minPrimeIndex, out int maxPrimeIndex) {
            var mul = new Rational(1);
            for (int i = 0; i < subgroup.Length; ++i) {
                mul *= subgroup[i];
            }
            int[] pows = mul.GetPrimePowers();
            //
            maxPrimeIndex = Powers.GetLength(pows) - 1;
            minPrimeIndex = 0;
            while (minPrimeIndex <= maxPrimeIndex && pows[minPrimeIndex] == 0) ++minPrimeIndex; // skip heading zeros
        }

        #region Generate items
        private Dictionary<Rational, Item> _generatedItems; // temporal dict for generation

        protected void GenerateItems() {
            _cursorItem = null;
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
            //!!! code smell: refactor down to RationalGenerator.MakeRational to support subgroup and narrow parents ?

            // make sure his parent is added
            Item parentItem = null;
            if (r.GetPowerCount() - 1 > _minPrimeIndex) { // we don't draw lines between base intervals
                Rational parent = r.GetNarrowParent(_narrowPrimes);
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
                distance = distance,
            };

            /*
            bool inBands = _bands.AddItem(item.cents, item);
            if (!inBands) {
                //!!! skip if out of bands?
            }
            */

            // also needed to get item visibility
            item.harmonicity = GetHarmonicity(distance); // 0..1
            //item.radius = GetPointRadius(item.harmonicity);

            _generatedItems[r] = item;

            return item;
        }
        #endregion

        private static float GetHarmonicity(double distance) { //!!! might be moved out
            return (float)Math.Exp(-distance * 1.2); // 0..1
        }

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
        public Rational UpdateCursorItem() {
            _cursorItem = null;
            if (_items != null) {
                //!!! here should be the search by band neighbors
                float dist = float.MaxValue;
                for (int i = 0; i < _items.Length; ++i) {
                    Item item = _items[i];
                    if (item.visible) {
                        float d = Math.Abs(item.cents - (float)_cursorCents);
                        if (dist > d) {
                            dist = d;
                            _cursorItem = item;
                        }
                    }
                }
            }
            if (_cursorItem == null) return default(Rational);
            return _cursorItem.rational;
        }

        private void UpdateBasis() {
            if (_octaveWidth == 0f) {
                _basis = null;
                return;
            }

            // Set basis
            int basisSize = _maxPrimeIndex + 1;
            _narrowCents = new float[basisSize];
            _basis       = new Point[basisSize];

            for (int i = 0; i < basisSize; ++i) {
                Rational n = _narrowPrimes[i];
                if (n.IsDefault()) continue;

                float narrowCents = 0f;

                if (_temperamentMatrix == null) {
                    narrowCents = (float)n.ToCents();
                } else  {
                    float[] coords = _temperamentMatrix.FindFloatCoordinates(n); //!!! these coords might be saved to not recalculate on measure change
                    if (coords == null) {
                        //throw new Exception("Can't solve temperament");
                        narrowCents = (float)n.ToCents();
                    } else {
                        for (int j = 0; j < coords.Length; ++j) {
                            narrowCents += coords[j] * _temperamentCents[j];
                        }
                    }
                }

                _narrowCents[i] = narrowCents;
                _basis[i] = GetPoint(narrowCents);

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

        private void UpdateItemPos(Item item) {
            if (_basis       == null) throw new Exception("Basis not set");
            if (_narrowCents == null) throw new Exception("Narrow cents not set");
            var pows = item.rational.GetNarrowPowers(_narrowPrimes);
            if (pows == null) throw new Exception("Invalid item rational");
            float c = 0f;
            var   p = new Point(0, 0);
            for (int i = 0; i < pows.Length; ++i) {
                var e = pows[i];
                if (e != 0) {
                    c += _narrowCents[i] * e;
                    p += _basis[i]       * e;
                }
            }
            item.cents = c;
            item.pos   = p;
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

        private bool IsUpdating(UpdateFlags flags) {
            return (_updateFlags & flags) != 0;
        }

        public void UpdateItems()
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
            if (_basis == null) return;

            // Visibility
            if (IsUpdating(UpdateFlags.Items | UpdateFlags.Basis | UpdateFlags.RadiusFactor | UpdateFlags.Bounds))
            {
                for (int i = 0; i < _items.Length; ++i) {
                    Item item = _items[i];
                    if (IsUpdating(UpdateFlags.Items | UpdateFlags.Basis)) {
                        UpdateItemPos(item);
                    }
                    if (IsUpdating(UpdateFlags.Items | UpdateFlags.RadiusFactor)) {
                        item.radius = GetPointRadius(item.harmonicity);
                    }
                    if (IsUpdating(UpdateFlags.Items | UpdateFlags.Basis | UpdateFlags.RadiusFactor | UpdateFlags.Bounds)) {
                        item.visible = IsPointVisible(item.pos.Y, item.radius);
                    }
                }
            }

            // reset update flags
            _updateFlags = UpdateFlags.None;
        }

        private const float _lineWidthFactor = 0.612f;

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
                _highlightColors[i] = RationalColors.GetColor(hue, Rationals.Utils.Interp(0.75, 0.3, k));
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
                var hue = _colors.GetRationalHue(item.rational.GetNarrowPowers(_narrowPrimes));
                item.colors = new Color[2] {
                    RationalColors.GetColor(hue, Rationals.Utils.Interp(1, 0.4, item.harmonicity)),
                    RationalColors.GetColor(hue, Rationals.Utils.Interp(0.4, 0, item.harmonicity)),
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
                        Tempered t = _selection[i];
                        if (t.centsDelta == 0 && item.rational.Equals(t.rational)) {
                            selected = true;
                            break;
                        }
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

        public void DrawGrid(Image image, int highlightCursorMode)
        {
            if (_items != null) {
                _groupLines  = image.Group().Add(id: "groupLines");
                _groupPoints = image.Group().Add(id: "groupPoints");
                _groupText   = image.Group().Add(id: "groupText");

                // Find cursor parents to highlight
                List<Rational> hs = null;
                if (highlightCursorMode == 1) { // highlight nearest rational and its parents
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
            }

            if (highlightCursorMode == 2) { // highlight cursor cents
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
            b.AppendFormat("Cursor: {0:F2}c", _cursorCents);
            b.AppendLine();
            Rational c = default(Rational);
            if (_cursorItem != null) {
                c = _cursorItem.rational;
                if (!c.IsDefault()) {
                    float pureCents  = (float)c.ToCents();
                    b.AppendFormat("{0} {1} {2} {3}{4}c\n", 
                        c.FormatFraction(), 
                        c.FormatMonzo(), 
                        c.FormatNarrows(_narrowPrimes),
                        pureCents,
                        _temperamentCents == null ? "" : 
                            (_cursorItem.cents - pureCents).ToString("+0.00;-0.00")
                    );
                    b.AppendLine();
                    //b.AppendFormat("Distance {0:F3}", _harmonicity.GetDistance(c));
                    //b.AppendLine();
                }
            }
            // Selection
            if (_selection != null) {
                for (int i = 0; i < _selection.Length; ++i) {
                    Tempered t = _selection[i];
                    b.Append(t.ToString());
                    if (!c.IsDefault() && !t.rational.IsDefault() && t.centsDelta == 0) {
                        b.AppendFormat(" * {0} = {1}", (c / t.rational).FormatFraction(), c.FormatFraction());
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
                    double d = _harmonicity.GetDistance(r);
                    float w = GetHarmonicity(d); // 0..1
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
}
