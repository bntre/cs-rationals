using System;
using System.Collections.Generic;


namespace Rationals
{
    //!!! probably it's better to use IntervalTree
    public class Bands<T>
    {
        //!!! use barks here ? https://en.wikipedia.org/wiki/Bark_scale

        protected float _bandWidth; // in cents
        protected int _bandCount;
        protected int _bandShift; // negative
        protected List<T>[] _bands;

        protected int GetBandIndex(float cents) {
            return (int)Math.Round(cents / _bandWidth) - _bandShift;
        }

        public Bands(float bandWidth = 100, float cents0 = -7 * 1200f, float cents1 = 7 * 1200f) {
            _bandWidth = bandWidth;
            _bandCount = (int)Math.Ceiling((cents1 - cents0) / _bandWidth);
            _bandShift = (int)Math.Floor((cents0) / _bandWidth);
            _bands = new List<T>[_bandCount];
            for (int i = 0; i < _bandCount; ++i) {
                _bands[i] = new List<T>();
            }
        }

        public bool AddItem(float cents, T item) {
            int index = GetBandIndex(cents);
            if (index < 0 || index >= _bandCount) return false;
            _bands[index].Add(item);
            return true;
        }

        public T[] GetRangeItems(float cents0, float cents1) {
            int i0 = Math.Max(GetBandIndex(cents0), 0);
            int i1 = Math.Min(GetBandIndex(cents1), _bandCount - 1);
            //
            int len = 0;
            for (int i = i0; i <= i1; ++i) {
                len += _bands[i].Count;
            }
            //
            T[] result = new T[len];
            int p = 0;
            for (int i = i0; i <= i1; ++i) {
                _bands[i].CopyTo(result, p);
                p += _bands[i].Count;
            }
            return result;
        }

        public T[] GetNeighbors(float cents, float distanceCents) {
            return GetRangeItems(
                cents - distanceCents,
                cents + distanceCents
            );
        }
    }


    public class IntervalTree<Item, Value>
        where Value : IComparable<Value>
    {
        public delegate Value GetValue(Item a);
        public delegate bool HandleInterval(Item i0, Item i1); // return true to go deeper

        protected GetValue _getValue;

        public class Interval {
            public Item     item  = default(Item);
            public Interval left  = null;
            public Interval right = null;
            public Interval up    = null;
        }
        public struct LeveledItem { // used to trace the tree
            public Item item;
            public int level;
        }

        public Interval root = new Interval { }; // open and empty interval

        public IntervalTree(GetValue getItemValue) {
            _getValue = getItemValue;
        }

        public Interval Add(Item item) {
            return Add(root, item);
        }
        public List<Item> GetItems(Interval i = null) {
            var items = new List<Item>();
            GetItems(i ?? root, items);
            return items;
        }
        public void GetItems(IList<Item> items, Interval i = null) {
            GetItems(i ?? root, items);
        }
        public List<Item> GetItems(Value start, Value end) {
            var items = new List<Item>();
            GetItems(root, start,end, false,false, items);
            return items;
        }
        public List<LeveledItem> GetLeveledItems(Interval i = null) {
            var items = new List<LeveledItem>();
            GetItems(i ?? root, items, 0);
            return items;
        }
        public void FindIntervalRange(Value value, out Item i0, out Item i1) {
            i0 = i1 = default(Item);
            FindIntervalRange(root, value, ref i0, ref i1);
        }

        public Item GetIntervalLeftItem(Interval i) {
            if (i.up == null) return default(Item);
            if (i.up.right == i) return i.up.item;
            return GetIntervalLeftItem(i.up);
        }
        public Item GetIntervalRightItem(Interval i) {
            if (i.up == null) return default(Item);
            if (i.up.left == i) return i.up.item;
            return GetIntervalRightItem(i.up);
        }
        public void IterateIntervals(HandleInterval handle, Interval i = null) {
            if (i == null) i = root;
            Item i0 = GetIntervalLeftItem(i);
            Item i1 = GetIntervalRightItem(i);
            IterateIntervals(i, i0,i1, handle);
        }

        protected Interval Add(Interval i, Item item) {
            if (i.left == null) { // not forked yet
                i.item  = item;
                i.left  = new Interval { up = i };
                i.right = new Interval { up = i };
                return i;
            } else { // forked
                int c = _getValue(item).CompareTo(_getValue(i.item));
                if (c == 0) return i; // item already added
                return Add(c < 0 ? i.left : i.right, item);
            }
        }

        protected void GetItems(Interval i, IList<Item> items) {
            if (i.left == null) return; // empty interval
            GetItems(i.left, items);
            items.Add(i.item);
            GetItems(i.right, items);
        }

        protected void GetItems(Interval i, Value v0, Value v1, bool whole0, bool whole1, IList<Item> items) {
            if (i.left == null) return; // empty interval
            // recompare if needed
            Value v = (whole0 && whole1) ? default(Value) : _getValue(i.item);
            int c0 = whole0 ? -1 : v0.CompareTo(v);
            int c1 = whole1 ?  1 : v1.CompareTo(v);
            // collect items
            if (c0 < 0) GetItems(i.left, v0, v1, whole0, whole1 || (c1 >= 0), items);
            if (c0 <= 0 && c1 >= 0) items.Add(i.item);
            if (c1 > 0) GetItems(i.right, v0, v1, whole0 || (c0 <= 0), whole1, items);
        }
        protected void GetItems(Interval i, IList<LeveledItem> items, int level) {
            if (i.left == null) return; // empty interval
            GetItems(i.left, items, level + 1);
            items.Add(new LeveledItem { item = i.item, level = level });
            GetItems(i.right, items, level + 1);
        }

        protected void FindIntervalRange(Interval i, Value value, ref Item i0, ref Item i1) {
            if (i.left == null) return; // empty interval
            Value v = _getValue(i.item);
            int c = value.CompareTo(v);
            if (c == 0) {
                i0 = i1 = i.item;
            } else {
                if (c < 0) {
                    i1 = i.item;
                    i = i.left;
                } else {
                    i0 = i.item;
                    i = i.right;
                }
                FindIntervalRange(i, value, ref i0, ref i1);
            }
        }

        protected void IterateIntervals(Interval i, Item i0, Item i1, HandleInterval handle) {
            if (i == null) return;
            bool goDeeper = handle(i0, i1);
            if (!goDeeper) return;
            if (i.left == null) return; // not forked
            IterateIntervals(i.left,  i0, i.item, handle);
            IterateIntervals(i.right, i.item, i1, handle);
        }

        /*
        protected void FormatItems(Interval i, IList<string> items, Func<Item, string> format, int tab) {
            if (i.left == null) return; // empty interval
            FormatItems(i.left, items, format, tab + 1);
            items.Add(new String('·', tab) + format(i.item));
            FormatItems(i.right, items, format, tab + 1);
        }
        */
    }
}
