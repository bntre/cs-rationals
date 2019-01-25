using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// https://en.wikipedia.org/wiki/List_of_pitch_intervals
// https://en.wikipedia.org/wiki/Comma_(music)
// https://en.xen.wiki/w/Gallery_of_just_intervals
// https://en.xen.wiki/w/Category:Comma
// https://en.xen.wiki/w/12edo
// https://en.xen.wiki/w/List_of_superparticular_intervals

namespace Rationals {
    public static class Library {
        private static Dictionary<int, string> _rationalNames;

        private static int GetHash(Rational r) {
            return Powers.GetHash(r.GetPrimePowers());
        }
        private static void SetName(Rational r, string name) {
            _rationalNames[GetHash(r)] = name;
        }

        public static string GetName(Rational r) {
            // init once
            if (_rationalNames == null) {
                _rationalNames = new Dictionary<int, string>();
                // read library
                //!!! read from table file
                SetName(new Rational(32805, 32768), "Schisma");
                SetName(new Rational(81, 80), "Syntonic comma");
                SetName(new Rational(128, 125), "Enharmonic diesis");
            }
            // look for a name
            int h = GetHash(r);
            string name = null;
            _rationalNames.TryGetValue(h, out name);
            return name;
        }

    }
}
