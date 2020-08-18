using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics;

//!!! too dummy module - make some Tests.Win.Run

namespace Rationals.IntegersColored.Win
{
    using Rationals.IntegersColored;
    using Rationals.Testing.Win;

    static class Program
    {
        [STAThread]
        public static void Main()
        {
            var painting = new Painting();

            Utils.RunImageInput(painting, "IntegersColored");
        }
    }
}