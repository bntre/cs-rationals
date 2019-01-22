using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rationals
{
    public static class Grid
    {
        public interface IGridNodeHandler {
            double HandleGridNode(int[] node); // return positive distance or negative result to stop growing the branch
        }

        private struct Node {
            public int[] coordinates;
            public double distance;
        }

        private static void AddNode(List<Node> nodes, Node node) {
            // keep sorted by 'distance'
            for (int i = nodes.Count; i >= 0; --i) {
                if (i == 0 || nodes[i - 1].distance <= node.distance) {
                    nodes.Insert(i, node);
                    return;
                }
            }
        }

        private static Node PopNode(List<Node> nodes) {
            Node n = nodes[0];
            nodes.RemoveAt(0);
            return n;
        }

        private static int[] MakeStep(int[] coordinates, int direction) { // direction is -1, 1 or 0
            int len = coordinates.Length;
            if (direction == 0) len += 1; // add 0 coordinate to next level = growing down
            int[] result = new int[len];
            coordinates.CopyTo(result, 0);
            if (direction != 0) {
                result[len - 1] += direction;
            }
            return result;
        }

        public static void Iterate(IGridNodeHandler handler)
        {
            var nodes = new List<Node>(); // sorted by distance

            // Handle/add root
            int[] c = new[] { 0 };
            double d = handler.HandleGridNode(c);
            if (d >= 0) {
                AddNode(nodes, new Node { coordinates = c, distance = d });
            }

            while (nodes.Count > 0)
            {
                Node node = PopNode(nodes);

                int last = node.coordinates.Last();

                if (last >= 0) {
                    c = MakeStep(node.coordinates, 1);
                    d = handler.HandleGridNode(c);
                    if (d >= 0) {
                        AddNode(nodes, new Node { coordinates = c, distance = d });
                    }
                }

                if (last <= 0) {
                    c = MakeStep(node.coordinates, -1);
                    d = handler.HandleGridNode(c);
                    if (d >= 0) {
                        AddNode(nodes, new Node { coordinates = c, distance = d });
                    }
                }

                // use the same distance to growing down node
                if (d >= 0) {
                    c = MakeStep(node.coordinates, 0);
                    AddNode(nodes, new Node { coordinates = c, distance = d });
                }
            }

        }

    }

}
