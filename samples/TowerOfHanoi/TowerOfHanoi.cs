using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Linq;
using System.Diagnostics;

using Torec.Drawing;
using Color = System.Drawing.Color;


namespace Rationals
{
    public class BinaryTree<T> {
        protected int _levelCount = 0;
        protected int _itemCount = 0;
        protected T[] _items;

        public BinaryTree(int levelCount) {
            _levelCount = levelCount;
            _itemCount = 1 << _levelCount;
            _items = new T[_itemCount];
        }

        public int GetId(int level, int index) {
            int levelSize = 1 << level;
            if (index < 0 || index >= levelSize) throw new IndexOutOfRangeException();
            return (1 << level) + index;
        }

        public int GetParentId(int id) { return id / 2; }

        public int GetRightParentId(int id) {
            if ((id & 1) == 0) return id / 2;
            return GetRightParentId(id / 2);
        }

        public T GetItem(int id) {
            return _items[id];
        }
        public void SetItem(int id, T item) {
            _items[id] = item;
        }

        public T GetItem(int level, int index) {
            return _items[GetId(level, index)];
        }
        public void SetItem(int level, int index, T item) {
            _items[GetId(level, index)] = item;
        }

    }
}

namespace Rationals.Hanoi
{
    internal class State {
        public int[][] rods;
        public string Format() {
            return String.Join(" ", rods.Select(r => String.Join(",", r).PadRight(10)));
        }
    }
    internal class Step {
        public int rod0; // source
        public int rod1; // destination
    }
    internal class TreeItem {
        public State state;
        public Step step;
    }

    internal class Tree : BinaryTree<TreeItem>
    {
        public Tree(int levelCount) : base(levelCount)
        {
            // Create all items
            for (int i = 0; i < _itemCount; ++i) {
                _items[i] = new TreeItem { };
            }

            // Start and build the tree
            GetItem(1, 0).state = ParseState("0  ");
            GetItem(1, 1).state = ParseState("  0");
            GetItem(0, 0).step = new Step { rod0 = 0, rod1 = 2 };
            Build();
        }

        private static State ParseState(string s) {
            return new State {
                rods = s
                    .Split(' ')
                    .Select(ps => ps == ""
                        ? new int[] { }
                        : ps.Split(',')
                            .Select(p => int.Parse(p))
                            .ToArray()
                    )
                    .ToArray()
            };
        }

        private static State InheritState(State s, int rod, int disk) {
            var rods = s.rods.Select(r => r.ToList()).ToArray();
            rods[rod].Add(disk); // add 'disk' to the 'rod'
            return new State {
                rods = rods.Select(r => r.ToArray()).ToArray()
            };
        }

        private void Build(int level = 0, int id = 1)
        {
            // level+1:   s0 -> s1 -> s2 -> s3
            // level:        S0    ->    S1

            int id0 = id * 2;
            int id1 = id * 2 + 1;

            Step step = GetItem(id).step;
            State S0 = GetItem(id0).state;
            State S1 = GetItem(id1).state;

            int rod2 = 3 - step.rod0 - step.rod1; // free rod for next level
            int level1 = level + 1; // next level = disk id

            if (id0*2 >= _itemCount) return;

            GetItem(id0*2  ).state = InheritState(S0, step.rod0, level1);
            GetItem(id0*2+1).state = InheritState(S0,      rod2, level1);
            GetItem(id1*2  ).state = InheritState(S1,      rod2, level1);
            GetItem(id1*2+1).state = InheritState(S1, step.rod1, level1);

            GetItem(id0).step = new Step { rod0 = step.rod0, rod1 =      rod2 };
            GetItem(id1).step = new Step { rod0 =      rod2, rod1 = step.rod1 };

            // recurse
            Build(level1, id0);
            Build(level1, id1);
        }

        public void Trace(int level) {
            Debug.WriteLine("------ Level {0}", level);
            for (int i = 0; i < (1<<level); ++i) {
                int id0 = GetId(level, i);
                int idStep = GetRightParentId(id0);
                var state0 = GetItem(id0).state;
                string stepFormat = null;
                if (idStep != 0) {
                    var step = GetItem(idStep).step;
                    var rods0 = GetItem(id0  ).state.rods;
                    var rods1 = GetItem(id0+1).state.rods;
                    int pos0 = rods0[step.rod0].Length - 1;
                    int pos1 = rods1[step.rod1].Length - 1;
                    int disk = rods0[step.rod0][pos0];
                    stepFormat = String.Format("disk {0}: {1} ({2}) -> {3} ({4})",
                        disk,
                        step.rod0, pos0, step.rod1, pos1);
                }
                Debug.WriteLine("[{0,2}] {1} step [{2,2}]: {3}",
                    id0, GetItem(id0).state.Format(),
                    idStep, stepFormat
                );
            }
        }

        public int GetStateCount(int levelIndex) {
            return 1 << levelIndex;
        }

        #region Per disk info. For drawing.
        public struct StepDisk {
            public int rod0;
            public int rod1;
            public int pos0;
            public int pos1;
            public bool move; // if 'move' then 'rod1' and 'pos1' are set
        }
        public StepDisk[] GetStepDisks(int levelIndex, int state0Index) {
            var ds = new StepDisk[levelIndex]; // levelIndex == disk count
            //
            int id0 = GetId(levelIndex, state0Index);
            var state0 = GetItem(id0).state;
            for (int r = 0; r < 3; ++r) {
                for (int i = 0; i < state0.rods[r].Length; ++i) {
                    int disk = state0.rods[r][i];
                    ds[disk].rod0 = r;
                    ds[disk].pos0 = i;
                }
            }
            // 
            int stepId = GetRightParentId(id0);
            if (stepId != 0) {
                Step step = GetItem(stepId).step;
                int disk = state0.rods[step.rod0].Last();
                ds[disk].move = true;
                ds[disk].rod1 = step.rod1;
                ds[disk].pos1 = state0.rods[step.rod1].Length;
            }
            //
            return ds;
        }
        #endregion

        #region For sound.
        public int[] GetStepDestinationRod(int levelIndex, int state0Index, out int rodIndex) {
            rodIndex = 0;
            //
            int id0 = GetId(levelIndex, state0Index);
            int id1 = id0 + 1;
            int stepId = GetRightParentId(id0);
            if (stepId != 0) {
                Step step = GetItem(stepId).step;
                State state1 = GetItem(id1).state;
                rodIndex = step.rod1;
                return state1.rods[rodIndex];
            }
            return null;
        }
        #endregion
    }

    class TowerView
    {
        public TowerView(int levelCount = 10, Tree tree = null)
        {
            _levelCount = levelCount;

            // set or build tree
            _tree = tree ?? new Tree(_levelCount);

            // prepare graphics
            const int size = 1200; // image size
            const float r = _imageUserRadius;
            _viewport = new Viewport(size, size, -r, r, -r, r);
        }

        // set once
        private int _levelCount; // level 0 - not used; level 1 - moving disk 0;..
        private Tree _tree;
        private const float _imageUserRadius = 2.5f;
        private Viewport _viewport;

        // set per level
        private int _currentLevel = -1;
        private LevelDisk[] _levelDisks; // current level disk info

        // set per step
        private Tree.StepDisk[] _stepDisks;
        
        // set per frame
        private Image _image = null;
        private FrameRod[] _frameRods;


        #region Helpers
        internal static Complex ToComplex(Point p) { return new Complex(p.X, p.Y); }
        internal static Point FromComplex(Complex c) { return new Point((float)c.Real, (float)c.Imaginary); }
        internal static Color MakeColor(long color) { unchecked { return Color.FromArgb((int)color); } }
        #endregion


        private struct LevelDisk {
            public double radius;
            public double height;
            public Color color;
        }

        private void ResetLevelDisks()
        {
            // _currentLevel == level disk count

            _levelDisks = new LevelDisk[_levelCount];

            const double radiusFactor = 0.73;

            // calculate full height

            double fullHeight = 0;

            for (int i = 0; i < _levelCount; ++i) {
                var d = _levelDisks[i];
                bool isFlatten = i >= _currentLevel;

                d.radius = Math.Pow(radiusFactor, i + 1);
                d.height = Math.Pow(radiusFactor, i) - d.radius;

                if (isFlatten) d.height *= 0.2;

                fullHeight += d.height;

                _levelDisks[i] = d;
            }

            // 

            double currentY = 0; // up to fullHeight

            for (int i = 0; i < _levelCount; ++i) {
                var d = _levelDisks[i];
                bool isFlatten = i >= _currentLevel;

                currentY += d.height;
                double c = currentY / fullHeight; // (0..1]

                //c = Math.Pow(c, 0.4);
                d.color = MakeColor(0xFF000000 +
                    //0x010101 * (int)(0xFF * c)
                    0x010101 * (int)(0xBF * c)
                );

                _levelDisks[i] = d;
            }
        }


        private double GetStepMovePart() {
            return 0.75;
        }

        private struct FrameRod {
            public Complex pos; // normal
            public double height;
        }

        public static double GetLevelPhaseRodAngle(int rodIndex, double levelPhase) {
            double p = (-rodIndex - levelPhase) / 3.0 + 0.5;
            return 2 * Math.PI * p; // in radians
        }

        private void ResetFrameRods(double levelPhase) {
            _frameRods = new FrameRod[3];
            for (int i = 0; i < 3; ++i) {
                double a = GetLevelPhaseRodAngle(i, levelPhase);
                _frameRods[i].pos = Complex.FromPolarCoordinates(1.0, a);
                _frameRods[i].height = 0.0;
            }
        }

        private Complex GetDiskRodPosition(int rod, LevelDisk levelDisk) {
            Complex r = _frameRods[rod].pos;
            _frameRods[rod].height += levelDisk.height;
            Complex p = r * (1.0 - _frameRods[rod].height) * 1.5;
            return p;
        }

        private Complex GetDiskMovePosition(Tree.StepDisk stepDisk, LevelDisk levelDisk, double movePhase) {
            Complex pos0 = GetDiskRodPosition(stepDisk.rod0, levelDisk);
            if (movePhase == 0 || !stepDisk.move) {
                return pos0;
            }
            Complex pos1 = GetDiskRodPosition(stepDisk.rod1, levelDisk);
            double k = Math.Pow(movePhase, 1.7); // compact parabolic
            return pos0 * (1 - k) + pos1 * k;
        }

        public Image RenderLevelPhase(int levelIndex, double levelPhase)
        {
            Debug.Assert(0 <= levelPhase && levelPhase < 1);

            if (_currentLevel != levelIndex && levelIndex >= 0) {
                _currentLevel = levelIndex;
                ResetLevelDisks();
            }

            ResetFrameRods(levelPhase);

            // Prepare image
            _image = new Image(_viewport);

            // fill background
            float imR = _imageUserRadius;
            _image.Rectangle(new[] { new Point(-imR, imR), new Point(imR, -imR) }) // looks ugly - fix Rectangle by viewport Bounds
                .Add()
                //.FillStroke(Color.Empty, Color.Black, 0.01f);
                //.FillStroke(MakeColor(0xFFFFEFFF), Color.Empty);
                .FillStroke(Color.White, Color.Empty);

            // draw rod guides
            foreach (var r in _frameRods) {
                _image.Line(new[] {
                        FromComplex(Complex.Zero),
                        FromComplex(r.pos * _imageUserRadius * 2)
                    })
                    .Add()
                    .FillStroke(Color.Empty, Color.LightGray, 0.01f);
            }

            // draw disks
            if (levelIndex >= 0)
            {
                // Get step index and phase
                int stateCount = _tree.GetStateCount(levelIndex);
                double statePhase = levelPhase * stateCount;
                int stateIndex = (int)Math.Floor(statePhase);
                statePhase -= stateIndex;

                double movePart = GetStepMovePart();
                double moveStart = 1.0 - movePart;
                double movePhase = 0;
                if (statePhase > moveStart) {
                    movePhase = (statePhase - moveStart) / movePart;
                }

                // Draw step & flatten disks
                _stepDisks = _tree.GetStepDisks(levelIndex, stateIndex);
                for (int d = 0; d < _levelCount; ++d) {
                    bool isFlatten = d >= _stepDisks.Length;
                    var stepDisk = isFlatten
                        ? _stepDisks.Last() // allow flatten disks (on the last one)
                        : _stepDisks[d];
                    var levelDisk = _levelDisks[d];
                    //
                    Complex pos = GetDiskMovePosition(stepDisk, levelDisk, movePhase);
                    _image.Circle(FromComplex(pos), (float)levelDisk.radius)
                        .Add()
                        .FillStroke(levelDisk.color, Color.Empty);
                }
            }

            return _image;
        }
    }


    class Timeline
    {
        #region Cycles <-> Time (sec)
#if false
        static double Log2(double c) { return Math.Log2(c); }
        static double Pow2(double t) { return Math.Pow(2, t); }
        //
        static double _timeScale = 20.0; // time math units to seconds
        static double _cycleOrigin = 8;
        static double _timeOrigin = Log2(_cycleOrigin) * _timeScale; // in seconds
        public double CycleToTime(double c) {
            return _timeOrigin - Log2(_cycleOrigin - c) * _timeScale;
        }
        public double TimeToCycle(double t) {
            return _cycleOrigin - Pow2((_timeOrigin - t) / _timeScale);
        }
#else
        static double _timeScale = 1.0; // time math units to seconds
        static double _e = Math.Pow(2, .5);
        public double CycleToTime(double c) {
            //return (Math.Pow(_e, c) - 1.0) * _timeScale;
            return Math.Pow(_e, c) * _timeScale;
        }
        public double TimeToCycle(double t) {
            //return Math.Log(1.0 + t / _timeScale, _e);
            if (t == 0) t = 0.00000001;
            return Math.Log(t / _timeScale, _e);
        }
#endif
        #endregion

        public int[] CycleLevels = new[] {
            //1, 1, 1, 1,
            //2, 2,
            //5
            1,1,1,1,
            2,2, 3,3,
            4, 5, 6, 7,
        };
    }


#region Note Partials
    struct Partial {
        public Rational rational;
        public double harmonicity;

        public static Partial[] MakePartials(IHarmonicity harmonicity, Rational[] subgroup, int partialCount) {
            // subgroup
            Vectors.Matrix matrix = new Vectors.Matrix(subgroup, makeDiagonal: true);
            // partials
            var partials = new List<Partial>();
            for (int i = 1; i < 200; ++i) {
                var r = new Rational(i);
                if (matrix.FindCoordinates(r) == null) continue; // skip if out of subgroup
                partials.Add(new Partial {
                    rational = r,
                    harmonicity = Utils.GetHarmonicity(
                        harmonicity.GetDistance(r)
                    )
                });
                if (partials.Count == partialCount) break;
            }
            return partials.ToArray();
        }
    }
#endregion Note Partials

    static class Program {

        static void Test1_Tree() {
            int levelCount = 5;
            Tree tree = new Tree(levelCount);
            tree.Trace(levelCount - 1);
        }

        static void RecreateDirectory(string dir) {
            if (System.IO.Directory.Exists(dir)) {
                System.IO.Directory.Delete(dir, true);
                while (System.IO.Directory.Exists(dir)) {
                    System.Threading.Thread.Sleep(100);
                }
            }
            System.IO.Directory.CreateDirectory(dir);
        }

        static void Test2_TowerView()
        {
            TowerView towerView = new TowerView();

            string framesDir = "frames";
            RecreateDirectory(framesDir);
            
            int levelIndex = 5; // level index == disk count

            int frameCount = 1;
            for (int i = 0; i < frameCount; ++i)
            { 
                Image image = towerView.RenderLevelPhase(levelIndex, (double)i / frameCount);

                string pngPath = String.Format(framesDir + "\\frame_{0}_{1:000}.png", levelIndex, i);
                image.WritePng(pngPath, true);
                if (i == 0) Image.Show(pngPath);
            }
        }

        static void Test3_Timescale()
        {
            var t = new Timeline();

            for (int c = 0; c < t.CycleLevels.Length; ++c) {
                double t0 = t.CycleToTime(c);
                double t1 = t.CycleToTime(c + 1.0);
                Debug.WriteLine("Cycle {0}. Level {1}: {2:0.000} - {3:0.000} ({4:0.000}) sec",
                    c, t.CycleLevels[c], t0, t1, t1 - t0
                );
            }
        }

        static void AddNote(Wave.PartialTimeline timeline, double time, double duration, double gain, double cents, Partial[] partials, double balance) {
            //
            double ta = 0.01; // attack
            double tr = Math.Max(0.1, duration - ta); // release
            //
            foreach (Partial p in partials) {
                double c  = cents + p.rational.ToCents();
                double hz = Wave.Partials.CentsToHz(c);
                double level = Math.Pow(p.harmonicity, 7.0f);
                //
                timeline.AddPartial(
                    (int)(time * 1000),
                    hz,
                    (int)(ta * 1000),
                    (int)(tr * p.harmonicity * 1000),
                    (float)(gain * level / partials.Length),
                    (float)balance,
                    -4f
                );
            }
        }

        static void Split(double v, out int integral, out double fractional) {
            integral = (int)Math.Floor(v);
            fractional = v - integral;
        }

        static void Render()
        {
            bool makeFrames = false;
            bool makeWave = true;

            var levelCount = 10;

            Tree tree = new Tree(levelCount);

            Timeline cycles = new Timeline();
            int[] cycleLevels = cycles.CycleLevels;

            if (makeFrames)
            {
                TowerView view = new TowerView(levelCount, tree);

                string framesDir = "frames";
                RecreateDirectory(framesDir);

                for (int frameIndex = 0; ; ++frameIndex)
                {
                    // get cycle, level and phase
                    Split(
                        cycles.TimeToCycle(frameIndex / 30.0), // 30 fps
                        out int    cycleIndex, 
                        out double cyclePhase
                    );
                    if (cycleIndex >= cycleLevels.Length) break; // end
                    int levelIndex = cycleIndex >= 0 ? cycleLevels[cycleIndex] : -1;

                    Debug.WriteLine("Render frame {0}", frameIndex);

                    // render
                    string pngFile = String.Format(
                        //"frame_c{0:00}_l{1:00}_f{2:0000}.png", cycleIndex, levelIndex, frameIndex -- Pattern type 'glob' was selected but globbing is not supported by this libavformat build
                        "frame_f{0:0000}.png", frameIndex
                    );

                    Image image = view.RenderLevelPhase(levelIndex, cyclePhase);
                    image.WritePng(
                        System.IO.Path.Join(framesDir, pngFile), 
                        true
                    );
                }
            }

            if (makeWave)
            {
                string waveFile = "towerOfHanoi1.wav";

                var waveFormat = new Wave.WaveFormat { bytesPerSample = 2, sampleRate = 44100, channels = 2 };
                var waveTimeline = new Wave.PartialTimeline(waveFormat);
                
                // define a rational note for each disk
                Rational[] diskRationals = new Rational[levelCount];
                for (int i = 0; i < levelCount; ++i) {
                    diskRationals[i] = Rational.Prime(i);
                }

                // define partials per level
                Partial[][] levelPartials = new Partial[levelCount][];
                IHarmonicity harmonicity = Utils.CreateHarmonicity("Barlow", normalize: true);
                for (int levelIndex = 1; levelIndex < levelCount; ++levelIndex) {
                    Rational[] subgroup = Rational.Primes(primeCount: levelIndex);
                    levelPartials[levelIndex] = Partial.MakePartials(harmonicity, subgroup, 20);
                }

                // fill wave timeline
                for (int cycleIndex = 0; cycleIndex < cycleLevels.Length; ++cycleIndex) {
                    int levelIndex = cycleLevels[cycleIndex];
                    int stateCount = tree.GetStateCount(levelIndex);

                    Debug.WriteLine("Cycle {0}. Level {1}", cycleIndex, levelIndex);

                    for (int stateIndex = 0; stateIndex < (stateCount - 1); ++stateIndex) { // last state has no step
                        // get rational to play note
                        int rodIndex;
                        int[] disks = tree.GetStepDestinationRod(levelIndex, stateIndex, out rodIndex);
                        int disk = disks.Last(); // moved disk
                        Rational r = diskRationals[disk];
                        //
                        int[] skipped = Enumerable.Range(0, disk).Except(disks).ToArray();
                        if (skipped.Any()) {
                            // make inversion from sum of skipped intervals
                            Rational sum = Rational.One;
                            foreach (int s in skipped) sum *= diskRationals[s];
                            r = sum / r;
                            // add some octaves to make closer to "rod height" interval
                            Rational limit = diskRationals[disks.Length - 1];
                            while (r < limit) r *= 2;
                        }

                        Debug.WriteLine("  State {0}/{1}. Move disk {2}; skipped {3} -> play {4}",
                            stateIndex+1, stateCount, 
                            diskRationals[disk],
                            skipped.Length == 0 
                                ? "none" 
                                : String.Join(",", skipped.Select(s => diskRationals[s].ToString())),
                            r);

                        // gain
                        double diskNormal = (double)disk / levelIndex; // [0..1)
                        double gain = Math.Pow(7.0, -diskNormal); // always 1.0 for disk 0
                        // time & duration
                        int diskLevel = disk + 1;
                        double p0 = (double)(stateIndex + 1) / stateCount;
                        double p1 = p0 + 1.0 / (1 << diskLevel);
                        double t0 = cycles.CycleToTime(cycleIndex + p0);
                        double t1 = cycles.CycleToTime(cycleIndex + p1);
                        double duration = t1 - t0;
                        duration = Math.Max(1.2 / (1 << disk), duration); // set min length here. fine tuning for first cycles
                        // balance
                        double rodAngle = TowerView.GetLevelPhaseRodAngle(rodIndex, p0);
                        double balance = Math.Cos(rodAngle); // -1 .. 1
                        //
                        AddNote(
                            waveTimeline, 
                            t0, duration, gain * 1.62, 
                            r.ToCents() - (1200 * 3), 
                            levelPartials[levelIndex],
                            balance * 0.62
                        );
                    }
                }

                // export wave timeline to file
                Debug.WriteLine("Writing {0}", (object)waveFile);
                using (var w = new Wave.WaveWriter(waveFormat, waveFile)) {
                    byte[] buffer = new byte[waveFormat.bytesPerSample * waveFormat.sampleRate]; // for 1 sec
                    while (waveTimeline.Fill(buffer)) {
                        w.Write(buffer);
                    }
                }

            }

            // To join wave and frames
            //  https://trac.ffmpeg.org/wiki/Slideshow
            //  C:\Users\Massalogin\Downloads\Programs\ffmpeg-win-2.2.2\ffmpeg.exe -r 30 -i frames/frame_f%04d.png -i towerOfHanoi1.wav -r 30 -y out3.mp4
        }

        static int Main() {
            //Test1_Tree();
            //Test2_TowerView();
            //Test3_Timescale();

            Render();

            return 0;
        }
    }
}
