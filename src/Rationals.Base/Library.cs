using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        private static Dictionary<Rational, string> _names;         // Rational => name
        private static Dictionary<string, Rational> _rationals;     // name => Rational

        // https://www.huygens-fokker.org/docs/intervals.html Huygens-Fokker foundation. The List is compiled by Manuel Op de Coul.
        private static readonly string _libraryPath = "res/Stichting Huygens-Fokker_ List of intervals.html";

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
#if IGNORE_RATIONAL_LIBRARY
            Add(25, 24, "Chroma, Chromatic semitone");
            Add(81, 80, "Syntonic comma");
            Add(128, 125, "Enharmonic diesis, Lesser diesis");
            Add(32805, 32768, "Schisma");
            Add(2048, 2025, "Diaschisma"); // A diaschisma is the difference between a schisma and a syntonic comma
            Add(250, 243, "Porcupine comma, Maximal diesis, Major diesis");
            Add(16875, 16384, "Negri comma, Double augmentation diesis");
            Add(648, 625, "Diminished comma, Major diesis, Greater diesis");
            Add(2, 1, "Octave");
            Add(3, 1, "Tritave");
#else
            string libraryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _libraryPath);
            if (!File.Exists(libraryPath)) {
                Debug.WriteLine("No Rational names library found: " + _libraryPath);
                return;
            }
            using (var sr = new StreamReader(libraryPath)) {
                bool started = false;
                string line;
                while ((line = sr.ReadLine()) != null) {
                    if (!started) {
                        started = line.Contains("<pre>");
                    } else {
                        string[] parts = line.Split(" ".ToCharArray(), 2, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 2) {
                            Rational r = Rational.Parse(parts[0]);
                            if (!r.IsDefault()) {
                                Add(r, parts[1]);
                            }
                        }
                    }
                }
            }
#endif
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
