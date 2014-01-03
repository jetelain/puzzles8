using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PuzzleCollection.Games.Structures
{
    public class Node234<T>
    {
        public Node234<T> parent;
        public readonly Node234<T>[] kids = new Node234<T>[4];
        public readonly int[] counts = new int[4];
        public readonly T[] elems = new T[3];
    }
}
