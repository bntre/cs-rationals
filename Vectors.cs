using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Rationals
{
    public static class Vectors
    {
        private static void Solve(int a, int b, out int x, out int y) {
            // Solve ax + by = 0
            bool sameSign = Math.Sign(a) == Math.Sign(b);
            int[] d = Powers.FromFraction(Math.Abs(b), Math.Abs(a));
            int[] d0, d1;
            Powers.Split(d, out d0, out d1);
            x = (int)Powers.ToInt(d0);
            y = (int)Powers.ToInt(d1);
            if (sameSign) y = -y;
        }

        public static int FindCoordinates(int[][] basis, int[] vector, int vectorLength, out int[] result)
        {
            result = null;

            // We use https://en.wikipedia.org/wiki/Gaussian_elimination

            int basisSize = basis.Length;
            
            // clone integers to matrix
            int[,] m = new int[vectorLength, basisSize + 1];
            for (int i = 0; i < vectorLength; ++i) {
                for (int j = 0; j < basisSize; ++j) {
                    m[i, j] = Powers.SafeAt(basis[j], i);
                }
                m[i, basisSize] = Powers.SafeAt(vector, i);
            }

            int[] r        = new int[vectorLength]; // row order: virtual index -> real index
            int[] leadCols = new int[vectorLength]; // save for back propagation
            for (int i = 0; i < vectorLength; ++i) {
                r[i]        = i; // default order
                leadCols[i] = -1; // no lead found for this row
            }

            // put to echelon form
            int row = 0; // virtual index
            int col = 0;
            while (row < vectorLength && col <= basisSize) //???
            {
                {
                    // find a pivot for column - and swap to top
                    int ri = -1;
                    int vi = -1;
                    for (vi = row; vi < vectorLength; ++vi) {
                        if (m[r[vi], col] != 0) {
                            ri = r[vi]; // real index found
                            break;
                        }
                    }
                    if (ri == -1) {
                        ++col; // no pivot - next column
                        continue;
                    }
                    if (row != vi) { // swap row <-> vi
                        r[vi] = r[row];
                        r[row] = ri;
                    }
                }

                bool rowsBelow = row + 1 < vectorLength;
                if (rowsBelow) {
                    // zero below cells in this col
                    int lead = m[r[row], col];
                    for (int vi = row + 1; vi < vectorLength; ++vi) {
                        int ri = r[vi];
                        int c = m[ri, col];
                        if (c == 0) continue;

                        int d0, d1;
                        Solve(lead, c, out d0, out d1);

                        m[ri, col] = 0;
                        for (int j = col + 1; j <= basisSize; ++j) {
                            m[ri, j] = m[r[row], j] * d0 + m[ri, j] * d1;
                        }
                    }
                }

                leadCols[row] = col;
                ++col;
                ++row;
            }

            /*
            for (int i = 0; i < vectorLength; ++i) {
                for (int j = 0; j <= basisSize; ++j) {
                    Debug.Write(String.Format("{0,5}", m[i, j]));
                }
                Debug.Write("\n");
            }
            */

            // back substitution

            int[] resultCoords = new int[basisSize];

            for (row = vectorLength - 1; row >= 0; --row) {
                col = leadCols[row];
                if (col == -1) continue; // no lead in this row (all zeros?)
                if (col == basisSize) return -1; // No solutions - independent vectors
                int b = m[r[row], basisSize];
                if (b == 0) {
                    /*
                    m[r[row], col] = 0; //!!! not needed
                    for (int vi = row - 1; vi >= 0; --vi) {
                        m[r[vi], col] = 0;
                    }
                    */
                    resultCoords[col] = 0; //!!! not needed
                } else {
                    int lead = m[r[row], col];
                    if (b % lead != 0) return -2; // No integer solution or Invalid basis
                    int res = b / lead;
                    //m[r[row], basisSize] = res;  //!!! not needed
                    //m[r[row], col] = 1;  //!!! not needed
                    for (int vi = row - 1; vi >= 0; --vi) {
                        int c = m[r[vi], col];
                        //m[r[vi], col] = 0;
                        m[r[vi], basisSize] -= c * res;
                    }
                    resultCoords[col] = res;
                }
            }

            result = resultCoords;
            return 0;
        }

        public static int[] FindCoordinates(Rational[] basis, Rational vector, int vectorLength) {
            int basisSize = basis.Length;
            //
            int[][] b = new int[basisSize][];
            for (int i = 0; i < basisSize; ++i) {
                b[i] = basis[i].GetPrimePowers();
            }
            //
            int[] v = vector.GetPrimePowers();
            //
            int[] coords;
            int result = FindCoordinates(b, v, vectorLength, out coords);

#if DEBUG
            if (result == 0) {
                Rational r = Rational.One;
                for (int i = 0; i < coords.Length; ++i) {
                    r *= basis[i].Pow(coords[i]);
                }
                if (!r.Equals(vector)) throw new Exception(
                    String.Format("FindCoordinates failed: {0} != {1}", r, vector)
                );
            }
#endif

            return coords;
        }


        #region Tests
        private static void CheckVector(Rational[] basis, Rational vector) {
            int basisSize = basis.Length;
            for (int i = 0; i < basisSize; ++i) {
                Debug.WriteLine(String.Format("Basis {0}. {1,-15} {2}", i, basis[i].FormatFraction(), basis[i].FormatMonzo()));
            }
            Debug.WriteLine(String.Format("Vector {0,-15} {1}", vector.FormatFraction(), vector.FormatMonzo()));
            //
            string result = null;
            int[] coords = FindCoordinates(basis, vector, vector.GetPowerCount());
            if (coords == null) {
                result = "Invalid Basis or Out of basis subspace";
            } else {
                result = "";
                Rational r = Rational.One;
                for (int i = 0; i < coords.Length; ++i) {
                    r *= basis[i].Pow(coords[i]);
                    result += String.Format(" * ({0})^{1}", basis[i].FormatFraction(), coords[i]);
                }
                result += " = " + r.FormatFraction();
            }
            Debug.WriteLine(result);
        }

        private static void Test1() {
            Rational r0 = new Rational(25, 24);         //         25/24 |-3 -1 2>  Chroma, Chromatic semitone
            Rational r1 = new Rational(81, 80);         //         81/80 |-4 4 -1>  Syntonic comma
            Rational r2 = new Rational(128, 125);       //       128/125 |7 0 -3>   Enharmonic diesis, Lesser diesis
            Rational r3 = new Rational(2048, 2025);     //     2048/2025 |11 -4 -2> Diaschisma (128/125 / 81/80)     
            Rational r4 = new Rational(531441, 524288); // 531441/524288 |-19 12 >  Pif (81/80 * 32805/32768)

            //CheckBasis(new[] { r0, r1, r2 }, r3);
            //CheckVector(new[] { r4, r2, r3, r1, r2 }, r1);
            CheckVector(new[] { r0, r4, r1, r2 }, r3);
        }

        public static void Test() {
            Test1();
        }
        #endregion
    }


}
