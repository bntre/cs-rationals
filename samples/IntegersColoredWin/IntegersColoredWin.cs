#define USE_XAML

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Windows;


#if USE_XAML
namespace Rationals.IntegersColored.Win
{
    // WPF magic creates MainWindow.g.i.cs which implements InitializeComponent() from MainWindow.xaml
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }
    }
}
#endif


namespace Rationals.IntegersColored.Win
{
    using Mode = Painting.ViewMode;

    using WindowInfo = Torec.UI.WindowInfo<Window, Torec.Drawing.Image>;

    static class Program
    {
        static void AddControl(WindowInfo wi, Painting painting, string nativeName, Mode mode) {
            wi.AddControl(painting, painting, nativeName, contextId: (int)mode);
        }

        [STAThread]
        public static void Main()
        {
            var painting = new Painting(useTimeline: false);
            var timeline = new Painting(useTimeline: true);

            var wi = new WindowInfo { title = "Integers Colored" };
#if USE_XAML
            wi.window = new MainWindow();
            AddControl(wi, painting, "cell00", Mode.Linear);
            AddControl(wi, painting, "cell10", Mode.Logarithmic);
            AddControl(wi, timeline, "cell01", Mode.Logarithmic | Mode.TimelineFlag);
            AddControl(wi, timeline, "cell11", Mode.Logarithmic);
#else
            // Just set the whole content
            wi.AddControl(painting);
#endif

            Torec.UI.Win.Utils.RunWindow(wi);
        }
    }
}