//#define TRACE_MATRIX

using System;
using System.Collections.Generic;
#if DEBUG
  using System.Linq;
  using System.Diagnostics;
#endif

namespace Rationals
{
    // Vector math. Rationals as integer vectors.
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

        private static int GetMaxLength(Rational[] rs, int count) {
            int len = 0;
            for (int i = 0; i < count; ++i) {
                int l = rs[i].GetHighPrimeIndex() + 1;
                if (len < l) len = l;
            }
            return len;
        }

        // System of linear equations
        public class Matrix {
            public int basisSize = 0; // < width
            public int width;  // vector count
            public int height; // vector length
            public int[,] m; // matrix (transposed memory to quick copying vector arrays)
            //
            public int[] ro; // row order
            public int[] leadCols; // column -> row
            //

            public Matrix(int[][] basis, int[] vector, int vectorLength) {
                basisSize = basis.Length;
                width = basisSize + 1;
                height = vectorLength;
                //
                m = new int[width, height];
                for (int j = 0; j < basisSize; ++j) {
                    //!!! fail: C# can't copy memory from 1d to 2d - we might use 1d array for matrix!
                    //Array.Copy(basis[i], 0, m, i * height, vectorLength);
                    for (int i = 0; i < vectorLength; ++i) m[j, i] = Powers.SafeAt(basis[j], i);
                }
                //Array.Copy(vector, 0, m, basisSize * height, vectorLength);
                for (int i = 0; i < vectorLength; ++i) m[basisSize, i] = Powers.SafeAt(vector, i);

                Init();
            }

            private void Init() {
                ro = new int[height];
                leadCols = new int[height];
                for (int i = 0; i < height; ++i) {
                    ro[i] = i; // default order
                    leadCols[i] = -1; // no lead found for this row
                }
            }

            //!!! standard basis added here
            public Matrix(Rational[] basis, int vectorLength = -1, int vectorCount = -1, bool makeDiagonal = false) {
                //
                if (basis == null) throw new ArgumentException();
                if (vectorCount  == -1) vectorCount  = basis.Length; // other vectors may be invalid
                if (vectorLength == -1) vectorLength = GetMaxLength(basis, vectorCount);
                //
                basisSize = vectorCount;
                width = basisSize + vectorLength; // we add standard basis here
                height = vectorLength;
                //
                m = new int[width, height];
                for (int j = 0; j < basisSize; ++j) {
                    //!!! fail: C# can't copy memory from 1d to 2d - we might use 1d array for matrix!
                    int[] pows = basis[j].GetPrimePowers();
                    //Array.Copy(basis[i], 0, m, i * height, vectorLength);
                    for (int i = 0; i < vectorLength; ++i) m[j, i] = Powers.SafeAt(pows, i);
                }
                // add standard basis
                for (int j = 0; j < vectorLength; ++j) {
                    m[basisSize + j, j] = 1;
                }

                Init();

                if (makeDiagonal) {
                    MakeEchelon();
                    ReduceRows();
                }
            }

            public void Trace(string caption = null) {
#if TRACE_MATRIX
                if (caption != null) Debug.WriteLine(caption);
                for (int i = 0; i < height; ++i) {
                    for (int j = 0; j < width; ++j) {
                        Debug.Write(String.Format("{0,5}", m[j, ro[i]]));
                    }
                    Debug.Write("\n");
                }
#else
                return;
#endif
            }

            private int GetColumnLead(int col, int row) {
                // find a lead in this column for current row
                for (int i = row; i < height; ++i) {
                    int lead = m[col, ro[i]];
                    if (lead != 0) { // lead found
                        if (i != row) { // swap rows
                            int ri = ro[i]; // real index
                            ro[i] = ro[row];
                            ro[row] = ri;
                        }
                        return lead;
                    }
                }
                return 0; // only zeros left on this column
            }

            private void SolveRow(int col, int row, int i) {
                int c = m[col, ro[i]];
                if (c == 0) return; // already solved
                //
                int lead = m[col, ro[row]];
                //
                int d0, d1;
                Solve(lead, c, out d0, out d1);
                //
                m[col, ro[i]] = 0;
                //for (int j = col + 1; j < width; ++j) { //!!! bad optimization
                for (int j = 0; j < width; ++j) if (j != col) {
                    m[j, ro[i]] = 
                        m[j, ro[i]] * d1 + 
                        m[j, ro[row]] * d0;
                }
            }

            //
            public void MakeEchelon()
            {
                int row = 0;
                int col = 0;
                while (row < height && col < basisSize)
                {
                    // find a lead in this column for current row
                    int lead = GetColumnLead(col, row);
                    if (lead == 0) { // no lead found: only zeros left in this column
                        ++col;
                        continue;
                    }

                    // make all zeros above the lead
                    for (int i = row + 1; i < height; ++i) {
                        SolveRow(col, row, i);
                    }

                    leadCols[row] = col;

                    Trace(String.Format("-- {0},{1} ->", row, col));

                    ++col;
                    ++row;
                }
            }

            public int[] FindCoordinates() {

                // We use https://en.wikipedia.org/wiki/Gaussian_elimination

                Trace("----------------------------------------- start:");

                //
                MakeEchelon(); //!!! move out
                //

                // back substitution to reduced row echelon form

                int[] coordinates = new int[basisSize];

                int colPrev = -1;

                for (int row = height - 1; row >= 0; --row)
                {
                    int col = leadCols[row];
                    if (col == -1) continue; // no lead in this row (all zeros?)

                    int b = m[basisSize, ro[row]];
                    if (b == 0) {
                        for (int vi = row; vi >= 0; --vi) m[col, ro[vi]] = 0; //!!! for debug
                        Trace(String.Format("-- back for {0},{1} ->", row, col));

                        continue; // no need of this row coordinate
                    }

                    int res = 0;
                    for (; colPrev == -1 || col < colPrev; ++col) { // we check all leads in this row to find an integer solution
                        if (col == basisSize) return null; // last column reached - no solutions (independent vectors?)
                        int lead = m[col, ro[row]];
                        if (lead == 0) continue; // try next lead (we need to get b != 0)
                        if (b % lead != 0) continue; // try next lead (non-integer coordinate)
                        res = b / lead;
                        break; // next row
                    }
                    if (res == 0) return null; // no integer solution found
                    m[basisSize, ro[row]] = res; //!!! for debug
                    m[col, ro[row]] = 1; //!!! for debug
                    for (int vi = row - 1; vi >= 0; --vi) {
                        int c = m[col, ro[vi]];
                        m[basisSize, ro[vi]] -= c * res;
                        m[col, ro[vi]] = 0; //!!! for debug
                    }
                    coordinates[col] = res;
                    colPrev = col;

                    Trace(String.Format("-- back for {0},{1} ->", row, col));
                }

                return coordinates;
            }

            public void ReduceRows() {

                for (int row = height - 1; row >= 0; --row)
                {
                    int col = leadCols[row];
                    if (col == -1) continue; // no lead in this row (all zeros?)

                    for (int i = row - 1; i >= 0; --i) {
                        SolveRow(col, row, i);
                    }

                    Trace(String.Format("-- reduce {0},{1} ->", row, col));
                }
            }

            public int[] FindCoordinates(Rational vector) {
                int len = vector.GetHighPrimeIndex() + 1;
                if (len > height) return null;
                //if (width < basisSize + len) throw new Exception("");
                int[] pows = vector.GetPrimePowers();
                int[] v = new int[height];
                Array.Copy(pows,0, v,0, len);
                //
                int[] result = new int[basisSize];
                for (int row = 0; row < height; ++row) {
                    //
                    int col = leadCols[row];
                    int lead = col == -1 ? 0 : m[col, ro[row]];
                    //
                    int b = 0;
                    for (int i = 0; i < height; ++i) {
                        b += v[i] * m[basisSize + i, ro[row]];
                    }
                    //
                    if (lead == 0) {
                        if (b != 0) return null;
                    } else {
                        if (b % lead != 0) return null; // non-integer coordinate
                        result[col] = b / lead;
                    }
                }
                return result;
            }

            public float[] FindFloatCoordinates(Rational vector) {
                int len = vector.GetHighPrimeIndex() + 1;
                if (len > height) return null;
                //if (width < basisSize + len) throw new Exception("");
                int[] pows = vector.GetPrimePowers();
                int[] v = new int[height];
                Array.Copy(pows, 0, v, 0, len);
                //
                float[] result = new float[basisSize];
                for (int row = 0; row < height; ++row) {
                    //
                    int col = leadCols[row];
                    int lead = col == -1 ? 0 : m[col, ro[row]];
                    //
                    int b = 0;
                    for (int i = 0; i < height; ++i) {
                        b += v[i] * m[basisSize + i, ro[row]];
                    }
                    //
                    if (lead == 0) {
                        if (b != 0) return null; // no solution
                    } else {
                        result[col] = (float)b / lead;
                    }
                }
                return result;
            }

            public RationalX[] FindRationalCoordinates(Rational vector) {
                //!!! seems fails if vector == 1/1
                int len = vector.GetHighPrimeIndex() + 1;
                if (len > height) return null;
                //if (width < basisSize + len) throw new Exception("");
                int[] pows = vector.GetPrimePowers();
                int[] v = new int[height];
                Array.Copy(pows, 0, v, 0, len);
                //
                RationalX[] result = new RationalX[basisSize];
                for (int row = 0; row < height; ++row) {
                    //
                    int col = leadCols[row];
                    int lead = col == -1 ? 0 : m[col, ro[row]];
                    //
                    int b = 0;
                    for (int i = 0; i < height; ++i) {
                        b += v[i] * m[basisSize + i, ro[row]];
                    }
                    //
                    if (lead == 0) {
                        if (b != 0) return null; // no solution
                    } else {
                        result[col] = new RationalX(b, lead);
                    }
                }
                return result;
            }

        }

        public static int[] FindCoordinates(Rational[] basis, Rational vector, int vectorLength) {
            int basisSize = basis.Length;

            // get prime powers
            int[][] b = new int[basisSize][];
            for (int i = 0; i < basisSize; ++i) {
                b[i] = basis[i].GetPrimePowers();
            }
            int[] v = vector.GetPrimePowers();

            //
            Matrix matrix = new Matrix(b, v, vectorLength);
            int[] coordinates = matrix.FindCoordinates();

#if DEBUG
            // check result
            if (coordinates != null) {
                Rational r = Rational.One;
                for (int i = 0; i < coordinates.Length; ++i) {
                    r *= basis[i].Power(coordinates[i]);
                }
                if (!r.Equals(vector)) throw new Exception(
                    String.Format("FindCoordinates failed: {0} != {1}", r, vector)
                );
            }
#endif

            return coordinates;
        }

        /*
        public static int[] FindCoordinates(Rational[] basis, Rational vector, int vectorLength)
        {
            int basisSize = basis.Length;

            // get prime powers
            int[][] b = new int[basisSize][];
            for (int i = 0; i < basisSize; ++i) {
                b[i] = basis[i].GetPrimePowers();
            }
            int[] v = vector.GetPrimePowers();

            //
            int[] coordinates = FindCoordinates(b, v, vectorLength);

#if DEBUG
            // check result
            if (coordinates != null) {
                Rational r = Rational.One;
                for (int i = 0; i < coordinates.Length; ++i) {
                    r *= basis[i].Pow(coordinates[i]);
                }
                if (!r.Equals(vector)) throw new Exception(
                    String.Format("FindCoordinates failed: {0} != {1}", r, vector)
                );
            }
#endif

            return coordinates;
        }
        */

        #region Tests
#if DEBUG
        private static void CheckVector(Rational[] basis, Rational vector, int vectorLength, bool addStandard = false) {
            // add standard basis
            int basisSize = basis.Length;
            if (addStandard) {
                Array.Resize(ref basis, basisSize + vectorLength);
                for (int i = 0; i < vectorLength; ++i) {
                    basis[basisSize + i] = Rational.Prime(i);
                }
            }
            //
            for (int i = 0; i < basis.Length; ++i) {
                Debug.WriteLine(String.Format("Basis {0}. {1,-15} {2}", i, basis[i].FormatFraction(), basis[i].FormatMonzo()));
            }
            Debug.WriteLine(String.Format("Vector {0,-15} {1}", vector.FormatFraction(), vector.FormatMonzo()));
            //
            int[] coords = FindCoordinates(basis, vector, vectorLength);
            if (coords == null) {
                Debug.WriteLine("Invalid Basis or Out of basis subspace");
            } else {
                Debug.WriteLine("Coordinates: " + Powers.ToString(coords, "()"));
                //
                Debug.WriteLine(vector.FormatFraction() + " = ");
                for (int i = 0; i < basisSize; ++i) {
                    int e = Powers.SafeAt(coords, i);
                    Debug.Print(" * ({0})^{1}", basis[i].FormatFraction(), e);
                }
                if (addStandard) {
                    Rational r = new Rational(coords.Skip(basisSize).ToArray());
                    Debug.Print(" * {0} {1} {2}", r.FormatFraction(), r.FormatMonzo(), r.FormatNarrowPowers());
                }
            }
        }

        private static void Test1() {
            Rational r0 = new Rational(25, 24);         //         25/24 |-3 -1 2>  Chroma, Chromatic semitone
            Rational r1 = new Rational(81, 80);         //         81/80 |-4 4 -1>  Syntonic comma
            Rational r2 = new Rational(128, 125);       //       128/125 |7 0 -3>   Enharmonic diesis, Lesser diesis
            Rational r3 = new Rational(2048, 2025);     //     2048/2025 |11 -4 -2> Diaschisma (128/125 / 81/80)     
            Rational r4 = new Rational(531441, 524288); // 531441/524288 |-19 12 >  Pif (81/80 * 32805/32768)

            //CheckBasis(new[] { r0, r1, r2 }, r3, 3);
            //CheckVector(new[] { r4, r2, r3, r1, r2 }, r1, 3);
            //CheckVector(new[] { r0, r4, r1, r2 }, r3, 3);

            CheckVector(
                new[] {
                    //new Rational(2, 3),
                    //new Rational(2, 1),
                    r1,
                    r2,
                    new Rational(25, 18),
                    //new Rational(45, 32),
                    //new Rational(36, 25),
                },
                //new Rational(new[] {0,1,0}),
                //new Rational(2, 9),
                //new Rational(27, 8),
                //new Rational(10, 3),
                //new Rational(25, 18),
                //new Rational(45, 32),
                new Rational(36, 25),
                //new Rational(16, 9),

                //addStandard: true,
                vectorLength: 3
            );
        }

        private static void Test2() {
            Rational r0 = new Rational(25, 24);
            Rational r1 = new Rational(81, 80);
            Rational r2 = new Rational(128, 125);
            Debug.Print("{0}", new Rational(45, 32) * r1.Power(-1)); // 25/18
        }

        private static void Test3_FindCoordinates() {
            Rational r0 = new Rational(25, 24);         //         25/24 |-3 -1 2>  Chroma, Chromatic semitone
            Rational r1 = new Rational(81, 80);         //         81/80 |-4 4 -1>  Syntonic comma
            Rational r2 = new Rational(128, 125);       //       128/125 |7 0 -3>   Enharmonic diesis, Lesser diesis
            Rational r3 = new Rational(2048, 2025);     //     2048/2025 |11 -4 -2> Diaschisma (128/125 / 81/80)     
            Rational r4 = new Rational(531441, 524288); // 531441/524288 |-19 12 >  Pif (81/80 * 32805/32768)

            Rational[] rs = new[] {
                //new Rational(2, 1),
                //new Rational(3, 2),
                r1,
                r2,
                //new Rational(36, 25),
                //new Rational(9, 8),
                //new Rational(2, 1),
                Rational.Prime(0), // 2
                Rational.Prime(1), // 3
                Rational.Prime(2), // 5
                Rational.Prime(3), // 7
            };

            var matrix = new Matrix(rs, vectorLength: 3);
            matrix.Trace("----------------------------------------- start:");
            matrix.MakeEchelon();
            matrix.ReduceRows();

            var r =
                //new Rational(81, 80)
                //r2
                //new Rational(10, 9)
                new Rational(9, 8)
            ;

            var coords = matrix
                //.FindCoordinates(r);
                .FindFloatCoordinates(r);

            Debug.WriteLine(r.FormatFraction() + " =");
            if (coords != null) {
                for (int i = 0; i < coords.Length; ++i) {
                    Debug.WriteLine("{0,8}: {1,5}", rs[i].FormatFraction(), coords[i]);
                }
            }
        }

        private static void Test4_Temperament() {
            Rational[] vectors = new[] {
                new Rational(81, 80),
                new Rational(128, 125),
                new Rational(648, 625),
                new Rational(2, 1),
                new Rational(3, 2),
                new Rational(5, 4),
            };

            float[] temperament = new float[vectors.Length];
            for (int i = 0; i < vectors.Length; ++i) {
                float c = (float)vectors[i].ToCents();
                temperament[i] = c;
                if (c < 70) temperament[i] = 0; // comma - temper out
            }

            var matrix = new Matrix(vectors, 3);

            matrix.Trace("----------------------------------------- start:");

            matrix.MakeEchelon();
            matrix.ReduceRows();

            Rational[] rs = new[] {
                new Rational(2, 1),
                new Rational(3, 2),
                new Rational(5, 4),
                new Rational(6, 5),
                new Rational(10, 9),
                new Rational(8, 9),
            };
            foreach (Rational r in rs) {
                float[] coords = matrix.FindFloatCoordinates(r);
                float cents = 0;
                if (coords != null) {
                    for (int i = 0; i < coords.Length; ++i) {
                        cents += temperament[i] * coords[i];
                    }
                }
                Debug.WriteLine("{0} ({1}): \t{2} -> {3}", 
                    r.FormatFraction(),
                    (float)r.ToCents(),
                    coords == null ? "--" : String.Join(",", coords.Select(c => c.ToString())),
                    cents
                );
            }
        }

        public static void Test() {
            //Test1();
            //Test2();
            Test3_FindCoordinates();
            //Test4_Temperament();
        }
#endif
#endregion
    }

}
