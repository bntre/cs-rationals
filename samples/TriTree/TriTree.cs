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
    // Single sector Grid for a Tree

    class GridNode {
        public int code;
        public int[] neighbors;
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
                code / 100,
                code / 10 % 10,
                code      % 10
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
        public static Grid Instance = MakeGrid();

        // Edge Ids
        public static int GetEdgeId(GridNode node, int dir) {
            int code0 = node.code;
            int code1 = node.neighbors[dir];
            return code0 < code1
                ? (code0 * 1000) + code1
                : (code1 * 1000) + code0;
        }
        public static int[] GetAllEdgeIds() {
            var ids = new List<int>();
            foreach (GridNode n in Grid.Instance.nodes) {
                if (n != null) {
                    for (int d = 0; d < n.neighbors.Length; ++d) {
                        ids.Add(GetEdgeId(n, d));
                    }
                }
            }
            return ids.Distinct().OrderBy(i => i).ToArray();
        }
    }

    //--------------------------------------------------
    // A Tree (wihtin the Grid)

    class TreeBranch {
        public GridNode node;
        public int parentDir = -1; // direction to parent

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

        public bool HasParent() { return parentDir != -1; }
        public int? GetParentCode() {
            if (!HasParent()) return null;
            return node.neighbors[parentDir];
        }

        public TreeBranch Clone() {
            return new TreeBranch { 
                node      = this.node,
                parentDir = this.parentDir
            };
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

    class Tree {
        public TreeBranch[] branches;

        public Tree Clone() {
            return new Tree {
                branches = this.branches
                    .Select(b => b.Clone())
                    .ToArray()
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
        private static readonly int[] GridEdgeIds = Grid.GetAllEdgeIds();
        public int GetId() {
            int id = 0;
            foreach (TreeBranch b in branches) {
                if (b.HasParent()) {
                    int edgeId = Grid.GetEdgeId(b.node, b.parentDir);
                    int bit = Array.IndexOf(GridEdgeIds, edgeId);
                    id |= 1 << bit;
                }
            }
            return id;
        }

    }

    static class TreeDrawer
    {
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
        static readonly double[,] SectorAngles = MakeSectorAngles();
        static double[] RotateToSector(double x, double y, int sector) {
            double cos = SectorAngles[sector, 0];
            double sin = SectorAngles[sector, 1];
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

        static void DrawBranch(Image image, Point origin, TreeBranch b, int sector) {
            if (!b.HasParent()) return;
            Point p0 = GetNodePoint(b.node.code,             sector);
            Point p1 = GetNodePoint(b.GetParentCode().Value, sector);
            image.Line(new[] { origin + p0, origin + p1 })
                .Add()
                .FillStroke(Color.Empty, Color.Gray, 0.15f);
        }

        public static void Draw(Image image, Point origin, Tree tree) {
            for (int s = 0; s < 3; ++s) {
                foreach (TreeBranch b in tree.branches) {
                    DrawBranch(image, origin, b, s);
                }
            }
        }

        static int fileCounter = 0;
        public static void Draw(Tree tree, bool svg = false, bool show = false) {
            var viewport = new Viewport(200, 200, -3,3, -3,3, false);
            var image = new Image(viewport);
            
            Draw(image, Point.Empty, tree);
            
            string path = String.Format("TriTree_{0:X}", tree.GetId());
            path = String.Format("{0}_{1}", ++fileCounter, path);
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

        public static Image mainImage = new Image(
            new Viewport(250,250*140, 0,35, 0,35*140, false)
        );
        public static void SaveMainImage() {
            string path = "TriTree_main.png";
            mainImage.WritePng(path);
            //Image.Show(path);
        }
    }

    //--------------------------------------------------
    // Super tree (tree of trees)

    class TreeNode<N> where N : class {
        public N parent = null;
        public List<N> children = new List<N>();
    }

    class SuperTreeNode : TreeNode<SuperTreeNode> {
        public Tree tree;
    }

    class SuperTree {
        private SuperTreeNode _root = null;
        private HashSet<int> _treeIds = new HashSet<int>();

        public SuperTreeNode AddNode(Tree tree, SuperTreeNode parent) {
            var node = new SuperTreeNode();
            node.tree = tree;
            if (parent != null) {
                node.parent = parent;
                node.parent.children.Add(node);
            }
            return node;
        }


        public static IEnumerable<Tree> GrowTree(Tree tree) {
            if (tree.IsFull()) yield break;
            foreach (TreeBranch b in tree.branches.Reverse()) { // start from new branches
                int len = b.node.neighbors.Length;
                int[] dirs = b.HasParent()
                    ? Enumerable.Range(1, len-1).Select(d => (b.parentDir + d) % len).ToArray()
                    : Enumerable.Range(0, len  ).ToArray();
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

        /*
        public IEnumerable<SuperTreeNode> EnumerateNodes(SuperTreeNode node) {
            if (node == null) node = this._root;
            if (node == null) yield break;
            yield return node;
            foreach (var c in node.children) {
                foreach (var n in EnumerateNodes(c)) {
                    yield return n;
                }
            }
        }
        */

        List<int> _superLeaves = new List<int>();
        // 1830 - with skipped by id
        // 736 - unique
        // 16*23-1 = 367 (on old poster)
        //           367 * 2 = 734
        // _treeIds - 1504


        public void GrowNode(SuperTreeNode node) {
            bool grown = false;
            foreach (Tree tree in GrowTree(node.tree)) {
                grown = true;
                if (_treeIds.Add(tree.GetId())) {
                    Console.WriteLine("New Tree: {0}", tree.Format());
                    //if (tree.IsFull()) {
                    //   _superLeaves.Add(tree.GetId());
                    //}
                    var newNode = AddNode(tree, node);
                    GrowNode(newNode);
                } else {
                    Console.WriteLine("Skipped Tree: {0}", tree.Format());
                    if (!tree.IsFull()) { }
                }
            }
            if (!grown) {
                Console.WriteLine("Can't grow Tree: {0}", node.tree.Format());
                //TreeDrawer.Draw(node.tree, svg: false, show: false);
                if (node.tree.IsFull()) _superLeaves.Add(node.tree.GetId());
            }
        }

        public void GrowSuperTree() {
            _root = AddNode(Tree.Parse("-00"), null);
            GrowNode(_root);
        }

        private float DrawSuperTree(SuperTreeNode node, Point origin) {
            //
            TreeDrawer.Draw(TreeDrawer.mainImage, origin, node.tree);
            //
            origin.X += 5;
            float shiftY = 0;
            foreach (SuperTreeNode child in node.children) {
                shiftY += DrawSuperTree(child, origin + new Point(0, shiftY));
                shiftY += child != node.children.Last() ? 5 : 1;
            }
            return shiftY;
        }

        public void Build() {
            GrowSuperTree();

            // Draw nodes
            DrawSuperTree(_root, new Point(0, 3));
            TreeDrawer.SaveMainImage();

            Debug.Assert(_superLeaves.Distinct().Count() == _superLeaves.Count);
            Console.WriteLine("Super leaves: {0}", _superLeaves.Count);
        }
    }

    //--------------------------------------------------

    static class Program
    {
        static void Test2_MakeSuperTree() {
            var superTree = new SuperTree();
            superTree.Build();
        }

        static void Test1_MakeTree() {
            // Make a Tree
            var tree = Tree.Parse("-00;00-01;00-11;11-12;12-02;102-21");
            Console.WriteLine("Tree: {0}", tree.Format());
            TreeDrawer.Draw(tree, show: true);
        }

        static int Main() {
            //Test1_MakeTree();
            Test2_MakeSuperTree();

            return 0;
        }
    }
}

