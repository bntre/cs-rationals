//#define USE_XAML

using System;
using System.Collections.Generic;
using System.Text;
//using System.Linq;
using System.Diagnostics;
//using System.Windows;
using System.Windows.Input;

using Torec.UI;
using Torec.Channels;
using Torec.Drawing;
using Color = System.Drawing.Color;


namespace ChordShaker
{
    #region Signals
    public class SignalBase {
        List<SignalBase> _parents = null;
        List<SignalBase> _children = null;

        static private void AddSafe(ref List<SignalBase> signals, SignalBase item) {
            if (signals == null) signals = new List<SignalBase>();
            signals.Add(item);
        }
        protected void AddParent(SignalBase parent) {
            AddSafe(ref _parents, parent);
            AddSafe(ref parent._children, this);
        }

        protected virtual void Update() { }

        public void RunCascade() {
            var updated = new HashSet<SignalBase>();
            Cascade(updated);
        }
        private void Cascade(HashSet<SignalBase> updated) {
            if (_parents != null) {
                for (int i = 0; i < _parents.Count; ++i) {
                    if (!updated.Contains(_parents[i])) {
                        return; // some parent not updated
                    }
                }
            }
            Update();
            updated.Add(this);
            if (_children != null) {
                for (int i = 0; i < _children.Count; ++i) {
                    _children[i].Cascade(updated);
                }
            }
        }
    }

    public class SignalSamples : SignalBase
    {
        public struct Sample {
            public double time;
            public double value;
            //
            public static Sample Invalid = new Sample { time = double.NaN };
            public bool IsValid() { return !double.IsNaN(time); }
        }
        protected List<Sample> _samples = new List<Sample>();

        public virtual void Add(double time, double value) {
            _samples.Add(new Sample { time = time, value = value });
            //!!! delete outdated values here
        }

        public int GetSampleCount() {
            return _samples.Count;
        }
        public Sample GetSample(int index) {
            if (index < 0) index += _samples.Count;
            if (index < 0 || index >= _samples.Count) return Sample.Invalid;
            return _samples[index];
        }
        public Sample[] GetSamples() {
            return _samples.ToArray();
        }
    }

    public class Derivative : SignalSamples {
        protected SignalSamples _original;
        private double _scale = 1.0;
        public Derivative(SignalSamples original, double scale = 1.0) {
            AddParent(original);
            _original = original;
            _scale = scale;
        }
        protected override void Update() {
            double value = 0;
            var s1 = _original.GetSample(-1);
            var s2 = _original.GetSample(-2);
            Debug.Assert(s1.IsValid());
            if (s2.IsValid()) {
                value = (s1.value - s2.value) / (s1.time - s2.time) * _scale;
            }
            Add(s1.time, value);
        }
    }

    public class Extremas : SignalSamples {
        protected SignalSamples _original;
        private int _lastSign = 0;
        public Extremas(SignalSamples original) {
            AddParent(original);
            _original = original;
        }
        protected override void Update() {
            var s1 = _original.GetSample(-1);
            var s2 = _original.GetSample(-2);
            Debug.Assert(s1.IsValid());
            if (s2.IsValid()) {
                int sign = Math.Sign(s1.value - s2.value);
                if (_lastSign != sign) {
                    if (_lastSign != 0) { // s2 was an extrema - collect its time and sign
                        Add(s2.time, _lastSign);
                    }
                    _lastSign = sign;
                }
            }
        }
    }

    public class Envelope : SignalSamples {
        private SignalSamples _original;
        private SignalSamples _extremas;
        private int _sign;
        public Envelope(SignalSamples original, SignalSamples extremas, bool upper) {
            AddParent(original);
            AddParent(extremas);
            _original = original;
            _extremas = extremas;
            _sign = upper ? 1 : -1;
        }
        protected override void Update() {
            var sO = _original.GetSample(-2);
            var sE = _extremas.GetSample(-1);
            if (sO.IsValid() && sE.IsValid()) {
                if (sE.time == sO.time && sE.value == _sign) {
                    base.Add(sO.time, sO.value);
                }
            }
        }
    }


    public class Integrate : SignalSamples {
        SignalSamples _original;
        double _scale = 1.0;
        public Integrate(SignalSamples original, double scale = 1.0) {
            AddParent(original);
            _original = original;
            _scale = scale;
        }

        protected override void Update() {
            //                  samples
            //                 A       B
            //  Parent        -2      -1
            //  Intergal      -1    [Add]

            double value = 0;
            double timeA = -1;
            double timeB = -1;

            int countI = this.GetSampleCount();
            if (countI > 0) {
                var sA = this.GetSample(-1);
                value = sA.value;
                timeA = sA.time;
            }

            int countP = _original.GetSampleCount();
            Debug.Assert(countP > 0);

            if (countP == 1) {
                var sB = _original.GetSample(-1);
                timeB = sB.time;
            } else {
                var sA = _original.GetSample(-2);
                var sB = _original.GetSample(-1);
                Debug.Assert(countI == 0 || timeA == sA.time);
                timeB = sB.time;

                double S = (timeB - timeA) * (sA.value + sB.value) / 2 * _scale;
                //value += S;
#if true
                value = value + S;
#else
                double dt = timeB - timeA; // (0..
                //double c = Math.Pow(0.001, dt * 1);
                double c = Math.Exp(-dt * 3); // c==1 if dt==0

                value = (value * c) + S * (1.0 - c);

                Debug.WriteLine("Integrate: dt {0:0.0000} S {1:0.0000} c {2:0.0000} value {3}", dt, S, c, value);
#endif
            }

            base.Add(timeB, value);
        }
    }
#endregion Signals

    public class Painting : InteractiveControl, IDrawer<Image>
    {
        public Painting()
        {
            InitChannels();

            InitSignals();

            base.ChannelsChanged += Input_ChannelsChanged;

            _stopWatch.Start();
        }

        protected Channel _channelMouseY;

        protected SignalSamples _signalMouse;
        protected SignalSamples _signalMouseD;
        protected SignalSamples _signalMouseDD;
        protected SignalSamples _signalMouseI;
        protected SignalSamples _signalMouseExtrem;
        protected SignalSamples _signalMouseEnvUpper;
        protected SignalSamples _signalMouseEnvLower;

        protected Stopwatch _stopWatch = new Stopwatch();

        private void InitChannels() {
            //const int X = 0;
            const int Y = 1;

            var mouseY = new ChannelInfo(-1,1, 0, "mouseY");

            _channelMouseY = base.MakeChannel(mouseY, MouseButtons.None, KeyModifiers.None, Y);
        }

        private void InitSignals() {
            _signalMouse   = new SignalSamples();
            _signalMouseD  = new Derivative(_signalMouse,  scale: 0.1);
            _signalMouseDD = new Derivative(_signalMouseD, scale: 0.1);
            _signalMouseI  = new Integrate (_signalMouse,  scale: 1.0);
            _signalMouseExtrem   = new Extremas(_signalMouse);
            _signalMouseEnvUpper = new Envelope(_signalMouse, _signalMouseExtrem, true);
            _signalMouseEnvLower = new Envelope(_signalMouse, _signalMouseExtrem, false);
        }

        protected double GetCurrentTime() {
            return _stopWatch.ElapsedMilliseconds / 1000.0;
        }

        public void DrawSignal(Image image, double currentTime, double timeSpan, SignalSamples signal, Color color, double scaleY = 1.0)
        {
            int segmentLimit = 200; //!!! configure
            var poses = new List<Point>();
            for (int i = 1; i <= segmentLimit; ++i) {
                var s = signal.GetSample(-i);
                if (!s.IsValid()) break; // no more samples
                double t = s.time - currentTime;
                Point pos = new Point((float)t, (float)(s.value * scaleY));
                poses.Insert(0, pos);
                if (t < -timeSpan) break; // gone out of view
            }
            DrawSignal(image, poses.ToArray(), color);
        }

        protected void DrawSignal(Image image, Point[] poses, Color color) {
            // Draw circles
            foreach (Point pos in poses) {
                image.Circle(pos, 0.01f)
                    .Add()
                    .FillStroke(color, Color.Empty);
            }
            // Draw line
            if (poses.Length >= 2) {
                image.Line(poses)
                    .Add()
                    .FillStroke(Color.Empty, color, strokeWidth: 0.005f);
            }
        }

#region InteractiveControl
        protected void Input_ChannelsChanged(int[] channelIds)
        {
            if (!_stopWatch.IsRunning) return;

            double time = GetCurrentTime();

            if (Array.IndexOf(channelIds, _channelMouseY.GetId()) != -1) {
                double mouseY = - _channelMouseY.GetValue(); // flip here
                Debug.WriteLine("Signal mouse {0}", mouseY);
                _signalMouse.Add(time, mouseY);
                _signalMouse.RunCascade();
            }

            // Initiate redrawing
            UpdateImage?.Invoke();
        }
        public override void DoIdle() {
            // Initiate redrawing continuously
            if (_stopWatch.IsRunning) {
                UpdateImage?.Invoke();
            }
        }
        public override void OnKeyDown(int keyCode, KeyModifiers mods) {
            base.OnKeyDown(keyCode, mods);
            if ((Key)keyCode == Key.Space)
            {
                if (_stopWatch.IsRunning) {
                    _stopWatch.Stop();
                } else {
                    _stopWatch.Start();
                }
            }
        }
#endregion InteractiveControl

#region IDrawer
        public event Action UpdateImage;

        public Image GetImage(int pixelWidth, int pixelHeight, int contextId)
        {
            double timeSpan = 10.0; // in seconds

            float x0 = (float)-timeSpan;
            float x1 = 0f;

            Viewport viewport = new Viewport(
                pixelWidth,
                pixelHeight,
                x0,x1, -1f,1f,
                yUp: true
            );

            Image image = new Image(viewport); // recreate image

            // draw grid bg
            var bounds = viewport.GetUserBounds();
            image.Rectangle(bounds)
                .Add().FillStroke(Color.White, Color.Empty);
            Color gridColor = ColorUtils.MakeColor(0xFFEEEEEE);
            image.Line(new[] { new Point(bounds[0].X, 0), new Point(bounds[1].X, 0) })
                .Add().FillStroke(Color.Empty, gridColor, 0.01f);
            for (float x = 0; x > -timeSpan; x -= 1f) {
                image.Line(new[] { new Point(x, bounds[0].Y), new Point(x, bounds[1].Y) })
                    .Add().FillStroke(Color.Empty, gridColor, 0.01f);
            }

            /*
            var groupLines   = image.Group().Add();
            var groupCircles = image.Group().Add();
            var groupNumbers = image.Group().Add();
            */
#if DEBUG
            timeSpan -= 1.0; // make cutting visible
#endif
            // draw signals
            double currentTime = GetCurrentTime();
            //DrawSignal(image, currentTime, _signalMouseDD, Color.Green);
            DrawSignal(image, currentTime, timeSpan, _signalMouseD,  Color.Red);
            DrawSignal(image, currentTime, timeSpan, _signalMouse,   Color.Blue);
            //DrawSignal(image, currentTime, _signalMouseI,  Color.DarkOrange, 5.0);
            DrawSignal(image, currentTime, timeSpan, _signalMouseExtrem,  Color.GreenYellow);
            DrawSignal(image, currentTime, timeSpan, _signalMouseEnvUpper,  Color.YellowGreen);
            DrawSignal(image, currentTime, timeSpan, _signalMouseEnvLower,  Color.YellowGreen);

            return image;
        }
#endregion IDrawer

    }


#if USE_XAML
        // WPF magic creates MainWindow.g.i.cs which implements InitializeComponent() from MainWindow.xaml
        public partial class MainWindow : System.Windows.Window {
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
            var painting = new Painting();

            var wi = new WindowInfo<System.Windows.Window, Image>();
            wi.title = "Chort Shaker";

#if USE_XAML
            wi.window = new MainWindow();
            wi.AddControl(painting, painting, "cell00");
#else
            // Just set the whole window content
            wi.AddControl(painting, painting);
#endif

            Torec.UI.Win.Utils.RunWindow(wi);
        }
    }
}