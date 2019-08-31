using System.Diagnostics;
using Rationals.Testing;

namespace Rationals.Base.UnitTests
{
    [Test]
    class RationalsTests
    {
        [Fact]
        void Test1_Rationals()
        {
            Assert.Equals(new Rational(1), Rational.One);
            Assert.Equals(new Rational(2), Rational.Two);

            var r0 = new Rational(4, 5);
            var r1 = new Rational(6, 4);

            Assert.Equals(new Rational(4, 5), r0);

            Assert.Equals("4/5", r0.FormatFraction());
            Assert.Equals("4/5", r0.ToString());
            Assert.Equals("3/2", r1.ToString());

            Assert.Equals("6/5",  (r0 * r1).FormatFraction());
            Assert.Equals("8/15", (r0 / r1).FormatFraction());

            var r3 = new Rational(81, 80);
            Assert.Equal("|-4 4 -1>", r3.FormatMonzo());
            Assert.Equal("|-2 4 -1}", r3.FormatNarrows());
        }

        [Fact]
        void Test5_ParseRationals()
        {
            Rational r0 = Rational.Parse(" 81 / 80 \n");
            Assert.Equal("|-4 4 -1>", r0.FormatMonzo());

            Rational r1 = Rational.Parse(" | 7 \t 0 -3> ");
            Assert.Equal("|7 0 -3>", r1.FormatMonzo());
        }

    }
}