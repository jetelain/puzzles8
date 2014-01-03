using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Map
{
    public sealed class MapGame : GameBase<MapSettings, MapState, MapMove, MapDrawState, MapUI>
    {
        const int FOUR=4;
        const int THREE=(FOUR-1);
        const int FIVE=(FOUR+1);
        const int SIX=(FOUR+2);

        const int TE=0;
        const int BE=1; 
        const int LE=2; 
        const int RE=3;               /* top/bottom/left/right edges */

        const int COL_BACKGROUND=0;
        const int COL_GRID=1;
        const int COL_0=2; 
        const int COL_1=3;
        const int COL_2=4; 
        const int COL_3=5;
        const int COL_ERROR=6;
        const int COL_ERRTEXT=7;
        const int NCOLOURS=8;

        internal const int DIFF_EASY = 0;
        internal const int DIFF_NORMAL = 1;
        internal const int DIFF_HARD = 2;
        internal const int DIFF_RECURSE = 3;
        internal const int DIFFCOUNT = 4;

        private static MapSettings[] presets = new[] 
        {
            new MapSettings( w:20, h:15, n:30, diff:DIFF_EASY ),
            new MapSettings( w:20, h:15, n:30, diff:DIFF_NORMAL ),
            new MapSettings( w:20, h:15, n:30, diff:DIFF_HARD ),
            new MapSettings( w:20, h:15, n:30, diff:DIFF_RECURSE ),

            new MapSettings( w:30, h:25, n:75, diff:DIFF_NORMAL ),
            new MapSettings( w:30, h:25, n:75, diff:DIFF_HARD )
        };

        public override MapSettings DefaultSettings
        {
            get { return presets[1]; }
        }

        public override IEnumerable<MapSettings> PresetsSettings
        {
            get { return presets; }
        }

        public override MapMove ParseMove(MapSettings settings, string moveString)
        {
            return MapMove.Parse(settings, moveString);
        }

        public override MapSettings ParseSettings(string settingsString)
        {
            return MapSettings.Parse(settingsString);
        }
        /* ----------------------------------------------------------------------
         * Cumulative frequency table functions.
         */

        /*
         * Initialise a cumulative frequency table. (Hardly worth writing
         * this function; all it does is to initialise everything in the
         * array to zero.)
         */
        static void cf_init(int[] table, int n)
        {
            int i;

            for (i = 0; i < n; i++)
                table[i] = 0;
        }

        /*
         * Increment the count of symbol `sym' by `count'.
         */
        static void cf_add(int[] table, int n, int sym, int count)
        {
            int bit;

            bit = 1;
            while (sym != 0) {
                if ((sym & bit) != 0) {
                    table[sym] += count;
                    sym &= ~bit;
                }
                bit <<= 1;
            }

            table[0] += count;
        }

        /*
         * Cumulative frequency lookup: return the total count of symbols
         * with value less than `sym'.
         */
        static int cf_clookup(int[] table, int n, int sym)
        {
            int bit, index, limit, count;

            if (sym == 0)
                return 0;

            Debug.Assert(0 < sym && sym <= n);

            count = table[0];		       /* start with the whole table size */

            bit = 1;
            while (bit < n)
                bit <<= 1;

            limit = n;

            while (bit > 0) {
                /*
                 * Find the least number with its lowest set bit in this
                 * position which is greater than or equal to sym.
                 */
                index = ((sym + bit - 1) & ~(bit * 2 - 1)) + bit;

                if (index < limit) {
                    count -= table[index];
                    limit = index;
                }

                bit >>= 1;
            }

            return count;
        }

        /*
         * Single frequency lookup: return the count of symbol `sym'.
         */
        static int cf_slookup(int[] table, int n, int sym)
        {
            int count, bit;

            Debug.Assert(0 <= sym && sym < n);

            count = table[sym];

            for (bit = 1; sym+bit < n && ((sym & bit) == 0); bit <<= 1)
                count -= table[sym + bit];

            return count;
        }

        /*
         * Return the largest symbol index such that the cumulative
         * frequency up to that symbol is less than _or equal to_ count.
         */
        static int cf_whichsym(int[] table, int n, int count)
        {
            int bit, sym, top;

            Debug.Assert(count >= 0 && count < table[0]);

            bit = 1;
            while (bit < n)
                bit <<= 1;

            sym = 0;
            top = table[0];

            while (bit > 0) {
                if (sym + bit < n) {
                    if (count >= top - table[sym + bit])
                        sym += bit;
                    else
                        top -= table[sym + bit];
                }

                bit >>= 1;
            }

            return sym;
        }
        
        /* ----------------------------------------------------------------------
         * Map generation.
         * 
         * FIXME: this isn't entirely optimal at present, because it
         * inherently prioritises growing the largest region since there
         * are more squares adjacent to it. This acts as a destabilising
         * influence leading to a few large regions and mostly small ones.
         * It might be better to do it some other way.
         */

        const int WEIGHT_INCREASED =2;             /* for increased perimeter */
        const int WEIGHT_DECREASED =4;             /* for decreased perimeter */
        const int WEIGHT_UNCHANGED =3;             /* for unchanged perimeter */

        /*
         * Look at a square and decide which colours can be extended into
         * it.
         * 
         * If called with index < 0, it adds together one of
         * WEIGHT_INCREASED, WEIGHT_DECREASED or WEIGHT_UNCHANGED for each
         * colour that has a valid extension (according to the effect that
         * it would have on the perimeter of the region being extended) and
         * returns the overall total.
         * 
         * If called with index >= 0, it returns one of the possible
         * colours depending on the value of index, in such a way that the
         * number of possible inputs which would give rise to a given
         * return value correspond to the weight of that value.
         */
        static int extend_options(int w, int h, int n, int[] map,
                                  int x, int y, int index)
        {
            int c, i, dx, dy;
            int[] col = new int[8];
            int total = 0;

            if (map[y*w+x] >= 0) {
                Debug.Assert(index < 0);
                return 0;                      /* can't do this square at all */
            }

            /*
             * Fetch the eight neighbours of this square, in order around
             * the square.
             */
            for (dy = -1; dy <= +1; dy++)
                for (dx = -1; dx <= +1; dx++) {
                    int index2 = (dy < 0 ? 6-dx : dy > 0 ? 2+dx : 2*(1+dx));
                    if (x+dx >= 0 && x+dx < w && y+dy >= 0 && y+dy < h)
                        col[index2] = map[(y+dy)*w+(x+dx)];
                    else
                        col[index2] = -1;
                }

            /*
             * Iterate over each colour that might be feasible.
             * 
             * FIXME: this routine currently has O(n) running time. We
             * could turn it into O(FOUR) by only bothering to iterate over
             * the colours mentioned in the four neighbouring squares.
             */

            for (c = 0; c < n; c++) {
                int count, neighbours, runs;

                /*
                 * One of the even indices of col (representing the
                 * orthogonal neighbours of this square) must be equal to
                 * c, or else this square is not adjacent to region c and
                 * obviously cannot become an extension of it at this time.
                 */
                neighbours = 0;
                for (i = 0; i < 8; i += 2)
                    if (col[i] == c)
                        neighbours++;
                if (neighbours==0)
                    continue;

                /*
                 * Now we know this square is adjacent to region c. The
                 * next question is, would extending it cause the region to
                 * become non-simply-connected? If so, we mustn't do it.
                 * 
                 * We determine this by looking around col to see if we can
                 * find more than one separate run of colour c.
                 */
                runs = 0;
                for (i = 0; i < 8; i++)
                    if (col[i] == c && col[(i+1) & 7] != c)
                        runs++;
                if (runs > 1)
                    continue;

                Debug.Assert(runs == 1);

                /*
                 * This square is a possibility. Determine its effect on
                 * the region's perimeter (computed from the number of
                 * orthogonal neighbours - 1 means a perimeter increase, 3
                 * a decrease, 2 no change; 4 is impossible because the
                 * region would already not be simply connected) and we're
                 * done.
                 */
                Debug.Assert(neighbours > 0 && neighbours < 4);
                count = (neighbours == 1 ? WEIGHT_INCREASED :
                         neighbours == 2 ? WEIGHT_UNCHANGED : WEIGHT_DECREASED);

                total += count;
                if (index >= 0 && index < count)
                    return c;
                else
                    index -= count;
            }

            Debug.Assert(index < 0);

            return total;
        }

        static void genmap(int w, int h, int n, int[] map, Random rs)
        {
            int wh = w*h;
            int x, y, i, k;
            int[] tmp;

            Debug.Assert(n <= wh);
            tmp = new int[wh];

            /*
             * Clear the map, and set up `tmp' as a list of grid indices.
             */
            for (i = 0; i < wh; i++) {
                map[i] = -1;
                tmp[i] = i;
            }

            /*
             * Place the region seeds by selecting n members from `tmp'.
             */
            k = wh;
            for (i = 0; i < n; i++) {
                int j = rs.Next(0, k); //random_upto(rs, k);
                map[tmp[j]] = i;
                tmp[j] = tmp[--k];
            }

            /*
             * Re-initialise `tmp' as a cumulative frequency table. This
             * will store the number of possible region colours we can
             * extend into each square.
             */
            cf_init(tmp, wh);

            /*
             * Go through the grid and set up the initial cumulative
             * frequencies.
             */
            for (y = 0; y < h; y++)
                for (x = 0; x < w; x++)
                    cf_add(tmp, wh, y*w+x,
                           extend_options(w, h, n, map, x, y, -1));

            /*
             * Now repeatedly choose a square we can extend a region into,
             * and do so.
             */
            while (tmp[0] > 0) {
                int k2 = rs.Next(0, tmp[0]); //random_upto(rs, tmp[0]);
                int sq;
                int colour;
                int xx, yy;

                sq = cf_whichsym(tmp, wh, k2);
                k2 -= cf_clookup(tmp, wh, sq);
                x = sq % w;
                y = sq / w;
                colour = extend_options(w, h, n, map, x, y, k2);

                map[sq] = colour;

                /*
                 * Re-scan the nine cells around the one we've just
                 * modified.
                 */
                for (yy = Math.Max(y-1, 0); yy < Math.Min(y+2, h); yy++)
                    for (xx = Math.Max(x-1, 0); xx < Math.Min(x+2, w); xx++) {
                        cf_add(tmp, wh, yy*w+xx,
                               -cf_slookup(tmp, wh, yy*w+xx) +
                               extend_options(w, h, n, map, xx, yy, -1));
                    }
            }

            /*
             * Finally, go through and normalise the region labels into
             * order, meaning that indistinguishable maps are actually
             * identical.
             */
            for (i = 0; i < n; i++)
                tmp[i] = -1;
            k = 0;
            for (i = 0; i < wh; i++) {
                Debug.Assert(map[i] >= 0);
                if (tmp[map[i]] < 0)
                    tmp[map[i]] = k++;
                map[i] = tmp[map[i]];
            }

        }

        /* ----------------------------------------------------------------------
         * Functions to handle graphs.
         */

        /*
         * Having got a map in a square grid, convert it into a graph
         * representation.
         */
        static int gengraph(int w, int h, int n, int[] map, int[] graph)
        {
            int i, j, x, y;

            /*
             * Start by setting the graph up as an adjacency matrix. We'll
             * turn it into a list later.
             */
            for (i = 0; i < n*n; i++)
	        graph[i] = 0;

            /*
             * Iterate over the map looking for all adjacencies.
             */
            for (y = 0; y < h; y++)
                for (x = 0; x < w; x++) {
	            int v, vx, vy;
	            v = map[y*w+x];
	            if (x+1 < w && (vx = map[y*w+(x+1)]) != v)
		        graph[v*n+vx] = graph[vx*n+v] = 1;
	            if (y+1 < h && (vy = map[(y+1)*w+x]) != v)
		        graph[v*n+vy] = graph[vy*n+v] = 1;
	        }

            /*
             * Turn the matrix into a list.
             */
            for (i = j = 0; i < n*n; i++)
	        if (graph[i] != 0)
	            graph[j++] = i;

            return j;
        }

        static int graph_edge_index(int[] graph, int n, int ngraph, int i, int j)
        {
            int v = i*n+j;
            int top, bot, mid;

            bot = -1;
            top = ngraph;
            while (top - bot > 1) {
	        mid = (top + bot) / 2;
	        if (graph[mid] == v)
	            return mid;
	        else if (graph[mid] < v)
	            bot = mid;
	        else
	            top = mid;
            }
            return -1;
        }

        static bool graph_adjacent(int[]graph, int n, int ngraph, int i, int j) 
        {
            return (graph_edge_index((graph), (n), (ngraph), (i), (j)) >= 0);
        }

        static int graph_vertex_start(int[] graph, int n, int ngraph, int i)
        {
            int v = i*n;
            int top, bot, mid;

            bot = -1;
            top = ngraph;
            while (top - bot > 1) {
	        mid = (top + bot) / 2;
	        if (graph[mid] < v)
	            bot = mid;
	        else
	            top = mid;
            }
            return top;
        }

        /* ----------------------------------------------------------------------
         * Generate a four-colouring of a graph.
         *
         * FIXME: it would be nice if we could convert this recursion into
         * pseudo-recursion using some sort of explicit stack array, for
         * the sake of the Palm port and its limited stack.
         */

        static bool fourcolour_recurse(int[] graph, int n, int ngraph,
			              int[] colouring, int[] scratch, Random rs)
        {
            int nfree, nvert, start, i, j, k, c, ci;
            int[] cs = new int[FOUR];

            /*
             * Find the smallest number of free colours in any uncoloured
             * vertex, and count the number of such vertices.
             */

            nfree = FIVE;		       /* start off bigger than FOUR! */
            nvert = 0;
            for (i = 0; i < n; i++)
	        if (colouring[i] < 0 && scratch[i*FIVE+FOUR] <= nfree) {
	            if (nfree > scratch[i*FIVE+FOUR]) {
		        nfree = scratch[i*FIVE+FOUR];
		        nvert = 0;
	            }
	            nvert++;
	        }

            /*
             * If there aren't any uncoloured vertices at all, we're done.
             */
            if (nvert == 0)
	        return true;		       /* we've got a colouring! */

            /*
             * Pick a random vertex in that set.
             */
            j = rs.Next(0, nvert); //  random_upto(rs, nvert);
            for (i = 0; i < n; i++)
	        if (colouring[i] < 0 && scratch[i*FIVE+FOUR] == nfree)
	            if (j-- == 0)
		        break;
            Debug.Assert(i < n);
            start = graph_vertex_start(graph, n, ngraph, i);

            /*
             * Loop over the possible colours for i, and recurse for each
             * one.
             */
            ci = 0;
            for (c = 0; c < FOUR; c++)
	        if (scratch[i*FIVE+c] == 0)
	            cs[ci++] = c;
            cs.Shuffle(ci, rs);//shuffle(cs, ci, sizeof(*cs), rs);

            while (ci-- > 0) {
	        c = cs[ci];

	        /*
	         * Fill in this colour.
	         */
	        colouring[i] = c;

	        /*
	         * Update the scratch space to reflect a new neighbour
	         * of this colour for each neighbour of vertex i.
	         */
	        for (j = start; j < ngraph && graph[j] < n*(i+1); j++) {
	            k = graph[j] - i*n;
	            if (scratch[k*FIVE+c] == 0)
		        scratch[k*FIVE+FOUR]--;
	            scratch[k*FIVE+c]++;
	        }

	        /*
	         * Recurse.
	         */
	        if (fourcolour_recurse(graph, n, ngraph, colouring, scratch, rs))
	            return true;	       /* got one! */

	        /*
	         * If that didn't work, clean up and try again with a
	         * different colour.
	         */
	        for (j = start; j < ngraph && graph[j] < n*(i+1); j++) {
	            k = graph[j] - i*n;
	            scratch[k*FIVE+c]--;
	            if (scratch[k*FIVE+c] == 0)
		        scratch[k*FIVE+FOUR]++;
	        }
	        colouring[i] = -1;
            }

            /*
             * If we reach here, we were unable to find a colouring at all.
             * (This doesn't necessarily mean the Four Colour Theorem is
             * violated; it might just mean we've gone down a dead end and
             * need to back up and look somewhere else. It's only an FCT
             * violation if we get all the way back up to the top level and
             * still fail.)
             */
            return false;
        }

        static void fourcolour(int[] graph, int n, int ngraph, int[] colouring,
		               Random rs)
        {
            int[] scratch;
            int i;

            /*
             * For each vertex and each colour, we store the number of
             * neighbours that have that colour. Also, we store the number
             * of free colours for the vertex.
             */
            scratch = new int[n * FIVE];
            for (i = 0; i < n * FIVE; i++)
	        scratch[i] = (i % FIVE == FOUR ? FOUR : 0);

            /*
             * Clear the colouring to start with.
             */
            for (i = 0; i < n; i++)
	        colouring[i] = -1;

            bool i2 = fourcolour_recurse(graph, n, ngraph, colouring, scratch, rs);
            Debug.Assert(i2);			       /* by the Four Colour Theorem :-) */

        }

        /* ----------------------------------------------------------------------
         * Non-recursive solver.
         */

        class solver_scratch {
            internal byte[] possible;	       /* bitmap of colours for each region */

            internal int[] graph;
            internal int n;
            internal int ngraph;

            internal int[] bfsqueue;
            internal int[] bfscolour;


            internal int depth;
        }

        static solver_scratch new_scratch(int[] graph, int n, int ngraph)
        {
            solver_scratch sc = new solver_scratch();

            sc.graph = graph;
            sc.n = n;
            sc.ngraph = ngraph;
            sc.possible = new byte[n];
            sc.depth = 0;
            sc.bfsqueue = new int[n];
            sc.bfscolour = new int[n];


            return sc;
        }



        /*
         * Count the bits in a word. Only needs to cope with FOUR bits.
         */
        static int bitcount(int word)
        {
            Debug.Assert(FOUR <= 4);                 /* or this needs changing */
            word = ((word & 0xA) >> 1) + (word & 0x5);
            word = ((word & 0xC) >> 2) + (word & 0x3);
            return word;
        }



        static bool place_colour(solver_scratch sc,
			        int[] colouring, int index, int colour

                                )
        {
            int[] graph = sc.graph;
            int n = sc.n, ngraph = sc.ngraph;
            int j, k;

            if ((sc.possible[index] & (1 << colour)) == 0) {

	        return false;		       /* can't do it */
            }

            sc.possible[index] = (byte)(1 << colour);
            colouring[index] = colour;



            /*
             * Rule out this colour from all the region's neighbours.
             */
            for (j = graph_vertex_start(graph, n, ngraph, index);
	         j < ngraph && graph[j] < n*(index+1); j++) {
	        k = graph[j] - index*n;

	        sc.possible[k] &= (byte)(~(1 << colour));
            }

            return true;
        }



        /*
         * Returns 0 for impossible, 1 for success, 2 for failure to
         * converge (i.e. puzzle is either ambiguous or just too
         * difficult).
         */
        static int map_solver(solver_scratch sc,
		              int[] graph, int n, int ngraph, int[] colouring,
                              int difficulty)
        {
            int i;

            if (sc.depth == 0) {
                /*
                 * Initialise scratch space.
                 */
                for (i = 0; i < n; i++)
                    sc.possible[i] = (1 << FOUR) - 1;

                /*
                 * Place clues.
                 */
                for (i = 0; i < n; i++)
                    if (colouring[i] >= 0) {
                        if (!place_colour(sc, colouring, i, colouring[i]

                                          )) {

                            return 0;	       /* the clues aren't even consistent! */
                        }
                    }
            }

            /*
             * Now repeatedly loop until we find nothing further to do.
             */
            while (true) {
	        bool done_something = false;

                if (difficulty < DIFF_EASY)
                    break;                     /* can't do anything at all! */

	        /*
	         * Simplest possible deduction: find a region with only one
	         * possible colour.
	         */
	        for (i = 0; i < n; i++) if (colouring[i] < 0) {
	            int p = sc.possible[i];

	            if (p == 0) {

		        return 0;	       /* puzzle is inconsistent */
                    }

	            if ((p & (p-1)) == 0) {    /* p is a power of two */
		        int c; bool ret;
		        for (c = 0; c < FOUR; c++)
		            if (p == (1 << c))
			        break;
		        Debug.Assert(c < FOUR);
		        ret = place_colour(sc, colouring, i, c

                                           );
                        /*
                         * place_colour() can only fail if colour c was not
                         * even a _possibility_ for region i, and we're
                         * pretty sure it was because we checked before
                         * calling place_colour(). So we can safely assert
                         * here rather than having to return a nice
                         * friendly error code.
                         */
                        Debug.Assert(ret);
		        done_something = true;
	            }
	        }

                if (done_something)
                    continue;

                if (difficulty < DIFF_NORMAL)
                    break;                     /* can't do anything harder */

                /*
                 * Failing that, go up one level. Look for pairs of regions
                 * which (a) both have the same pair of possible colours,
                 * (b) are adjacent to one another, (c) are adjacent to the
                 * same region, and (d) that region still thinks it has one
                 * or both of those possible colours.
                 * 
                 * Simplest way to do this is by going through the graph
                 * edge by edge, so that we start with property (b) and
                 * then look for (a) and finally (c) and (d).
                 */
                for (i = 0; i < ngraph; i++) {
                    int j1 = graph[i] / n, j2 = graph[i] % n;
                    int j, k, v, v2;


                    if (j1 > j2)
                        continue;              /* done it already, other way round */

                    if (colouring[j1] >= 0 || colouring[j2] >= 0)
                        continue;              /* they're not undecided */

                    if (sc.possible[j1] != sc.possible[j2])
                        continue;              /* they don't have the same possibles */

                    v = sc.possible[j1];
                    /*
                     * See if v contains exactly two set bits.
                     */
                    v2 = v & -v;           /* find lowest set bit */
                    v2 = v & ~v2;          /* clear it */
                    if (v2 == 0 || (v2 & (v2-1)) != 0)   /* not power of 2 */
                        continue;

                    /*
                     * We've found regions j1 and j2 satisfying properties
                     * (a) and (b): they have two possible colours between
                     * them, and since they're adjacent to one another they
                     * must use _both_ those colours between them.
                     * Therefore, if they are both adjacent to any other
                     * region then that region cannot be either colour.
                     * 
                     * Go through the neighbours of j1 and see if any are
                     * shared with j2.
                     */
                    for (j = graph_vertex_start(graph, n, ngraph, j1);
                         j < ngraph && graph[j] < n*(j1+1); j++) {
                        k = graph[j] - j1*n;
                        if (graph_adjacent(graph, n, ngraph, k, j2) &&
                            (sc.possible[k] & v) != 0) {

                            sc.possible[k] &= (byte)~v;
                            done_something = true;
                        }
                    }
                }

                if (done_something)
                    continue;

                if (difficulty < DIFF_HARD)
                    break;                     /* can't do anything harder */

                /*
                 * Right; now we get creative. Now we're going to look for
                 * `forcing chains'. A forcing chain is a path through the
                 * graph with the following properties:
                 * 
                 *  (a) Each vertex on the path has precisely two possible
                 *      colours.
                 * 
                 *  (b) Each pair of vertices which are adjacent on the
                 *      path share at least one possible colour in common.
                 * 
                 *  (c) Each vertex in the middle of the path shares _both_
                 *      of its colours with at least one of its neighbours
                 *      (not the same one with both neighbours).
                 * 
                 * These together imply that at least one of the possible
                 * colour choices at one end of the path forces _all_ the
                 * rest of the colours along the path. In order to make
                 * real use of this, we need further properties:
                 * 
                 *  (c) Ruling out some colour C from the vertex at one end
                 *      of the path forces the vertex at the other end to
                 *      take colour C.
                 * 
                 *  (d) The two end vertices are mutually adjacent to some
                 *      third vertex.
                 * 
                 *  (e) That third vertex currently has C as a possibility.
                 * 
                 * If we can find all of that lot, we can deduce that at
                 * least one of the two ends of the forcing chain has
                 * colour C, and that therefore the mutually adjacent third
                 * vertex does not.
                 * 
                 * To find forcing chains, we're going to start a bfs at
                 * each suitable vertex of the graph, once for each of its
                 * two possible colours.
                 */
                for (i = 0; i < n; i++) {
                    int c;

                    if (colouring[i] >= 0 || bitcount(sc.possible[i]) != 2)
                        continue;

                    for (c = 0; c < FOUR; c++)
                        if ((sc.possible[i] & (1 << c)) != 0) {
                            int j, k, gi, origc, currc, head, tail;
                            /*
                             * Try a bfs from this vertex, ruling out
                             * colour c.
                             * 
                             * Within this loop, we work in colour bitmaps
                             * rather than actual colours, because
                             * converting back and forth is a needless
                             * computational expense.
                             */

                            origc = 1 << c;

                            for (j = 0; j < n; j++) {
                                sc.bfscolour[j] = -1;

                            }
                            head = tail = 0;
                            sc.bfsqueue[tail++] = i;
                            sc.bfscolour[i] = sc.possible[i] &~ origc;

                            while (head < tail) {
                                j = sc.bfsqueue[head++];
                                currc = sc.bfscolour[j];

                                /*
                                 * Try neighbours of j.
                                 */
                                for (gi = graph_vertex_start(graph, n, ngraph, j);
                                     gi < ngraph && graph[gi] < n*(j+1); gi++) {
                                    k = graph[gi] - j*n;

                                    /*
                                     * To continue with the bfs in vertex
                                     * k, we need k to be
                                     *  (a) not already visited
                                     *  (b) have two possible colours
                                     *  (c) those colours include currc.
                                     */

                                    if (sc.bfscolour[k] < 0 &&
                                        colouring[k] < 0 &&
                                        bitcount(sc.possible[k]) == 2 &&
                                        (sc.possible[k] & currc) != 0) {
                                        sc.bfsqueue[tail++] = k;
                                        sc.bfscolour[k] =
                                            sc.possible[k] &~ currc;

                                    }

                                    /*
                                     * One other possibility is that k
                                     * might be the region in which we can
                                     * make a real deduction: if it's
                                     * adjacent to i, contains currc as a
                                     * possibility, and currc is equal to
                                     * the original colour we ruled out.
                                     */
                                    if (currc == origc &&
                                        graph_adjacent(graph, n, ngraph, k, i) &&
                                        (sc.possible[k] & currc) != 0) {

                                        sc.possible[k] &= (byte)~origc;
                                        done_something = true;
                                    }
                                }
                            }

                            Debug.Assert(tail <= n);
                        }
                }

	        if (!done_something)
	            break;
            }

            /*
             * See if we've got a complete solution, and return if so.
             */
            for (i = 0; i < n; i++)
	        if (colouring[i] < 0)
                    break;
            if (i == n) {

                return 1;                      /* success! */
            }

            /*
             * If recursion is not permissible, we now give up.
             */
            if (difficulty < DIFF_RECURSE) {

                return 2;                      /* unable to complete */
            }

            /*
             * Now we've got to do something recursive. So first hunt for a
             * currently-most-constrained region.
             */
            {
                int best, bestc;
                solver_scratch rsc;
                int[] subcolouring, origcolouring;
                int ret, subret;
                bool we_already_got_one;

                best = -1;
                bestc = FIVE;

                for (i = 0; i < n; i++) if (colouring[i] < 0) {
                    int p = sc.possible[i];
                    //enum { compile_time_Debug.Assertion = 1 / (FOUR <= 4) };
                    int c;

                    /* Count the set bits. */
                    c = (p & 5) + ((p >> 1) & 5);
                    c = (c & 3) + ((c >> 2) & 3);
                    Debug.Assert(c > 1);             /* or colouring[i] would be >= 0 */

                    if (c < bestc) {
                        best = i;
                        bestc = c;
                    }
                }

                Debug.Assert(best >= 0);             /* or we'd be solved already */


                /*
                 * Now iterate over the possible colours for this region.
                 */
                rsc = new_scratch(graph, n, ngraph);
                rsc.depth = sc.depth + 1;
                origcolouring = new int[n];
                Array.Copy(colouring, origcolouring, n); // memcpy(origcolouring, colouring, n * sizeof(int));
                subcolouring = new int[n];
                we_already_got_one = false;
                ret = 0;

                for (i = 0; i < FOUR; i++) {
                    if ((sc.possible[best] & (1 << i))==0)
                        continue;

                    Array.Copy(sc.possible, rsc.possible, n); //  memcpy(rsc.possible, sc.possible, n);
                    Array.Copy(origcolouring, subcolouring, n); // memcpy(subcolouring, origcolouring, n * sizeof(int));

                    place_colour(rsc, subcolouring, best, i
                                 );

                    subret = map_solver(rsc, graph, n, ngraph,
                                        subcolouring, difficulty);


                    /*
                     * If this possibility turned up more than one valid
                     * solution, or if it turned up one and we already had
                     * one, we're definitely ambiguous.
                     */
                    if (subret == 2 || (subret == 1 && we_already_got_one)) {
                        ret = 2;
                        break;
                    }

                    /*
                     * If this possibility turned up one valid solution and
                     * it's the first we've seen, copy it into the output.
                     */
                    if (subret == 1) {
                        Array.Copy(subcolouring, colouring, n); //memcpy(colouring, subcolouring, n * sizeof(int));
                        we_already_got_one = true;
                        ret = 1;
                    }

                    /*
                     * Otherwise, this guess led to a contradiction, so we
                     * do nothing.
                     */
                }
                return ret;
            }
        }



        
/* ----------------------------------------------------------------------
 * Game generation main function.
 */
public override string GenerateNewGameDescription(MapSettings @params, Random rs, out string aux, int interactive)
{
    solver_scratch sc = null;
    int[] map, graph, colouring, colouring2, regions, cfreq = new int[FOUR];
    int i, j, w, h, n, solveret, ngraph;
    int wh;
    int mindiff, tries;
    //string ret, buf[80];
    //int retlen, retsize;
    StringBuilder ret = new StringBuilder();

    w = @params.w;
    h = @params.h;
    n = @params.n;
    wh = w*h;

    aux = null;

    map = new int[wh];
    graph = new int[n*n];
    colouring = new int[n];
    colouring2 = new int[n];
    regions = new int[n];

    /*
     * This is the minimum difficulty below which we'll completely
     * reject a map design. Normally we set this to one below the
     * requested difficulty, ensuring that we have the right
     * result. However, for particularly dense maps or maps with
     * particularly few regions it might not be possible to get the
     * desired difficulty, so we will eventually drop this down to
     * -1 to indicate that any old map will do.
     */
    mindiff = @params.diff;
    tries = 50;

    while (true) {

        /*
         * Create the map.
         */
        genmap(w, h, n, map, rs);


        /*
         * Convert the map into a graph.
         */
        ngraph = gengraph(w, h, n, map, graph);


        /*
         * Colour the map.
         */
        fourcolour(graph, n, ngraph, colouring, rs);


        ///*
        // * Encode the solution as an aux string.
        // */
        //retlen = retsize = 0;
        //ret = null;
        //for (i = 0; i < n; i++) {
        //    int len;

        //    if (colouring[i] < 0)
        //        continue;

        //    len = sprintf(buf, "%s%d:%d", i ? ";" : "S;", colouring[i], i);
        //    if (retlen + len >= retsize) {
        //        retsize = retlen + len + 256;
        //        ret = sresize(ret, retsize, char);
        //    }
        //    strcpy(ret + retlen, buf);
        //    retlen += len;
        //}
        //*aux = ret;

        /*
         * Remove the region colours one by one, keeping
         * solubility. Also ensure that there always remains at
         * least one region of every colour, so that the user can
         * drag from somewhere.
         */
        for (i = 0; i < FOUR; i++)
            cfreq[i] = 0;
        for (i = 0; i < n; i++) {
            regions[i] = i;
            cfreq[colouring[i]]++;
        }
        for (i = 0; i < FOUR; i++)
            if (cfreq[i] == 0)
                continue;

        regions.Shuffle(n, rs);


        sc = new_scratch(graph, n, ngraph);

        for (i = 0; i < n; i++) {
            j = regions[i];

            if (cfreq[colouring[j]] == 1)
                continue;              /* can't remove last region of colour */

            Array.Copy(colouring, colouring2, n);
            colouring2[j] = -1;
            solveret = map_solver(sc, graph, n, ngraph, colouring2,
				  @params.diff);
            Debug.Assert(solveret >= 0);	       /* mustn't be impossible! */
            if (solveret == 1) {
                cfreq[colouring[j]]--;
                colouring[j] = -1;
            }
        }


        /*
         * Finally, check that the puzzle is _at least_ as hard as
         * required, and indeed that it isn't already solved.
         * (Calling map_solver with negative difficulty ensures the
         * latter - if a solver which _does nothing_ can solve it,
         * it's too easy!)
         */
        Array.Copy(colouring, colouring2, n);
        if (map_solver(sc, graph, n, ngraph, colouring2,
                       mindiff - 1) == 1) {
	    /*
	     * Drop minimum difficulty if necessary.
	     */
	    if (mindiff > 0 && (n < 9 || n > 2*wh/3)) {
		if (tries-- <= 0)
		    mindiff = 0;       /* give up and go for Easy */
	    }
            continue;
	}

        break;
    }

    /*
     * Encode as a game ID. We do this by:
     * 
     * 	- first going along the horizontal edges row by row, and
     * 	  then the vertical edges column by column
     * 	- encoding the lengths of runs of edges and runs of
     * 	  non-edges
     * 	- the decoder will reconstitute the region boundaries from
     * 	  this and automatically number them the same way we did
     * 	- then we encode the initial region colours in a Slant-like
     * 	  fashion (digits 0-3 interspersed with letters giving
     * 	  lengths of runs of empty spaces).
     */
    {
	int run, pv;

	/*
	 * Start with a notional non-edge, so that there'll be an
	 * explicit `a' to distinguish the case where we start with
	 * an edge.
	 */
	run = 1;
	pv = 0;

	for (i = 0; i < w*(h-1) + (w-1)*h; i++) {
	    int x, y, dx, dy, v;

	    if (i < w*(h-1)) {
		/* Horizontal edge. */
		y = i / w;
		x = i % w;
		dx = 0;
		dy = 1;
	    } else {
		/* Vertical edge. */
		x = (i - w*(h-1)) / h;
		y = (i - w*(h-1)) % h;
		dx = 1;
		dy = 0;
	    }


	    v = (map[y*w+x] != map[(y+dy)*w+(x+dx)]) ? 1 : 0;

	    if (pv != v) {
		ret.Append((char)('a'-1 + run));
		run = 1;
		pv = v;
	    } else {
		/*
		 * 'z' is a special case in this encoding. Rather
		 * than meaning a run of 26 and a state switch, it
		 * means a run of 25 and _no_ state switch, because
		 * otherwise there'd be no way to encode runs of
		 * more than 26.
		 */
		if (run == 25) {
		    ret.Append( 'z');
		    run = 0;
		}
		run++;
	    }
	}

	ret.Append((char)('a'-1 + run));
	ret.Append(  ',');

	run = 0;
	for (i = 0; i < n; i++) {


	    if (colouring[i] < 0) {
		/*
		 * In _this_ encoding, 'z' is a run of 26, since
		 * there's no implicit state switch after each run.
		 * Confusingly different, but more compact.
		 */
		if (run == 26) {
		    ret.Append( 'z');
		    run = 0;
		}
		run++;
	    } else {
		if (run > 0)
		    ret.Append( (char)('a'-1 + run));
		ret.Append( (char)('0' + colouring[i]));
		run = 0;
	    }
	}
	if (run > 0)
	    ret.Append((char)('a'-1 + run));



    }


    return ret.ToString();
}

static string parse_edge_list(MapSettings @params, ref string desc,
                             int[] map)
{
    int w = @params.w, h = @params.h, wh = w*h, n = @params.n;
    int i, k, pos;
    int p = 0;
    bool state;

    Dsf.dsf_init(map.Segment(wh), wh);

    pos = -1;
    state = false;

    /*
     * Parse the game description to get the list of edges, and
     * build up a disjoint set forest as we go (by identifying
     * pairs of squares whenever the edge list shows a non-edge).
     */
    while (p < desc.Length && desc[p] != ',') {
        if (desc[p] < 'a' || desc[p] > 'z')
	    return "Unexpected character in edge list";
        if (desc[p] == 'z')
	    k = 25;
	else
            k = desc[p] - 'a' + 1;
	while (k-- > 0) {
	    int x, y, dx, dy;

	    if (pos < 0) {
		pos++;
		continue;
	    } else if (pos < w*(h-1)) {
		/* Horizontal edge. */
		y = pos / w;
		x = pos % w;
		dx = 0;
		dy = 1;
	    } else if (pos < 2*wh-w-h) {
		/* Vertical edge. */
		x = (pos - w*(h-1)) / h;
		y = (pos - w*(h-1)) % h;
		dx = 1;
		dy = 0;
	    } else
		return "Too much data in edge list";
	    if (!state)
		Dsf.dsf_merge(map.Segment(wh), y*w+x, (y+dy)*w+(x+dx));

	    pos++;
	}
    if (desc[p] != 'z')
	    state = !state;
	p++;
    }
    Debug.Assert(pos <= 2*wh-w-h);
    if (pos < 2*wh-w-h)
	return "Too little data in edge list";

    /*
     * Now go through again and allocate region numbers.
     */
    pos = 0;
    for (i = 0; i < wh; i++)
	map[i] = -1;
    for (i = 0; i < wh; i++) {
	k = Dsf.dsf_canonify(map.Segment(wh), i);
	if (map[k] < 0)
	    map[k] = pos++;
	map[i] = map[k];
    }
    if (pos != n)
	return "Edge list defines the wrong number of regions";

    desc = desc.Substring(p);

    return null;
}

static string validate_desc(MapSettings @params, string desc)
{
    int w = @params.w, h = @params.h, wh = w*h, n = @params.n;
    int area;
    int [] map;
    string ret;

    map = new int[2*wh];
    ret = parse_edge_list(@params, ref desc, map);
    if (ret != null)
	return ret;

    int descPos = 0;
    if (desc[descPos] != ',')
	return "Expected comma before clue list";
    descPos++;			       /* eat comma */

    area = 0;
    while (descPos < desc.Length) {
        if (desc[descPos] >= '0' && desc[descPos] < '0' + FOUR)
	    area++;
        else if (desc[descPos] >= 'a' && desc[descPos] <= 'z')
            area += desc[descPos] - 'a' + 1;
	else
	    return "Unexpected character in clue list";
    descPos++;
    }
    if (area < n)
	return "Too little data in clue list";
    else if (area > n)
	return "Too much data in clue list";

    return null;
}

        // was new_game
public override MapState CreateNewGameFromDescription(MapSettings @params, string desc)
{
    int w = @params.w, h = @params.h, wh = w*h, n = @params.n;
    int i, pos;
    string p;
    int pPos;
    MapState state = new MapState();

    state.p = @params;
    state.colouring = new int[n];
    for (i = 0; i < n; i++)
	state.colouring[i] = -1;
    state.pencil = new int[n];
    for (i = 0; i < n; i++)
	state.pencil[i] = 0;

    state.completed = state.cheated = false;

    state.map = new MapData();
    state.map.refcount = 1;
    state.map.map = new int[wh*4];
    state.map.graph = new int[n*n];
    state.map.n = n;
    state.map.immutable = new bool[n];
    for (i = 0; i < n; i++)
	state.map.immutable[i] = false;

    p = desc;
    pPos = 0;
    {
	string ret;
	ret = parse_edge_list(@params, ref p, state.map.map);
	Debug.Assert(ret == null);
    }

    /*
     * Set up the other three quadrants in `map'.
     */
    for (i = wh; i < 4*wh; i++)
	state.map.map[i] = state.map.map[i % wh];

    Debug.Assert(p[pPos] == ',');
    pPos++;

    /*
     * Now process the clue list.
     */
    pos = 0;
    while (pPos<p.Length)
    {
        if (p[pPos] >= '0' && p[pPos] < '0' + FOUR)
        {
            state.colouring[pos] = p[pPos] - '0';
	    state.map.immutable[pos] = true;
	    pos++;
	} else {
        Debug.Assert(p[pPos] >= 'a' && p[pPos] <= 'z');
        pos += p[pPos] - 'a' + 1;
	}
        pPos++;
    }
    Debug.Assert(pos == n);

    state.map.ngraph = gengraph(w, h, n, state.map.map, state.map.graph);

    /*
     * Attempt to smooth out some of the more jagged region
     * outlines by the judicious use of diagonally divided squares.
     */
    {
        Random rs = new Random(desc.Length);
        int[]squares = new int[wh];
        bool done_something;

        for (i = 0; i < wh; i++)
            squares[i] = i;
        squares.Shuffle(wh, rs);

        do {
            done_something = false;
            for (i = 0; i < wh; i++) {
                int y = squares[i] / w, x = squares[i] % w;
                int c = state.map.map[y*w+x];
                int tc, bc, lc, rc;

                if (x == 0 || x == w-1 || y == 0 || y == h-1)
                    continue;

                if (state.map.map[TE * wh + y*w+x] !=
                    state.map.map[BE * wh + y*w+x])
                    continue;

                tc = state.map.map[BE * wh + (y-1)*w+x];
                bc = state.map.map[TE * wh + (y+1)*w+x];
                lc = state.map.map[RE * wh + y*w+(x-1)];
                rc = state.map.map[LE * wh + y*w+(x+1)];

                /*
                 * If this square is adjacent on two sides to one
                 * region and on the other two sides to the other
                 * region, and is itself one of the two regions, we can
                 * adjust it so that it's a diagonal.
                 */
                if (tc != bc && (tc == c || bc == c)) {
                    if ((lc == tc && rc == bc) ||
                        (lc == bc && rc == tc)) {
                        state.map.map[TE * wh + y*w+x] = tc;
                        state.map.map[BE * wh + y*w+x] = bc;
                        state.map.map[LE * wh + y*w+x] = lc;
                        state.map.map[RE * wh + y*w+x] = rc;
                        done_something = true;
                    }
                }
            }
        } while (done_something);
    }

    /*
     * Analyse the map to find a canonical line segment
     * corresponding to each edge, and a canonical point
     * corresponding to each region. The former are where we'll
     * eventually put error markers; the latter are where we'll put
     * per-region flags such as numbers (when in diagnostic mode).
     */
    {
	int[] bestx, besty, an;
    int pass;
	float[] ax, ay, best;

	ax = new float[state.map.ngraph + n];
	ay = new float[state.map.ngraph + n];
	an = new int[state.map.ngraph + n];
	bestx = new int[state.map.ngraph + n];
	besty = new int[state.map.ngraph + n];
	best = new float[state.map.ngraph + n];

	for (i = 0; i < state.map.ngraph + n; i++) {
	    bestx[i] = besty[i] = -1;
	    best[i] = (float)(2*(w+h)+1);
	    ax[i] = ay[i] = 0.0F;
	    an[i] = 0;
	}

	/*
	 * We make two passes over the map, finding all the line
	 * segments separating regions and all the suitable points
	 * within regions. In the first pass, we compute the
	 * _average_ x and y coordinate of all the points in a
	 * given class; in the second pass, for each such average
	 * point, we find the candidate closest to it and call that
	 * canonical.
	 * 
	 * Line segments are considered to have coordinates in
	 * their centre. Thus, at least one coordinate for any line
	 * segment is always something-and-a-half; so we store our
	 * coordinates as twice their normal value.
	 */
	for (pass = 0; pass < 2; pass++) {
	    int x, y;

	    for (y = 0; y < h; y++)
		for (x = 0; x < w; x++) {
		    int[] ex = new int[4], ey = new int[4], ea = new int[4], eb = new int[4];
            int en = 0;

		    /*
		     * Look for an edge to the right of this
		     * square, an edge below it, and an edge in the
		     * middle of it. Also look to see if the point
		     * at the bottom right of this square is on an
		     * edge (and isn't a place where more than two
		     * regions meet).
		     */
		    if (x+1 < w) {
			/* right edge */
			ea[en] = state.map.map[RE * wh + y*w+x];
			eb[en] = state.map.map[LE * wh + y*w+(x+1)];
                        ex[en] = (x+1)*2;
                        ey[en] = y*2+1;
                        en++;
		    }
		    if (y+1 < h) {
			/* bottom edge */
			ea[en] = state.map.map[BE * wh + y*w+x];
			eb[en] = state.map.map[TE * wh + (y+1)*w+x];
                        ex[en] = x*2+1;
                        ey[en] = (y+1)*2;
                        en++;
		    }
		    /* diagonal edge */
		    ea[en] = state.map.map[TE * wh + y*w+x];
		    eb[en] = state.map.map[BE * wh + y*w+x];
                    ex[en] = x*2+1;
                    ey[en] = y*2+1;
                    en++;

		    if (x+1 < w && y+1 < h) {
			/* bottom right corner */
			int[] oct = new int[8];
            int othercol, nchanges;
			oct[0] = state.map.map[RE * wh + y*w+x];
			oct[1] = state.map.map[LE * wh + y*w+(x+1)];
			oct[2] = state.map.map[BE * wh + y*w+(x+1)];
			oct[3] = state.map.map[TE * wh + (y+1)*w+(x+1)];
			oct[4] = state.map.map[LE * wh + (y+1)*w+(x+1)];
			oct[5] = state.map.map[RE * wh + (y+1)*w+x];
			oct[6] = state.map.map[TE * wh + (y+1)*w+x];
			oct[7] = state.map.map[BE * wh + y*w+x];

			othercol = -1;
			nchanges = 0;
			for (i = 0; i < 8; i++) {
			    if (oct[i] != oct[0]) {
				if (othercol < 0)
				    othercol = oct[i];
				else if (othercol != oct[i])
				    break;   /* three colours at this point */
			    }
			    if (oct[i] != oct[(i+1) & 7])
				nchanges++;
			}

			/*
			 * Now if there are exactly two regions at
			 * this point (not one, and not three or
			 * more), and only two changes around the
			 * loop, then this is a valid place to put
			 * an error marker.
			 */
			if (i == 8 && othercol >= 0 && nchanges == 2) {
			    ea[en] = oct[0];
			    eb[en] = othercol;
			    ex[en] = (x+1)*2;
			    ey[en] = (y+1)*2;
			    en++;
			}

                        /*
                         * If there's exactly _one_ region at this
                         * point, on the other hand, it's a valid
                         * place to put a region centre.
                         */
                        if (othercol < 0) {
			    ea[en] = eb[en] = oct[0];
			    ex[en] = (x+1)*2;
			    ey[en] = (y+1)*2;
			    en++;
                        }
		    }

		    /*
		     * Now process the points we've found, one by
		     * one.
		     */
		    for (i = 0; i < en; i++) {
			int emin = Math.Min(ea[i], eb[i]);
			int emax = Math.Max(ea[i], eb[i]);
			int gindex;

                        if (emin != emax) {
                            /* Graph edge */
                            gindex =
                                graph_edge_index(state.map.graph, n,
                                                 state.map.ngraph, emin,
                                                 emax);
                        } else {
                            /* Region number */
                            gindex = state.map.ngraph + emin;
                        }

			Debug.Assert(gindex >= 0);

			if (pass == 0) {
			    /*
			     * In pass 0, accumulate the values
			     * we'll use to compute the average
			     * positions.
			     */
			    ax[gindex] += ex[i];
			    ay[gindex] += ey[i];
			    an[gindex] += 1;
			} else {
			    /*
			     * In pass 1, work out whether this
			     * point is closer to the average than
			     * the last one we've seen.
			     */
			    float dx, dy, d;

			    Debug.Assert(an[gindex] > 0);
			    dx = ex[i] - ax[gindex];
			    dy = ey[i] - ay[gindex];
			    d = (float)Math.Sqrt(dx*dx + dy*dy);
			    if (d < best[gindex]) {
				best[gindex] = d;
				bestx[gindex] = ex[i];
				besty[gindex] = ey[i];
			    }
			}
		    }
		}

	    if (pass == 0) {
		for (i = 0; i < state.map.ngraph + n; i++)
		    if (an[i] > 0) {
			ax[i] /= an[i];
			ay[i] /= an[i];
		    }
	    }
	}

	state.map.edgex = new int[state.map.ngraph];
	state.map.edgey = new int[state.map.ngraph];
        Array.Copy(bestx, state.map.edgex, state.map.ngraph );
        Array.Copy(besty, state.map.edgey, state.map.ngraph);

	state.map.regionx = new int[n];
	state.map.regiony = new int[n];
    Array.Copy(bestx, state.map.ngraph, state.map.regionx, 0, n);
    Array.Copy(besty, state.map.ngraph, state.map.regiony, 0, n);

	for (i = 0; i < state.map.ngraph; i++)
	    if (state.map.edgex[i] < 0) {
		/* Find the other representation of this edge. */
		int e = state.map.graph[i];
		int iprime = graph_edge_index(state.map.graph, n,
					      state.map.ngraph, e%n, e/n);
		Debug.Assert(state.map.edgex[iprime] >= 0);
		state.map.edgex[i] = state.map.edgex[iprime];
		state.map.edgey[i] = state.map.edgey[iprime];
	    }

    }

    return state;
}

static MapState dup_game(MapState state)
{
    MapState ret = new MapState();

    ret.p = state.p;
    ret.colouring = new int[state.p.n];
    Array.Copy(state.colouring, ret.colouring, state.p.n);
    ret.pencil = new int[state.p.n];
    Array.Copy(state.pencil, ret.pencil, state.p.n);
    ret.map = state.map;
    ret.map.refcount++;
    ret.completed = state.completed;
    ret.cheated = state.cheated;

    return ret;
}


public override MapMove CreateSolveGameMove(MapState state, MapState currstate, MapMove aux, out string error)
{
    error = null;
    if (aux != null) {
        return aux;
    }
	/*
	 * Use the solver.
	 */
	int[] colouring;
	solver_scratch sc;
	int sret;
	int i;

	colouring = new int[state.map.n];
	Array.Copy(state.colouring, colouring, state.map.n);

	sc = new_scratch(state.map.graph, state.map.n, state.map.ngraph);
	sret = map_solver(sc, state.map.graph, state.map.n,
			 state.map.ngraph, colouring, DIFFCOUNT-1);

	if (sret != 1) {
	    if (sret == 0)
		error = "Puzzle is inconsistent";
	    else
		error = "Unable to find a unique solution for this puzzle";
	    return null;
	}

    MapMove ret = new MapMove();
    ret.isSolve = true;
    ret.data = new List<Tuple<bool, int, int>>();
	for (i = 0; i < state.map.n; i++) {

	    Debug.Assert(colouring[i] >= 0);
            if (colouring[i] == currstate.colouring[i])
                continue;
	    Debug.Assert(!state.map.immutable[i]);

            //len = sprintf(buf, ";%d:%d", colouring[i], i);
        ret.data.Add(new Tuple<bool, int, int>(false, colouring[i], i));

        }


	return ret;

}


public override MapUI CreateUI(MapState state)
{
    MapUI ui = new MapUI();
    ui.dragx = ui.dragy = -1;
    ui.drag_colour = -2;
    ui.drag_pencil = 0;
    ui.show_numbers = false;
    ui.cur_x = ui.cur_y = 0;
    ui.cur_visible = ui.cur_moved = false;
    ui.cur_lastmove = 0;
    return ui;
}

static void game_changed_state(MapUI ui, MapState oldstate,
                               MapState newstate)
{
}


/* Flags in `drawn'. */
const ulong ERR_BASE      =0x00800000L;
const ulong ERR_MASK = 0xFF800000L;
const ulong PENCIL_T_BASE = 0x00080000L;
const ulong PENCIL_T_MASK = 0x00780000L;
const ulong PENCIL_B_BASE = 0x00008000L;
const ulong PENCIL_B_MASK = 0x00078000L;
const ulong PENCIL_MASK = 0x007F8000L;
const ulong SHOW_NUMBERS = 0x00004000L;

static int TILESIZE(MapDrawState ds) { return (ds.tilesize); }
static int BORDER(MapDrawState ds) { return TILESIZE(ds); }
static int COORD(MapDrawState ds,int x)  { return ( (x) * TILESIZE(ds) + BORDER(ds) ); }
static int FROMCOORD(MapDrawState ds, int x)  { return ( ((x) - BORDER(ds) + TILESIZE(ds)) / TILESIZE(ds) - 1 ); }

 /*
  * EPSILON_FOO are epsilons added to absolute cursor position by
  * cursor movement, such that in pathological cases (e.g. a very
  * small diamond-shaped area) it's relatively easy to select the
  * region you wanted.
  */

static int EPSILON_X(Buttons button) { return (((button) == Buttons.CURSOR_RIGHT) ? +1 : 
                           ((button) == Buttons.CURSOR_LEFT)  ? -1 : 0); }
static int EPSILON_Y(Buttons button) { return (((button) ==Buttons. CURSOR_DOWN)  ? +1 : 
                           ((button) == Buttons.CURSOR_UP)    ? -1 : 0); }


static int region_from_coords(MapState state,
                              MapDrawState ds, int x, int y)
{
    int w = state.p.w, h = state.p.h, wh = w*h /*, n = state.p.n */;
    int tx = FROMCOORD(ds, x), ty = FROMCOORD(ds, y);
    int dx = x - COORD(ds, tx), dy = y - COORD(ds, ty);
    int quadrant;

    if (tx < 0 || tx >= w || ty < 0 || ty >= h)
        return -1;                     /* border */

    quadrant = 2 * (dx > dy?1:0) + (TILESIZE(ds) - (dx > dy?1:0));
    quadrant = (quadrant == 0 ? BE :
                quadrant == 1 ? LE :
                quadrant == 2 ? RE : TE);

    return state.map.map[quadrant * wh + ty*w+tx];
}

internal override void SetKeyboardCursorVisible(MapUI ui, int tileSize, bool value)
{
    ui.cur_visible = value;
    if (value)
    {
        MapDrawState ds = new MapDrawState() { tilesize = tileSize };
        ui.dragx = COORD(ds, ui.cur_x) + TILESIZE(ds) / 2;
        ui.dragy = COORD(ds, ui.cur_y) + TILESIZE(ds) / 2;
    }
}

public override MapMove InterpretMove(MapState state, MapUI ui, MapDrawState ds, int x, int y, Buttons button, bool isTouchOrStylus)
{
    bool alt_button;

    ///*
    // * Enable or disable numeric labels on regions.
    // */
    //if (button == 'l' || button == 'L') {
    //    ui.show_numbers = !ui.show_numbers;
    //    return "";
    //}

    if (Misc.IS_CURSOR_MOVE(button)) {
        Misc.move_cursor(button, ref ui.cur_x, ref ui.cur_y, state.p.w, state.p.h, false);
        ui.cur_visible = true;
        ui.cur_moved = true;
        ui.cur_lastmove = button;
        ui.dragx = COORD(ds,ui.cur_x) + TILESIZE(ds)/2 + EPSILON_X(button);
        ui.dragy = COORD(ds,ui.cur_y) + TILESIZE(ds)/2 + EPSILON_Y(button);
        return null;
    }
    if (Misc.IS_CURSOR_SELECT(button)) {
        if (!ui.cur_visible) {
            ui.dragx = COORD(ds,ui.cur_x) + TILESIZE(ds)/2 + EPSILON_X(ui.cur_lastmove);
            ui.dragy = COORD(ds,ui.cur_y) + TILESIZE(ds)/2 + EPSILON_Y(ui.cur_lastmove);
            ui.cur_visible = true;
            return null;
        }
        if (ui.drag_colour == -2) { /* not currently cursor-dragging, start. */
            int r = region_from_coords(state, ds, ui.dragx, ui.dragy);
            if (r >= 0) {
                ui.drag_colour = state.colouring[r];
                ui.drag_pencil = (ui.drag_colour >= 0) ? 0 : state.pencil[r];
            } else {
                ui.drag_colour = -1;
                ui.drag_pencil = 0;
            }
            ui.cur_moved = false;
            return null;
        } else { /* currently cursor-dragging; drop the colour in the new region. */
            x = COORD(ds,ui.cur_x) + TILESIZE(ds)/2 + EPSILON_X(ui.cur_lastmove);
            y = COORD(ds,ui.cur_y) + TILESIZE(ds)/2 + EPSILON_Y(ui.cur_lastmove);
            alt_button = button == Buttons.CURSOR_SELECT2;
            /* Double-select removes current colour. */
            if (!ui.cur_moved) ui.drag_colour = -1;
            goto drag_dropped;
        }
    }

    if (button == Buttons.LEFT_BUTTON || button == Buttons.RIGHT_BUTTON) {
	int r = region_from_coords(state, ds, x, y);

        if (r >= 0) {
            ui.drag_colour = state.colouring[r];
	    ui.drag_pencil = state.pencil[r];
	    if (ui.drag_colour >= 0)
		ui.drag_pencil = 0;  /* should be already, but double-check */
	} else {
            ui.drag_colour = -1;
	    ui.drag_pencil = 0;
	}
        ui.dragx = x;
        ui.dragy = y;
        ui.cur_visible = false;
        return null;
    }

    if ((button == Buttons.LEFT_DRAG || button == Buttons.RIGHT_DRAG) &&
        ui.drag_colour > -2) {
        ui.dragx = x;
        ui.dragy = y;
        return null;
    }

    if ((button == Buttons.LEFT_RELEASE || button == Buttons.RIGHT_RELEASE) &&
        ui.drag_colour > -2) {
        alt_button = button == Buttons.RIGHT_RELEASE;
        goto drag_dropped;
    }

    return null;

drag_dropped:
    {
	int r = region_from_coords(state, ds, x, y);
        int c = ui.drag_colour;
	int p = ui.drag_pencil;
	int oldp;

        /*
         * Cancel the drag, whatever happens.
         */
        ui.drag_colour = -2;

	if (r < 0)
        return null;                 /* drag into border; do nothing else */

	if (state.map.immutable[r])
        return null;                 /* can't change this region */

        if (state.colouring[r] == c && state.pencil[r] == p)
            return null;                 /* don't _need_ to change this region */

	if (alt_button) {
	    if (state.colouring[r] >= 0) {
		/* Can't pencil on a coloured region */
            return null;
	    } else if (c >= 0) {
		/* Right-dragging from colour to blank toggles one pencil */
		p = state.pencil[r] ^ (1 << c);
		c = -1;
	    }
	    /* Otherwise, right-dragging from blank to blank is equivalent
	     * to left-dragging. */
	}

    MapMove ret = new MapMove();
    ret.isSolve = false;
    ret.data = new List<Tuple<bool, int, int>>();

	oldp = state.pencil[r];
	if (c != state.colouring[r]) {
	    //bufp += sprintf(bufp, ";%c:%d", (int)(c < 0 ? 'C' : '0' + c), r);
        ret.data.Add(new Tuple<bool, int, int>(false, c < 0 ? -1 : c, r ));
	    if (c >= 0)
		oldp = 0;
	}
	if (p != oldp) {
	    int i;
	    for (i = 0; i < FOUR; i++)
		if (((oldp ^ p) & (1 << i)) != 0)
		    //bufp += sprintf(bufp, ";p%c:%d", (int)('0' + i), r);
            ret.data.Add(new Tuple<bool, int, int>(true, i, r));
	}

    return ret;
    }
}

public override MapState ExecuteMove(MapState state, MapMove move)
{
    int n = state.p.n;
    MapState ret = dup_game(state);
    int k, i;

    //while (*move) {
    //    int pencil = false;

    //c = *move;
    //    if (c == 'p') {
    //        pencil = true;
    //        c = *++move;
    //    }
    //if ((c == 'C' || (c >= '0' && c < '0'+FOUR)) &&
    //    sscanf(move+1, ":%d%n", &k, &adv) == 1 &&
    //    k >= 0 && k < state.p.n) {
    //    move += 1 + adv;
    //        if (pencil) {
    //    if (ret.colouring[k] >= 0) {
    //        return null;
    //    }
    //            if (c == 'C')
    //                ret.pencil[k] = 0;
    //            else
    //                ret.pencil[k] ^= 1 << (c - '0');
    //        } else {
    //            ret.colouring[k] = (c == 'C' ? -1 : c - '0');
    //            ret.pencil[k] = 0;
    //        }
    //} else if (*move == 'S') {
    //    move++;
    //    ret.cheated = true;
    //} else {
    //    return null;
    //}

    //if (*move && *move != ';') {
    //    return null;
    //}
    //if (*move)
    //    move++;
    //}

    if (move.isSolve)
    {
        ret.cheated = true;
    }
    foreach (var tuple in move.data)
    {
        var pencil = tuple.Item1;
        var isClear = tuple.Item2 == -1;
        k = tuple.Item3;
        if (pencil)
        {
            if (ret.colouring[k] >= 0)
            {
                return null;
            }
            if (isClear)
                ret.pencil[k] = 0;
            else
                ret.pencil[k] ^= 1 << tuple.Item2;
        }
        else
        {
            ret.colouring[k] = (isClear ? -1 : tuple.Item2);
            ret.pencil[k] = 0;
        }
    }
    /*
     * Check for completion.
     */
    if (!ret.completed) {
	bool ok = true;

	for (i = 0; i < n; i++)
	    if (ret.colouring[i] < 0) {
		ok = false;
		break;
	    }

	if (ok) {
	    for (i = 0; i < ret.map.ngraph; i++) {
		int j = ret.map.graph[i] / n;
		int k2 = ret.map.graph[i] % n;
		if (ret.colouring[j] == ret.colouring[k2]) {
		    ok = false;
		    break;
		}
	    }
	}

	if (ok)
	    ret.completed = true;
    }

    return ret;
}

/* ----------------------------------------------------------------------
 * Drawing routines.
 */
public override void ComputeSize(MapSettings @params, int tilesize, out int x, out int y)
{
    /* Ick: fake up `ds.tilesize' for macro expansion purposes */
    MapDrawState ds = new MapDrawState(){tilesize = tilesize};

    x = @params.w * TILESIZE(ds) + 2 * BORDER(ds) + 1;
    y = @params.h * TILESIZE(ds) + 2 * BORDER(ds) + 1;
}

public override void SetTileSize(Drawing dr, MapDrawState ds, MapSettings @params, int tilesize)
{
    ds.tilesize = tilesize;

    //Debug.Assert(!ds.bl);                   /* set_size is never called twice */
    //ds.bl = blitter_new(dr, TILESIZE(ds)+3, TILESIZE(ds)+3);
}

private static readonly float[][] map_colours = new float[][] {
    new[] {0.75F, 0.25F, 0.25F},
    new[] {0.3F,  0.7F,  0.3F},
    new[] {0.3F,  0.3F,  0.7F},
    new[] {0.85F, 0.85F, 0.1F},
};
//const int map_hatching = new int[FOUR] {
//    HATCH_VERT, HATCH_SLASH, HATCH_HORIZ, HATCH_BACKSLASH
//};

public override float[] GetColours(Frontend fe, out int ncolours)
{

    float[] ret = new float[3 * NCOLOURS];

    fe.frontend_default_colour(ret, COL_BACKGROUND * 3);

    ret[COL_GRID * 3 + 0] = 0.0F;
    ret[COL_GRID * 3 + 1] = 0.0F;
    ret[COL_GRID * 3 + 2] = 0.0F;

    Array.Copy(map_colours[0], 0, ret, COL_0 * 3, 3);
    Array.Copy(map_colours[1], 0, ret, COL_1 * 3, 3);
    Array.Copy(map_colours[2], 0, ret, COL_2 * 3, 3);
    Array.Copy(map_colours[3], 0, ret, COL_3 * 3, 3);

    ret[COL_ERROR * 3 + 0] = 1.0F;
    ret[COL_ERROR * 3 + 1] = 0.0F;
    ret[COL_ERROR * 3 + 2] = 0.0F;

    ret[COL_ERRTEXT * 3 + 0] = 1.0F;
    ret[COL_ERRTEXT * 3 + 1] = 1.0F;
    ret[COL_ERRTEXT * 3 + 2] = 1.0F;

    ncolours = NCOLOURS;
    return ret;
}

        public override MapDrawState CreateDrawState(Drawing dr, MapState state)
{
    MapDrawState ds = new MapDrawState();
    int i;

    ds.tilesize = 0;
    ds.drawn = new ulong[state.p.w * state.p.h];
    for (i = 0; i < state.p.w * state.p.h; i++)
	ds.drawn[i] = 0xFFFFL;
    ds.todraw = new ulong[state.p.w * state.p.h];
    ds.started = false;
    //ds.bl = null;
    //ds.drag_visible = false;
    ds.dragx = ds.dragy = -1;

    return ds;
}


static void draw_error(Drawing dr, MapDrawState ds, int x, int y)
{
    int[] coords = new int[8];
    int yext, xext;

    /*
     * Draw a diamond.
     */
    coords[0] = x - TILESIZE(ds)*2/5;
    coords[1] = y;
    coords[2] = x;
    coords[3] = y - TILESIZE(ds) * 2 / 5;
    coords[4] = x + TILESIZE(ds) * 2 / 5;
    coords[5] = y;
    coords[6] = x;
    coords[7] = y + TILESIZE(ds) * 2 / 5;
    dr.draw_polygon(coords, 4, COL_ERROR, COL_GRID);

    /*
     * Draw an exclamation mark in the diamond. This turns out to
     * look unpleasantly off-centre if done via draw_text, so I do
     * it by hand on the basis that exclamation marks aren't that
     * difficult to draw...
     */
    xext = TILESIZE(ds) / 16;
    yext = TILESIZE(ds) * 2 / 5 - (xext * 2 + 2);
    dr.draw_rect(x-xext, y-yext, xext*2+1, yext*2+1 - (xext*3),
	      COL_ERRTEXT);
    dr.draw_rect(x-xext, y+yext-xext*2+1, xext*2+1, xext*2, COL_ERRTEXT);
}

static void draw_square(Drawing dr, MapDrawState ds,
			MapSettings @params, MapData map,
			int x, int y, ulong v)
{
    int w = @params.w, h = @params.h, wh = w*h;
    int tv, bv, xo, yo, i, j, oldj;
    ulong errs, pencil, show_numbers;

    errs = v & ERR_MASK;
    v &= ~ERR_MASK;
    pencil = v & PENCIL_MASK;
    v &= ~PENCIL_MASK;
    show_numbers = v & SHOW_NUMBERS;
    v &= ~SHOW_NUMBERS;
    tv = (int)(v / FIVE);
    bv = (int)(v % FIVE);

    dr.clip(COORD(ds, x), COORD(ds, y), TILESIZE(ds), TILESIZE(ds));

    /*
     * Draw the region colour.
     */
    dr.draw_rect(COORD(ds,x), COORD(ds,y), TILESIZE(ds), TILESIZE(ds),
	      (tv == FOUR ? COL_BACKGROUND : COL_0 + tv));
    /*
     * Draw the second region colour, if this is a diagonally
     * divided square.
     */
    if (map.map[TE * wh + y*w+x] != map.map[BE * wh + y*w+x]) {
        int[] coords = new int[6];
        coords[0] = COORD(ds,x)-1;
        coords[1] = COORD(ds,y+1)+1;
        if (map.map[LE * wh + y*w+x] == map.map[TE * wh + y*w+x])
            coords[2] = COORD(ds,x+1)+1;
        else
            coords[2] = COORD(ds,x)-1;
        coords[3] = COORD(ds,y)-1;
        coords[4] = COORD(ds,x+1)+1;
        coords[5] = COORD(ds,y+1)+1;
        dr.draw_polygon(coords, 3,
                     (bv == FOUR ? COL_BACKGROUND : COL_0 + bv), COL_GRID);
    }

    /*
     * Draw `pencil marks'. Currently we arrange these in a square
     * formation, which means we may be in trouble if the value of
     * FOUR changes later...
     */
    Debug.Assert(FOUR == 4);
    for (yo = 0; yo < 4; yo++)
	for (xo = 0; xo < 4; xo++) {
	    int te = map.map[TE * wh + y*w+x];
	    int e, ee, c;

	    e = (yo < xo && yo < 3-xo ? TE :
		 yo > xo && yo > 3-xo ? BE :
		 xo < 2 ? LE : RE);
	    ee = map.map[e * wh + y*w+x];

	    if (xo != (yo * 2 + 1) % 5)
		continue;
	    c = yo;

	    if ((pencil & ((ee == te ? PENCIL_T_BASE : PENCIL_B_BASE) << c)) == 0)
		continue;

	    if (yo == xo &&
		(map.map[TE * wh + y*w+x] != map.map[LE * wh + y*w+x]))
		continue;	       /* avoid TL-BR diagonal line */
	    if (yo == 3-xo &&
		(map.map[TE * wh + y*w+x] != map.map[RE * wh + y*w+x]))
		continue;	       /* avoid BL-TR diagonal line */

	    dr.draw_circle( COORD(ds,x) + (xo+1)*TILESIZE(ds)/5,
			COORD(ds,y) + (yo+1)*TILESIZE(ds)/5,
			TILESIZE(ds)/7, COL_0 + c, COL_0 + c);
	}

    /*
     * Draw the grid lines, if required.
     */
    if (x <= 0 || map.map[RE*wh+y*w+(x-1)] != map.map[LE*wh+y*w+x])
	dr.draw_rect(COORD(ds,x), COORD(ds,y), 1, TILESIZE(ds), COL_GRID);
    if (y <= 0 || map.map[BE*wh+(y-1)*w+x] != map.map[TE*wh+y*w+x])
	dr.draw_rect(COORD(ds,x), COORD(ds,y), TILESIZE(ds), 1, COL_GRID);
    if (x <= 0 || y <= 0 ||
        map.map[RE*wh+(y-1)*w+(x-1)] != map.map[TE*wh+y*w+x] ||
        map.map[BE*wh+(y-1)*w+(x-1)] != map.map[LE*wh+y*w+x])
	dr.draw_rect(COORD(ds,x), COORD(ds,y), 1, 1, COL_GRID);

    /*
     * Draw error markers.
     */
    for (yo = 0; yo < 3; yo++)
	for (xo = 0; xo < 3; xo++)
	    if ((errs & (ERR_BASE << (yo*3+xo))) != 0)
		draw_error(dr, ds,
			   (COORD(ds,x)*2+TILESIZE(ds)*xo)/2,
			   (COORD(ds,y)*2+TILESIZE(ds)*yo)/2);

    /*
     * Draw region numbers, if desired.
     */
    if (show_numbers != 0) {
        oldj = -1;
        for (i = 0; i < 2; i++) {
            j = map.map[(i!=0?BE:TE)*wh+y*w+x];
            if (oldj == j)
                continue;
            oldj = j;

            xo = map.regionx[j] - 2*x;
            yo = map.regiony[j] - 2*y;
            if (xo >= 0 && xo <= 2 && yo >= 0 && yo <= 2) {
                dr.draw_text((COORD(ds,x)*2+TILESIZE(ds)*xo)/2,
                          (COORD(ds,y)*2 + TILESIZE(ds)*yo) / 2,
                          Drawing.FONT_VARIABLE, 3*TILESIZE(ds)/5,
                          Drawing.ALIGN_HCENTRE | Drawing.ALIGN_VCENTRE,
                          COL_GRID, j.ToString());
            }
        }
    }

    dr.unclip();

    dr.draw_update(COORD(ds,x), COORD(ds,y), TILESIZE(ds), TILESIZE(ds));
}

int flash_type = -1;
const float flash_length = 0.30F;

public override void Redraw(Drawing dr, MapDrawState ds, MapState oldstate, MapState state, int dir, MapUI ui, float animtime, float flashtime)
{
    int w = state.p.w, h = state.p.h, wh = w*h, n = state.p.n;
    int x, y, i;
    int flash;

    //if (ds.drag_visible) {
    //    blitter_load(dr, ds.bl, ds.dragx, ds.dragy);
    //    draw_update(dr, ds.dragx, ds.dragy, TILESIZE(ds) + 3, TILESIZE(ds) + 3);
    //    ds.drag_visible = false;
    //}

    /*
     * The initial contents of the window are not guaranteed and
     * can vary with front ends. To be on the safe side, all games
     * should start by drawing a big background-colour rectangle
     * covering the whole window.
     */
    if (!ds.started) {
	int ww, wh2;

    ComputeSize(state.p, TILESIZE(ds), out ww, out wh2);
    dr.draw_rect(0, 0, ww, wh2, COL_BACKGROUND);
    dr.draw_rect(COORD(ds, 0), COORD(ds, 0), w * TILESIZE(ds) + 1, h * TILESIZE(ds) + 1,
		  COL_GRID);

    dr.draw_update(0, 0, ww, wh2);
	ds.started = true;
    }

    if (flashtime != 0) {
	if (flash_type == 1)
	    flash = (int)(flashtime * FOUR / flash_length);
	else
	    flash = 1 + (int)(flashtime * THREE / flash_length);
    } else
	flash = -1;

    /*
     * Set up the `todraw' array.
     */
    for (y = 0; y < h; y++)
	for (x = 0; x < w; x++) {
	    int tv = state.colouring[state.map.map[TE * wh + y*w+x]];
	    int bv = state.colouring[state.map.map[BE * wh + y*w+x]];
            ulong v;

	    if (tv < 0)
		tv = FOUR;
	    if (bv < 0)
		bv = FOUR;

	    if (flash >= 0) {
		if (flash_type == 1) {
		    if (tv == flash)
			tv = FOUR;
		    if (bv == flash)
			bv = FOUR;
		} else if (flash_type == 2) {
		    if ((flash % 2)!=0)
			tv = bv = FOUR;
		} else {
		    if (tv != FOUR)
			tv = (tv + flash) % FOUR;
		    if (bv != FOUR)
			bv = (bv + flash) % FOUR;
		}
	    }

            v = (ulong)(tv * FIVE + bv);

            /*
             * Add pencil marks.
             */
	    for (i = 0; i < FOUR; i++) {
		if (state.colouring[state.map.map[TE * wh + y*w+x]] < 0 &&
            (state.pencil[state.map.map[TE * wh + y * w + x]] & (1 << i)) != 0)
		    v |= PENCIL_T_BASE << i;
		if (state.colouring[state.map.map[BE * wh + y*w+x]] < 0 &&
		    (state.pencil[state.map.map[BE * wh + y*w+x]] & (1<<i)) != 0)
		    v |= PENCIL_B_BASE << i;
	    }

            if (ui.show_numbers)
                v |= SHOW_NUMBERS;

	    ds.todraw[y*w+x] = v;
	}

    /*
     * Add error markers to the `todraw' array.
     */
    for (i = 0; i < state.map.ngraph; i++) {
	int v1 = state.map.graph[i] / n;
	int v2 = state.map.graph[i] % n;
	int xo, yo;

	if (state.colouring[v1] < 0 || state.colouring[v2] < 0)
	    continue;
	if (state.colouring[v1] != state.colouring[v2])
	    continue;

	x = state.map.edgex[i];
	y = state.map.edgey[i];

	xo = x % 2; x /= 2;
	yo = y % 2; y /= 2;

	ds.todraw[y*w+x] |= ERR_BASE << (yo*3+xo);
	if (xo == 0) {
	    Debug.Assert(x > 0);
	    ds.todraw[y*w+(x-1)] |= ERR_BASE << (yo*3+2);
	}
	if (yo == 0) {
	    Debug.Assert(y > 0);
	    ds.todraw[(y-1)*w+x] |= ERR_BASE << (2*3+xo);
	}
	if (xo == 0 && yo == 0) {
	    Debug.Assert(x > 0 && y > 0);
	    ds.todraw[(y-1)*w+(x-1)] |= ERR_BASE << (2*3+2);
	}
    }

    /*
     * Now actually draw everything.
     */
    for (y = 0; y < h; y++)
	for (x = 0; x < w; x++) {
	    ulong v = ds.todraw[y*w+x];
	    if (ds.drawn[y*w+x] != v) {
		draw_square(dr, ds, state.p, state.map, x, y, v);
		ds.drawn[y*w+x] = v;
	    }
	}

    /*
     * Draw the dragged colour blob if any.
     */
    if ((ui.drag_colour > -2) || ui.cur_visible) {
        int bg, iscur = 0;
        if (ui.drag_colour >= 0)
            bg = COL_0 + ui.drag_colour;
        else if (ui.drag_colour == -1) {
            bg = COL_BACKGROUND;
        } else {
            int r = region_from_coords(state, ds, ui.dragx, ui.dragy);
            int c = (r < 0) ? -1 : state.colouring[r];
            Debug.Assert(ui.cur_visible);
            /*bg = COL_GRID;*/
            bg = (c < 0) ? COL_BACKGROUND : COL_0 + c;
            iscur = 1;
        }

        ds.dragx = ui.dragx - TILESIZE(ds) / 2 - 2;
        ds.dragy = ui.dragy - TILESIZE(ds) / 2 - 2;
        //blitter_save(dr, ds.bl, ds.dragx, ds.dragy);
        dr.draw_circle(ui.dragx, ui.dragy,
                    iscur != 0 ? TILESIZE(ds) / 4 : TILESIZE(ds) / 2, bg, COL_GRID);
	for (i = 0; i < FOUR; i++)
        if ((ui.drag_pencil & (1 << i)) != 0)
            dr.draw_circle(ui.dragx + ((i * 4 + 2) % 10 - 3) * TILESIZE(ds) / 10,
                ui.dragy + (i * 2 - 3) * TILESIZE(ds) / 10,
                TILESIZE(ds) / 8, COL_0 + i, COL_0 + i);
    dr.draw_update(ds.dragx, ds.dragy, TILESIZE(ds) + 3, TILESIZE(ds) + 3);
        //ds.drag_visible = true;
    }
}

//static float game_anim_length(MapState oldstate,
//                              MapState newstate, int dir, MapUI ui)
//{
//    return 0.0F;
//}

//static float game_flash_length(MapState oldstate,
//                               MapState newstate, int dir, MapUI ui)
//{
//    if (!oldstate.completed && newstate.completed &&
//    !oldstate.cheated && !newstate.cheated) {
//    if (flash_type < 0) {
//        string env = getenv("MAP_ALTERNATIVE_FLASH");
//        if (env)
//        flash_type = atoi(env);
//        else
//        flash_type = 0;
//        flash_length = (flash_type == 1 ? 0.50F : 0.30F);
//    }
//    return flash_length;
//    } else
//    return 0.0F;
//}

public override float CompletedFlashDuration(MapSettings settings)
{

        return flash_length;
    
}

//static int game_status(MapState state)
//{
//    return state.completed ? +1 : 0;
//}

//static int game_timing_state(MapState state, MapUI ui)
//{
//    return true;
//}



    }
}
