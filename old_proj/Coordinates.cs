using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rationals
{
    public static class Coordinates
    {
        // return positive distance or -1 to stop growing the branch
        public delegate double HandleCoordinates(int[] coordinates);

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

        public static void Iterate(HandleCoordinates handle)
        {
            var nodes = new List<Node>(); // sorted by distance

            // Handle/add root
            int[] c = new[] { 0 };
            double d = handle(c);
            if (d >= 0) {
                AddNode(nodes, new Node { coordinates = c, distance = d });
            }

            //int counter = 0;

            while (nodes.Count > 0)
            {
                Node node = PopNode(nodes);

                /*
                System.Diagnostics.Debug.Print("{0}. Full count {1,5} First {2,-20} Last {3,-20}",
                    counter++,
                    nodes.Count,
                    Powers.ToString(node.coordinates),
                    nodes.Count == 0 ? "" : Powers.ToString(nodes[nodes.Count - 1].coordinates)
                );
                */

                int last = node.coordinates[node.coordinates.Length - 1];

                if (last >= 0) {
                    c = MakeStep(node.coordinates, 1);
                    d = handle(c);
                    if (d >= 0) {
                        AddNode(nodes, new Node { coordinates = c, distance = d });
                    }
                }

                if (last <= 0) {
                    c = MakeStep(node.coordinates, -1);
                    d = handle(c);
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
