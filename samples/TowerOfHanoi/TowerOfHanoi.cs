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

        public struct StepDisk {
            public int rod0;
            public int rod1;
            public int pos0;
            public int pos1;
            public bool move; // if 'move' then 'rod1' and 'pos1' are set
        }

        public StepDisk[] GetStepDisks(int levelIndex, int stepIndex) {
            var ds = new StepDisk[levelIndex]; // levelIndex == disk count
            //
            int id0 = GetId(levelIndex, stepIndex);
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
                var step = GetItem(stepId).step;
                int disk = state0.rods[step.rod0].Last();
                ds[disk].move = true;
                ds[disk].rod1 = step.rod1;
                ds[disk].pos1 = state0.rods[step.rod1].Length;
            }
            //
            return ds;
        }

    }

    public class Tower
    {
        public Tower()
        {
            // build tree
            _tree = new Tree(_levelCount);

            // prepare graphics
            const int size = 700; // image size
            const float r = _imageUserRadius;
            _viewport = new Viewport(size, size, -r, r, -r, r);
        }

        // set once
        private int _levelCount = 10; // level 0 - empty; level 1 - moving disk 0;..
        private Tree _tree;
        private const float _imageUserRadius = 2.5f;
        private Viewport _viewport;

        // set per level
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

        private void ResetLevelDisks(int levelIndex)
        {
            // levelIndex == level disk count

            _levelDisks = new LevelDisk[_levelCount];

            double fullHeight = 0;

            const double radiusFactor = 0.73;

            for (int i = 0; i < _levelCount; ++i) {
                var d = _levelDisks[i];
                bool isFlatten = i >= levelIndex;

                d.radius = Math.Pow(radiusFactor, i + 1);
                d.height = Math.Pow(radiusFactor, i) - d.radius;

                if (isFlatten) d.height *= 0.2;

                fullHeight += d.height;

                _levelDisks[i] = d;
            }

            double currentY = 0; // up to fullHeight

            for (int i = 0; i < _levelCount; ++i) {
                var d = _levelDisks[i];
                bool isFlatten = i >= levelIndex;

                currentY += d.height;
                double c = currentY / fullHeight; // (0..1]

                //c = Math.Pow(c, 0.4);
                d.color = MakeColor(
                    0x010101
                    //(isFlatten ? 0x010001 : 0x010101)
                    * (int)(0xFF * c) + 0xFF000000
                );

                _levelDisks[i] = d;
            }
        }


        private double GetStepMovePart() {
            //return 0.4;
            return 1.0;
        }

        private struct FrameRod {
            public Complex pos; // normal
            public double height;
        }

        private void ResetFrameRods(double levelPhase) {
            _frameRods = new FrameRod[3];
            for (int i = 0; i < 3; ++i) {
                double p = (- i - levelPhase) / 3.0 + 0.5;
                _frameRods[i].pos = Complex.FromPolarCoordinates(1.0, 2 * Math.PI * p);
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

        private void RenderLevelPhase(int levelIndex, double levelPhase)
        {
            Debug.Assert(0 <= levelPhase && levelPhase < 1);

            // Get step index and phase
            double stepPhase = levelPhase * (1 << levelIndex);
            int stepIndex = (int)Math.Floor(stepPhase);
            stepPhase -= stepIndex;

            double movePart = GetStepMovePart();
            double moveStart = 1.0 - movePart;
            double movePhase = 0;
            if (stepPhase > moveStart) {
                movePhase = (stepPhase - moveStart) / movePart;
            }

            ResetFrameRods(levelPhase);

            // Draw rod guides
            foreach (var r in _frameRods) {
                _image.Line(new[] {
                        FromComplex(Complex.Zero),
                        FromComplex(r.pos * _imageUserRadius * 2)
                    })
                    .Add()
                    .FillStroke(Color.Empty, Color.LightGray, 0.01f);
            }

            // Draw step & flatten disks
            _stepDisks = _tree.GetStepDisks(levelIndex, stepIndex);
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

        public void Render()
        {
            string framesDir = "frames";
            if (System.IO.Directory.Exists(framesDir)) {
                System.IO.Directory.Delete(framesDir, true);
                System.Threading.Thread.Sleep(100);
            }
            System.IO.Directory.CreateDirectory(framesDir);

            int levelIndex = 5; // level index == disk count

            ResetLevelDisks(levelIndex);

            int frameCount = 100;
            for (int i = 0; i < frameCount; ++i)
            { 
                _image = new Image(_viewport);

                float r = _imageUserRadius;
                _image.Rectangle(new[] { new Point(-r, r), new Point(r, -r) })
                    .Add()
                    //.FillStroke(Color.Empty, Color.Black, 0.01f);
                    .FillStroke(MakeColor(0xFFFFEFFF), Color.Empty);

                RenderLevelPhase(levelIndex, (double)i / frameCount);

                string pngPath = String.Format(framesDir + "\\frame_{0}_{1:000}.png", levelIndex, i);
                _image.WritePng(pngPath, true);
                if (i == 0) Image.Show(pngPath);
            }


        }
    }


    static class Program {

        static void Test1_Tree() {
            int levelCount = 5;
            Tree tree = new Tree(levelCount);
            tree.Trace(levelCount - 1);
        }

        static void Test2_Tower() {
            Tower tower = new Tower();
            tower.Render();
        }

        static int Main() {
            //Test1_Tree();
            Test2_Tower();

            return 0;
        }
    }
}
