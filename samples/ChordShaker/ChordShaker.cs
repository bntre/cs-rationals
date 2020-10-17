#define USE_XAML

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Windows;

using W = Torec.UI.Win;

using WindowInfo = Torec.UI.WindowInfo<System.Windows.Window, Torec.Drawing.Image>;

namespace ChordShaker
{




#if USE_XAML
    // WPF magic creates MainWindow.g.i.cs which implements InitializeComponent() from MainWindow.xaml
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }
    }
#endif

    static class Program
    {
        [STAThread]
        public static void Main()
        {
            //var painting = new Painting(useTimeline: false);
            //var timeline = new Painting(useTimeline: true);

            var wi = new WindowInfo { title = "Integers Colored" };
#if USE_XAML
            wi.window = new MainWindow();
            //wi.AddControl(painting, "cell00", contextId: (int)Mode.Linear);
            //wi.AddControl(painting, "cell10", contextId: (int)Mode.Logarithmic);
            //wi.AddControl(timeline, "cell01", contextId: (int)(Mode.Logarithmic | Mode.TimelineFlag));
            //wi.AddControl(timeline, "cell11", contextId: (int)Mode.Logarithmic);
#else
            // Just set the whole content
            wi.AddControl(painting);
#endif

            W.Utils.RunWindow(wi);
        }
    }
}