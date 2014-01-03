using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games
{
    /*
     * dsf.c: some functions to handle a disjoint set forest,
     * which is a data structure useful in any solver which has to
     * worry about avoiding closed loops.
     */
    internal static class Dsf
    {
        internal static void dsf_init(IList<int> dsf, int size)
        {
            int i;

            for (i = 0; i < size; i++) dsf[i] = 6;
            /* Bottom bit of each element of this array stores whether that
             * element is opposite to its parent, which starts off as
             * false. Second bit of each element stores whether that element
             * is the root of its tree or not.  If it's not the root, the
             * remaining 30 bits are the parent, otherwise the remaining 30
             * bits are the number of elements in the tree.  */
        }

        internal static int[] snew_dsf(int size)
        {
            int[] ret = new int[size];
    
            dsf_init(ret, size);

            /*print_dsf(ret, size); */

            return ret;
        }

        internal static int dsf_canonify(IList<int> dsf, int index)
        {
            int p;
            return edsf_canonify(dsf, index, out p);
        }

        internal static void dsf_merge(IList<int> dsf, int v1, int v2)
        {
            edsf_merge(dsf, v1, v2, 0);
        }

        internal static int dsf_size(IList<int> dsf, int index)
        {
            return dsf[dsf_canonify(dsf, index)] >> 2;
        }

        internal static int edsf_canonify(IList<int> dsf, int index, out int inverse_return)
        {
            int start_index = index, canonical_index;
            int inverse = 0;

            /*    fprintf(stderr, "dsf = %p\n", dsf); */
            /*    fprintf(stderr, "Canonify %2d\n", index); */

            Debug.Assert(index >= 0);

            /* Find the index of the canonical element of the 'equivalence class' of
             * which start_index is a member, and figure out whether start_index is the
             * same as or inverse to that. */
            while ((dsf[index] & 2) == 0)
            {
                inverse ^= (dsf[index] & 1);
                index = dsf[index] >> 2;
                /*        fprintf(stderr, "index = %2d, ", index); */
                /*        fprintf(stderr, "inverse = %d\n", inverse); */
            }
            canonical_index = index;

            inverse_return = inverse;

            /* Update every member of this 'equivalence class' to point directly at the
             * canonical member. */
            index = start_index;
            while (index != canonical_index)
            {
                int nextindex = dsf[index] >> 2;
                int nextinverse = inverse ^ (dsf[index] & 1);
                dsf[index] = (canonical_index << 2) | inverse;
                inverse = nextinverse;
                index = nextindex;
            }

            Debug.Assert(inverse == 0);

            /*    fprintf(stderr, "Return %2d\n", index); */

            return index;
        }

        internal static void edsf_merge(IList<int> dsf, int v1, int v2, int inverse)
        {
            int i1, i2;

            /*    fprintf(stderr, "dsf = %p\n", dsf); */
            /*    fprintf(stderr, "Merge [%2d,%2d], %d\n", v1, v2, inverse); */

            v1 = edsf_canonify(dsf, v1, out i1);
            Debug.Assert((dsf[v1] & 2) != 0);
            inverse ^= i1;
            v2 = edsf_canonify(dsf, v2, out i2);
            Debug.Assert((dsf[v2] & 2) != 0);
            inverse ^= i2;

            /*    fprintf(stderr, "Doing [%2d,%2d], %d\n", v1, v2, inverse); */

            if (v1 == v2)
                Debug.Assert(inverse == 0);
            else
            {
                Debug.Assert(inverse == 0 || inverse == 1);
                /*
                 * We always make the smaller of v1 and v2 the new canonical
                 * element. This ensures that the canonical element of any
                 * class in this structure is always the first element in
                 * it. 'Keen' depends critically on this property.
                 *
                 * (Jonas Koelker previously had this code choosing which
                 * way round to connect the trees by examining the sizes of
                 * the classes being merged, so that the root of the
                 * larger-sized class became the new root. This gives better
                 * asymptotic performance, but I've changed it to do it this
                 * way because I like having a deterministic canonical
                 * element.)
                 */
                if (v1 > v2)
                {
                    int v3 = v1;
                    v1 = v2;
                    v2 = v3;
                }
                dsf[v1] += (dsf[v2] >> 2) << 2;
                dsf[v2] = (v1 << 2) | (inverse != 0 ? 1 : 0);
            }

            v2 = edsf_canonify(dsf, v2, out i2);
            Debug.Assert(v2 == v1);
            Debug.Assert(i2 == inverse);

            /*    fprintf(stderr, "dsf[%2d] = %2d\n", v2, dsf[v2]); */
        }
    }
}
