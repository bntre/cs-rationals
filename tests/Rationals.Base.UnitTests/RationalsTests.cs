//using System.Diagnostics;
using System.Linq;
using Rationals.Testing;

namespace Rationals.Base.UnitTests
{
    [Test]
    class RationalsTests
    {
        [Fact]
        void Test1_Rationals()
        {
            Assert.Equal(new Rational(1), Rational.One);
            Assert.Equal(new Rational(2), Rational.Two);

            var r0 = new Rational(4, 5);
            var r1 = new Rational(6, 4);

            Assert.Equal(new Rational(4, 5), r0);

            Assert.Equal("4/5", r0.FormatFraction());
            Assert.Equal("4/5", r0.ToString());
            Assert.Equal("3/2", r1.ToString());

            Assert.Equal("6/5",  (r0 * r1).FormatFraction());
            Assert.Equal("8/15", (r0 / r1).FormatFraction());

            var r3 = new Rational(81, 80);
            Assert.Equal("|-4 4 -1>", r3.FormatMonzo());
            Assert.Equal("|-2 4 -1}", r3.FormatNarrowPowers());
        }

        [Fact]
        void Test5_ParseRationals()
        {
            Rational r0 = Rational.Parse(" 81 / 80 \n");
            Assert.Equal("|-4 4 -1>", r0.FormatMonzo());

            Rational r1 = Rational.Parse(" | 7 \t 0 -3> ");
            Assert.Equal("|7 0 -3>", r1.FormatMonzo());
        }

        [Fact]
        void Test_Narrows()
        {
            // Single narrow
            System.Action<string, string, string> testNarrow = (rationalText, baseText, narrowText) => {
                Rational r   = Rational.Parse(rationalText);
                Rational b   = Rational.Parse(baseText);
                Rational exp = Rational.Parse(narrowText);
                Rational res = NarrowUtils.MakeNarrow(r, b);
                Assert.Equal(exp, res);
            };
            testNarrow("3", "2",    "3/2");
            testNarrow("3", "1/2",  "3/2");
            testNarrow("5", "6",    "5/6");

            // Subgroup
            System.Action<string, string> testNarrows = (subgroupText, narrowsText) => {
                Rational[] rs = Rational.ParseRationals(subgroupText, ". ");
                Subgroup subgroup = new Subgroup(rs);
                Rational[] ns = subgroup.GetNarrowItems();
                Assert.Equal(narrowsText, Rational.FormatRationals(ns, ". "));
            };
            testNarrows("2. 3. 5", "2. 3/2. 5/4");
            testNarrows("3. 5. 7", "3. 5/3. 7/9");

            // Default narrows
            System.Action<int, string> testDefaultNarrows = (basePrimeIndex, narrowsText) => {
                Rational[] ns = NarrowUtils.GetDefault(10, basePrimeIndex);
                Assert.Equal(narrowsText.Replace(" ", ""), Rational.FormatRationals(ns, ","));
            };
            testDefaultNarrows(0, "2, 3/2, 5/4, 7/8, 11/8, 13/16, 17/16, 19/16, 23/16, 29/32");
            testDefaultNarrows(1, "2, 3,   5/3, 7/9, 11/9, 13/9,  17/27, 19/27, 23/27, 29/27");
            testDefaultNarrows(2, "2, 3,   5,   7/5, 11/5, 13/25, 17/25, 19/25, 23/25, 29/25");
            testDefaultNarrows(3, "2, 3,   5,   7,   11/7, 13/7,  17/7,  19/7,  23/49, 29/49");

            // Formatting
            Rational[] narrows = Rational.ParseRationals("2. 3/2. 5/4. 7/8");
            Assert.Equal("|1 1}",     NarrowUtils.FormatNarrowPowers(new Rational(3),     narrows));  // 3     = 2^1 * (3/2)^1
            Assert.Equal("|2 -4 1}",  NarrowUtils.FormatNarrowPowers(new Rational(80,81), narrows));  // 80/81 = 2^2 * (3/2)^-4 * (5/4)^1
        }

    }
}