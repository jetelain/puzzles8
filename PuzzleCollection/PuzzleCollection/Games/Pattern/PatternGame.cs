//#define STYLUS_BASED
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace PuzzleCollection.Games.Pattern
{
    public sealed class PatternGame : GameBase<PatternSettings, PatternState, PatternMove, PatternDrawState, PatternUI>
    {
        const int PREFERRED_TILE_SIZE = 24;
        private static int TILE_SIZE(PatternDrawState ds) { return (ds.tilesize); }
        private static int BORDER(PatternDrawState ds) { return (3 * TILE_SIZE(ds) / 4); }
        private static int TLBORDER(int d) { return ( (d) / 5 + 2 ); }
        private static int GUTTER(PatternDrawState ds) { return (TILE_SIZE(ds) / 2); }
        private static int FROMCOORD(PatternDrawState ds, int d, int x) { return  ( ((x) - (BORDER(ds) + GUTTER(ds) + TILE_SIZE(ds) * TLBORDER(d))) / TILE_SIZE(ds) );}
        private static int SIZE(PatternDrawState ds,int d) { return (2*BORDER(ds) + GUTTER(ds) + TILE_SIZE(ds) * (TLBORDER(d) + (d)));}
        private static double GETTILESIZE(int d, int w) { return ((double)w / (2.0 + (double)TLBORDER(d) + (double)(d)));}
        private static int TOCOORD(PatternDrawState ds, int d, int x) { return (BORDER(ds) + GUTTER(ds) + TILE_SIZE(ds) * (TLBORDER(d) + (x))); }


        const int COL_BACKGROUND=0;
        const int COL_EMPTY=1;
        const int COL_FULL=2;
        const int COL_TEXT=3;
        const int COL_UNKNOWN=4;
        const int COL_GRID=5;
        const int COL_CURSOR=6;
        const int COL_ERROR=7;
        const int NCOLOURS = 8;

        const byte GRID_UNKNOWN = 2;
        const byte GRID_FULL = 1;
        const byte GRID_EMPTY = 0;

        const float FLASH_TIME = 0.13f;

        private static PatternSettings[] presets = new[] 
        {
            new PatternSettings(10, 10),
            new PatternSettings(15, 15),
            new PatternSettings(20, 20),
            new PatternSettings(25, 25),
            new PatternSettings(30, 30)
        };

        public override PatternSettings DefaultSettings
        {
            get { return presets[1]; }
        }

        public override IEnumerable<PatternSettings> PresetsSettings
        {
            get { return presets; }
        }

        public override PatternMove ParseMove(PatternSettings settings, string moveString)
        {
            return PatternMove.Parse(settings, moveString);
        }

        public override PatternSettings ParseSettings(string settingsString)
        {
            return PatternSettings.Parse(settingsString);
        }

        /* ----------------------------------------------------------------------
         * Puzzle generation code.
         * 
         * For this particular puzzle, it seemed important to me to ensure
         * a unique solution. I do this the brute-force way, by having a
         * solver algorithm alongside the generator, and repeatedly
         * generating a random grid until I find one whose solution is
         * unique. It turns out that this isn't too onerous on a modern PC
         * provided you keep grid size below around 30. Any offers of
         * better algorithms, however, will be very gratefully received.
         * 
         * Another annoyance of this approach is that it limits the
         * available puzzles to those solvable by the algorithm I've used.
         * My algorithm only ever considers a single row or column at any
         * one time, which means it's incapable of solving the following
         * difficult example (found by Bella Image around 1995/6, when she
         * and I were both doing maths degrees):
         * 
         *        2  1  2  1 
         *
         *      +--+--+--+--+
         * 1 1  |  |  |  |  |
         *      +--+--+--+--+
         *   2  |  |  |  |  |
         *      +--+--+--+--+
         *   1  |  |  |  |  |
         *      +--+--+--+--+
         *   1  |  |  |  |  |
         *      +--+--+--+--+
         * 
         * Obviously this cannot be solved by a one-row-or-column-at-a-time
         * algorithm (it would require at least one row or column reading
         * `2 1', `1 2', `3' or `4' to get started). However, it can be
         * proved to have a unique solution: if the top left square were
         * empty, then the only option for the top row would be to fill the
         * two squares in the 1 columns, which would imply the squares
         * below those were empty, leaving no place for the 2 in the second
         * row. Contradiction. Hence the top left square is full, and the
         * unique solution follows easily from that starting point.
         * 
         * (The game ID for this puzzle is 4x4:2/1/2/1/1.1/2/1/1 , in case
         * it's useful to anyone.)
         */

        static int float_compare(double a, double b)
        {
            if (a < b)
                return -1;
            else if (a > b)
                return +1;
            else
                return 0;
        }

        

        static void generate(Random rs, int w, int h, byte []retgrid)
        {
            double[] fgrid;
            double[] fgrid2;
            int step, i, j;
            double threshold;

            fgrid = new double[w*h];

            for (i = 0; i < h; i++) {
                for (j = 0; j < w; j++) {
                    fgrid[i*w+j] = rs.NextDouble();
                }
            }

            /*
             * The above gives a completely random splattering of black and
             * white cells. We want to gently bias this in favour of _some_
             * reasonably thick areas of white and black, while retaining
             * some randomness and fine detail.
             * 
             * So we evolve the starting grid using a cellular automaton.
             * Currently, I'm doing something very simple indeed, which is
             * to set each square to the average of the surrounding nine
             * cells (or the average of fewer, if we're on a corner).
             */
            for (step = 0; step < 1; step++) {
                fgrid2 = new double[w*h];

                for (i = 0; i < h; i++) {
                    for (j = 0; j < w; j++) {
                        double sx, xbar;
                        int n, p, q;

                        /*
                         * Compute the average of the surrounding cells.
                         */
                        n = 0;
                        sx = 0d;
                        for (p = -1; p <= +1; p++) {
                            for (q = -1; q <= +1; q++) {
                                if (i+p < 0 || i+p >= h || j+q < 0 || j+q >= w)
                                    continue;
			        /*
			         * An additional special case not mentioned
			         * above: if a grid dimension is 2xn then
			         * we do not average across that dimension
			         * at all. Otherwise a 2x2 grid would
			         * contain four identical squares.
			         */
			        if ((h==2 && p!=0) || (w==2 && q!=0))
			            continue;
                                n++;
                                sx += fgrid[(i+p)*w+(j+q)];
                            }
                        }
                        xbar = sx / n;

                        fgrid2[i*w+j] = xbar;
                    }
                }

                fgrid = fgrid2;
            }

            fgrid2 = new double[w*h];
            Array.Copy(fgrid, fgrid2, w*h);
            Array.Sort(fgrid2,float_compare);
            threshold = fgrid2[w*h/2];
            for (i = 0; i < h; i++) {
                for (j = 0; j < w; j++) {
                    retgrid[i*w+j] = (fgrid[i*w+j] >= threshold ? GRID_FULL :
                                      GRID_EMPTY);
                }
            }
        }

        static int compute_rowdata(int[] ret, IList<byte> start, int len, int step)
        {
            int i, n;

            n = 0;

            for (i = 0; i < len; i++) {
                if (start[i*step] == GRID_FULL) {
                    int runlen = 1;
                    while (i+runlen < len && start[(i+runlen)*step] == GRID_FULL)
                        runlen++;
                    ret[n++] = runlen;
                    i += runlen;
                }

                if (i < len && start[i*step] == GRID_UNKNOWN)
                    return -1;
            }

            return n;
        }

        const int UNKNOWN = 0;
        const int BLOCK = 1;
        const int DOT = 2;
        const int STILL_UNKNOWN = 3;

        static bool do_recurse(byte[] known, IList<byte>deduced,
                               IList<byte>row,
		               IList<byte>minpos_done, IList<byte>maxpos_done,
		               IList<byte>minpos_ok, IList<byte>maxpos_ok,
		               int []data, int len,
                               int freespace, int ndone, int lowest)
        {
            int i, j, k;


            /* This algorithm basically tries all possible ways the given rows of
             * black blocks can be laid out in the row/column being examined.
             * Special care is taken to avoid checking the tail of a row/column
             * if the same conditions have already been checked during this recursion
             * The algorithm also takes care to cut its losses as soon as an
             * invalid (partial) solution is detected.
             */
            if (data[ndone] != 0) {
	        if (lowest >= minpos_done[ndone] && lowest <= maxpos_done[ndone]) {
	            if (lowest >= minpos_ok[ndone] && lowest <= maxpos_ok[ndone]) {
		        for (i=0; i<lowest; i++)
		            deduced[i] |= row[i];
	            }
	            return lowest >= minpos_ok[ndone] && lowest <= maxpos_ok[ndone];
	        } else {
	            if (lowest < minpos_done[ndone]) minpos_done[ndone] = (byte)lowest;
	            if (lowest > maxpos_done[ndone]) maxpos_done[ndone] = (byte)lowest;
	        }
	        for (i=0; i<=freespace; i++) {
	            j = lowest;
	            for (k=0; k<i; k++) {
		        if (known[j] == BLOCK) goto next_iter;
	                row[j++] = DOT;
	            }
	            for (k=0; k<data[ndone]; k++) {
		        if (known[j] == DOT) goto next_iter;
	                row[j++] = BLOCK;
	            }
	            if (j < len) {
		        if (known[j] == BLOCK) goto next_iter;
	                row[j++] = DOT;
	            }
	            if (do_recurse(known, deduced, row, minpos_done, maxpos_done,
	                           minpos_ok, maxpos_ok, data, len, freespace-i, ndone+1, j)) {
	                if (lowest < minpos_ok[ndone]) minpos_ok[ndone] = (byte)lowest;
	                if (lowest + i > maxpos_ok[ndone]) maxpos_ok[ndone] = (byte)(lowest + i);
	                if (lowest + i > maxpos_done[ndone]) maxpos_done[ndone] = (byte)(lowest + i);
	            }
	            next_iter:
	            j++;
	        }
	        return lowest >= minpos_ok[ndone] && lowest <= maxpos_ok[ndone];
            } else {
	        for (i=lowest; i<len; i++) {
	            if (known[i] == BLOCK) return false;
	            row[i] = DOT;
	            }
	        for (i=0; i<len; i++)
	            deduced[i] |= row[i];
	        return true;
            }
        }


        static bool do_row(byte[]known, IList<byte>deduced,
                          IList<byte>row,
                          IList<byte>minpos_done, IList<byte>maxpos_done,
		          IList<byte>minpos_ok, IList<byte>maxpos_ok,
                          IList<byte>start, int len, int step, int[]data,
		          uint []changed
		          )
        {
            int rowlen, i, freespace;
            bool done_any;

            freespace = len+1;
            for (rowlen = 0; data[rowlen] != 0; rowlen++) {
	        minpos_done[rowlen] = minpos_ok[rowlen] = (byte)(len - 1);
	        maxpos_done[rowlen] = maxpos_ok[rowlen] = 0;
	        freespace -= data[rowlen]+1;
            }

            for (i = 0; i < len; i++) {
	        known[i] = start[i*step];
	        deduced[i] = 0;
            }
            for (i = len - 1; i >= 0 && known[i] == DOT; i--)
                freespace--;

            do_recurse(known, deduced, row, minpos_done, maxpos_done, minpos_ok, maxpos_ok, data, len, freespace, 0, 0);

            done_any = false;
            for (i=0; i<len; i++)
	        if (deduced[i] != 0 && deduced[i] != STILL_UNKNOWN && known[i] == 0) {
	            start[i*step] = deduced[i];
	            if (changed != null) changed[i]++;
	            done_any = true;
	        }
            return done_any;
        }

        

        static bool solve_puzzle(PatternState state, byte[]grid,
                                int w, int h,
			        byte[]matrix, byte[]workspace,
			        uint[]changed_h, uint[]changed_w,
			        int[]rowdata
			        )
        {
            int i, j, max;
            bool ok;
            int max_h, max_w;

            Debug.Assert((state!=null) ^ (grid!=null));

            max = Math.Max(w, h);

            Array.Clear(matrix, 0, w*h);

            /* For each column, compute how many squares can be deduced
             * from just the row-data.
             * Later, changed_* will hold how many squares were changed
             * in every row/column in the previous iteration
             * Changed_* is used to choose the next rows / cols to re-examine
             */
            for (i=0; i<h; i++) {
	        int freespace;
	        if (state != null) {
                Array.Copy(state.rowdata, state.rowsize*(w+i), rowdata, 0, max);
	            rowdata[state.rowlen[w+i]] = 0;
	        } else {
	            rowdata[compute_rowdata(rowdata, grid.Segment(i*w), w, 1)] = 0;
	        }
	        for (j=0, freespace=w+1; rowdata[j]!= 0; j++) freespace -= rowdata[j] + 1;
	        for (j=0, changed_h[i]=0; rowdata[j] != 0; j++)
	            if (rowdata[j] > freespace)
		        changed_h[i] += (uint)(rowdata[j] - freespace);
            }
            for (i=0,max_h=0; i<h; i++)
	        if (changed_h[i] > max_h)
	            max_h = (int)changed_h[i];
            for (i=0; i<w; i++) {
	        int freespace;
	        if (state != null) {
                Array.Copy(state.rowdata, state.rowsize*i,rowdata,0, max);
	            rowdata[state.rowlen[i]] = 0;
	        } else {
	            rowdata[compute_rowdata(rowdata, grid.Segment(i), h, w)] = 0;
	        }
	        for (j=0, freespace=h+1; rowdata[j] != 0; j++) freespace -= rowdata[j] + 1;
	        for (j=0, changed_w[i]=0; rowdata[j] != 0; j++)
	            if (rowdata[j] > freespace)
		        changed_w[i] += (uint)(rowdata[j] - freespace);
            }
            for (i=0,max_w=0; i<w; i++)
	        if (changed_w[i] > max_w)
	            max_w = (int)changed_w[i];

            /* Solve the puzzle.
             * Process rows/columns individually. Deductions involving more than one
             * row and/or column at a time are not supported.
             * Take care to only process rows/columns which have been changed since they
             * were previously processed.
             * Also, prioritize rows/columns which have had the most changes since their
             * previous processing, as they promise the greatest benefit.
             * Extremely rectangular grids (e.g. 10x20, 15x40, etc.) are not treated specially.
             */
            do {
	        for (; max_h != 0 && max_h >= max_w; max_h--) {
	            for (i=0; i<h; i++) {
		        if (changed_h[i] >= max_h) {
		            if (state != null) {
			        Array.Copy(state.rowdata,  state.rowsize*(w+i), rowdata, 0, max);
			        rowdata[state.rowlen[w+i]] = 0;
		            } else {
			        rowdata[compute_rowdata(rowdata, grid.Segment(i*w), w, 1)] = 0;
		            }
		            do_row(workspace, workspace.Segment(max), workspace.Segment(2*max),
			           workspace.Segment(3*max), workspace.Segment(4*max),
			           workspace.Segment(5*max), workspace.Segment(6*max),
			           matrix.Segment(i*w), w, 1, rowdata, changed_w
			           );
		            changed_h[i] = 0;
		        }
	            }
	            for (i=0,max_w=0; i<w; i++)
		        if (changed_w[i] > max_w)
		            max_w = (int)changed_w[i];
	        }
	        for (; max_w != 0 && max_w >= max_h; max_w--) {
	            for (i=0; i<w; i++) {
		        if (changed_w[i] >= max_w) {
		            if (state != null) {
			        Array.Copy(state.rowdata, state.rowsize*i, rowdata, 0, max);
			        rowdata[state.rowlen[i]] = 0;
		            } else {
			        rowdata[compute_rowdata(rowdata, grid.Segment(i), h, w)] = 0;
		            }
		            do_row(workspace, workspace.Segment(max), workspace.Segment(2*max),
			           workspace.Segment(3*max), workspace.Segment(4*max),
			           workspace.Segment(5*max), workspace.Segment(6*max),
			           matrix.Segment(i), h, w, rowdata, changed_h
			           );
		            changed_w[i] = 0;
		        }
	            }
	            for (i=0,max_h=0; i<h; i++)
		        if (changed_h[i] > max_h)
		            max_h = (int)changed_h[i];
	        }
            } while (max_h>0 || max_w>0);

            ok = true;
            for (i=0; i<h; i++) {
	        for (j=0; j<w; j++) {
	            if (matrix[i*w+j] == UNKNOWN)
		        ok = false;
	        }
            }

            return ok;
        }

        static byte[]generate_soluble(Random rs, int w, int h)
        {
            int i, j, ntries, max;
            bool ok;
            byte[]grid, matrix, workspace;
            uint[]changed_h, changed_w;
            int[]rowdata;

            max = Math.Max(w, h);

            grid = new byte[w*h];
            /* Allocate this here, to avoid having to reallocate it again for every geneerated grid */
            matrix =new byte[w*h];
            workspace = new byte[max*7];
            changed_h = new uint[max+1];
            changed_w = new uint[max+1];
            rowdata = new int[max+1];

            ntries = 0;

            do {
                ntries++;

                generate(rs, w, h, grid);

                /*
                 * The game is a bit too easy if any row or column is
                 * completely black or completely white. An exception is
                 * made for rows/columns that are under 3 squares,
                 * otherwise nothing will ever be successfully generated.
                 */
                ok = true;
                if (w > 2) {
                    for (i = 0; i < h; i++) {
                        int colours = 0;
                        for (j = 0; j < w; j++)
                            colours |= (grid[i*w+j] == GRID_FULL ? 2 : 1);
                        if (colours != 3)
                            ok = false;
                    }
                }
                if (h > 2) {
                    for (j = 0; j < w; j++) {
                        int colours = 0;
                        for (i = 0; i < h; i++)
                            colours |= (grid[i*w+j] == GRID_FULL ? 2 : 1);
                        if (colours != 3)
                            ok = false;
                    }
                }
                if (!ok)
                    continue;

	        ok = solve_puzzle(null, grid, w, h, matrix, workspace,
			          changed_h, changed_w, rowdata);
            } while (!ok);

            return grid;
        }

        // new_game_desc / new_desc
        public override string GenerateNewGameDescription(PatternSettings @params, Random rs,
			           out string aux, int interactive)
        {
            byte[]grid;
            int i, j, max, rowlen;
            int[] rowdata;

            grid = generate_soluble(rs, @params.w, @params.h);
            max = Math.Max(@params.w, @params.h);
            rowdata = new int[max];

            /*
             * Save the solved game in aux.
             */
            {
	        StringBuilder ai = new StringBuilder();

                /*
                 * String format is exactly the same as a solve move, so we
                 * can just dupstr this in solve_game().
                 */

                ai.Append('S');

                for (i = 0; i < @params.w * @params.h; i++)
                    ai.Append(grid[i] != 0 ? '1' : '0');

	        aux = ai.ToString();
            }

            /*
             * Seed is a slash-separated list of row contents; each row
             * contents section is a dot-separated list of integers. Row
             * contents are listed in the order (columns left to right,
             * then rows top to bottom).
             * 
             * Simplest way to handle memory allocation is to make two
             * passes, first computing the seed size and then writing it
             * out.
             */
            StringBuilder desc = new StringBuilder();
            for (i = 0; i < @params.w + @params.h; i++) {
                if (i < @params.w)
                    rowlen = compute_rowdata(rowdata, grid.Segment(i), @params.h, @params.w);
                else
                    rowlen = compute_rowdata(rowdata, grid.Segment((i-@params.w)*@params.w),
                                             @params.w, 1);
                if (rowlen > 0) {
                    for (j = 0; j < rowlen; j++) {
                        desc.Append(rowdata[j]);
                        if (j+1 < rowlen)
                            desc.Append('.');
                        else
                            desc.Append('/');
                    }
                } else {
                    desc.Append('/');
                }
            }
            if (desc[desc.Length - 1] == '/')
            {
                desc.Remove(desc.Length - 1, 1);
            }
            return desc.ToString();
        }

        string validate_desc(PatternSettings @params, string desc)
        {
            int i, n, rowspace;
            int p;
            int descPos = 0;

            for (i = 0; i < @params.w + @params.h; i++) {
                if (i < @params.w)
                    rowspace = @params.h + 1;
                else
                    rowspace = @params.w + 1;

                if (descPos < desc.Length && char.IsDigit(desc[descPos])) {
                    do {
                        p = descPos;
                        while (descPos < desc.Length && char.IsDigit(desc[descPos])) descPos++;
                        n = int.Parse(desc.Substring(p, descPos - p), NumberFormatInfo.InvariantInfo);
                        rowspace -= n+1;

                        if (rowspace < 0) {
                            if (i < @params.w)
                                return "at least one column contains more numbers than will fit";
                            else
                                return "at least one row contains more numbers than will fit";
                        }
                    } while (desc[descPos++] == '.');
                } else {
                    descPos++;                    /* expect a slash immediately */
                }

                if (desc[-1] == '/') {
                    if (i+1 == @params.w + @params.h)
                        return "too many row/column specifications";
                } else if (desc[-1] == '\0') {
                    if (i+1 < @params.w + @params.h)
                        return "too few row/column specifications";
                } else
                    return "unrecognised character in game specification";
            }

            return null;
        }

        // was new_game
        public override PatternState CreateNewGameFromDescription(PatternSettings @params,
                                    string desc)
        {
            int i;
            PatternState state = new PatternState();

            state.w = @params.w;
            state.h = @params.h;

            state.grid = new byte[state.w * state.h];
            state.grid.SetAll(GRID_UNKNOWN);

            state.rowsize = Math.Max(state.w, state.h);
            state.rowdata = new int[state.rowsize * (state.w + state.h)];
            state.rowlen = new int[state.w + state.h];

            state.completed = state.cheated = false;

            int descPos = 0;
            for (i = 0; i < @params.w + @params.h; i++) {
                state.rowlen[i] = 0;
                if (descPos < desc.Length && char.IsDigit(desc[descPos])) {
                    do {
                        int p = descPos;
                        while (descPos < desc.Length && char.IsDigit(desc[descPos])) descPos++;
                        state.rowdata[state.rowsize * i + state.rowlen[i]++] =
                            int.Parse(desc.Substring(p, descPos-p), NumberFormatInfo.InvariantInfo);
                    } while (descPos < desc.Length && desc[descPos++] == '.');
                } else {
                    descPos++;                    /* expect a slash immediately */
                }
            }

            return state;
        }












        
        static PatternState dup_game(PatternState state)
        {
            PatternState ret = new PatternState();

            ret.w = state.w;
            ret.h = state.h;

            ret.grid = new byte[ret.w * ret.h];
            Array.Copy(state.grid, ret.grid, ret.w * ret.h);

            ret.rowsize = state.rowsize;
            ret.rowdata = new int[ret.rowsize * (ret.w + ret.h)];
            ret.rowlen = new int[ret.w + ret.h];
            Array.Copy( state.rowdata,ret.rowdata,
                   ret.rowsize * (ret.w + ret.h) );
            Array.Copy(state.rowlen,ret.rowlen, 
                   (ret.w + ret.h) );

            ret.completed = state.completed;
            ret.cheated = state.cheated;

            return ret;
        }

        // was solve_game
        public override PatternMove CreateSolveGameMove(PatternState state, PatternState currstate,
                                PatternMove ai, out string error)
        {
            byte []matrix;
            int w = state.w, h = state.h;
            int i;
            int max;
            bool ok;
            byte []workspace;
            uint[] changed_h, changed_w;
            int[] rowdata;

            error = null;
            /*
             * If we already have the solved state in ai, copy it out.
             */
            if (ai != null)
                return ai;
            

            max = Math.Max(w, h);
            matrix = new byte[w*h];
            workspace = new byte[max*7];
            changed_h = new uint[max+1];
            changed_w = new uint[max+1];
            rowdata = new int[max+1];

            ok = solve_puzzle(state, null, w, h, matrix, workspace,
		              changed_h, changed_w, rowdata);


            if (!ok) {
	            error = "Solving algorithm cannot complete this puzzle";
	            return null;
            }

            PatternMove ret = new PatternMove();
            ret.type = PatternMoveType.Solve;
            ret.data = new char[w * h];
            for (i = 0; i < w*h; i++) {
	            Debug.Assert(matrix[i] == BLOCK || matrix[i] == DOT);
	            ret.data[i] = matrix[i] == BLOCK ? '1' : '0';
            }
            return ret;
        }

        static bool game_can_format_as_text_now(PatternSettings @params)
        {
            return true;
        }

        static string game_text_format(PatternSettings @state)
        {
            return null;
        }

        public override PatternUI CreateUI(PatternState state)
        {
            PatternUI ret;

            ret = new PatternUI();
            ret.dragging = false;
            ret.cur_x = ret.cur_y = 0;
            ret.cur_visible = false;

            return ret;
        }


        static string encode_ui(PatternUI ui)
        {
            return null;
        }

        static void decode_ui(PatternUI ui, string encoding)
        {
        }

        static void game_changed_state(PatternUI ui, PatternState oldstate,
                                       PatternState newstate)
        {
        }

        internal override void SetKeyboardCursorVisible(PatternUI ui, int tileSize, bool value)
        {
            ui.cur_visible = value;
        }

        // was interpret_move
        public override PatternMove InterpretMove(PatternState state, PatternUI ui,
                                    PatternDrawState ds,
                                    int x, int y, Buttons button, bool isTouchOrStylus)
        {
            button &= ~Buttons.MOD_MASK;

            x = FROMCOORD(ds, state.w, x);
            y = FROMCOORD(ds, state.h, y);

            if (x >= 0 && x < state.w && y >= 0 && y < state.h &&
                (button == Buttons.LEFT_BUTTON || button == Buttons.RIGHT_BUTTON ||
                 button == Buttons.MIDDLE_BUTTON))
            {
        //#if STYLUS_BASED
                int currstate = state.grid[y * state.w + x];
        //#endif

                ui.dragging = true;

                if (button == Buttons.LEFT_BUTTON)
                {
                    ui.drag = Buttons.LEFT_DRAG;
                    ui.release = Buttons.LEFT_RELEASE;
                    if (isTouchOrStylus)
                    {
                        ui.state = currstate; //(currstate + 2) % 3; /* FULL . EMPTY . UNKNOWN */
                    }
                    else
                    {
                        ui.state = GRID_FULL;
                    }
                }
                else if (button == Buttons.RIGHT_BUTTON)
                {
                    ui.drag = Buttons.RIGHT_DRAG;
                    ui.release = Buttons.RIGHT_RELEASE;
                    if (isTouchOrStylus)
                    {
                        ui.state = currstate; //(currstate + 1) % 3; /* EMPTY . FULL . UNKNOWN */
                    }
                    else
                    {
                        ui.state = GRID_EMPTY;
                    }
                } else /* if (button == MIDDLE_BUTTON) */ {
                    ui.drag = Buttons.MIDDLE_DRAG;
                    ui.release = Buttons.MIDDLE_RELEASE;
                    ui.state = GRID_UNKNOWN;
                }

                ui.drag_start_x = ui.drag_end_x = x;
                ui.drag_start_y = ui.drag_end_y = y;
                ui.cur_visible = false;

                return null;		       /* UI activity occurred */
            }

            if (ui.dragging && button == ui.drag) {
                /*
                 * There doesn't seem much point in allowing a rectangle
                 * drag; people will generally only want to drag a single
                 * horizontal or vertical line, so we make that easy by
                 * snapping to it.
                 * 
                 * Exception: if we're _middle_-button dragging to tag
                 * things as UNKNOWN, we may well want to trash an entire
                 * area and start over!
                 */
                if (ui.state != GRID_UNKNOWN) {
                    if (Math.Abs(x - ui.drag_start_x) > Math.Abs(y - ui.drag_start_y))
                        y = ui.drag_start_y;
                    else
                        x = ui.drag_start_x;
                }

                if (x < 0) x = 0;
                if (y < 0) y = 0;
                if (x >= state.w) x = state.w - 1;
                if (y >= state.h) y = state.h - 1;

                ui.drag_end_x = x;
                ui.drag_end_y = y;

                return null;		       /* UI activity occurred */
            }

            if (ui.dragging && button == ui.release) {
                int x1, x2, y1, y2, xx, yy;
                bool move_needed = false;

                if (isTouchOrStylus)
                {
                    if (ui.drag_start_x == ui.drag_end_x && ui.drag_end_y == ui.drag_start_y)
                    {
                        // If user drag on more than one tile, simply expands origin color
                        ui.state = (ui.state + 2) % 3; /* FULL . EMPTY . UNKNOWN */
                    }
                }

                x1 = Math.Min(ui.drag_start_x, ui.drag_end_x);
                x2 = Math.Max(ui.drag_start_x, ui.drag_end_x);
                y1 = Math.Min(ui.drag_start_y, ui.drag_end_y);
                y2 = Math.Max(ui.drag_start_y, ui.drag_end_y);

                for (yy = y1; yy <= y2; yy++)
                    for (xx = x1; xx <= x2; xx++)
                        if (state.grid[yy * state.w + xx] != ui.state)
                            move_needed = true;

                ui.dragging = false;

                if (move_needed) {
                    return new PatternMove()
                    {
                        type =  (ui.state == GRID_FULL ? PatternMoveType.Full :
		                   ui.state == GRID_EMPTY ? PatternMoveType.Empty : PatternMoveType.Undefine),
                        x1 = x1,
                        y1 = y1,
                        x2 = x2-x1+1,
                        y2 = y2-y1+1
                    };
                } else
                    return null;		       /* UI activity occurred */
            }

            if (Misc.IS_CURSOR_MOVE(button)) {
                Misc.move_cursor(button, ref ui.cur_x, ref ui.cur_y, state.w, state.h, false);
                ui.cur_visible = true;
                return null;
            }
            if (Misc.IS_CURSOR_SELECT(button))
            {
                int currstate = state.grid[ui.cur_y * state.w + ui.cur_x];
                int newstate;

                if (!ui.cur_visible) {
                    ui.cur_visible = true;
                    return null;
                }

                if (button == Buttons.CURSOR_SELECT2)
                    newstate = currstate == GRID_UNKNOWN ? GRID_EMPTY :
                        currstate == GRID_EMPTY ? GRID_FULL : GRID_UNKNOWN;
                else
                    newstate = currstate == GRID_UNKNOWN ? GRID_FULL :
                        currstate == GRID_FULL ? GRID_EMPTY : GRID_UNKNOWN;

                return new PatternMove()
                    {
                        type =  (newstate == GRID_FULL ? PatternMoveType.Full :
		                   newstate == GRID_EMPTY ? PatternMoveType.Empty : PatternMoveType.Undefine),
                        x1 = ui.cur_x,
                        y1 = ui.cur_y,
                        x2 = 1,
                        y2 = 1
                    };

            }

            return null;
        }
        // was execute_move
        public override PatternState ExecuteMove(PatternState from, PatternMove move)
        {
            PatternState ret;
            int x1 = move.x1, x2 = move.x2, y1 = move.y1, y2 = move.y2, xx, yy;
            int val;

            if (move.type == PatternMoveType.Solve && move.data.Length == from.w * from.h) {
	        int i;

	        ret = dup_game(from);

	        for (i = 0; i < ret.w * ret.h; i++)
	            ret.grid[i] = (move.data[i] == '1' ? GRID_FULL : GRID_EMPTY);

	        ret.completed = ret.cheated = true;

	        return ret;
            } else if ( x1 >= 0 && x2 >= 0 && x1+x2 <= from.w &&
	        y1 >= 0 && y2 >= 0 && y1+y2 <= from.h) {

	        x2 += x1;
	        y2 += y1;
	        val = (move.type == PatternMoveType.Full ? GRID_FULL :
		         move.type == PatternMoveType.Empty ? GRID_EMPTY : GRID_UNKNOWN);

	        ret = dup_game(from);
	        for (yy = y1; yy < y2; yy++)
	            for (xx = x1; xx < x2; xx++)
		        ret.grid[yy * ret.w + xx] = (byte)val;

	        /*
	         * An actual change, so check to see if we've completed the
	         * game.
	         */
	        if (!ret.completed) {
	            int[] rowdata = new int[ret.rowsize];
	            int i, len;

	            ret.completed = true;

	            for (i=0; i<ret.w; i++) {
		        len = compute_rowdata(rowdata,
				              ret.grid.Segment(i), ret.h, ret.w);
 
		        if (len != ret.rowlen[i] || rowdata.CompareTo(0, ret.rowdata, i*ret.rowsize, 
			           len) != 0)
                {
		            ret.completed = false;
		            break;
		        }
	            }
	            for (i=0; i<ret.h; i++) {
		        len = compute_rowdata(rowdata,
				              ret.grid.Segment(i*ret.w), ret.w, 1);
		        if (len != ret.rowlen[i+ret.w] ||
                    rowdata.CompareTo(0, ret.rowdata, (i+ret.w)*ret.rowsize, 
			           len) != 0)
                {
		            ret.completed = false;
		            break;
		        }
	            }
	        }

	        return ret;
            } else
	        return null;
        }

        /* ----------------------------------------------------------------------
         * Error-checking during gameplay.
         */

        /*
         * The difficulty in error-checking Pattern is to make the error check
         * _weak_ enough. The most obvious way would be to check each row and
         * column by calling (a modified form of) do_row() to recursively
         * analyse the row contents against the clue set and see if the
         * GRID_UNKNOWNs could be filled in in any way that would end up
         * correct. However, this turns out to be such a strong error check as
         * to constitute a spoiler in many situations: you make a typo while
         * trying to fill in one row, and not only does the row light up to
         * indicate an error, but several columns crossed by the move also
         * light up and draw your attention to deductions you hadn't even
         * noticed you could make.
         *
         * So instead I restrict error-checking to 'complete runs' within a
         * row, by which I mean contiguous sequences of GRID_FULL bounded at
         * both ends by either GRID_EMPTY or the ends of the row. We identify
         * all the complete runs in a row, and verify that _those_ are
         * consistent with the row's clue list. Sequences of complete runs
         * separated by solid GRID_EMPTY are required to match contiguous
         * sequences in the clue list, whereas if there's at least one
         * GRID_UNKNOWN between any two complete runs then those two need not
         * be contiguous in the clue list.
         *
         * To simplify the edge cases, I pretend that the clue list for the
         * row is extended with a 0 at each end, and I also pretend that the
         * grid data for the row is extended with a GRID_EMPTY and a
         * zero-length run at each end. This permits the contiguity checker to
         * handle the fiddly end effects (e.g. if the first contiguous
         * sequence of complete runs in the grid matches _something_ in the
         * clue list but not at the beginning, this is allowable iff there's a
         * GRID_UNKNOWN before the first one) with minimal faff, since the end
         * effects just drop out as special cases of the normal inter-run
         * handling (in this code the above case is not 'at the end of the
         * clue list' at all, but between the implicit initial zero run and
         * the first nonzero one).
         *
         * We must also be a little careful about how we search for a
         * contiguous sequence of runs. In the clue list (1 1 2 1 2 3),
         * suppose we see a GRID_UNKNOWN and then a length-1 run. We search
         * for 1 in the clue list and find it at the very beginning. But now
         * suppose we find a length-2 run with no GRID_UNKNOWN before it. We
         * can't naively look at the next clue from the 1 we found, because
         * that'll be the second 1 and won't match. Instead, we must backtrack
         * by observing that the 2 we've just found must be contiguous with
         * the 1 we've already seen, so we search for the sequence (1 2) and
         * find it starting at the second 1. Now if we see a 3, we must
         * rethink again and search for (1 2 3).
         */

        class errcheck_state {
            /*
             * rowdata and rowlen point at the clue data for this row in the
             * game state.
             */
            internal IList<int> rowdata;
            internal int rowlen;
            /*
             * rowpos indicates the lowest position where it would be valid to
             * see our next run length. It might be equal to rowlen,
             * indicating that the next run would have to be the terminating 0.
             */
            internal int rowpos;
            /*
             * ncontig indicates how many runs we've seen in a contiguous
             * block. This is taken into account when searching for the next
             * run we find, unless ncontig is zeroed out first by encountering
             * a GRID_UNKNOWN.
             */
            internal int ncontig;
        };

        static int ROWDATA(errcheck_state es,int k)
        {
            return ((k) < 0 || (k) >= es.rowlen ? 0 : es.rowdata[(k)]);
        }

        static bool errcheck_found_run(errcheck_state es, int r)
        {
        /* Macro to handle the pretence that rowdata has a 0 at each end */

            /*
             * See if we can find this new run length at a position where it
             * also matches the last 'ncontig' runs we've seen.
             */
            int i, newpos;
            for (newpos = es.rowpos; newpos <= es.rowlen; newpos++) {

                if (ROWDATA(es, newpos) != r)
                    goto notfound;

                for (i = 1; i <= es.ncontig; i++)
                    if (ROWDATA(es, newpos - i) != ROWDATA(es, es.rowpos - i))
                        goto notfound;

                es.rowpos = newpos+1;
                es.ncontig++;
                return true;

              notfound:;
            }

            return false;

        }

        static bool check_errors(PatternState state, int i)
        {
            int start, step, end, j;
            int val, runlen;
            errcheck_state aes = new errcheck_state();
            errcheck_state es = aes;

            es.rowlen = state.rowlen[i];
            es.rowdata = state.rowdata.Segment(state.rowsize * i);
            /* Pretend that we've already encountered the initial zero run */
            es.ncontig = 1;
            es.rowpos = 0;

            if (i < state.w) {
                start = i;
                step = state.w;
                end = start + step * state.h;
            } else {
                start = (i - state.w) * state.w;
                step = 1;
                end = start + step * state.w;
            }

            runlen = -1;
            for (j = start - step; j <= end; j += step) {
                if (j < start || j == end)
                    val = GRID_EMPTY;
                else
                    val = state.grid[j];

                if (val == GRID_UNKNOWN) {
                    runlen = -1;
                    es.ncontig = 0;
                } else if (val == GRID_FULL) {
                    if (runlen >= 0)
                        runlen++;
                } else if (val == GRID_EMPTY) {
                    if (runlen > 0) {
                        if (!errcheck_found_run(es, runlen))
                            return true;       /* error! */
                    }
                    runlen = 0;
                }
            }

            /* Signal end-of-row by sending errcheck_found_run the terminating
             * zero run, which will be marked as contiguous with the previous
             * run if and only if there hasn't been a GRID_UNKNOWN before. */
            if (!errcheck_found_run(es, 0))
                return true;                   /* error at the last minute! */

            return false;                      /* no error */
        }


        // was game_compute_size
        public override void ComputeSize(PatternSettings @params, int tilesize,
                                      out int x, out int y)
        {
            /* Ick: fake up `ds.tilesize' for macro expansion purposes */
            PatternDrawState ds = new PatternDrawState(){ tilesize = tilesize};

            x = SIZE(ds,@params.w);
            y = SIZE(ds,@params.h);
        }

        // was game_set_size
        public override void SetTileSize(Drawing dr, PatternDrawState ds,
                                   PatternSettings @params, int tilesize)
        {
            ds.tilesize = tilesize;
        }

        // was game_colours
        public override float[] GetColours(Frontend fe, out int ncolours)
        {
            float[] ret = new float[3 * NCOLOURS];
            int i;

            fe.frontend_default_colour(ret, COL_BACKGROUND * 3);

            for (i = 0; i < 3; i++) {
                ret[COL_GRID    * 3 + i] = 0.3F;
                ret[COL_UNKNOWN * 3 + i] = 0.5F;
                ret[COL_TEXT    * 3 + i] = 0.0F;
                ret[COL_FULL    * 3 + i] = 0.0F;
                ret[COL_EMPTY   * 3 + i] = 1.0F;
            }
            ret[COL_CURSOR * 3 + 0] = 1.0F;
            ret[COL_CURSOR * 3 + 1] = 0.25F;
            ret[COL_CURSOR * 3 + 2] = 0.25F;
            ret[COL_ERROR * 3 + 0] = 1.0F;
            ret[COL_ERROR * 3 + 1] = 0.0F;
            ret[COL_ERROR * 3 + 2] = 0.0F;

            ncolours = NCOLOURS;
            return ret;
        }

        // was game_new_drawstate
        public override PatternDrawState CreateDrawState(Drawing dr, PatternState state)
        {
            PatternDrawState ds = new PatternDrawState();

            ds.started = false;
            ds.w = state.w;
            ds.h = state.h;
            ds.visible = new byte[ds.w * ds.h];
            ds.tilesize = 0;                  /* not decided yet */
            ds.visible.SetAll((byte)255);
            ds.numcolours = new byte[ds.w + ds.h];
            ds.numcolours.SetAll((byte)255);
            ds.cur_x = ds.cur_y = 0;

            return ds;
        }

        private static void grid_square(Drawing dr, PatternDrawState ds,
                                int y, int x, int state, bool cur)
        {
            int xl, xr, yt, yb, dx, dy, dw, dh;

            dr.draw_rect(TOCOORD(ds, ds.w, x), TOCOORD(ds, ds.h, y),
                      TILE_SIZE(ds), TILE_SIZE(ds), COL_GRID);

            xl = (x % 5 == 0 ? 1 : 0);
            yt = (y % 5 == 0 ? 1 : 0);
            xr = (x % 5 == 4 || x == ds.w-1 ? 1 : 0);
            yb = (y % 5 == 4 || y == ds.h-1 ? 1 : 0);

            dx = TOCOORD(ds, ds.w, x) + 1 + xl;
            dy = TOCOORD(ds,ds.h, y) + 1 + yt;
            dw = TILE_SIZE(ds) - xl - xr - 1;
            dh = TILE_SIZE(ds) - yt - yb - 1;

            dr.draw_rect(dx, dy, dw, dh,
                      (state == GRID_FULL ? COL_FULL :
                       state == GRID_EMPTY ? COL_EMPTY : COL_UNKNOWN));
            if (cur) {
                dr.draw_rect_outline(dx, dy, dw, dh, COL_CURSOR);
                dr.draw_rect_outline(dx + 1, dy + 1, dw - 2, dh - 2, COL_CURSOR);
            }

            dr.draw_update(TOCOORD(ds, ds.w, x), TOCOORD(ds, ds.h, y),
                        TILE_SIZE(ds), TILE_SIZE(ds));
        }

        /*
         * Draw the numbers for a single row or column.
         */
        private static void draw_numbers(Drawing dr, PatternDrawState ds,
                                 PatternState state, int i, bool erase, int colour)
        {
            int rowlen = state.rowlen[i];
            IList<int> rowdata = state.rowdata.Segment(state.rowsize * i);
            int nfit;
            int j;

            if (erase) {
                if (i < state.w) {
                    dr.draw_rect(TOCOORD(ds,state.w, i), 0,
                              TILE_SIZE(ds), BORDER(ds) + TLBORDER(state.h) * TILE_SIZE(ds),
                              COL_BACKGROUND);
                } else {
                    dr.draw_rect(0, TOCOORD(ds, state.h, i - state.w),
                              BORDER(ds) + TLBORDER(state.w) * TILE_SIZE(ds), TILE_SIZE(ds),
                              COL_BACKGROUND);
                }
            }

            /*
             * Normally I space the numbers out by the same distance as the
             * tile size. However, if there are more numbers than available
             * spaces, I have to squash them up a bit.
             */
            if (i < state.w)
                nfit = TLBORDER(state.h);
            else
                nfit = TLBORDER(state.w);
            nfit = Math.Max(rowlen, nfit) - 1;
            Debug.Assert(nfit > 0);

            for (j = 0; j < rowlen; j++) {
                int x, y;

                if (i < state.w) {
                    x = TOCOORD(ds, state.w, i);
                    y = BORDER(ds) + TILE_SIZE(ds) * (TLBORDER(state.h) - 1);
                    y -= ((rowlen - j - 1) * TILE_SIZE(ds)) * (TLBORDER(state.h) - 1) / nfit;
                } else {
                    y = TOCOORD(ds, state.h, i - state.w);
                    x = BORDER(ds) + TILE_SIZE(ds) * (TLBORDER(state.w) - 1);
                    x -= ((rowlen - j - 1) * TILE_SIZE(ds)) * (TLBORDER(state.w) - 1) / nfit;
                }

                dr.draw_text(x + TILE_SIZE(ds) / 2, y + TILE_SIZE(ds) / 2, Drawing.FONT_VARIABLE,
                          TILE_SIZE(ds) / 2, Drawing.ALIGN_HCENTRE | Drawing.ALIGN_VCENTRE, colour, rowdata[j].ToString());
            }

            if (i < state.w) {
                dr.draw_update( TOCOORD(ds, state.w, i), 0,
                            TILE_SIZE(ds), BORDER(ds) + TLBORDER(state.h) * TILE_SIZE(ds));
            } else {
                dr.draw_update(0, TOCOORD(ds, state.h, i - state.w),
                            BORDER(ds) + TLBORDER(state.w) * TILE_SIZE(ds), TILE_SIZE(ds));
            }
        }

        // was game_redraw
        public override void Redraw(Drawing dr, PatternDrawState ds,
                                PatternState oldstate, PatternState state,
                                int dir, PatternUI ui,
                                float animtime, float flashtime)
        {
            int i, j;
            int x1, x2, y1, y2;
            int cx, cy;
            bool cmoved;

            if (!ds.started) {
                /*
                 * The initial contents of the window are not guaranteed
                 * and can vary with front ends. To be on the safe side,
                 * all games should start by drawing a big background-
                 * colour rectangle covering the whole window.
                 */
                dr.draw_rect(0, 0, SIZE(ds, ds.w), SIZE(ds, ds.h), COL_BACKGROUND);

                /*
                 * Draw the grid outline.
                 */
                dr.draw_rect(TOCOORD(ds, ds.w, 0) - 1, TOCOORD(ds, ds.h, 0) - 1,
                          ds.w * TILE_SIZE(ds) + 3, ds.h * TILE_SIZE(ds) + 3,
                          COL_GRID);

                ds.started = true;

                dr.draw_update(0, 0, SIZE(ds, ds.w), SIZE(ds, ds.h));
            }

            if (ui.dragging) {
                x1 = Math.Min(ui.drag_start_x, ui.drag_end_x);
                x2 = Math.Max(ui.drag_start_x, ui.drag_end_x);
                y1 = Math.Min(ui.drag_start_y, ui.drag_end_y);
                y2 = Math.Max(ui.drag_start_y, ui.drag_end_y);
            } else {
                x1 = x2 = y1 = y2 = -1;        /* placate gcc warnings */
            }

            if (ui.cur_visible) {
                cx = ui.cur_x; cy = ui.cur_y;
            } else {
                cx = cy = -1;
            }
            cmoved = (cx != ds.cur_x || cy != ds.cur_y);

            /*
             * Now draw any grid squares which have changed since last
             * redraw.
             */
            for (i = 0; i < ds.h; i++) {
                for (j = 0; j < ds.w; j++) {
                    int val;
                    bool cc = false;

                    /*
                     * Work out what state this square should be drawn in,
                     * taking any current drag operation into account.
                     */
                    if (ui.dragging && x1 <= j && j <= x2 && y1 <= i && i <= y2)
                        val = ui.state;
                    else
                        val = state.grid[i * state.w + j];

                    if (cmoved) {
                        /* the cursor has moved; if we were the old or
                         * the new cursor position we need to redraw. */
                        if (j == cx && i == cy) cc = true;
                        if (j == ds.cur_x && i == ds.cur_y) cc = true;
                    }

                    /*
                     * Briefly invert everything twice during a completion
                     * flash.
                     */
                    if (flashtime > 0 &&
                        (flashtime <= FLASH_TIME/3 || flashtime >= FLASH_TIME*2/3) &&
                        val != GRID_UNKNOWN)
                        val = (GRID_FULL ^ GRID_EMPTY) ^ val;

                    if (ds.visible[i * ds.w + j] != val || cc) {
                        grid_square(dr, ds, i, j, val,
                                    (j == cx && i == cy));
                        ds.visible[i * ds.w + j] = (byte)val;
                    }
                }
            }
            ds.cur_x = cx; ds.cur_y = cy;

            /*
             * Redraw any numbers which have changed their colour due to error
             * indication.
             */
            for (i = 0; i < state.w + state.h; i++) {
                int colour = check_errors(state, i) ? COL_ERROR : COL_TEXT;
                if (ds.numcolours[i] != colour) {
                    draw_numbers(dr, ds, state, i, true, colour);
                    ds.numcolours[i] = (byte)colour;
                }
            }
        }

        //static float game_anim_length(PatternState oldstate,
        //                              PatternState newstate, int dir, PatternUI ui)
        //{
        //    return 0.0F;
        //}

        public override float CompletedFlashDuration(PatternSettings settings)
        {

                return FLASH_TIME;
            
        }

        //static float game_flash_length(PatternState oldstate,
        //                               PatternState newstate, int dir, PatternUI ui)
        //{
        //    if (!oldstate.completed && newstate.completed &&
        //    !oldstate.cheated && !newstate.cheated)
        //        return FLASH_TIME;
        //    return 0.0F;
        //}

        //static int game_status(PatternState state)
        //{
        //    return state.completed ? +1 : 0;
        //}

        //static bool game_timing_state(PatternState state, PatternUI ui)
        //{
        //    return true;
        //}

        //static void game_print_size(PatternSettings @params, out float x, out float y)
        //{
        //    int pw , ph;

        //    /*
        //     * I'll use 5mm squares by default.
        //     */
        //    game_compute_size(@params, 500, out pw, out ph);
        //    x = pw / 100.0F;
        //    y = ph / 100.0F;
        //}

        //static void game_print(Drawing dr, PatternState state, int tilesize)
        //{
        //    int w = state.w, h = state.h;
        //    int ink = dr.print_mono_colour(0);
        //    int x, y, i;

        //    /* Ick: fake up `ds.tilesize' for macro expansion purposes */
        //    PatternDrawState ds = new PatternDrawState();
        //    game_set_size(dr, ds, null, tilesize);

        //    /*
        //     * Border.
        //     */
        //    dr.print_line_width(TILE_SIZE(ds) / 16);
        //    dr.draw_rect_outline(TOCOORD(ds, w, 0), TOCOORD(ds, h, 0),
        //              w * TILE_SIZE(ds), h * TILE_SIZE(ds), ink);

        //    /*
        //     * Grid.
        //     */
        //    for (x = 1; x < w; x++) {
        //        dr.print_line_width(TILE_SIZE(ds) / (x % 5 != 0 ? 128 : 24));
        //    dr.draw_line(TOCOORD(ds, w, x), TOCOORD(ds, h, 0),
        //          TOCOORD(ds, w, x), TOCOORD(ds, h, h), ink);
        //    }
        //    for (y = 1; y < h; y++) {
        //        dr.print_line_width(TILE_SIZE(ds) / (y % 5 != 0 ? 128 : 24));
        //    dr.draw_line(TOCOORD(ds, w, 0), TOCOORD(ds, h, y),
        //          TOCOORD(ds, w, w), TOCOORD(ds, h, y), ink);
        //    }

        //    /*
        //     * Clues.
        //     */
        //    for (i = 0; i < state.w + state.h; i++)
        //        draw_numbers(dr, ds, state, i, false, ink);

        //    /*
        //     * Solution.
        //     */
        //    dr.print_line_width(TILE_SIZE(ds) / 128);
        //    for (y = 0; y < h; y++)
        //    for (x = 0; x < w; x++) {
        //        if (state.grid[y*w+x] == GRID_FULL)
        //            dr.draw_rect(TOCOORD(ds, w, x), TOCOORD(ds, h, y),
        //              TILE_SIZE(ds), TILE_SIZE(ds), ink);
        //        else if (state.grid[y*w+x] == GRID_EMPTY)
        //            dr.draw_circle(TOCOORD(ds, w, x) + TILE_SIZE(ds) / 2,
        //                TOCOORD(ds, h, y) + TILE_SIZE(ds) / 2,
        //                TILE_SIZE(ds) / 12, ink, ink);
        //    }
        //}








    }
}
