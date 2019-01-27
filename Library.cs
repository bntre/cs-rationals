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
        private static Dictionary<Rational, string> _names;
        private static Dictionary<string, Rational> _rationals;

        private static void Add(Rational r, string name) {
            _names[r] = name;
            _rationals[name] = r;
        }
        private static void Add(int n, int d, string name) {
            Add(new Rational(n, d), name);
        }

        private static void Init() {
            _names = new Dictionary<Rational, string>();
            _rationals = new Dictionary<string, Rational>();
            // read library
            //!!! read from table file
            Add(25, 24, "Chroma, Chromatic semitone");
            Add(81, 80, "Syntonic comma");
            Add(128, 125, "Enharmonic diesis, Lesser diesis");
            Add(32805, 32768, "Schisma");
            Add(2048, 2025, "Diaschisma"); // A diaschisma is the difference between a schisma and a syntonic comma
            Add(250, 243, "Porcupine comma, Maximal diesis, Major diesis");
            Add(16875, 16384, "Negri comma, Double augmentation diesis");
            Add(648, 625, "Diminished comma, Major diesis, Greater diesis");
        }

        public static string Find(Rational r) {
            if (_names == null) Init(); // init once
            // look for a name
            string name = null;
            _names.TryGetValue(r, out name);
            return name;
        }

        public static Rational Find(string name) {
            if (_rationals == null) Init(); // init once
            //
            Rational r;
            _rationals.TryGetValue(name, out r);
            return r;
        }

        public static bool Is(Rational r, string name) {
            if (_names == null) Init(); // init once
            return Find(r) == name;
        }

    }
}
