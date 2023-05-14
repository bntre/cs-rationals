using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Linq;
using System.Diagnostics;

using Torec.Drawing;
using Color = System.Drawing.Color;

namespace Rationals.TriTree
{
    //--------------------------------------------------
    // Single sector (of three) Grid

    //               .              /
    //          .     (221)     /
    //      .     (211)     02
    //          .       01
    //      .       00      12
    //          .       11
    //      .     (101)     22
    //          .       21
    //            (102)
    //               |
    //               |

    class GridNode {
        public int code;        //  AA (         2-digit code)
        public int[] neighbors; // SAA (sector + 2-digit code)
        public static GridNode FromString(string s) {
            string[] cs = s.Split(":,".ToCharArray());
            return new GridNode {
                code = int.Parse(cs[0]),
                neighbors = cs.Skip(1).Select(int.Parse).ToArray()
            };
        }
        public override string ToString() {
            return String.Format("{0}:{1}", 
                code, 
                String.Join(",", neighbors.Select(n => n.ToString()))
            );
        }

        public static int[] SplitCode(int code) { // split code to integers
            return new int[] {
                code / 100,         // sector {0, 1, 2}
                code / 10 % 10,     // coordinates
                code      % 10      //    from the code
            };
        }
    }

    class Grid {
        public GridNode[,] nodes;

        public void Trace() {
            foreach (GridNode n in nodes) {
                if (n != null) {
                    Debug.WriteLine("GridNode {0}", n);
                }
            }
        }

        public GridNode GetNode(int code) {
            int[] i = GridNode.SplitCode(code);
            return nodes[i[1], i[2]];
        }

        // Statics
        private static Grid MakeGrid() {
            Func<string, GridNode> N = GridNode.FromString;
            var grid = new Grid();
            grid.nodes = new GridNode[,] {
                { N("00:01,11"), N("01:02,12,11,00,211,221"), N("02:12,01,221") },
                { null,          N("11:22,21,101,00,01,12"),  N("12:22,11,01,02") },
                { null,          N("21:102,101,11,22"),       N("22:21,11,12") }
            };
            //grid.Trace();
            return grid;
        }
        public static readonly Grid Instance = MakeGrid();

        // Unique edge indices
        private static int GetEdgeId(GridNode node, int dir) {
            int code0 = node.code;
            int code1 = node.neighbors[dir];
            if (code1 >= 200) {
                code0 = (code0 + 100) % 300;
                code1 = (code1 + 100) % 300;
            }
            int id = code0 < code1
                ? (code0 * 1000) + code1
                : (code1 * 1000) + code0;
            return id;  // AABBB
        }
        private static readonly int[] _edgeIds = new[] {
            00001, 01002, 11012,  01012,  01011, 02012, 12022,  // first half of sector
            00011, 11022, 21101,  11021,  11101, 21022, 21102   // second
        };
        public static int GetEdgeIndex(GridNode node, int dir) {
            int edgeId = GetEdgeId(node, dir);
            int index = Array.IndexOf(_edgeIds, edgeId);
            Debug.Assert(index != -1);
            return index;
        }
    }

    //--------------------------------------------------
    // Tree

    class TreeBranch {
        public GridNode node;
        public int parentDir = -1;

        public bool IsRoot() { return parentDir == -1; }

        public int? GetParentCode() {
            if (IsRoot()) return null;
            return node.neighbors[parentDir];
        }

        public static TreeBranch MakeRoot() {
            return new TreeBranch {
                node = Grid.Instance.GetNode(0),
                parentDir = -1
            };
        }

        public static TreeBranch FromNeighbor(GridNode parent, int toNeighbor) {
            // normalize
            int parentCode = parent.code;
            int code       = toNeighbor;
            while (code >= 100) {
                code       -= 100;
                parentCode += 200;
            }
            parentCode %= 300;
            //
            GridNode node = Grid.Instance.GetNode(code);
            int parentDir = Array.IndexOf(node.neighbors, parentCode);
            Debug.Assert(parentDir != -1, "Parent not found");
            return new TreeBranch { node = node, parentDir = parentDir };
        }
        
        public static TreeBranch FromString(string s) {
            string[] cs = s.Split("-→".ToCharArray());
            var b = new TreeBranch();
            int code = int.Parse(cs[1]);
            Debug.Assert(code < 100, "Only parent may be out of main sector");
            b.node = Grid.Instance.GetNode(code);
            if (cs[0] != "") {
                int parentCode = int.Parse(cs[0]);
                b.parentDir = Array.IndexOf(b.node.neighbors, parentCode);
                Debug.Assert(b.parentDir != -1, "Parent not found");
            }
            return b;
        }

        public override string ToString() {
            return String.Format("{0}→{1}", GetParentCode(), node.code);
        }
    }

    [System.Diagnostics.DebuggerDisplay("{Format()}")]
    class Tree {
        public TreeBranch[] branches;

        public static Tree MakeRoot() {
            return new Tree {
                branches = new[] {
                    TreeBranch.MakeRoot()
                }
            };
        }

        public bool IsFull() {
            return GetLevel() == 6;
        }
        public int GetLevel() { // 0..6 - visible branch count (we have also one invisible root branch "-00")
            return branches.Length - 1;
        }

        public bool HasNode(int code) {
            var node = Grid.Instance.GetNode(code);
            return branches.Any(b => b.node == node);
        }


        static public Tree Parse(string s) {
            return new Tree {
                branches = s.Split(';')
                    .Select(b => TreeBranch.FromString(b))
                    .ToArray()
            };
        }

        public string Format() {
            return String.Format(
                //"{0} id:{1:X}",
                "{0}",
                String.Join(';', branches.Select(b => b.ToString())),
                GetId()
            );
        }

        // Tree Id - unique by view        
        public int GetId() {
            int id  = 0;
            int id0 = 0;
            int id1 = 0;
            foreach (TreeBranch b in branches) {
                if (b.IsRoot()) continue;
                int index = Grid.GetEdgeIndex(b.node, b.parentDir);
#if false
                id |= 1 << index;
            }
#else
                // this way we skip pi/3 rotation duplicates
                if (index < 7) {
                    id0 |= 1 << index;
                } else {
                    id1 |= 1 << (index - 7);
                }
            }
            id = id0 <= id1
                ? (id0 << 7) + id1
                : (id1 << 7) + id0;
#endif
            return id;
        }
    }

    static class TreeDrawer
    {
        public static bool Simple = false;
        public static bool Gray = false;
        public static Color SolidColor = Color.Empty;

        static readonly double Sqrt32 = Math.Pow(3, 0.5) / 2;

        static double[,] MakeSectorAngles() {
            var r = new double[3, 2];
            for (int s = 0; s < 3; ++s) {
                double a = s * Math.PI * 2/3;
                r[s, 0] = Math.Cos(a);
                r[s, 1] = Math.Sin(a);
            }
            return r;
        }
        static readonly double[,] _sectorAngles = MakeSectorAngles();
        static double[] RotateToSector(double x, double y, int sector) {
            double cos = _sectorAngles[sector, 0];
            double sin = _sectorAngles[sector, 1];
            return new[] {
                x*cos - y*sin,
                x*sin + y*cos
            };
        }

        static Point GetNodePoint(int code, int sector = 0) {
            code += sector * 100;
            int[] i = GridNode.SplitCode(code); // split code to integers
            double x = i[2] * Sqrt32;
            double y = i[1] - i[2] * 0.5;
            int    s = i[0] % 3;
            double[] xy = RotateToSector(x, y, s);
            return new Point((float)xy[0], (float)xy[1]);
        }

        static double Lerp(double k, double a, double b) { return (1-k)*a + k*b; }
        static Color MakeGray(double k) { int b = (int)Lerp(k, 0, 0xAA); return Color.FromArgb(b, b, b); }
        static Color GetColor(int i) {
            if (!SolidColor.IsEmpty) {
                return SolidColor;
            } else {
                double k = i / 6.0; // 0..1
                if (Gray) {
                    return MakeGray(k);
                } else {
                    return Color.FromArgb(
                        (int)Lerp(k, 0xEE, 0), // R
                        (int)Lerp(k, 0, 0xDD), // G
                        (int)0                 // B
                    );
                }
            }
        }
        static float GetWidth(int i) {
            //return (float)Math.Pow(0.618, i * 0.25) * 0.5f;
            return 0.22f;
        }

        static void DrawBranch(Image image, Point origin, TreeBranch b, int sector, int index) {
            if (b.IsRoot()) return;
            Point p0 = origin + GetNodePoint(b.GetParentCode().Value, sector);
            Point p1 = origin + GetNodePoint(b.node.code,             sector);
            if (Simple) {
                image.Line(new[] { p0, p1 })
                    .Add().FillStroke(Color.Empty, GetColor(index), 0.3f);
            } else {
                image.Circle(p1, GetWidth(index) / 2)
                    .Add(index: 1).FillStroke(GetColor(index), Color.Empty); // Add above the background rect
                image.Line(p0, p1, GetWidth(index-1), GetWidth(index))
                    .Add(index: 1).FillStroke(GetColor(index), Color.Empty);
            }
        }

        public static void Draw(Image image, Tree tree, Point origin = default(Point)) {
            int i = 0;
            foreach (TreeBranch b in tree.branches) {
                for (int s = 0; s < 3; ++s) {
                    DrawBranch(image, origin, b, s, i);
                }
                i += 1;
            }
        }

        static int _fileCounter = 0;
        public static void Draw(Tree tree, bool svg = false, bool show = false) {
            var viewport = new Viewport(200, 200, -3,3, -3,3, false);
            var image = new Image(viewport);
            
            Draw(image, tree);
            
            string path = String.Format("TriTree_{0:X}", tree.GetId());
            path = String.Format("{0}_{1}", ++_fileCounter, path);
            path = @"output\" + path;
            if (svg) {
                path += ".svg";
                image.WriteSvg(path);
            } else {
                path += ".png";
                image.WritePng(path);
            }

            if (show) {
                Image.Show(path);
            }
        }
    }

    //--------------------------------------------------
    // Super tree (tree of trees)

    class SuperTreeNode {
        public SuperTreeNode parent = null;
        public List<SuperTreeNode> children = new List<SuperTreeNode>();
        //
        public Tree tree;
        public bool toLeaf = false; // this super tree branch leads to a leaf
        public bool skipped = false; // skipped e.g. as a duplicate
    }

    class SuperTree
    {
        private SuperTreeNode _root = null;
        private HashSet<int> _treeIds = new HashSet<int>();

        //---------------------------------------------------------
        // Grow

        private SuperTreeNode AddNode(Tree tree, SuperTreeNode parent)
        {
            var node = new SuperTreeNode();
            node.tree = tree;
            if (parent != null) {
                node.parent = parent;
                node.parent.children.Add(node);
            }
            return node;
        }

        private static IEnumerable<Tree> GrowTree(Tree tree) {
            if (tree.IsFull()) yield break;
            foreach (TreeBranch b in tree.branches.Reverse()) { // start from new branches
                int len = b.node.neighbors.Length;
                int[] dirs = b.IsRoot()
                    ? Enumerable.Range(0, len  ).ToArray()
                    : Enumerable.Range(1, len-1).Select(d => (b.parentDir + d) % len).ToArray();
                foreach (int d in dirs) {
                    int toNeighbor = b.node.neighbors[d];
                    if (!tree.HasNode(toNeighbor)) {
                        TreeBranch newBranch = TreeBranch.FromNeighbor(b.node, toNeighbor);
                        yield return new Tree {
                            branches = tree.branches.Append(newBranch).ToArray()
                        };
                    }
                }
            }
        }

        List<int> _superLeaves = new List<int>(); //!!! is it just to count them?

        private void MarkToLeaf(SuperTreeNode node) {
            if (node != null && !node.toLeaf) {
                node.toLeaf = true;
                MarkToLeaf(node.parent);
            }
        }

        private void GrowNode(SuperTreeNode node) {
            bool grown = false;
            foreach (Tree tree in GrowTree(node.tree)) {
                grown = true;
                bool unique = _treeIds.Add(tree.GetId());
                SuperTreeNode newNode = AddNode(tree, node);
                if (unique) {
                    Debug.WriteLine("New Tree: {0}", (object)tree.Format());
                    if (tree.IsFull()) {
                        _superLeaves.Add(tree.GetId());
                        MarkToLeaf(newNode);
                    }
                    GrowNode(newNode);
                } else {
                    Debug.WriteLine("Skipped Tree: {0}", (object)tree.Format());
                    newNode.skipped = true;
                }
            }
            if (!grown) {
                Debug.WriteLine("Can't grow Tree: {0}", (object)node.tree.Format());
            }
        }

        public void GrowSuperTree() {
            Tree treeRoot = Tree.MakeRoot();
            _root = AddNode(treeRoot, null);
            GrowNode(_root);

            Debug.WriteLine("Super leaves: {0}", _superLeaves.Count);
        }

        //---------------------------------------------------------
        // Draw Super Tree (for debug?)

        private float DrawSuperTree(Image image, Point origin, SuperTreeNode node) {
            //
            TreeDrawer.Gray = node.skipped;
            TreeDrawer.Draw(image, node.tree, origin);
            //
            origin.X += 4.8f;
            float shiftY = 0;
            foreach (SuperTreeNode child in node.children) {
                shiftY += DrawSuperTree(image, origin + new Point(0, shiftY), child);
                shiftY += child != node.children.Last() ? 4.8f : 0.5f;
            }
            return shiftY;
        }

        public void DrawSuperTree()
        {
            Viewport viewport = new Viewport(260,260*218, 0,30, 0,30*218, false);
            Image image = new Image(viewport);
            image.RectangleFull(Color.White).Add();

            //TreeDrawer.Simple = true;

            Point origin = new Point(-2, 3);
            DrawSuperTree(image, origin, _root);

            string path = "TriTree_SuperTree.png";
            image.WritePng(path);
            //Image.Show(path);
        }

        //---------------------------------------------------------
        // Smooth animation

        struct SmoothFrame {
            public SuperTreeNode node;
            public int soundBits; // 1 - start, 2 - stop, 0 - keep
            public int length;
        }

        private static int Fib(int i) {
            if (i <= 1) return 1;
            return Fib(i - 1) + Fib(i - 2);
        }

        private static IEnumerable<SmoothFrame> EnumerateFrames(SuperTreeNode node) {
            int level = node.tree.GetLevel();
            //int length = Fib(6 - level);
            //int length = new[] { 3,2,2,1,1,1,1 }[level];
            int length = 1;
            //
            var children = node.children.Where(c => c.toLeaf && !c.skipped).ToArray();
            if (children.Any()) {
                yield return new SmoothFrame { node = node, soundBits = 1, length = length };
                foreach (var c in children) {
                    foreach (var n in EnumerateFrames(c)) {
                        yield return n;
                    }
                    if (c != children.Last() && level != 5) { // don't yield between-leaf branches
                        yield return new SmoothFrame { node = node, soundBits = 0, length = length };
                    }
                }
                yield return new SmoothFrame { node = node, soundBits = 2, length = length };
            } else {
                yield return new SmoothFrame { node = node, soundBits = 1|2, length = length };
            }
        }

        #region Note Partials // like in TowerOfHanoi.cs
        struct Partial {
            public Rational rational;
            public double harmonicity;
        }
        static Partial[] MakePartials(IHarmonicity harmonicity, Rational[] subgroup, int partialCount) {
            // subgroup
            Vectors.Matrix matrix = new Vectors.Matrix(subgroup, makeDiagonal: true);
            // partials
            var partials = new List<Partial>();
            for (int i = 1; i < 200; ++i) {
                var r = new Rational(i);
                if (matrix.FindCoordinates(r) == null) continue; // skip if out of subgroup
                partials.Add(new Partial {
                    rational = r,
                    harmonicity = harmonicity.GetDistance(r),
                });
                if (partials.Count == partialCount) break;
            }
            return partials.ToArray();
        }
        static Partial[] NotePartials = MakePartials(
            HarmonicityUtils.CreateHarmonicity("Barlow", normalize: true),
            Rational.Primes(primeCount: 3),
            20
        );
        #endregion Note Partials

        public static void AddNote(Wave.PartialTimeline timeline, double startSec, double endSec, Rational note, double gain = 1.0, double balance = 0) {
            double duration = endSec - startSec;
            
            duration *= 2.0; // lengthen all notes

            double ta = 0.01; // attack
            double tr = Math.Max(0.1, duration - ta); // release
            double cents = note.ToCents();

            foreach (Partial p in NotePartials) {
                double c  = cents + p.rational.ToCents();
                double hz = Wave.Partials.CentsToHz(c);
                double level = Math.Pow(p.harmonicity, 10.0f); // less is more rude. multiply the gain accordingly!!!
                //
                timeline.AddPartial(
                    (int)(startSec * 1000),
                    hz,
                    (int)(ta * 1000),
                    (int)(tr * p.harmonicity * 1000),
                    (float)(gain * level / NotePartials.Length),
                    (float)balance,
                    -1f
                );
            }
        }

        static int GetBranchGlobalIndex(TreeBranch b) {
            int i = 0;
            foreach (GridNode n in Grid.Instance.nodes) {
                if (n == null) continue;
                if (n != b.node) {
                    i += n.neighbors.Length;
                } else {
                    i += b.parentDir;
                    return i;
                }
            }
            Debug.Assert(false, "Node not found");
            return -1;
        }

        //                                                             ↙   ↖   ↑   ↗  ↘
        static Rational[] AngleRationals = Rational.ParseRationals("1, 6/5, 4/3, 2, 3/2, 5/4", separator: ",");
        static Rational GetTreeNote(Tree tree) {
            var nodeRationals = new Dictionary<int, Rational>(); // grid node code => Rational
            var chord = new List<Rational>();
            for (int i = 0; i < tree.branches.Length; ++i) { // 0..6
                TreeBranch b = tree.branches[i];
                if (b.IsRoot()) {
                    Debug.Assert(b.node.code == 00);
                    nodeRationals[b.node.code] = Rational.Two.Power(-6);
                } else {
                    //          .  b.node
                    //          ↓  b.parentDir
                    //          ↑  b2.parentDir = dir1
                    //          .  b2.node
                    //        ↙                  dir0
                    //
                    //   o         parent of parent
                    TreeBranch b2 = TreeBranch.FromNeighbor(b.node, b.node.neighbors[b.parentDir]);
                    if (i <= 4) { // harmonic
                        int dir0 = tree.branches.First(bb => bb.node == b2.node).parentDir;
                        int dir1 = b2.parentDir;
                        int angle;
                        if (dir0 == -1) {
                            angle = dir1 + 3;
                        } else {
                            angle = dir1 - dir0;
                        }
                        Rational r = AngleRationals[(angle + 6) % 6];
                        Rational n = nodeRationals[b2.node.code] * r;
                        nodeRationals[b.node.code] = n;
                        // add the note to current chord
                        while (n >= Rational.Two) n /= 2;
                        while (n <  Rational.One) n *= 2;
                        chord.Add(n);
                    } else { // melodic
                        if (i == 5) {
                            chord = chord.Distinct().ToList();
                            chord.Sort();
                        }
                        int c = chord.Count;
                        int j = GetBranchGlobalIndex(b); // [0..28)
                        j = j % 8;
                        if (i == 6) j += 3;
                        int jm = Utils.Mod(j, c);
                        int jd = Utils.Div(j, c);
                        Rational n = chord[jm] * Rational.Two.Power(jd - 3);
                        nodeRationals[b.node.code] = n;
                    }
                }
            }
            int lastCode = tree.branches.Last().node.code;
            return nodeRationals[lastCode];
        }

        public void MakeTreesAnimation(bool makeFrames = false, bool makeSound = false, bool makeVideo = false)
        {
            int frameRate = 5; // Hz

            // Image
            Viewport viewport = new Viewport(1500,1500, -2.5f,2.5f, -2.5f,2.5f, false);
            TreeDrawer.Simple = false;

            // Sound
            var waveFormat   = new Wave.WaveFormat { bytesPerSample = 2, sampleRate = 44100, channels = 2 };
            var waveTimeline = new Wave.PartialTimeline(waveFormat);
            string waveFile  = @"sound.wav";
            int[] levelNoteStarts = new int[7]; Array.Fill(levelNoteStarts, -1);
            Rational[] levelNotes = new Rational[7]; // to end unfinished notes on debug
            double gain = 200.0;

            int frameCounter = 0;
            int fileCounter  = 0;
            int timeCounter  = 0;
            foreach (SmoothFrame frame in EnumerateFrames(_root))
            {
                //Debug.WriteLine("Frame {0}. Tree: {1}. Sound: {2}", frameCounter, frame.node.tree.Format(), frame.soundBits);

                if (makeFrames) {
                    Image image = new Image(viewport);
                    image.RectangleFull(Color.White).Add();
                    TreeDrawer.SolidColor = frame.node.tree.IsFull()
                        ? Color.Black
                        : ColorUtils.MakeColor(0xFF666666);
                    TreeDrawer.Draw(image, frame.node.tree);
                    //
                    string path0 = "";
                    for (int t = 0; t < frame.length; ++t) {
                        // 
                        string pattern = @"anim\TriTree_frame_{0:00000}.png";
                        string path = String.Format(pattern, ++fileCounter);
                        if (t == 0) {
                            image.WritePng(path);
                            path0 = path;
                        } else {
                            System.IO.File.Copy(path0, path, overwrite: true); // just copy the same file
                        }

                        Debug.WriteLine("Image saved: " + path);
                    }
                }

                if (makeSound) {
                    int level = frame.node.tree.GetLevel();
                    Rational note = GetTreeNote(frame.node.tree);
                    if (frame.soundBits != 0) {
                        Debug.WriteLine("{0:000} {1,-40} {2}{3}){4} {5:0.0}o",
                            frameCounter,
                            frame.node.tree.Format(),
                            new String('\t', level),
                            frame.soundBits,
                            note.FormatFraction(),
                            (note.ToCents() / 1200));
                    }
                    if ((frame.soundBits & 1) != 0) { // start note
                        Debug.Assert(levelNoteStarts[level] == -1);
                        levelNoteStarts[level] = timeCounter;
                        levelNotes     [level] = note;
                    }
                    if ((frame.soundBits & 2) != 0) { // end note
                        Debug.Assert(levelNoteStarts[level] != -1);
                        double startSec = (double)(levelNoteStarts[level])     / frameRate;
                        double endSec   = (double)(timeCounter + frame.length) / frameRate;
                        double balance  = (level - 3) * 0.3;
                        AddNote(waveTimeline, startSec, endSec, note, gain, balance);
                        //Debug.WriteLine("+++ AddNote s:{0:0.00}, d:{1:0.00} sec. Note {2} ({3:0.0})", startSec, endSec-startSec, note, note.ToCents());
                        levelNoteStarts[level] = -1;
                    }
                }

                timeCounter += frame.length;

                frameCounter += 1;
                //if (frameCounter > 200) break; //!!! temporal limit
            }

            if (makeSound) {
                for (int level = 0; level < 7; ++level) {
                    if (levelNoteStarts[level] != -1) {
                        double startSec = (double)levelNoteStarts[level] / frameRate;
                        double endSec   = (double)timeCounter            / frameRate;
                        AddNote(waveTimeline, startSec, endSec, levelNotes[level], gain: gain);
                    }
                }

                //
                Debug.WriteLine("Save the Wave: " + waveFile);
                using (var w = new Wave.WaveWriter(waveFormat, waveFile)) {
                    byte[] buffer = new byte[waveFormat.bytesPerSample * waveFormat.sampleRate]; // for 1 sec buffer
                    while (waveTimeline.Fill(buffer)) {
                        w.Write(buffer);
                    }
                }
            }

            if (makeVideo) {
                // https://trac.ffmpeg.org/wiki/Slideshow
                // ffmpeg -framerate 10 -i TriTree_%05d.png smooth.mp4
                // ffmpeg -framerate 10 -i TriTree_%05d.png -pix_fmt yuv420p smooth.mp4
                // ffmpeg -framerate 30 -i TriTree_%05d.png -pix_fmt yuv420p smooth.mp4
                // ffmpeg -framerate 10 -i TriTree_frame_%05d.png -pix_fmt yuv420p animation.mp4
                // ffmpeg -framerate 5 -i TriTree_frame_%05d.png -pix_fmt yuv420p animation.mp4
                // ffmpeg -r 5 -i anim\\TriTree_frame_%05d.png -i sound_cut.wav -r 5 -pix_fmt yuv420p -y TriTree.mp4
                if (makeFrames && !makeSound) {
                    Program.RunProcess("ffmpeg", String.Format(
                        "-framerate {0} -i anim\\TriTree_frame_%05d.png -pix_fmt yuv420p -y animation.mp4",
                        frameRate
                    ));
                }
                if (makeSound) {
                    Program.RunProcess("ffmpeg", String.Format(
                        "-r {0} -i anim\\TriTree_frame_%05d.png -i {1} -r {0} -pix_fmt yuv420p -y animation.mp4",
                        frameRate, waveFile
                    ));
                }
            }
        }

        //---------------------------------------------------------
        // Leaves

        private static IEnumerable<SuperTreeNode> EnumerateLeaves(SuperTreeNode node) {
            if (node.tree.IsFull()) {
                yield return node;
            } else {
                foreach (var c in node.children) {
                    if (!c.toLeaf) continue;
                    foreach (var n in EnumerateLeaves(c)) {
                        yield return n;
                    }
                }
            }
        }

        public void DrawLeaves()
        {
            Tree[] leaves = EnumerateLeaves(_root)
                .Select(n => n.tree)
                .ToArray();
            // 372 = 2^2 * 3 * 31 = 12 * 31
            // 373 prime
            // 374 = 2 * 11 * 17 = 22 * 17
            // 16*23-1 = 367 (on old poster)

            //Array.Sort(leaves.Select(t => t.GetId()).ToArray(), leaves); // sort by tree id - kind of shuffle
            float cellSize = 5.5f;
            Viewport viewport = new Viewport(17*100, 22*100, 0, 17*cellSize, 0, 22*cellSize, false);
            Image image = new Image(viewport);
            image.RectangleFull(Color.White).Add();
            int i = 1; // skip the corner slot
            foreach (Tree tree in leaves) {
                Point origin = new Point(i%17 + 0.5f, i/17 + 0.5f) * cellSize;
                TreeDrawer.Draw(image, tree, origin);
                i += 1;
                //if (i == 372/2-8 || i == 372/2+9) i += 1; // skip center slots
            }

            string path = "TriTree_Leaves.svg";
            image.WriteSvg(path);
            Image.Show(path);
        }
    }


    //--------------------------------------------------

    static class Program
    {
        public static int RunProcess(string fileName, string arguments) {
            var process = new Process();
            process.StartInfo = new ProcessStartInfo(fileName, arguments);
            process.Start();
            process.WaitForExit();
            int exitCode = process.ExitCode;
            process.Close();
            return exitCode;
        }

        static void Test1_MakeTree() {
            // Make a Tree
            var tree = Tree.Parse("-00;00-01;00-11;11-12;12-02;102-21");
            Debug.WriteLine("Tree: {0}", tree.Format());
            TreeDrawer.Draw(tree, show: true);
        }

        static void Test2_MakeSound() {
            var waveFormat = new Wave.WaveFormat { bytesPerSample = 2, sampleRate = 44100, channels = 2 };
            var waveTimeline = new Wave.PartialTimeline(waveFormat);
            string waveFile = @"sound_test.wav";

            for (int i = 0; i < 8; ++i) {
                double startSec  = 0.5 * i;
                double endSec    = startSec + 1;
                Rational note = Rational.Two.Power(i - 6);
                SuperTree.AddNote(waveTimeline, startSec, endSec, note, gain: 100.0);
            }

            using (var w = new Wave.WaveWriter(waveFormat, waveFile)) {
                byte[] buffer = new byte[waveFormat.bytesPerSample * waveFormat.sampleRate]; // for 1 sec buffer
                while (waveTimeline.Fill(buffer)) {
                    w.Write(buffer);
                }
            }
        }

        static int Main() {
            //Test1_MakeTree(); return 0;
            //Test2_MakeSound(); return 0;

            var superTree = new SuperTree();
            superTree.GrowSuperTree();

            //superTree.DrawSuperTree();
            //superTree.DrawLeaves();
            superTree.MakeTreesAnimation(makeFrames: true, makeSound: false, makeVideo: false);

            return 0;
        }
    }
}

