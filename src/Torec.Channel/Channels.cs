using System;
using System.Collections.Generic;
//using System.Linq;

using TDouble = System.Double;

namespace Torec
{
    public class ChannelInfo {
        public const TDouble NotAValue = TDouble.NaN;

        public ChannelInfo(TDouble range0, TDouble range1, TDouble defaultValue = NotAValue, string name = null) {
            _valueRange = new TDouble[] { range0, range1 };
            _defaultValue = defaultValue;
            _name = name;
        }

        public TDouble[] GetValueRange() { return _valueRange; }
        public TDouble GetDefaultValue() { return _defaultValue; }
        public string GetName() { return _name ?? "nonamed"; }

        protected TDouble[] _valueRange;
        protected TDouble _defaultValue = NotAValue;
        protected string _name = null; //!!! for debug only ?
    }

    public abstract class Channel
    {
        protected ChannelInfo _info;
        protected int _id;

        protected static int _idCounter = 0;

        public Channel(ChannelInfo info) {
            _info = info;
            _id = _idCounter ++;
        }

        public ChannelInfo GetInfo() { return _info; }
        public int GetId() { return _id; }
        public abstract TDouble GetValue();
    }
}
