using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using PuzzleCollection.Games.Structures;
using System;
using System.Diagnostics;
using System.Linq;

namespace PuzzleCollection.Test.Structures
{
    [TestClass]
    public class Tree234Test
    {
        /*
         * Error reporting function.
         */
        void error(string fmt, params object[] args)
        {
            Assert.Fail(string.Format(fmt, args));
        }

        /* The array representation of the data. */
        string[] array;
        int arraylen, arraysize;
        Func<string, string, int> cmp;

        /* The tree representation of the same data. */
        Tree234<string> tree;

        /*
         * Routines to provide a diagnostic printout of a tree. Currently
         * relies on every element in the tree being a one-character string
         * :-)
         */
        //class dispctx {
        //    internal StringBuilder[] levels;
        //} 

        //void dispnode(Node234<string> n, int level, dispctx ctx) {
        //    if (level == 0) {
        //    //int xpos =ctx.levels[0].Length;
        //    //int len;

        //    if (n.elems[2] != null)
        //        ctx.levels[0].AppendFormat(" {0}{1}{2}",
        //              n.elems[0], n.elems[1], n.elems[2]);
        //    else if (n.elems[1] != null)
        //        ctx.levels[0].AppendFormat(" {0}{1}",
        //              n.elems[0], n.elems[1]);
        //    else
        //        ctx.levels[0].AppendFormat(" {0}",
        //              n.elems[0]);
        //    //return xpos + 1 + (len-1) / 2;
        //    } else {
        //    //int[] xpos = new int[4];
        //    int nodelen, mypos, myleft, x, i, nkids;

        //    dispnode(n.kids[0], level-3, ctx);
        //    dispnode(n.kids[1], level-3, ctx);
        //    nkids = 2;
        //    if (n.kids[2] != null)
        //    {
        //        dispnode(n.kids[2], level-3, ctx);
        //        nkids = 3;
        //    }
        //    if (n.kids[3] != null)
        //    {
        //        dispnode(n.kids[3], level-3, ctx);
        //        nkids = 4;
        //    }

        //    if (nkids == 4)
        //        mypos = (xpos[1] + xpos[2]) / 2;
        //    else if (nkids == 3)
        //        mypos = xpos[1];
        //    else
        //        mypos = (xpos[0] + xpos[1]) / 2;
        //    nodelen = nkids * 2 - 1;
        //    myleft = mypos - ((nodelen-1)/2);
        //    Assert.IsTrue(myleft >= xpos[0]);
        //    Assert.IsTrue(myleft + nodelen-1 <= xpos[nkids-1]);

        //    x = ctx.levels[level].Length;
        //    while (x <= xpos[0] && x < myleft)
        //        ctx.levels[level].Append(' ');
        //    while (x < myleft)
        //        ctx.levels[level].Append('_');
        //    if (nkids==4)
        //        x += sprintf(ctx.levels[level]+x, ".%s.%s.%s.",
        //             n.elems[0], n.elems[1], n.elems[2]);
        //    else if (nkids==3)
        //        x += sprintf(ctx.levels[level]+x, ".%s.%s.",
        //             n.elems[0], n.elems[1]);
        //    else
        //        x += sprintf(ctx.levels[level]+x, ".%s.",
        //             n.elems[0]);
        //    while (x < xpos[nkids-1])
        //        ctx.levels[level][x++] = '_';
        //    ctx.levels[level][x] = '\0';

        //    x = ctx.levels[level-1].Length;
        //    for (i = 0; i < nkids; i++) {
        //        int rpos, pos;
        //        rpos = xpos[i];
        //        if (i > 0 && i < nkids-1)
        //        pos = myleft + 2*i;
        //        else
        //        pos = rpos;
        //        if (rpos < pos)
        //        rpos++;
        //        while (x < pos && x < rpos)
        //        ctx.levels[level-1][x++] = ' ';
        //        if (x == pos)
        //        ctx.levels[level-1][x++] = '|';
        //        while (x < pos || x < rpos)
        //        ctx.levels[level-1][x++] = '_';
        //        if (x == pos)
        //        ctx.levels[level-1][x++] = '|';
        //    }
        //    ctx.levels[level-1][x] = '\0';

        //    x = ctx.levels[level-2].Length;
        //    for (i = 0; i < nkids; i++) {
        //        int rpos = xpos[i];

        //        while (x < rpos)
        //        ctx.levels[level-2][x++] = ' ';
        //        ctx.levels[level-2][x++] = '|';
        //    }
        //    ctx.levels[level-2][x] = '\0';


        //    }
        //}

        void disptree(Tree234<string> t)
        {
            //dispctx ctx;
            //string leveldata;
            //int width = t.count234();
            //int ht = t.height234() * 3 - 2;
            //int i;

            //if (t.root==null) {
            //Debug.WriteLine("[empty tree]\n");
            //}

            //leveldata = smalloc(ht * (width+2));
            //ctx.levels = smalloc(ht * sizeof(char *));
            //for (i = 0; i < ht; i++) {
            //ctx.levels[i] = leveldata + i * (width+2);
            //ctx.levels[i][0] = '\0';
            //}

            //dispnode(t.root, ht-1, &ctx);

            //for (i = ht; i-- != 0 ;)
            //Debug.WriteLine("%s\n", ctx.levels[i]);
        }

        class chkctx
        {
            internal int treedepth;
            internal int elemcount;
        } ;

        int chknode(chkctx ctx, int level, Node234<string> node,
                string lowbound, string highbound)
        {
            int nkids, nelems;
            int i;
            int count;

            /* Count the non-null kids. */
            for (nkids = 0; nkids < 4 && node.kids[nkids] != null; nkids++) ;
            /* Ensure no kids beyond the first null are non-null. */
            for (i = nkids; i < 4; i++)
                if (node.kids[i] != null)
                {
                    error("node %p: nkids=%d but kids[%d] non-null",
                           node, nkids, i);
                }
                else if (node.counts[i] != 0)
                {
                    error("node %p: kids[%d] null but count[%d]=%d nonzero",
                           node, i, i, node.counts[i]);
                }

            /* Count the non-null elements. */
            for (nelems = 0; nelems < 3 && node.elems[nelems] != null; nelems++) ;
            /* Ensure no elements beyond the first null are non-null. */
            for (i = nelems; i < 3; i++)
                if (node.elems[i] != null)
                {
                    error("node %p: nelems=%d but elems[%d] non-null",
                           node, nelems, i);
                }

            if (nkids == 0)
            {
                /*
                 * If nkids==0, this is a leaf node; verify that the tree
                 * depth is the same everywhere.
                 */
                if (ctx.treedepth < 0)
                    ctx.treedepth = level;    /* we didn't know the depth yet */
                else if (ctx.treedepth != level)
                    error("node %p: leaf at depth %d, previously seen depth %d",
                           node, level, ctx.treedepth);
            }
            else
            {
                /*
                 * If nkids != 0, then it should be nelems+1, unless nelems
                 * is 0 in which case nkids should also be 0 (and so we
                 * shouldn't be in this condition at all).
                 */
                int shouldkids = (nelems != 0 ? nelems + 1 : 0);
                if (nkids != shouldkids)
                {
                    error("node %p: %d elems should mean %d kids but has %d",
                           node, nelems, shouldkids, nkids);
                }
            }

            /*
             * nelems should be at least 1.
             */
            if (nelems == 0)
            {
                error("node %p: no elems", node, nkids);
            }

            /*
             * Add nelems to the running element count of the whole tree.
             */
            ctx.elemcount += nelems;

            /*
             * Check ordering property: all elements should be strictly >
             * lowbound, strictly < highbound, and strictly < each other in
             * sequence. (lowbound and highbound are null at edges of tree
             * - both null at root node - and null is considered to be <
             * everything and > everything. IYSWIM.)
             */
            if (cmp != null)
            {
                for (i = -1; i < nelems; i++)
                {
                    string lower = (i == -1 ? lowbound : node.elems[i]);
                    string higher = (i + 1 == nelems ? highbound : node.elems[i + 1]);
                    if (lower != null && higher != null && cmp(lower, higher) >= 0)
                    {
                        error("node %p: kid comparison [%d=%s,%d=%s] failed",
                              node, i, lower, i + 1, higher);
                    }
                }
            }

            /*
             * Check parent pointers: all non-null kids should have a
             * parent pointer coming back to this node.
             */
            for (i = 0; i < nkids; i++)
                if (node.kids[i].parent != node)
                {
                    error("node %p kid %d: parent ptr is %p not %p",
                           node, i, node.kids[i].parent, node);
                }


            /*
             * Now (finally!) recurse into subtrees.
             */
            count = nelems;

            for (i = 0; i < nkids; i++)
            {
                string lower = (i == 0 ? lowbound : node.elems[i - 1]);
                string higher = (i >= nelems ? highbound : node.elems[i]);
                int subcount = chknode(ctx, level + 1, node.kids[i], lower, higher);
                if (node.counts[i] != subcount)
                {
                    error("node %p kid %d: count says %d, subtree really has %d",
                      node, i, node.counts[i], subcount);
                }
                count += subcount;
            }

            return count;
        }

        void verifytree(Tree234<string> tree, string[] array, int arraylen)
        {
            chkctx ctx = new chkctx();
            int i;
            string p;

            ctx.treedepth = -1;                /* depth unknown yet */
            ctx.elemcount = 0;                 /* no elements seen yet */
            /*
             * Verify validity of tree properties.
             */
            if (tree.root != null)
            {
                if (tree.root.parent != null)
                    error("root.parent is %p should be null", tree.root.parent);
                chknode(ctx, 0, tree.root, null, null);
            }
            Debug.WriteLine("tree depth: %d\n", ctx.treedepth);
            /*
             * Enumerate the tree and ensure it matches up to the array.
             */
            for (i = 0; null != (p = tree.index234(i)); i++)
            {
                if (i >= arraylen)
                    error("tree contains more than %d elements", arraylen);
                if (array[i] != p)
                    error("enum at position %d: array says %s, tree says %s",
                           i, array[i], p);
            }
            if (ctx.elemcount != i)
            {
                error("tree really contains %d elements, enum gave %d",
                       ctx.elemcount, i);
            }
            if (i < arraylen)
            {
                error("enum gave only %d elements, array has %d", i, arraylen);
            }
            i = tree.count234();
            if (ctx.elemcount != i)
            {
                error("tree really contains %d elements, count234 gave %d",
                  ctx.elemcount, i);
            }
        }
        void verify() { verifytree(tree, array, arraylen); }

        void internal_addtest(string elem, int index, string realret)
        {
            int i, j;
            string retval;

            if (arraysize < arraylen + 1)
            {
                arraysize = arraylen + 1 + 256;
                if (array == null)
                {
                    array = new string[arraysize];
                }
                else
                {
                    Array.Resize(ref array, arraysize);
                }
            }

            i = index;
            /* now i points to the first element >= elem */
            retval = elem;                  /* expect elem returned (success) */
            for (j = arraylen; j > i; j--)
                array[j] = array[j - 1];
            array[i] = elem;                /* add elem to array */
            arraylen++;

            if (realret != retval)
            {
                error("add: retval was %p expected %p", realret, retval);
            }

            verify();
        }

        void addtest(string elem)
        {
            int i;
            string realret;

            realret = tree.add234(elem);

            i = 0;
            while (i < arraylen && cmp(elem, array[i]) > 0)
                i++;
            if (i < arraylen && cmp(elem, array[i]) == 0)
            {
                string retval = array[i];       /* expect that returned not elem */
                if (realret != retval)
                {
                    error("add: retval was %p expected %p", realret, retval);
                }
            }
            else
                internal_addtest(elem, i, realret);
        }

        void addpostest(string elem, int i)
        {
            string realret;

            realret = tree.addpos234(elem, i);

            internal_addtest(elem, i, realret);
        }

        void delpostest(int i)
        {
            int index = i;
            string elem = array[i], ret;

            /* i points to the right element */
            while (i < arraylen - 1)
            {
                array[i] = array[i + 1];
                i++;
            }
            arraylen--;			       /* delete elem from array */

            if (tree.cmp != null)
                ret = tree.del234(elem);
            else
                ret = tree.delpos234(index);

            if (ret != elem)
            {
                error("del returned %p, expected %p", ret, elem);
            }

            verify();
        }

        void deltest(string elem)
        {
            int i;

            i = 0;
            while (i < arraylen && cmp(elem, array[i]) > 0)
                i++;
            if (i >= arraylen || cmp(elem, array[i]) != 0)
                return;                        /* don't do it! */
            delpostest(i);
        }

        /* A sample data set and test utility. Designed for pseudo-randomness,
         * and yet repeatability. */

        /*
         * This random number generator uses the `portable implementation'
         * given in ANSI C99 draft N869. It assumes `unsigned' is 32 bits;
         * change it if not.
         */
        int randomnumber(ref uint seed)
        {
            seed *= 1103515245;
            seed += 12345;
            return (int)((seed) / 65536) % 32768;
        }

        //int mycmp(object av, object bv) {
        //    char const *a = (char const *)av;
        //    char const *b = (char const *)bv;
        //    return strcmp(a, b);
        //}

        string[] strings = new[]{
    "0", "2", "3", "I", "K", "d", "H", "J", "Q", "N", "n", "q", "j", "i",
    "7", "G", "F", "D", "b", "x", "g", "B", "e", "v", "V", "T", "f", "E",
    "S", "8", "A", "k", "X", "p", "C", "R", "a", "o", "r", "O", "Z", "u",
    "6", "1", "w", "L", "P", "M", "c", "U", "h", "9", "t", "5", "W", "Y",
    "m", "s", "l", "4",
#if false
    "a", "ab", "absque", "coram", "de",
    "palam", "clam", "cum", "ex", "e",
    "sine", "tenus", "pro", "prae",
    "banana", "carrot", "cabbage", "broccoli", "onion", "zebra",
    "penguin", "blancmange", "pangolin", "whale", "hedgehog",
    "giraffe", "peanut", "bungee", "foo", "bar", "baz", "quux",
    "murfl", "spoo", "breen", "flarn", "octothorpe",
    "snail", "tiger", "elephant", "octopus", "warthog", "armadillo",
    "aardvark", "wyvern", "dragon", "elf", "dwarf", "orc", "goblin",
    "pixie", "basilisk", "warg", "ape", "lizard", "newt", "shopkeeper",
    "wand", "ring", "amulet"
#endif
};


        void findtest()
        {
            Rel234[] rels = new Rel234[]{
	Rel234.REL234_EQ, Rel234.REL234_GE, Rel234.REL234_LE, Rel234.REL234_LT, Rel234.REL234_GT
    };
            string[] relnames = new[]{
	"EQ", "GE", "LE", "LT", "GT"
    };
            Rel234 rel;
            int i, j, index;
            string p, ret, realret, realret2;
            int lo, hi, mid = 0, c = 0;

            for (i = 0; i < (int)strings.Length; i++)
            {
                p = strings[i];
                for (j = 0; j < rels.Length; j++)
                {
                    rel = rels[j];

                    lo = 0; hi = arraylen - 1;
                    while (lo <= hi)
                    {
                        mid = (lo + hi) / 2;
                        c = string.CompareOrdinal(p, array[mid]);
                        if (c < 0)
                            hi = mid - 1;
                        else if (c > 0)
                            lo = mid + 1;
                        else
                            break;
                    }

                    if (c == 0)
                    {
                        if (rel == Rel234.REL234_LT)
                            ret = (mid > 0 ? array[--mid] : null);
                        else if (rel == Rel234.REL234_GT)
                            ret = (mid < arraylen - 1 ? array[++mid] : null);
                        else
                            ret = array[mid];
                    }
                    else
                    {
                        Assert.IsTrue(lo == hi + 1);
                        if (rel == Rel234.REL234_LT || rel == Rel234.REL234_LE)
                        {
                            mid = hi;
                            ret = (hi >= 0 ? array[hi] : null);
                        }
                        else if (rel == Rel234.REL234_GT || rel == Rel234.REL234_GE)
                        {
                            mid = lo;
                            ret = (lo < arraylen ? array[lo] : null);
                        }
                        else
                            ret = null;
                    }

                    realret = tree.findrelpos234(p, null, rel, out index);
                    if (realret != ret)
                    {
                        error("find(\"%s\",%s) gave %s should be %s",
                              p, relnames[j], realret, ret);
                    }
                    if (realret != null && index != mid)
                    {
                        error("find(\"%s\",%s) gave %d should be %d",
                              p, relnames[j], index, mid);
                    }
                    if (realret != null && rel == Rel234.REL234_EQ)
                    {
                        realret2 = tree.index234(index);
                        if (realret2 != realret)
                        {
                            error("find(\"%s\",%s) gave %s(%d) but %d . %s",
                              p, relnames[j], realret, index, index, realret2);
                        }
                    }
#if false
	    Debug.WriteLine("find(\"%s\",%s) gave %s(%d)\n", p, relnames[j],
		   realret, index);
#endif
                }
            }

            realret = tree.findrelpos234(null, null, Rel234.REL234_GT, out index);
            if (arraylen != 0 && (realret != array[0] || index != 0))
            {
                error("find(null,GT) gave %s(%d) should be %s(0)",
                      realret, index, array[0]);
            }
            else if (arraylen == 0 && (realret != null))
            {
                error("find(null,GT) gave %s(%d) should be null",
                      realret, index);
            }

            realret = tree.findrelpos234(null, null, Rel234.REL234_LT, out index);
            if (arraylen != 0 && (realret != array[arraylen - 1] || index != arraylen - 1))
            {
                error("find(null,LT) gave %s(%d) should be %s(0)",
                      realret, index, array[arraylen - 1]);
            }
            else if (arraylen == 0 && (realret != null))
            {
                error("find(null,LT) gave %s(%d) should be null",
                      realret, index);
            }
        }

        void splittest(Tree234<string> tree, string[] array, int arraylen)
        {
            int i;
            Tree234<string> tree3, tree4;
            for (i = 0; i <= arraylen; i++)
            {
                tree3 = tree.copytree234<object>(null, null);
                tree4 = tree3.splitpos234(i, false);
                verifytree(tree3, array, i);
                verifytree(tree4, array.Skip(i).ToArray(), arraylen - i);
                tree3.join234(tree4);
                verifytree(tree3, array, arraylen);
            }
        }


        [TestMethod]
        public void Tree234_MainTest()
        {
            int[] @in = new int[strings.Length];
            int i, j, k;
            int tmplen;
            bool tworoot;
            uint seed = 0;
            Tree234<string> tree2, tree3, tree4;

            for (i = 0; i < (int)strings.Length; i++) @in[i] = 0;
            array = null;
            arraylen = arraysize = 0;
            tree = new Tree234<string>(string.CompareOrdinal);
            cmp = string.CompareOrdinal;

            verify();
            for (i = 0; i < 10000; i++)
            {
                j = randomnumber(ref seed);
                j %= strings.Length;
                Debug.WriteLine("trial: {0}", i);
                if (@in[j] != 0)
                {
                    Debug.WriteLine("deleting {0} ({1})", strings[j], j);
                    deltest(strings[j]);
                    @in[j] = 0;
                }
                else
                {
                    Debug.WriteLine("adding {0} ({1})", strings[j], j);
                    addtest(strings[j]);
                    @in[j] = 1;
                }
                disptree(tree);
                findtest();
            }

            while (arraylen > 0)
            {
                j = randomnumber(ref seed);
                j %= arraylen;
                deltest(array[j]);
            }


            /*
             * Now try an unsorted tree. We don't really need to test
             * delpos234 because we know del234 is based on it, so it's
             * already been tested in the above sorted-tree code; but for
             * completeness we'll use it to tear down our unsorted tree
             * once we've built it.
             */
            tree = new Tree234<string>(null);
            cmp = null;
            verify();
            for (i = 0; i < 1000; i++)
            {
                Debug.WriteLine("trial: {0}", i);
                j = randomnumber(ref seed);
                j %= strings.Length;
                k = randomnumber(ref seed);
                k %= tree.count234() + 1;
                Debug.WriteLine("adding string %s at index %d\n", strings[j], k);
                addpostest(strings[j], k);
            }

            /*
             * While we have this tree in its full form, we'll take a copy
             * of it to use in split and join testing.
             */
            tree2 = tree.copytree234<object>(null, null);
            verifytree(tree2, array, arraylen);/* check the copy is accurate */
            /*
             * Split tests. Split the tree at every possible point and
             * check the resulting subtrees.
             */
            tworoot = (tree2.root.elems[1] == null);/* see if it has a 2-root */
            splittest(tree2, array, arraylen);
            /*
             * Now do the split test again, but on a tree that has a 2-root
             * (if the previous one didn't) or doesn't (if the previous one
             * did).
             */
            tmplen = arraylen;
            while ((tree2.root.elems[1] == null) == tworoot)
            {
                tree2.delpos234(--tmplen);
            }
            Debug.WriteLine("now trying splits on second tree\n");
            splittest(tree2, array, tmplen);

            /*
             * Back to the main testing of uncounted trees.
             */
            while (tree.count234() > 0)
            {
                Debug.WriteLine("cleanup: tree size %d\n", tree.count234());
                j = randomnumber(ref seed);
                j %= tree.count234();
                Debug.WriteLine("deleting string %s from index %d\n", array[j], j);
                delpostest(j);
            }

            /*
             * Finally, do some testing on split/join on _sorted_ trees. At
             * the same time, we'll be testing split on very small trees.
             */
            tree = new Tree234<string>(string.CompareOrdinal);
            cmp = string.CompareOrdinal;
            arraylen = 0;
            for (i = 0; i < 17; i++)
            {
                tree2 = tree.copytree234<object>(null, null);
                splittest(tree2, array, arraylen);
                if (i < 16)
                    addtest(strings[i]);
            }

            /*
             * Test silly cases of join: join(emptytree, emptytree), and
             * also ensure join correctly spots when sorted trees fail the
             * ordering constraint.
             */
            tree = new Tree234<string>(string.CompareOrdinal);
            tree2 = new Tree234<string>(string.CompareOrdinal);
            tree3 = new Tree234<string>(string.CompareOrdinal);
            tree4 = new Tree234<string>(string.CompareOrdinal);
            Assert.IsTrue(string.CompareOrdinal(strings[0], strings[1]) < 0);   /* just in case :-) */
            tree2.add234(strings[1]);
            tree4.add234(strings[0]);
            array[0] = strings[0];
            array[1] = strings[1];
            verifytree(tree, array, 0);
            verifytree(tree2, array.Skip(1).ToArray(), 1);
            verifytree(tree3, array, 0);
            verifytree(tree4, array, 1);

            /*
             * So:
             *  - join(tree,tree3) should leave both tree and tree3 unchanged.
             *  - joinr(tree,tree2) should leave both tree and tree2 unchanged.
             *  - join(tree4,tree3) should leave both tree3 and tree4 unchanged.
             *  - join(tree, tree2) should move the element from tree2 to tree.
             *  - joinr(tree4, tree3) should move the element from tree4 to tree3.
             *  - join(tree,tree3) should return null and leave both unchanged.
             *  - join(tree3,tree) should work and create a bigger tree in tree3.
             */
            Assert.IsTrue(tree == tree.join234(tree3));
            verifytree(tree, array, 0);
            verifytree(tree3, array, 0);
            Assert.IsTrue(tree2 == tree.join234r(tree2));
            verifytree(tree, array, 0);
            verifytree(tree2, array.Skip(1).ToArray(), 1);
            Assert.IsTrue(tree4 == tree4.join234(tree3));
            verifytree(tree3, array, 0);
            verifytree(tree4, array, 1);
            Assert.IsTrue(tree == tree.join234(tree2));
            verifytree(tree, array.Skip(1).ToArray(), 1);
            verifytree(tree2, array, 0);
            Assert.IsTrue(tree3 == tree4.join234r(tree3));
            verifytree(tree3, array, 1);
            verifytree(tree4, array, 0);
            Assert.IsTrue(null == tree.join234(tree3));
            verifytree(tree, array.Skip(1).ToArray(), 1);
            verifytree(tree3, array, 1);
            Assert.IsTrue(tree3 == tree3.join234(tree));
            verifytree(tree3, array, 2);
            verifytree(tree, array, 0);
        }
    }
}
