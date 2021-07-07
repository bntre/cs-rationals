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
                    Console.WriteLine("GridNode {0}", n);
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
            return branches.Length == 7;
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
            return String.Format("{0} id:{1:X}",
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
        public static bool Solid = false;

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
            double k = i / 6.0; // 0..1
            if (!Gray) {
                return Color.FromArgb(
                    (int)Lerp(k, 0xEE, 0), // R
                    (int)Lerp(k, 0, 0xDD), // G
                    (int)0                 // B
                );
            } else if (Solid) {
                //return Color.Gray;
                return Color.Black;
            } else {
                return MakeGray(k);
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
                    ? Enumerable.Range(0, len).ToArray()
                    : Enumerable.Range(1, len - 1).Select(d => (b.parentDir + d) % len).ToArray();
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

        List<int> _superLeaves = new List<int>();

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
                    Console.WriteLine("New Tree: {0}", tree.Format());
                    if (tree.IsFull()) {
                        _superLeaves.Add(tree.GetId());
                        MarkToLeaf(newNode);
                    }
                    GrowNode(newNode);
                } else {
                    Console.WriteLine("Skipped Tree: {0}", tree.Format());
                    newNode.skipped = true;
                }
            }
            if (!grown) {
                Console.WriteLine("Can't grow Tree: {0}", node.tree.Format());
            }
        }

        public void GrowSuperTree() {
            Tree treeRoot = Tree.MakeRoot();
            _root = AddNode(treeRoot, null);
            GrowNode(_root);

            Console.WriteLine("Super leaves: {0}", _superLeaves.Count);
        }

        //---------------------------------------------------------

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

        private static IEnumerable<SuperTreeNode> EnumerateNodesSmooth(SuperTreeNode node) {
            yield return node;
            var children = node.children.Where(c => c.toLeaf).ToArray();
            if (children.Any()) {
                foreach (var c in children) {
                    foreach (var n in EnumerateNodesSmooth(c)) {
                        yield return n;
                    }
                    yield return node;
                }
            }
        }

        private int Fib(int i) {
            if (i <= 1) return 1;
            return Fib(i - 1) + Fib(i - 2);
        }

        public void DrawTreesSmooth()
        {
            Viewport viewport = new Viewport(600,600, -3,3, -3,3, false);

            int counter = 0;
            foreach (var node in EnumerateNodesSmooth(_root))
            {
                Image image = new Image(viewport);
                image.RectangleFull(Color.White).Add();
                TreeDrawer.Draw(image, node.tree);
                //
                //int times = 1;
                int times = Fib(7 - node.tree.branches.Length);
                string path0 = "";
                for (int t = 0; t < times; ++t) {
                    // 
                    string pattern = @"smooth\TriTree_{0:00000}.png";
                    string path = String.Format(pattern, ++counter, node.tree.GetId());
                    if (t == 0) {
                        image.WritePng(path);
                        path0 = path;
                    } else {
                        System.IO.File.Copy(path0, path, overwrite: true);
                    }

                    Console.WriteLine("File {0}. Tree saved: {1}", path, node.tree.Format());

                    //if (counter > 100) return; //!!! limit file count
                }
            }

            // https://trac.ffmpeg.org/wiki/Slideshow
            // ffmpeg -framerate 10 -i TriTree_%05d.png smooth.mp4
            // ffmpeg -framerate 10 -i TriTree_%05d.png -pix_fmt yuv420p smooth.mp4
            // ffmpeg -framerate 30 -i TriTree_%05d.png -pix_fmt yuv420p smooth.mp4
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
        static void Test1_MakeTree() {
            // Make a Tree
            var tree = Tree.Parse("-00;00-01;00-11;11-12;12-02;102-21");
            Console.WriteLine("Tree: {0}", tree.Format());
            TreeDrawer.Draw(tree, show: true);
        }

        static int Main() {
            //Test1_MakeTree(); return;

            var superTree = new SuperTree();
            superTree.GrowSuperTree();

            superTree.DrawSuperTree();
            //superTree.DrawLeaves();
            //superTree.DrawTreesSmooth();

            return 0;
        }
    }
}

