/*
 * tree234.c: reasonably generic counted 2-3-4 tree routines.
 * 
 * This file is copyright 1999-2001 Simon Tatham.
 * C# port copyright 2013 Julien Etelain.
 * 
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or
 * sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT.  IN NO EVENT SHALL SIMON TATHAM BE LIABLE FOR
 * ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
 * CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
 * CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Structures
{
    public class Tree234<T> where T : class
    {
        private delegate void TowardDelegate<T2>(Node234<T2> n, int ki, ref int k, ref int index);

        public Node234<T> root;
        public readonly Func<T, T, int> cmp;

        public Tree234(Func<T, T, int> cmp)
        {
            this.root = null;
            this.cmp = cmp;
        }

        private static int countnode234(Node234<T> n)
        {
            int count = 0;
            int i;
            if (n == null)
                return 0;
            for (i = 0; i < 4; i++)
                count += n.counts[i];
            for (i = 0; i < 3; i++)
                if (n.elems[i] != null)
                    count++;
            return count;
        }

        public int count234()
        {
            if (root != null)
                return countnode234(root);
            else
                return 0;
        }

        /*
         * Propagate a node overflow up a tree until it stops. Returns 0 or
         * 1, depending on whether the root had to be split or not.
         */
        private static int add234_insert(Node234<T> left, T e, Node234<T> right,
                     ref Node234<T> root, Node234<T> n, int ki)
        {
            int lcount, rcount;
            /*
             * We need to insert the new left/element/right set in n at
             * child position ki.
             */
            lcount = countnode234(left);
            rcount = countnode234(right);
            while (n != null)
            {
                //LOG(("  at %p: %p/%d \"%s\" %p/%d \"%s\" %p/%d \"%s\" %p/%d\n",
                //     n,
                //     n.kids[0], n.counts[0], n.elems[0],
                //     n.kids[1], n.counts[1], n.elems[1],
                //     n.kids[2], n.counts[2], n.elems[2],
                //     n.kids[3], n.counts[3]));
                //LOG(("  need to insert %p/%d \"%s\" %p/%d at position %d\n",
                //     left, lcount, e, right, rcount, ki));
                if (n.elems[1] == null)
                {
                    /*
                     * Insert in a 2-node; simple.
                     */
                    if (ki == 0)
                    {
                        //LOG(("  inserting on left of 2-node\n"));
                        n.kids[2] = n.kids[1]; n.counts[2] = n.counts[1];
                        n.elems[1] = n.elems[0];
                        n.kids[1] = right; n.counts[1] = rcount;
                        n.elems[0] = e;
                        n.kids[0] = left; n.counts[0] = lcount;
                    }
                    else
                    { /* ki == 1 */
                        //LOG(("  inserting on right of 2-node\n"));
                        n.kids[2] = right; n.counts[2] = rcount;
                        n.elems[1] = e;
                        n.kids[1] = left; n.counts[1] = lcount;
                    }
                    if (n.kids[0] != null) n.kids[0].parent = n;
                    if (n.kids[1] != null) n.kids[1].parent = n;
                    if (n.kids[2] != null) n.kids[2].parent = n;
                    //LOG(("  done\n"));
                    break;
                }
                else if (n.elems[2] == null)
                {
                    /*
                     * Insert in a 3-node; simple.
                     */
                    if (ki == 0)
                    {
                        //LOG(("  inserting on left of 3-node\n"));
                        n.kids[3] = n.kids[2]; n.counts[3] = n.counts[2];
                        n.elems[2] = n.elems[1];
                        n.kids[2] = n.kids[1]; n.counts[2] = n.counts[1];
                        n.elems[1] = n.elems[0];
                        n.kids[1] = right; n.counts[1] = rcount;
                        n.elems[0] = e;
                        n.kids[0] = left; n.counts[0] = lcount;
                    }
                    else if (ki == 1)
                    {
                        //LOG(("  inserting in middle of 3-node\n"));
                        n.kids[3] = n.kids[2]; n.counts[3] = n.counts[2];
                        n.elems[2] = n.elems[1];
                        n.kids[2] = right; n.counts[2] = rcount;
                        n.elems[1] = e;
                        n.kids[1] = left; n.counts[1] = lcount;
                    }
                    else
                    { /* ki == 2 */
                        //LOG(("  inserting on right of 3-node\n"));
                        n.kids[3] = right; n.counts[3] = rcount;
                        n.elems[2] = e;
                        n.kids[2] = left; n.counts[2] = lcount;
                    }
                    if (n.kids[0] != null) n.kids[0].parent = n;
                    if (n.kids[1] != null) n.kids[1].parent = n;
                    if (n.kids[2] != null) n.kids[2].parent = n;
                    if (n.kids[3] != null) n.kids[3].parent = n;
                    //LOG(("  done\n"));
                    break;
                }
                else
                {
                    Node234<T> m = new Node234<T>();
                    m.parent = n.parent;
                    //LOG(("  splitting a 4-node; created new node %p\n", m));
                    /*
                     * Insert in a 4-node; split into a 2-node and a
                     * 3-node, and move focus up a level.
                     * 
                     * I don't think it matters which way round we put the
                     * 2 and the 3. For simplicity, we'll put the 3 first
                     * always.
                     */
                    if (ki == 0)
                    {
                        m.kids[0] = left; m.counts[0] = lcount;
                        m.elems[0] = e;
                        m.kids[1] = right; m.counts[1] = rcount;
                        m.elems[1] = n.elems[0];
                        m.kids[2] = n.kids[1]; m.counts[2] = n.counts[1];
                        e = n.elems[1];
                        n.kids[0] = n.kids[2]; n.counts[0] = n.counts[2];
                        n.elems[0] = n.elems[2];
                        n.kids[1] = n.kids[3]; n.counts[1] = n.counts[3];
                    }
                    else if (ki == 1)
                    {
                        m.kids[0] = n.kids[0]; m.counts[0] = n.counts[0];
                        m.elems[0] = n.elems[0];
                        m.kids[1] = left; m.counts[1] = lcount;
                        m.elems[1] = e;
                        m.kids[2] = right; m.counts[2] = rcount;
                        e = n.elems[1];
                        n.kids[0] = n.kids[2]; n.counts[0] = n.counts[2];
                        n.elems[0] = n.elems[2];
                        n.kids[1] = n.kids[3]; n.counts[1] = n.counts[3];
                    }
                    else if (ki == 2)
                    {
                        m.kids[0] = n.kids[0]; m.counts[0] = n.counts[0];
                        m.elems[0] = n.elems[0];
                        m.kids[1] = n.kids[1]; m.counts[1] = n.counts[1];
                        m.elems[1] = n.elems[1];
                        m.kids[2] = left; m.counts[2] = lcount;
                        /* e = e; */
                        n.kids[0] = right; n.counts[0] = rcount;
                        n.elems[0] = n.elems[2];
                        n.kids[1] = n.kids[3]; n.counts[1] = n.counts[3];
                    }
                    else
                    { /* ki == 3 */
                        m.kids[0] = n.kids[0]; m.counts[0] = n.counts[0];
                        m.elems[0] = n.elems[0];
                        m.kids[1] = n.kids[1]; m.counts[1] = n.counts[1];
                        m.elems[1] = n.elems[1];
                        m.kids[2] = n.kids[2]; m.counts[2] = n.counts[2];
                        n.kids[0] = left; n.counts[0] = lcount;
                        n.elems[0] = e;
                        n.kids[1] = right; n.counts[1] = rcount;
                        e = n.elems[2];
                    }
                    m.kids[3] = n.kids[3] = n.kids[2] = null;
                    m.counts[3] = n.counts[3] = n.counts[2] = 0;
                    m.elems[2] = n.elems[2] = n.elems[1] = null;
                    if (m.kids[0] != null) m.kids[0].parent = m;
                    if (m.kids[1] != null) m.kids[1].parent = m;
                    if (m.kids[2] != null) m.kids[2].parent = m;
                    if (n.kids[0] != null) n.kids[0].parent = n;
                    if (n.kids[1] != null) n.kids[1].parent = n;
                    //LOG(("  left (%p): %p/%d \"%s\" %p/%d \"%s\" %p/%d\n", m,
                    // m.kids[0], m.counts[0], m.elems[0],
                    // m.kids[1], m.counts[1], m.elems[1],
                    // m.kids[2], m.counts[2]));
                    //LOG(("  right (%p): %p/%d \"%s\" %p/%d\n", n,
                    // n.kids[0], n.counts[0], n.elems[0],
                    // n.kids[1], n.counts[1]));
                    left = m; lcount = countnode234(left);
                    right = n; rcount = countnode234(right);
                }
                if (n.parent != null)
                    ki = (n.parent.kids[0] == n ? 0 :
                      n.parent.kids[1] == n ? 1 :
                      n.parent.kids[2] == n ? 2 : 3);
                n = n.parent;
            }

            /*
             * If we've come out of here by `break', n will still be
             * non-null and all we need to do is go back up the tree
             * updating counts. If we've come here because n is null, we
             * need to create a new root for the tree because the old one
             * has just split into two. */
            if (n != null)
            {
                while (n.parent != null)
                {
                    int count = countnode234(n);
                    int childnum;
                    childnum = (n.parent.kids[0] == n ? 0 :
                        n.parent.kids[1] == n ? 1 :
                        n.parent.kids[2] == n ? 2 : 3);
                    n.parent.counts[childnum] = count;
                    n = n.parent;
                }
                return 0;		       /* root unchanged */
            }
            else
            {
                //LOG(("  root is overloaded, split into two\n"));
                root = new Node234<T>();
                root.kids[0] = left; root.counts[0] = lcount;
                root.elems[0] = e;
                root.kids[1] = right; root.counts[1] = rcount;
                root.elems[1] = null;
                root.kids[2] = null; root.counts[2] = 0;
                root.elems[2] = null;
                root.kids[3] = null; root.counts[3] = 0;
                root.parent = null;
                if (root.kids[0] != null) root.kids[0].parent = root;
                if (root.kids[1] != null) root.kids[1].parent = root;
                //LOG(("  new root is %p/%d \"%s\" %p/%d\n",
                //     (*root).kids[0], (*root).counts[0],
                //     (*root).elems[0],
                //     (*root).kids[1], (*root).counts[1]));
                return 1;		       /* root moved */
            }
        }

        /*
         * Add an element e to a 2-3-4 tree t. Returns e on success, or if
         * an existing element compares equal, returns that.
         */
        private static T add234_internal(Tree234<T> t, T e, int index)
        {
            Node234<T> n;
            int ki = 0;
            T orig_e = e;
            int c;

            //LOG(("adding element \"%s\" to tree %p\n", e, t));
            if (t.root == null)
            {
                t.root = new Node234<T>();
                t.root.elems[1] = t.root.elems[2] = null;
                t.root.kids[0] = t.root.kids[1] = null;
                t.root.kids[2] = t.root.kids[3] = null;
                t.root.counts[0] = t.root.counts[1] = 0;
                t.root.counts[2] = t.root.counts[3] = 0;
                t.root.parent = null;
                t.root.elems[0] = e;
                //LOG(("  created root %p\n", t.root));
                return orig_e;
            }

            n = t.root;
            while (n != null)
            {
                //LOG(("  node %p: %p/%d \"%s\" %p/%d \"%s\" %p/%d \"%s\" %p/%d\n",
                //     n,
                //     n.kids[0], n.counts[0], n.elems[0],
                //     n.kids[1], n.counts[1], n.elems[1],
                //     n.kids[2], n.counts[2], n.elems[2],
                //     n.kids[3], n.counts[3]));
                if (index >= 0)
                {
                    if (n.kids[0] == null)
                    {
                        /*
                         * Leaf node. We want to insert at kid position
                         * equal to the index:
                         * 
                         *   0 A 1 B 2 C 3
                         */
                        ki = index;
                    }
                    else
                    {
                        /*
                         * Internal node. We always descend through it (add
                         * always starts at the bottom, never in the
                         * middle).
                         */
                        if (index <= n.counts[0])
                        {
                            ki = 0;
                        }
                        else if ((index -= n.counts[0] + 1) <= n.counts[1])
                        {
                            ki = 1;
                        }
                        else if ((index -= n.counts[1] + 1) <= n.counts[2])
                        {
                            ki = 2;
                        }
                        else if ((index -= n.counts[2] + 1) <= n.counts[3])
                        {
                            ki = 3;
                        }
                        else
                            return null;       /* error: index out of range */
                    }
                }
                else
                {
                    if ((c = t.cmp(e, n.elems[0])) < 0)
                        ki = 0;
                    else if (c == 0)
                        return n.elems[0];	       /* already exists */
                    else if (n.elems[1] == null || (c = t.cmp(e, n.elems[1])) < 0)
                        ki = 1;
                    else if (c == 0)
                        return n.elems[1];	       /* already exists */
                    else if (n.elems[2] == null || (c = t.cmp(e, n.elems[2])) < 0)
                        ki = 2;
                    else if (c == 0)
                        return n.elems[2];	       /* already exists */
                    else
                        ki = 3;
                }
                //LOG(("  moving to child %d (%p)\n", ki, n.kids[ki]));
                if (n.kids[ki] == null)
                    break;
                n = n.kids[ki];
            }

            add234_insert(null, e, null, ref t.root, n, ki);

            return orig_e;
        }

        public T add234(T e)
        {
            if (cmp == null)		       /* tree is unsorted */
                return null;

            return add234_internal(this, e, -1);
        }
        public T addpos234(T e, int index)
        {
            if (index < 0 ||		       /* index out of range */
            cmp != null)			       /* tree is sorted */
                return null;		       /* return failure */

            return add234_internal(this, e, index);  /* this checks the upper bound */
        }

        /*
         * Look up the element at a given numeric index in a 2-3-4 tree.
         * Returns null if the index is out of range.
         */
        public T index234(int index)
        {
            Node234<T> n;

            if (this.root == null)
                return null;		       /* tree is empty */

            if (index < 0 || index >= countnode234(this.root))
                return null;		       /* out of range */

            n = this.root;

            while (n != null)
            {
                if (index < n.counts[0])
                    n = n.kids[0];
                else if ((index -= n.counts[0] + 1) < 0)
                    return n.elems[0];
                else if (index < n.counts[1])
                    n = n.kids[1];
                else if ((index -= n.counts[1] + 1) < 0)
                    return n.elems[1];
                else if (index < n.counts[2])
                    n = n.kids[2];
                else if ((index -= n.counts[2] + 1) < 0)
                    return n.elems[2];
                else
                    n = n.kids[3];
            }

            /* We shouldn't ever get here. I wonder how we did. */
            return null;
        }

        /*
         * Find an element e in a sorted 2-3-4 tree t. Returns null if not
         * found. e is always passed as the first argument to cmp, so cmp
         * can be an asymmetric function if desired. cmp can also be passed
         * as null, in which case the compare function from the tree proper
         * will be used.
         */
        public T findrelpos234(T e, Func<T, T, int> cmp,
            Rel234 relation, out int index)
        {
            return findrelpos234(this, e, cmp, relation, out index);
        }


        private static T findrelpos234(Tree234<T> t, T e, Func<T, T, int> cmp,
                    Rel234 relation, out int index)
        {
            Node234<T> n;
            T ret;
            int c;
            int idx, ecount, kcount, cmpret;

            index = -1;

            if (t.root == null)
                return null;

            if (cmp == null)
                cmp = t.cmp;

            n = t.root;
            /*
             * Attempt to find the element itself.
             */
            idx = 0;
            ecount = -1;
            /*
             * Prepare a fake `cmp' result if e is null.
             */
            cmpret = 0;
            if (e == null)
            {
                Debug.Assert(relation == Rel234.REL234_LT || relation == Rel234.REL234_GT);
                if (relation == Rel234.REL234_LT)
                    cmpret = +1;	       /* e is a max: always greater */
                else if (relation == Rel234.REL234_GT)
                    cmpret = -1;	       /* e is a min: always smaller */
            }
            while (true)
            {
                for (kcount = 0; kcount < 4; kcount++)
                {
                    if (kcount >= 3 || n.elems[kcount] == null ||
                    (c = cmpret != 0 ? cmpret : cmp(e, n.elems[kcount])) < 0)
                    {
                        break;
                    }
                    if (n.kids[kcount] != null) idx += n.counts[kcount];
                    if (c == 0)
                    {
                        ecount = kcount;
                        break;
                    }
                    idx++;
                }
                if (ecount >= 0)
                    break;
                if (n.kids[kcount] != null)
                    n = n.kids[kcount];
                else
                    break;
            }

            if (ecount >= 0)
            {
                /*
                 * We have found the element we're looking for. It's
                 * n.elems[ecount], at tree index idx. If our search
                 * relation is EQ, LE or GE we can now go home.
                 */
                if (relation != Rel234.REL234_LT && relation != Rel234.REL234_GT)
                {
                    index = idx;
                    return n.elems[ecount];
                }

                /*
                 * Otherwise, we'll do an indexed lookup for the previous
                 * or next element. (It would be perfectly possible to
                 * implement these search types in a non-counted tree by
                 * going back up from where we are, but far more fiddly.)
                 */
                if (relation == Rel234.REL234_LT)
                    idx--;
                else
                    idx++;
            }
            else
            {
                /*
                 * We've found our way to the bottom of the tree and we
                 * know where we would insert this node if we wanted to:
                 * we'd put it in in place of the (empty) subtree
                 * n.kids[kcount], and it would have index idx
                 * 
                 * But the actual element isn't there. So if our search
                 * relation is EQ, we're doomed.
                 */
                if (relation == Rel234.REL234_EQ)
                    return null;

                /*
                 * Otherwise, we must do an index lookup for index idx-1
                 * (if we're going left - LE or LT) or index idx (if we're
                 * going right - GE or GT).
                 */
                if (relation == Rel234.REL234_LT || relation == Rel234.REL234_LE)
                {
                    idx--;
                }
            }

            /*
             * We know the index of the element we want; just call index234
             * to do the rest. This will return null if the index is out of
             * bounds, which is exactly what we want.
             */
            ret = t.index234(idx);
            if (ret != null) index = idx;
            return ret;
        }

        public T find234( T e, Func<T, T, int> cmp)
        {
            return find234(this, e, cmp);
        }

        private static T find234(Tree234<T> t, T e, Func<T, T, int> cmp)
        {
            int temp;
            return findrelpos234(t, e, cmp, Rel234.REL234_EQ, out temp);
        }
        private static T findrel234(Tree234<T> t, T e, Func<T, T, int> cmp, Rel234 relation)
        {
            int temp;
            return findrelpos234(t, e, cmp, relation, out temp);
        }
        private static T findpos234(Tree234<T> t, T e, Func<T, T, int> cmp, out int index)
        {
            return findrelpos234(t, e, cmp, Rel234.REL234_EQ, out index);
        }

        /*
         * Tree transformation used in delete and split: move a subtree
         * right, from child ki of a node to the next child. Update k and
         * index so that they still point to the same place in the
         * transformed tree. Assumes the destination child is not full, and
         * that the source child does have a subtree to spare. Can cope if
         * the destination child is undersized.
         * 
         *                . C .                     . B .
         *               /     \     .            /     \
         * [more] a A b B c   d D e      [more] a A b   c C d D e
         * 
         *                 . C .                     . B .
         *                /     \    .             /     \
         *  [more] a A b B c     d        [more] a A b   c C d
         */
        private static void trans234_subtree_right(Node234<T> n, int ki, ref int k, ref int index)
        {
            Node234<T> src, dest;
            int i, srclen, adjust;

            src = n.kids[ki];
            dest = n.kids[ki + 1];

            //LOG(("  trans234_subtree_right(%p, %d):\n", n, ki));
            //LOG(("    parent %p: %p/%d \"%s\" %p/%d \"%s\" %p/%d \"%s\" %p/%d\n",
            // n,
            // n.kids[0], n.counts[0], n.elems[0],
            // n.kids[1], n.counts[1], n.elems[1],
            // n.kids[2], n.counts[2], n.elems[2],
            // n.kids[3], n.counts[3]));
            //LOG(("    src %p: %p/%d \"%s\" %p/%d \"%s\" %p/%d \"%s\" %p/%d\n",
            // src,
            // src.kids[0], src.counts[0], src.elems[0],
            // src.kids[1], src.counts[1], src.elems[1],
            // src.kids[2], src.counts[2], src.elems[2],
            // src.kids[3], src.counts[3]));
            //LOG(("    dest %p: %p/%d \"%s\" %p/%d \"%s\" %p/%d \"%s\" %p/%d\n",
            // dest,
            // dest.kids[0], dest.counts[0], dest.elems[0],
            // dest.kids[1], dest.counts[1], dest.elems[1],
            // dest.kids[2], dest.counts[2], dest.elems[2],
            // dest.kids[3], dest.counts[3]));
            /*
             * Move over the rest of the destination node to make space.
             */
            dest.kids[3] = dest.kids[2]; dest.counts[3] = dest.counts[2];
            dest.elems[2] = dest.elems[1];
            dest.kids[2] = dest.kids[1]; dest.counts[2] = dest.counts[1];
            dest.elems[1] = dest.elems[0];
            dest.kids[1] = dest.kids[0]; dest.counts[1] = dest.counts[0];

            /* which element to move over */
            i = (src.elems[2] != null ? 2 : src.elems[1] != null ? 1 : 0);

            dest.elems[0] = n.elems[ki];
            n.elems[ki] = src.elems[i];
            src.elems[i] = null;

            dest.kids[0] = src.kids[i + 1]; dest.counts[0] = src.counts[i + 1];
            src.kids[i + 1] = null; src.counts[i + 1] = 0;

            if (dest.kids[0] != null) dest.kids[0].parent = dest;

            adjust = dest.counts[0] + 1;

            n.counts[ki] -= adjust;
            n.counts[ki + 1] += adjust;

            srclen = n.counts[ki];

            //if (k) {
            //LOG(("    before: k,index = %d,%d\n", (*k), (*index)));
            if (k == ki && index > srclen)
            {
                index -= srclen + 1;
                k++;
            }
            else if (k == ki + 1)
            {
                index += adjust;
            }
            //LOG(("    after: k,index = %d,%d\n", (*k), (*index)));
            //}

            //LOG(("    parent %p: %p/%d \"%s\" %p/%d \"%s\" %p/%d \"%s\" %p/%d\n",
            // n,
            // n.kids[0], n.counts[0], n.elems[0],
            // n.kids[1], n.counts[1], n.elems[1],
            // n.kids[2], n.counts[2], n.elems[2],
            // n.kids[3], n.counts[3]));
            //LOG(("    src %p: %p/%d \"%s\" %p/%d \"%s\" %p/%d \"%s\" %p/%d\n",
            // src,
            // src.kids[0], src.counts[0], src.elems[0],
            // src.kids[1], src.counts[1], src.elems[1],
            // src.kids[2], src.counts[2], src.elems[2],
            // src.kids[3], src.counts[3]));
            //LOG(("    dest %p: %p/%d \"%s\" %p/%d \"%s\" %p/%d \"%s\" %p/%d\n",
            // dest,
            // dest.kids[0], dest.counts[0], dest.elems[0],
            // dest.kids[1], dest.counts[1], dest.elems[1],
            // dest.kids[2], dest.counts[2], dest.elems[2],
            // dest.kids[3], dest.counts[3]));
        }

        /*
         * Tree transformation used in delete and split: move a subtree
         * left, from child ki of a node to the previous child. Update k
         * and index so that they still point to the same place in the
         * transformed tree. Assumes the destination child is not full, and
         * that the source child does have a subtree to spare. Can cope if
         * the destination child is undersized. 
         *
         *      . B .                             . C .
         *     /     \                .         /     \
         *  a A b   c C d D e [more]      a A b B c   d D e [more]
         *
         *     . A .                             . B .
         *    /     \                 .        /     \
         *   a   b B c C d [more]            a A b   c C d [more]
         */
        private static void trans234_subtree_left(Node234<T> n, int ki, ref int k, ref int index)
        {
            Node234<T> src, dest;
            int i, adjust;

            src = n.kids[ki];
            dest = n.kids[ki - 1];

            //LOG(("  trans234_subtree_left(%p, %d):\n", n, ki));
            //LOG(("    parent %p: %p/%d \"%s\" %p/%d \"%s\" %p/%d \"%s\" %p/%d\n",
            // n,
            // n.kids[0], n.counts[0], n.elems[0],
            // n.kids[1], n.counts[1], n.elems[1],
            // n.kids[2], n.counts[2], n.elems[2],
            // n.kids[3], n.counts[3]));
            //LOG(("    dest %p: %p/%d \"%s\" %p/%d \"%s\" %p/%d \"%s\" %p/%d\n",
            // dest,
            // dest.kids[0], dest.counts[0], dest.elems[0],
            // dest.kids[1], dest.counts[1], dest.elems[1],
            // dest.kids[2], dest.counts[2], dest.elems[2],
            // dest.kids[3], dest.counts[3]));
            //LOG(("    src %p: %p/%d \"%s\" %p/%d \"%s\" %p/%d \"%s\" %p/%d\n",
            // src,
            // src.kids[0], src.counts[0], src.elems[0],
            // src.kids[1], src.counts[1], src.elems[1],
            // src.kids[2], src.counts[2], src.elems[2],
            // src.kids[3], src.counts[3]));

            /* where in dest to put it */
            i = (dest.elems[1] != null ? 2 : dest.elems[0] != null ? 1 : 0);
            dest.elems[i] = n.elems[ki - 1];
            n.elems[ki - 1] = src.elems[0];

            dest.kids[i + 1] = src.kids[0]; dest.counts[i + 1] = src.counts[0];

            if (dest.kids[i + 1] != null) dest.kids[i + 1].parent = dest;

            /*
             * Move over the rest of the source node.
             */
            src.kids[0] = src.kids[1]; src.counts[0] = src.counts[1];
            src.elems[0] = src.elems[1];
            src.kids[1] = src.kids[2]; src.counts[1] = src.counts[2];
            src.elems[1] = src.elems[2];
            src.kids[2] = src.kids[3]; src.counts[2] = src.counts[3];
            src.elems[2] = null;
            src.kids[3] = null; src.counts[3] = 0;

            adjust = dest.counts[i + 1] + 1;

            n.counts[ki] -= adjust;
            n.counts[ki - 1] += adjust;

            //if (k) {
            //LOG(("    before: k,index = %d,%d\n", (*k), (*index)));
            if (k == ki)
            {
                index -= adjust;
                if (index < 0)
                {
                    index += n.counts[ki - 1] + 1;
                    k--;
                }
            }
            //LOG(("    after: k,index = %d,%d\n", (*k), (*index)));
            //}

            //LOG(("    parent %p: %p/%d \"%s\" %p/%d \"%s\" %p/%d \"%s\" %p/%d\n",
            // n,
            // n.kids[0], n.counts[0], n.elems[0],
            // n.kids[1], n.counts[1], n.elems[1],
            // n.kids[2], n.counts[2], n.elems[2],
            // n.kids[3], n.counts[3]));
            //LOG(("    dest %p: %p/%d \"%s\" %p/%d \"%s\" %p/%d \"%s\" %p/%d\n",
            // dest,
            // dest.kids[0], dest.counts[0], dest.elems[0],
            // dest.kids[1], dest.counts[1], dest.elems[1],
            // dest.kids[2], dest.counts[2], dest.elems[2],
            // dest.kids[3], dest.counts[3]));
            //LOG(("    src %p: %p/%d \"%s\" %p/%d \"%s\" %p/%d \"%s\" %p/%d\n",
            // src,
            // src.kids[0], src.counts[0], src.elems[0],
            // src.kids[1], src.counts[1], src.elems[1],
            // src.kids[2], src.counts[2], src.elems[2],
            // src.kids[3], src.counts[3]));
        }

        /*
         * Tree transformation used in delete and split: merge child nodes
         * ki and ki+1 of a node. Update k and index so that they still
         * point to the same place in the transformed tree. Assumes both
         * children _are_ sufficiently small.
         *
         *      . B .                .
         *     /     \     .        |
         *  a A b   c C d      a A b B c C d
         * 
         * This routine can also cope with either child being undersized:
         * 
         *     . A .                 .
         *    /     \      .        |
         *   a     b B c         a A b B c
         *
         *    . A .                  .
         *   /     \       .        |
         *  a   b B c C d      a A b B c C d
         */
        private static void trans234_subtree_merge(Node234<T> n, int ki, ref int k, ref int index)
        {
            Node234<T> left, right;
            int i, leftlen, rightlen, lsize, rsize;

            left = n.kids[ki]; leftlen = n.counts[ki];
            right = n.kids[ki + 1]; rightlen = n.counts[ki + 1];

            //LOG(("  trans234_subtree_merge(%p, %d):\n", n, ki));
            //LOG(("    parent %p: %p/%d \"%s\" %p/%d \"%s\" %p/%d \"%s\" %p/%d\n",
            // n,
            // n.kids[0], n.counts[0], n.elems[0],
            // n.kids[1], n.counts[1], n.elems[1],
            // n.kids[2], n.counts[2], n.elems[2],
            // n.kids[3], n.counts[3]));
            //LOG(("    left %p: %p/%d \"%s\" %p/%d \"%s\" %p/%d \"%s\" %p/%d\n",
            // left,
            // left.kids[0], left.counts[0], left.elems[0],
            // left.kids[1], left.counts[1], left.elems[1],
            // left.kids[2], left.counts[2], left.elems[2],
            // left.kids[3], left.counts[3]));
            //LOG(("    right %p: %p/%d \"%s\" %p/%d \"%s\" %p/%d \"%s\" %p/%d\n",
            // right,
            // right.kids[0], right.counts[0], right.elems[0],
            // right.kids[1], right.counts[1], right.elems[1],
            // right.kids[2], right.counts[2], right.elems[2],
            // right.kids[3], right.counts[3]));

            Debug.Assert(left.elems[2] == null && right.elems[2] == null);   /* neither is large! */
            lsize = (left.elems[1] != null ? 2 : left.elems[0] != null ? 1 : 0);
            rsize = (right.elems[1] != null ? 2 : right.elems[0] != null ? 1 : 0);

            left.elems[lsize] = n.elems[ki];

            for (i = 0; i < rsize + 1; i++)
            {
                left.kids[lsize + 1 + i] = right.kids[i];
                left.counts[lsize + 1 + i] = right.counts[i];
                if (left.kids[lsize + 1 + i] != null)
                    left.kids[lsize + 1 + i].parent = left;
                if (i < rsize)
                    left.elems[lsize + 1 + i] = right.elems[i];
            }

            n.counts[ki] += rightlen + 1;

            //sfree(right);

            /*
             * Move the rest of n up by one.
             */
            for (i = ki + 1; i < 3; i++)
            {
                n.kids[i] = n.kids[i + 1];
                n.counts[i] = n.counts[i + 1];
            }
            for (i = ki; i < 2; i++)
            {
                n.elems[i] = n.elems[i + 1];
            }
            n.kids[3] = null;
            n.counts[3] = 0;
            n.elems[2] = null;

            //if (k) {
            //LOG(("    before: k,index = %d,%d\n", (*k), (*index)));
            if (k == ki + 1)
            {
                k--;
                index += leftlen + 1;
            }
            else if (k > ki + 1)
            {
                k--;
            }
            //LOG(("    after: k,index = %d,%d\n", (*k), (*index)));
            //}

            //LOG(("    parent %p: %p/%d \"%s\" %p/%d \"%s\" %p/%d \"%s\" %p/%d\n",
            // n,
            // n.kids[0], n.counts[0], n.elems[0],
            // n.kids[1], n.counts[1], n.elems[1],
            // n.kids[2], n.counts[2], n.elems[2],
            // n.kids[3], n.counts[3]));
            //LOG(("    merged %p: %p/%d \"%s\" %p/%d \"%s\" %p/%d \"%s\" %p/%d\n",
            // left,
            // left.kids[0], left.counts[0], left.elems[0],
            // left.kids[1], left.counts[1], left.elems[1],
            // left.kids[2], left.counts[2], left.elems[2],
            // left.kids[3], left.counts[3]));

        }

        /*
         * Delete an element e in a 2-3-4 tree. Does not free the element,
         * merely removes all links to it from the tree nodes.
         */
        private static T delpos234_internal(Tree234<T> t, int index)
        {
            Node234<T> n;
            T retval;
            int ki = 0, i;

            retval = null;

            n = t.root;		       /* by assumption this is non-null */
            //LOG(("deleting item %d from tree %p\n", index, t));
            while (true)
            {
                Node234<T> sub;

                //LOG(("  node %p: %p/%d \"%s\" %p/%d \"%s\" %p/%d \"%s\" %p/%d index=%d\n",
                //     n,
                //     n.kids[0], n.counts[0], n.elems[0],
                //     n.kids[1], n.counts[1], n.elems[1],
                //     n.kids[2], n.counts[2], n.elems[2],
                //     n.kids[3], n.counts[3],
                //     index));
                if (index <= n.counts[0])
                {
                    ki = 0;
                }
                else if ((index -= n.counts[0] + 1) <= n.counts[1])
                {
                    ki = 1;
                }
                else if ((index -= n.counts[1] + 1) <= n.counts[2])
                {
                    ki = 2;
                }
                else if ((index -= n.counts[2] + 1) <= n.counts[3])
                {
                    ki = 3;
                }
                else
                {
                    Debug.Assert(false);		       /* can't happen */
                }

                if (n.kids[0] == null)
                    break;		       /* n is a leaf node; we're here! */

                /*
                 * Check to see if we've found our target element. If so,
                 * we must choose a new target (we'll use the old target's
                 * successor, which will be in a leaf), move it into the
                 * place of the old one, continue down to the leaf and
                 * delete the old copy of the new target.
                 */
                if (index == n.counts[ki])
                {
                    Node234<T> m;
                    //LOG(("  found element in internal node, index %d\n", ki));
                    Debug.Assert(n.elems[ki] != null);      /* must be a kid _before_ an element */
                    ki++; index = 0;
                    for (m = n.kids[ki]; m.kids[0] != null; m = m.kids[0])
                        continue;
                    //LOG(("  replacing with element \"%s\" from leaf node %p\n",
                    // m.elems[0], m));
                    retval = n.elems[ki - 1];
                    n.elems[ki - 1] = m.elems[0];
                }

                /*
                 * Recurse down to subtree ki. If it has only one element,
                 * we have to do some transformation to start with.
                 */
                //LOG(("  moving to subtree %d\n", ki));
                sub = n.kids[ki];
                if (sub.elems[1] == null)
                {
                    //LOG(("  subtree has only one element!\n"));
                    if (ki > 0 && n.kids[ki - 1].elems[1] != null)
                    {
                        /*
                         * Child ki has only one element, but child
                         * ki-1 has two or more. So we need to move a
                         * subtree from ki-1 to ki.
                         */
                        trans234_subtree_right(n, ki - 1, ref ki, ref index);
                    }
                    else if (ki < 3 && n.kids[ki + 1] != null &&
                           n.kids[ki + 1].elems[1] != null)
                    {
                        /*
                         * Child ki has only one element, but ki+1 has
                         * two or more. Move a subtree from ki+1 to ki.
                         */
                        trans234_subtree_left(n, ki + 1, ref ki, ref index);
                    }
                    else
                    {
                        /*
                         * ki is small with only small neighbours. Pick a
                         * neighbour and merge with it.
                         */
                        trans234_subtree_merge(n, ki > 0 ? ki - 1 : ki, ref ki, ref index);
                        sub = n.kids[ki];

                        if (n.elems[0] == null)
                        {
                            /*
                             * The root is empty and needs to be
                             * removed.
                             */
                            //LOG(("  shifting root!\n"));
                            t.root = sub;
                            sub.parent = null;
                            //sfree(n);
                            n = null;
                        }
                    }
                }

                if (n != null)
                    n.counts[ki]--;
                n = sub;
            }

            /*
             * Now n is a leaf node, and ki marks the element number we
             * want to delete. We've already arranged for the leaf to be
             * bigger than minimum size, so let's just go to it.
             */
            Debug.Assert(n.kids[0] == null);
            if (retval == null)
                retval = n.elems[ki];

            for (i = ki; i < 2 && n.elems[i + 1] != null; i++)
                n.elems[i] = n.elems[i + 1];
            n.elems[i] = null;

            /*
             * It's just possible that we have reduced the leaf to zero
             * size. This can only happen if it was the root - so destroy
             * it and make the tree empty.
             */
            if (n.elems[0] == null)
            {
                //LOG(("  removed last element in tree, destroying empty root\n"));
                Debug.Assert(n == t.root);
                //sfree(n);
                t.root = null;
            }

            return retval;		       /* finished! */
        }
        public T delpos234(int index)
        {
            if (index < 0 || index >= countnode234(this.root))
                return null;
            return delpos234_internal(this, index);
        }
        public T del234(T e)
        {
            int index = 0;
            if (findrelpos234(this, e, null, Rel234.REL234_EQ, out index) == null)
                return null;		       /* it wasn't in there anyway */
            return delpos234_internal(this, index); /* it's there; delete it. */
        }

        /*
         * Join two subtrees together with a separator element between
         * them, given their relative height.
         * 
         * (Height<0 means the left tree is shorter, >0 means the right
         * tree is shorter, =0 means (duh) they're equal.)
         * 
         * It is assumed that any checks needed on the ordering criterion
         * have _already_ been done.
         * 
         * The value returned in `height' is 0 or 1 depending on whether the
         * resulting tree is the same height as the original larger one, or
         * one higher.
         */
        private static Node234<T> join234_internal(Node234<T> left, T sep,
                         Node234<T> right, ref int height)
        {
            Node234<T> root, node;
            int relht = height;
            int ki;

            //LOG(("  join: joining %p \"%s\" %p, relative height is %d\n",
            // left, sep, right, relht));
            if (relht == 0)
            {
                /*
                 * The trees are the same height. Create a new one-element
                 * root containing the separator and pointers to the two
                 * nodes.
                 */
                Node234<T> newroot;
                newroot = new Node234<T>();
                newroot.kids[0] = left; newroot.counts[0] = countnode234(left);
                newroot.elems[0] = sep;
                newroot.kids[1] = right; newroot.counts[1] = countnode234(right);
                newroot.elems[1] = null;
                newroot.kids[2] = null; newroot.counts[2] = 0;
                newroot.elems[2] = null;
                newroot.kids[3] = null; newroot.counts[3] = 0;
                newroot.parent = null;
                if (left != null) left.parent = newroot;
                if (right != null) right.parent = newroot;
                height = 1;
                //LOG(("  join: same height, brand new root\n"));
                return newroot;
            }

            /*
             * This now works like the addition algorithm on the larger
             * tree. We're replacing a single kid pointer with two kid
             * pointers separated by an element; if that causes the node to
             * overload, we split it in two, move a separator element up to
             * the next node, and repeat.
             */
            if (relht < 0)
            {
                /*
                 * Left tree is shorter. Search down the right tree to find
                 * the pointer we're inserting at.
                 */
                node = root = right;
                while (++relht < 0)
                {
                    node = node.kids[0];
                }
                ki = 0;
                right = node.kids[ki];
            }
            else
            {
                /*
                 * Right tree is shorter; search down the left to find the
                 * pointer we're inserting at.
                 */
                node = root = left;
                while (--relht > 0)
                {
                    if (node.elems[2] != null)
                        node = node.kids[3];
                    else if (node.elems[1] != null)
                        node = node.kids[2];
                    else
                        node = node.kids[1];
                }
                if (node.elems[2] != null)
                    ki = 3;
                else if (node.elems[1] != null)
                    ki = 2;
                else
                    ki = 1;
                left = node.kids[ki];
            }

            /*
             * Now proceed as for addition.
             */
            height = add234_insert(left, sep, right, ref root, node, ki);

            return root;
        }
        private static int height234(Tree234<T> t)
        {
            int level = 0;
            Node234<T> n = t.root;
            while (n != null)
            {
                level++;
                n = n.kids[0];
            }
            return level;
        }
        public Tree234<T> join234(Tree234<T> t2)
        {
            int size2 = countnode234(t2.root);
            if (size2 > 0)
            {
                T element;
                int relht;

                if (this.cmp != null)
                {
                    int temp;
                    element = t2.index234(0);
                    element = findrelpos234(this, element, null, Rel234.REL234_GE, out temp);
                    if (element != null)
                        return null;
                }

                element = t2.delpos234(0);
                relht = height234(this) - height234(t2);
                this.root = join234_internal(this.root, element, t2.root, ref relht);
                t2.root = null;
            }
            return this;
        }
        public Tree234<T> join234r(Tree234<T> t2)
        {
            int size1 = countnode234(this.root);
            if (size1 > 0)
            {
                T element;
                int relht;

                if (t2.cmp != null)
                {
                    int temp;
                    element = this.index234(size1 - 1);
                    element = findrelpos234(t2, element, null, Rel234.REL234_LE, out temp);
                    if (element != null)
                        return null;
                }

                element = this.delpos234(size1 - 1);
                relht = height234(this) - height234(t2);
                t2.root = join234_internal(this.root, element, t2.root, ref relht);
                this.root = null;
            }
            return t2;
        }

        /*
         * Split out the first <index> elements in a tree and return a
         * pointer to the root node. Leave the root node of the remainder
         * in t.
         */
        private static Node234<T> split234_internal(Tree234<T> t, int index)
        {
            Node234<T>[] halves = new Node234<T>[] { null, null };
            Node234<T> n, sib, sub;
            Node234<T> lparent, rparent;
            int ki, pki, i, half, lcount, rcount;

            n = t.root;
            //LOG(("splitting tree %p at point %d\n", t, index));

            /*
             * Easy special cases. After this we have also dealt completely
             * with the empty-tree case and we can assume the root exists.
             */
            if (index == 0)		       /* return nothing */
                return null;
            if (index == countnode234(t.root))
            {   /* return the whole tree */
                Node234<T> ret = t.root;
                t.root = null;
                return ret;
            }

            /*
             * Search down the tree to find the split point.
             */
            halves[0] = halves[1] = null;
            lparent = rparent = null;
            pki = -1;
            while (n != null)
            {
                //LOG(("  node %p: %p/%d \"%s\" %p/%d \"%s\" %p/%d \"%s\" %p/%d index=%d\n",
                //     n,
                //     n.kids[0], n.counts[0], n.elems[0],
                //     n.kids[1], n.counts[1], n.elems[1],
                //     n.kids[2], n.counts[2], n.elems[2],
                //     n.kids[3], n.counts[3],
                //     index));
                lcount = index;
                rcount = countnode234(n) - lcount;
                if (index <= n.counts[0])
                {
                    ki = 0;
                }
                else if ((index -= n.counts[0] + 1) <= n.counts[1])
                {
                    ki = 1;
                }
                else if ((index -= n.counts[1] + 1) <= n.counts[2])
                {
                    ki = 2;
                }
                else
                {
                    index -= n.counts[2] + 1;
                    ki = 3;
                }

                //LOG(("  splitting at subtree %d\n", ki));
                sub = n.kids[ki];

                //LOG(("  splitting at child index %d\n", ki));

                /*
                 * Split the node, put halves[0] on the right of the left
                 * one and halves[1] on the left of the right one, put the
                 * new node pointers in halves[0] and halves[1], and go up
                 * a level.
                 */
                sib = new Node234<T>();
                for (i = 0; i < 3; i++)
                {
                    if (i + ki < 3 && n.elems[i + ki] != null)
                    {
                        sib.elems[i] = n.elems[i + ki];
                        sib.kids[i + 1] = n.kids[i + ki + 1];
                        if (sib.kids[i + 1] != null) sib.kids[i + 1].parent = sib;
                        sib.counts[i + 1] = n.counts[i + ki + 1];
                        n.elems[i + ki] = null;
                        n.kids[i + ki + 1] = null;
                        n.counts[i + ki + 1] = 0;
                    }
                    else
                    {
                        sib.elems[i] = null;
                        sib.kids[i + 1] = null;
                        sib.counts[i + 1] = 0;
                    }
                }
                if (lparent != null)
                {
                    lparent.kids[pki] = n;
                    lparent.counts[pki] = lcount;
                    n.parent = lparent;
                    rparent.kids[0] = sib;
                    rparent.counts[0] = rcount;
                    sib.parent = rparent;
                }
                else
                {
                    halves[0] = n;
                    n.parent = null;
                    halves[1] = sib;
                    sib.parent = null;
                }
                lparent = n;
                rparent = sib;
                pki = ki;
                //LOG(("  left node %p: %p/%d \"%s\" %p/%d \"%s\" %p/%d \"%s\" %p/%d\n",
                //     n,
                //     n.kids[0], n.counts[0], n.elems[0],
                //     n.kids[1], n.counts[1], n.elems[1],
                //     n.kids[2], n.counts[2], n.elems[2],
                //     n.kids[3], n.counts[3]));
                //LOG(("  right node %p: %p/%d \"%s\" %p/%d \"%s\" %p/%d \"%s\" %p/%d\n",
                //     sib,
                //     sib.kids[0], sib.counts[0], sib.elems[0],
                //     sib.kids[1], sib.counts[1], sib.elems[1],
                //     sib.kids[2], sib.counts[2], sib.elems[2],
                //     sib.kids[3], sib.counts[3]));

                n = sub;
            }

            /*
             * We've come off the bottom here, so we've successfully split
             * the tree into two equally high subtrees. The only problem is
             * that some of the nodes down the fault line will be smaller
             * than the minimum permitted size. (Since this is a 2-3-4
             * tree, that means they'll be zero-element one-child nodes.)
             */
            //LOG(("  fell off bottom, lroot is %p, rroot is %p\n",
            // halves[0], halves[1]));
            Debug.Assert(halves[0] != null);
            Debug.Assert(halves[1] != null);
            lparent.counts[pki] = rparent.counts[0] = 0;
            lparent.kids[pki] = rparent.kids[0] = null;

            /*
             * So now we go back down the tree from each of the two roots,
             * fixing up undersize nodes.
             */
            for (half = 0; half < 2; half++)
            {
                /*
                 * Remove the root if it's undersize (it will contain only
                 * one child pointer, so just throw it away and replace it
                 * with its child). This might happen several times.
                 */
                while (halves[half] != null && halves[half].elems[0] == null)
                {
                    //LOG(("  root %p is undersize, throwing away\n", halves[half]));
                    halves[half] = halves[half].kids[0];
                    halves[half].parent = null;
                    //LOG(("  new root is %p\n", halves[half]));
                }

                n = halves[half];
                while (n != null)
                {
                    TowardDelegate<T> toward;
                    int ni, merge;

                    /*
                     * Now we have a potentially undersize node on the
                     * right (if half==0) or left (if half==1). Sort it
                     * out, by merging with a neighbour or by transferring
                     * subtrees over. At this time we must also ensure that
                     * nodes are bigger than minimum, in case we need an
                     * element to merge two nodes below.
                     */
                    //LOG(("  node %p: %p/%d \"%s\" %p/%d \"%s\" %p/%d \"%s\" %p/%d\n",
                    // n,
                    // n.kids[0], n.counts[0], n.elems[0],
                    // n.kids[1], n.counts[1], n.elems[1],
                    // n.kids[2], n.counts[2], n.elems[2],
                    // n.kids[3], n.counts[3]));
                    if (half == 1)
                    {
                        ki = 0;		       /* the kid we're interested in */
                        ni = 1;		       /* the neighbour */
                        merge = 0;	       /* for merge: leftmost of the two */
                        toward = trans234_subtree_left;
                    }
                    else
                    {
                        ki = (n.kids[3] != null ? 3 : n.kids[2] != null ? 2 : 1);
                        ni = ki - 1;
                        merge = ni;
                        toward = trans234_subtree_right;
                    }

                    sub = n.kids[ki];
                    if (sub != null && sub.elems[1] == null)
                    {
                        /*
                         * This node is undersized or minimum-size. If we
                         * can merge it with its neighbour, we do so;
                         * otherwise we must be able to transfer subtrees
                         * over to it until it is greater than minimum
                         * size.
                         */
                        bool undersized = (sub.elems[0] == null);
                        //LOG(("  child %d is %ssize\n", ki,
                        //     undersized ? "under" : "minimum-"));
                        //LOG(("  neighbour is %s\n",
                        //     n.kids[ni].elems[2] ? "large" :
                        //     n.kids[ni].elems[1] ? "medium" : "small"));
                        if (n.kids[ni].elems[1] == null ||
                            (undersized && n.kids[ni].elems[2] == null))
                        {
                            /*
                             * Neighbour is small, or possibly neighbour is
                             * medium and we are undersize.
                             */
                            int tempKi = -1, tempIndex = -1;
                            trans234_subtree_merge(n, merge, ref tempKi, ref tempIndex);
                            sub = n.kids[merge];
                            if (n.elems[0] == null)
                            {
                                /*
                                 * n is empty, and hence must have been the
                                 * root and needs to be removed.
                                 */
                                Debug.Assert(n.parent == null);
                                //LOG(("  shifting root!\n"));
                                halves[half] = sub;
                                halves[half].parent = null;
                                //sfree(n);
                            }
                        }
                        else
                        {
                            int tempKi = -1, tempIndex = -1;
                            /* Neighbour is big enough to move trees over. */
                            toward(n, ni, ref tempKi, ref tempIndex);
                            if (undersized)
                                toward(n, ni, ref tempKi, ref tempIndex);
                        }
                    }
                    n = sub;
                }
            }

            t.root = halves[1];
            return halves[0];
        }
        public Tree234<T> splitpos234(int index, bool before)
        {
            Tree234<T> ret;
            Node234<T> n;
            int count;

            count = countnode234(this.root);
            if (index < 0 || index > count)
                return null;		       /* error */
            ret = new Tree234<T>(this.cmp);
            n = split234_internal(this, index);
            if (before)
            {
                /* We want to return the ones before the index. */
                ret.root = n;
            }
            else
            {
                /*
                 * We want to keep the ones before the index and return the
                 * ones after.
                 */
                ret.root = this.root;
                this.root = n;
            }
            return ret;
        }
        private static Tree234<T> split234(Tree234<T> t, T e, Func<T, T, int> cmp, Rel234 rel)
        {
            bool before;
            int index;

            Debug.Assert(rel != Rel234.REL234_EQ);

            if (rel == Rel234.REL234_GT || rel == Rel234.REL234_GE)
            {
                before = true;
                rel = (rel == Rel234.REL234_GT ? Rel234.REL234_LE : Rel234.REL234_LT);
            }
            else
            {
                before = false;
            }
            if (findrelpos234(t, e, cmp, rel, out index) == null)
                index = 0;

            return t.splitpos234(index + 1, before);
        }

        private static Node234<T> copynode234<TState>(Node234<T> n, Func<TState, T, T> copyfn, TState copyfnstate)
        {
            int i;
            Node234<T> n2 = new Node234<T>();

            for (i = 0; i < 3; i++)
            {
                if (n.elems[i] != null && copyfn != null)
                    n2.elems[i] = copyfn(copyfnstate, n.elems[i]);
                else
                    n2.elems[i] = n.elems[i];
            }

            for (i = 0; i < 4; i++)
            {
                if (n.kids[i] != null)
                {
                    n2.kids[i] = copynode234(n.kids[i], copyfn, copyfnstate);
                    n2.kids[i].parent = n2;
                }
                else
                {
                    n2.kids[i] = null;
                }
                n2.counts[i] = n.counts[i];
            }

            return n2;
        }
        public Tree234<T> copytree234<TState>(Func<TState, T, T> copyfn, TState copyfnstate)
        {
            Tree234<T> t2;

            t2 = new Tree234<T>(this.cmp);
            if (this.root != null)
            {
                t2.root = copynode234(this.root, copyfn, copyfnstate);
                t2.root.parent = null;
            }
            else
                t2.root = null;

            return t2;
        }
    }
}
