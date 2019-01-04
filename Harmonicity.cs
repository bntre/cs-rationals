﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Harmonic distance

// Based on https://bitbucket.org/bntr/harmony/src/default/harmonicity.py


namespace Rationals
{
    // Interfaces

    public interface IHarmonicity {
        double GetDistance(Rational r);
    }

    public interface IRationalHandler {
        void HandleRational(Rational r, double distance);
    }



    public class EulerHarmonicity : IHarmonicity {
        public EulerHarmonicity() { }
        // IHarmonicity
        public double GetDistance(Rational r) {
            return 1.0;
        }
    }

    // Tenney
    // http://www.marcsabat.com/pdfs/MM.pdf
    // HD = log2(ab) ??
    // https://en.xen.wiki/w/Tenney_Height ?

    // Wiseman
    // https://gist.github.com/endolith/118429
    //   A Mathematical Theory of Sensory Harmonics by Gus Wiseman
    //      http://web.archive.org/web/20170212112934/http://www.nafindix.com/math/sensory.pdf
    // Same as https://en.xen.wiki/w/Benedetti_height ?


    public class BarlowHarmonicity : IHarmonicity {
        // See: "Musical scale rationalization – a graph-theoretic approach" by Albert Gräf
        //   http://www.musikwissenschaft.uni-mainz.de/Musikinformatik/schriftenreihe/nr45/scale.pdf
        public BarlowHarmonicity() { }
        // IHarmonicity
        public double GetDistance(Rational r) {
            double d = 0.0;
            int[] pows = r.GetPrimePowers();
            for (int i = 0; i < pows.Length; ++i) {
                int e = pows[i];
                int p = Utils.GetPrime(i);
                d += (double)Math.Abs(e) * 2 * Utils.Pow(p - 1, 2) / p;
            }
            return d;
        }
    }

    public class SimpleHarmonicity : IHarmonicity {
        private double _exp;
        public SimpleHarmonicity(double exp) { _exp = exp; }
        // IHarmonicity
        public double GetDistance(Rational r) {
            double d = 0.0;
            int[] pows = r.GetPrimePowers();
            for (int i = 0; i < pows.Length; ++i) {
                int e = pows[i];
                int p = Utils.GetPrime(i);
                d += e*e * Math.Pow(p, _exp);
            }
            return d;
        }
    }


    public static class RationalIterator {
        // Iterate rationals tree. Breadth-first by distance.

        private struct Node {
            public Rational rational;
            public double distance;
            public int level; // tree level e.g. prime index
            public int direction; // branch direction: 1, -1, or 0 (if both)
        }

        private static void AddNode(List<Node> nodes, Node node) {
            // keep sorted by 'distance'
            for (int i = nodes.Count; i > 0; --i) {
                if (nodes[i-1].distance <= node.distance) {
                    nodes.Insert(i, node);
                    return;
                }
            }
            nodes.Insert(0, node);
        }

        public static void Iterate(IHarmonicity harmonicity, int levelLimit, double distanceLimit, IRationalHandler handler)
        {
            var nodes = new List<Node>(); // sorted by distance

            Rational root = new Rational(1);
            nodes.Insert(0, new Node {
                rational = root,
                distance = harmonicity.GetDistance(root),
                level = 0,
                direction = 0, // not grown yet
            });

            while (nodes.Count > 0)
            {
                Node node = nodes[0];
                nodes.RemoveAt(0);

                bool retry = node.level > 0 && node.direction == 0;
                if (!retry) {
                    handler.HandleRational(node.rational, node.distance);
                }

                // next prime level
                if (node.level < levelLimit) {
                    // same rational, same distance, grow on next level in both directions
                    nodes.Insert(0, new Node {
                        rational = node.rational,
                        distance = node.distance,
                        level = node.level + 1,
                        direction = 0,
                    });
                }

                // same prime
                Rational step = Rational.Prime(node.level); //!!! make some map by level
                if (node.direction >= 0) {
                    Rational r = node.rational * step;
                    double d = harmonicity.GetDistance(r);
                    if (d <= distanceLimit) {
                        AddNode(nodes, new Node {
                            rational = r,
                            distance = d,
                            level = node.level,
                            direction = 1,
                        });
                    }
                }
                if (node.direction <= 0) {
                    Rational r = node.rational / step;
                    double d = harmonicity.GetDistance(r);
                    if (d <= distanceLimit) {
                        AddNode(nodes, new Node {
                            rational = r,
                            distance = d,
                            level = node.level,
                            direction = -1,
                        });
                    }
                }

            }


        }

    }

}
