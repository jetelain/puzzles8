using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Slant
{
    /*
 * Scratch space for solver.
 */
internal class SlantSolverScratch  {
    /*
     * Disjoint set forest which tracks the connected sets of
     * points.
     */
    internal int[] connected;

    /*
     * Counts the number of possible exits from each connected set
     * of points. (That is, the number of possible _simultaneous_
     * exits: an unconnected point labelled 2 has an exit count of
     * 2 even if all four possible edges are still under
     * consideration.)
     */
    internal int[] exits;

    /*
     * Tracks whether each connected set of points includes a
     * border point.
     */
    internal bool[] border;

    /*
     * Another disjoint set forest. This one tracks _squares_ which
     * are known to slant in the same direction.
     */
    internal int[] equiv;

    /*
     * Stores slash values which we know for an equivalence class.
     * When we fill in a square, we set slashval[canonify(x)] to
     * the same value as soln[x], so that we can then spot other
     * squares equivalent to it and fill them in immediately via
     * their known equivalence.
     */
    internal sbyte[] slashval;

    /*
     * Stores possible v-shapes. This array is w by h in size, but
     * not every bit of every entry is meaningful. The bits mean:
     * 
     *  - bit 0 for a square means that that square and the one to
     *    its right might form a v-shape between them
     *  - bit 1 for a square means that that square and the one to
     *    its right might form a ^-shape between them
     *  - bit 2 for a square means that that square and the one
     *    below it might form a >-shape between them
     *  - bit 3 for a square means that that square and the one
     *    below it might form a <-shape between them
     * 
     * Any starting 1 or 3 clue rules out four bits in this array
     * immediately; a 2 clue propagates any ruled-out bit past it
     * (if the two squares on one side of a 2 cannot be a v-shape,
     * then neither can the two on the other side be the same
     * v-shape); we can rule out further bits during play using
     * partially filled 2 clues; whenever a pair of squares is
     * known not to be _either_ kind of v-shape, we can mark them
     * as equivalent.
     */
    internal byte[]  vbitmap;

    /*
     * Useful to have this information automatically passed to
     * solver subroutines. (This pointer is not dynamically
     * allocated by new_scratch and free_scratch.)
     */
    internal sbyte[] clues;
};

}
