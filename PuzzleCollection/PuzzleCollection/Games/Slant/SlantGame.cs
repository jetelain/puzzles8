using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Slant
{
    public sealed class SlantGame : GameBase<SlantSettings, SlantState, SlantMove, SlantDrawState, SlantUI>
    {
const int COL_BACKGROUND=0;
const int     COL_GRID=1;
const int     COL_INK=2;
const int     COL_SLANT1=3;
const int     COL_SLANT2=4;
const int     COL_ERROR=5;
const int     COL_CURSOR=6;
const int     COL_FILLEDSQUARE=7;
const int     NCOLOURS=8;

const int DIFF_EASY = 0;
const int DIFF_HARD = 1;


const int ERR_VERTEX= 1;
const int ERR_SQUARE =2;


private static readonly SlantSettings[] slant_presets = new SlantSettings[]{
    new SlantSettings(5, 5, DIFF_EASY),
    new SlantSettings(5, 5, DIFF_HARD),
    new SlantSettings(8, 8, DIFF_EASY), // <= DEFAULT
    new SlantSettings(8, 8, DIFF_HARD),
    new SlantSettings(12, 10, DIFF_EASY),
    new SlantSettings(12, 10, DIFF_HARD),
};

public override IEnumerable<SlantSettings> PresetsSettings
{
    get { return slant_presets; }
}

public override SlantSettings DefaultSettings
{
    get { return slant_presets[2]; }
}
public override SlantSettings ParseSettings(string settingsString)
{
    return SlantSettings.Parse(settingsString);
}

static SlantSolverScratch new_scratch(int w, int h)
{
    int W = w+1, H = h+1;
    SlantSolverScratch ret = new SlantSolverScratch ();
    ret.connected = new  int[W*H];
    ret.exits = new  int[W*H];
    ret.border = new  bool[W*H];
    ret.equiv = new  int[w*h];
    ret.slashval = new  sbyte[w*h];
    ret.vbitmap = new  byte[w*h];
    return ret;
}


/*
 * Wrapper on Dsf.dsf_merge() which updates the `exits' and `border'
 * arrays.
 */
static void merge_vertices(int []connected,
			   SlantSolverScratch sc, int i, int j)
{
    int exits = -1;
    bool border = false;    /* initialise to placate optimiser */

    if (sc!=null) {
	i = Dsf.dsf_canonify(connected, i);
	j = Dsf.dsf_canonify(connected, j);

	/*
	 * We have used one possible exit from each of the two
	 * classes. Thus, the viable exit count of the new class is
	 * the sum of the old exit counts minus two.
	 */
	exits = sc.exits[i] + sc.exits[j] - 2;

	border = sc.border[i] || sc.border[j];
    }

    Dsf.dsf_merge(connected, i, j);

    if (sc!=null) {
	i = Dsf.dsf_canonify(connected, i);
	sc.exits[i] = exits;
	sc.border[i] = border;
    }
}

/*
 * Called when we have just blocked one way out of a particular
 * point. If that point is a non-clue point (thus has a variable
 * number of exits), we have therefore decreased its potential exit
 * count, so we must decrement the exit count for the group as a
 * whole.
 */
static void decr_exits(SlantSolverScratch sc, int i)
{
    if (sc.clues[i] < 0) {
	i = Dsf.dsf_canonify(sc.connected, i);
	sc.exits[i]--;
    }
}

static void fill_square(int w, int h, int x, int y, int v,
			sbyte[] soln,
			int[] connected, SlantSolverScratch sc)
{
    int W = w+1 /*, H = h+1 */;

    Debug.Assert(x >= 0 && x < w && y >= 0 && y < h);

    if (soln[y*w+x] != 0) {
	return;			       /* do nothing */
    }

#if SOLVER_DIAGNOSTICS
    if (verbose)
	printf("  placing %c in %d,%d\n", v == -1 ? '\\' : '/', x, y);
#endif

    soln[y*w+x] = (sbyte)v;

    if (sc!=null) {
	int c = Dsf.dsf_canonify(sc.equiv, y*w+x);
    sc.slashval[c] = (sbyte)v;
    }

    if (v < 0) {
	merge_vertices(connected, sc, y*W+x, (y+1)*W+(x+1));
	if (sc!=null) {
	    decr_exits(sc, y*W+(x+1));
	    decr_exits(sc, (y+1)*W+x);
	}
    } else {
	merge_vertices(connected, sc, y*W+(x+1), (y+1)*W+x);
	if (sc!=null) {
	    decr_exits(sc, y*W+x);
	    decr_exits(sc, (y+1)*W+(x+1));
	}
    }
}

static bool vbitmap_clear(int w, int h, SlantSolverScratch sc,
                         int x, int y, int vbits)
{
    bool done_something = false;
    int vbit;

    for (vbit = 1; vbit <= 8; vbit <<= 1)
        if ((vbits & sc.vbitmap[y*w+x] & vbit)!=0) {
            done_something = true;
#if SOLVER_DIAGNOSTICS
            if (verbose) {
                va_list ap;

                printf("ruling out %c shape at (%d,%d)-(%d,%d) (",
                       "!v^!>!!!<"[vbit], x, y,
                       x+((vbit&0x3)!=0), y+((vbit&0xC)!=0));

                va_start(ap, reason);
                vprintf(reason, ap);
                va_end(ap);

                printf(")\n");
            }
#endif
            sc.vbitmap[y*w+x] = (byte)(sc.vbitmap[y*w+x] & ~vbit);
        }

    return done_something;
}

/*
 * Solver. Returns 0 for impossibility, 1 for success, 2 for
 * ambiguity or failure to converge.
 */
static int slant_solve(int w, int h, sbyte[] clues,
		       sbyte[] soln, SlantSolverScratch sc,
		       int difficulty)
{
    int W = w+1, H = h+1;
    int x, y, i, j;
    bool done_something;

    /*
     * Clear the output.
     */
    soln.SetAll((sbyte) 0);

    sc.clues = clues;

    /*
     * Establish a disjoint set forest for tracking connectedness
     * between grid points.
     */
    Dsf.dsf_init(sc.connected, W*H);

    /*
     * Establish a disjoint set forest for tracking which squares
     * are known to slant in the same direction.
     */
    Dsf.dsf_init(sc.equiv, w*h);

    /*
     * Clear the slashval array.
     */
    sc.slashval.SetAll((sbyte)0);

    /*
     * Set up the vbitmap array. Initially all types of v are possible.
     */
    sc.vbitmap.SetAll((byte)0xF);

    /*
     * Initialise the `exits' and `border' arrays. These are used
     * to do second-order loop avoidance: the dual of the no loops
     * constraint is that every point must be somehow connected to
     * the border of the grid (otherwise there would be a solid
     * loop around it which prevented this).
     * 
     * I define a `dead end' to be a connected group of points
     * which contains no border point, and which can form at most
     * one new connection outside itself. Then I forbid placing an
     * edge so that it connects together two dead-end groups, since
     * this would yield a non-border-connected isolated subgraph
     * with no further scope to extend it.
     */
    for (y = 0; y < H; y++)
	for (x = 0; x < W; x++) {
	    if (y == 0 || y == H-1 || x == 0 || x == W-1)
		sc.border[y*W+x] = true;
	    else
		sc.border[y*W+x] = false;

	    if (clues[y*W+x] < 0)
		sc.exits[y*W+x] = 4;
	    else
		sc.exits[y*W+x] = clues[y*W+x];
	}

    /*
     * Repeatedly try to deduce something until we can't.
     */
    do {
	done_something = false;

	/*
	 * Any clue point with the number of remaining lines equal
	 * to zero or to the number of remaining undecided
	 * neighbouring squares can be filled in completely.
	 */
	for (y = 0; y < H; y++)
	    for (x = 0; x < W; x++) {
		SlantNeighbours[] neighbours = new SlantNeighbours[]{new SlantNeighbours(),new SlantNeighbours(),new SlantNeighbours(),new SlantNeighbours()};
		int nneighbours;
		int nu, nl, c, s, eq, eq2, last, meq, mj1, mj2;

		if ((c = clues[y*W+x]) < 0)
		    continue;

		/*
		 * We have a clue point. Start by listing its
		 * neighbouring squares, in order around the point,
		 * together with the type of slash that would be
		 * required in that square to connect to the point.
		 */
		nneighbours = 0;
		if (x > 0 && y > 0) {
		    neighbours[nneighbours].pos = (y-1)*w+(x-1);
		    neighbours[nneighbours].slash = -1;
		    nneighbours++;
		}
		if (x > 0 && y < h) {
		    neighbours[nneighbours].pos = y*w+(x-1);
		    neighbours[nneighbours].slash = +1;
		    nneighbours++;
		}
		if (x < w && y < h) {
		    neighbours[nneighbours].pos = y*w+x;
		    neighbours[nneighbours].slash = -1;
		    nneighbours++;
		}
		if (x < w && y > 0) {
		    neighbours[nneighbours].pos = (y-1)*w+x;
		    neighbours[nneighbours].slash = +1;
		    nneighbours++;
		}

		/*
		 * Count up the number of undecided neighbours, and
		 * also the number of lines already present.
		 *
		 * If we're not on DIFF_EASY, then in this loop we
		 * also track whether we've seen two adjacent empty
		 * squares belonging to the same equivalence class
		 * (meaning they have the same type of slash). If
		 * so, we count them jointly as one line.
		 */
		nu = 0;
		nl = c;
		last = neighbours[nneighbours-1].pos;
		if (soln[last] == 0)
		    eq = Dsf.dsf_canonify(sc.equiv, last);
		else
		    eq = -1;
		meq = mj1 = mj2 = -1;
		for (i = 0; i < nneighbours; i++) {
		    j = neighbours[i].pos;
		    s = neighbours[i].slash;
		    if (soln[j] == 0) {
			nu++;	       /* undecided */
			if (meq < 0 && difficulty > DIFF_EASY) {
			    eq2 = Dsf.dsf_canonify(sc.equiv, j);
			    if (eq == eq2 && last != j) {
				/*
				 * We've found an equivalent pair.
				 * Mark it. This also inhibits any
				 * further equivalence tracking
				 * around this square, since we can
				 * only handle one pair (and in
				 * particular we want to avoid
				 * being misled by two overlapping
				 * equivalence pairs).
				 */
				meq = eq;
				mj1 = last;
				mj2 = j;
				nl--;   /* count one line */
				nu -= 2;   /* and lose two undecideds */
			    } else
				eq = eq2;
			}
		    } else {
			eq = -1;
			if (soln[j] == s)
			    nl--;      /* here's a line */
		    }
		    last = j;
		}

		/*
		 * Check the counts.
		 */
		if (nl < 0 || nl > nu) {
		    /*
		     * No consistent value for this at all!
		     */
#if SOLVER_DIAGNOSTICS
		    if (verbose)
			printf("need %d / %d lines around clue point at %d,%d!\n",
			       nl, nu, x, y);
#endif
		    return 0;	       /* impossible */
		}

		if (nu > 0 && (nl == 0 || nl == nu)) {
#if SOLVER_DIAGNOSTICS
		    if (verbose) {
			if (meq >= 0)
			    printf("partially (since %d,%d == %d,%d) ",
				   mj1%w, mj1/w, mj2%w, mj2/w);
			printf("%s around clue point at %d,%d\n",
			       nl ? "filling" : "emptying", x, y);
		    }
#endif
		    for (i = 0; i < nneighbours; i++) {
			j = neighbours[i].pos;
			s = neighbours[i].slash;
			if (soln[j] == 0 && j != mj1 && j != mj2)
			    fill_square(w, h, j%w, j/w, (nl!=0 ? s : -s), soln,
					sc.connected, sc);
		    }

		    done_something = true;
		} else if (nu == 2 && nl == 1 && difficulty > DIFF_EASY) {
		    /*
		     * If we have precisely two undecided squares
		     * and precisely one line to place between
		     * them, _and_ those squares are adjacent, then
		     * we can mark them as equivalent to one
		     * another.
		     * 
		     * This even applies if meq >= 0: if we have a
		     * 2 clue point and two of its neighbours are
		     * already marked equivalent, we can indeed
		     * mark the other two as equivalent.
		     * 
		     * We don't bother with this on DIFF_EASY,
		     * since we wouldn't have used the results
		     * anyway.
		     */
		    last = -1;
		    for (i = 0; i < nneighbours; i++) {
			j = neighbours[i].pos;
			if (soln[j] == 0 && j != mj1 && j != mj2) {
			    if (last < 0)
				last = i;
			    else if (last == i-1 || (last == 0 && i == 3))
				break; /* found a pair */
			}
		    }
		    if (i < nneighbours) {
			int sv1, sv2;

			Debug.Assert(last >= 0);
			/*
			 * neighbours[last] and neighbours[i] are
			 * the pair. Mark them equivalent.
			 */
#if SOLVER_DIAGNOSTICS
			if (verbose) {
			    if (meq >= 0)
				printf("since %d,%d == %d,%d, ",
				       mj1%w, mj1/w, mj2%w, mj2/w);
			}
#endif
			mj1 = neighbours[last].pos;
			mj2 = neighbours[i].pos;
#if SOLVER_DIAGNOSTICS
			if (verbose)
			    printf("clue point at %d,%d implies %d,%d == %d,"
				   "%d\n", x, y, mj1%w, mj1/w, mj2%w, mj2/w);
#endif
			mj1 = Dsf.dsf_canonify(sc.equiv, mj1);
			sv1 = sc.slashval[mj1];
			mj2 = Dsf.dsf_canonify(sc.equiv, mj2);
			sv2 = sc.slashval[mj2];
			if (sv1 != 0 && sv2 != 0 && sv1 != sv2) {
#if SOLVER_DIAGNOSTICS
			    if (verbose)
				printf("merged two equivalence classes with"
				       " different slash values!\n");
#endif
			    return 0;
			}
			sv1 = sv1!=0 ? sv1 : sv2;
			Dsf.dsf_merge(sc.equiv, mj1, mj2);
			mj1 = Dsf.dsf_canonify(sc.equiv, mj1);
			sc.slashval[mj1] = (sbyte)sv1;
		    }
		}
	    }

	if (done_something)
	    continue;

	/*
	 * Failing that, we now apply the second condition, which
	 * is that no square may be filled in such a way as to form
	 * a loop. Also in this loop (since it's over squares
	 * rather than points), we check slashval to see if we've
	 * already filled in another square in the same equivalence
	 * class.
	 * 
	 * The slashval check is disabled on DIFF_EASY, as is dead
	 * end avoidance. Only _immediate_ loop avoidance remains.
	 */
	for (y = 0; y < h; y++)
	    for (x = 0; x < w; x++) {
            bool fs, bs;
            int v;
		int c1, c2;
#if SOLVER_DIAGNOSTICS
		char *reason = "<internal error>";
#endif

		if (soln[y*w+x]!=0)
		    continue;	       /* got this one already */

		fs = false;
		bs = false;

		if (difficulty > DIFF_EASY)
		    v = sc.slashval[Dsf.dsf_canonify(sc.equiv, y*w+x)];
		else
		    v = 0;

		/*
		 * Try to rule out connectivity between (x,y) and
		 * (x+1,y+1); if successful, we will deduce that we
		 * must have a forward slash.
		 */
		c1 = Dsf.dsf_canonify(sc.connected, y*W+x);
		c2 = Dsf.dsf_canonify(sc.connected, (y+1)*W+(x+1));
		if (c1 == c2) {
		    fs = true;
#if SOLVER_DIAGNOSTICS
		    reason = "simple loop avoidance";
#endif
		}
		if (difficulty > DIFF_EASY &&
		    !sc.border[c1] && !sc.border[c2] &&
		    sc.exits[c1] <= 1 && sc.exits[c2] <= 1) {
		    fs = true;
#if SOLVER_DIAGNOSTICS
		    reason = "dead end avoidance";
#endif
		}
		if (v == +1) {
		    fs = true;
#if SOLVER_DIAGNOSTICS
		    reason = "equivalence to an already filled square";
#endif
		}

		/*
		 * Now do the same between (x+1,y) and (x,y+1), to
		 * see if we are required to have a backslash.
		 */
		c1 = Dsf.dsf_canonify(sc.connected, y*W+(x+1));
		c2 = Dsf.dsf_canonify(sc.connected, (y+1)*W+x);
		if (c1 == c2) {
		    bs = true;
#if SOLVER_DIAGNOSTICS
		    reason = "simple loop avoidance";
#endif
		}
		if (difficulty > DIFF_EASY &&
		    !sc.border[c1] && !sc.border[c2] &&
		    sc.exits[c1] <= 1 && sc.exits[c2] <= 1) {
		    bs = true;
#if SOLVER_DIAGNOSTICS
		    reason = "dead end avoidance";
#endif
		}
		if (v == -1) {
		    bs = true;
#if SOLVER_DIAGNOSTICS
		    reason = "equivalence to an already filled square";
#endif
		}

		if (fs && bs) {
		    /*
		     * No consistent value for this at all!
		     */
#if SOLVER_DIAGNOSTICS
		    if (verbose)
			printf("%d,%d has no consistent slash!\n", x, y);
#endif
		    return 0;          /* impossible */
		}

		if (fs) {
#if SOLVER_DIAGNOSTICS
		    if (verbose)
			printf("employing %s\n", reason);
#endif
		    fill_square(w, h, x, y, +1, soln, sc.connected, sc);
		    done_something = true;
		} else if (bs) {
#if SOLVER_DIAGNOSTICS
		    if (verbose)
			printf("employing %s\n", reason);
#endif
		    fill_square(w, h, x, y, -1, soln, sc.connected, sc);
		    done_something = true;
		}
	    }

	if (done_something)
	    continue;

        /*
         * Now see what we can do with the vbitmap array. All
         * vbitmap deductions are disabled at Easy level.
         */
        if (difficulty <= DIFF_EASY)
            continue;

	for (y = 0; y < h; y++)
	    for (x = 0; x < w; x++) {
                int s, c;

                /*
                 * Any line already placed in a square must rule
                 * out any type of v which contradicts it.
                 */
                if ((s = soln[y*w+x]) != 0) {
                    if (x > 0)
                        done_something |=
                        vbitmap_clear(w, h, sc, x-1, y, (s < 0 ? 0x1 : 0x2));
                    if (x+1 < w)
                        done_something |=
                        vbitmap_clear(w, h, sc, x, y, (s < 0 ? 0x2 : 0x1));
                    if (y > 0)
                        done_something |=
                        vbitmap_clear(w, h, sc, x, y-1, (s < 0 ? 0x4 : 0x8));
                    if (y+1 < h)
                        done_something |=
                        vbitmap_clear(w, h, sc, x, y, (s < 0 ? 0x8 : 0x4));
                }

                /*
                 * If both types of v are ruled out for a pair of
                 * adjacent squares, mark them as equivalent.
                 */
                if (x+1 < w && (sc.vbitmap[y*w+x] & 0x3)==0) {
                    int n1 = y*w+x, n2 = y*w+(x+1);
                    if (Dsf.dsf_canonify(sc.equiv, n1) !=
                        Dsf.dsf_canonify(sc.equiv, n2)) {
                        Dsf.dsf_merge(sc.equiv, n1, n2);
                        done_something = true;
#if SOLVER_DIAGNOSTICS
                        if (verbose)
                            printf("(%d,%d) and (%d,%d) must be equivalent"
                                   " because both v-shapes are ruled out\n",
                                   x, y, x+1, y);
#endif
                    }
                }
                if (y+1 < h && (sc.vbitmap[y*w+x] & 0xC)==0) {
                    int n1 = y*w+x, n2 = (y+1)*w+x;
                    if (Dsf.dsf_canonify(sc.equiv, n1) !=
                        Dsf.dsf_canonify(sc.equiv, n2)) {
                        Dsf.dsf_merge(sc.equiv, n1, n2);
                        done_something = true;
#if SOLVER_DIAGNOSTICS
                        if (verbose)
                            printf("(%d,%d) and (%d,%d) must be equivalent"
                                   " because both v-shapes are ruled out\n",
                                   x, y, x, y+1);
#endif
                    }
                }

                /*
                 * The remaining work in this loop only works
                 * around non-edge clue points.
                 */
                if (y == 0 || x == 0)
                    continue;
		if ((c = clues[y*W+x]) < 0)
		    continue;

                /*
                 * x,y marks a clue point not on the grid edge. See
                 * if this clue point allows us to rule out any v
                 * shapes.
                 */

                if (c == 1) {
                    /*
                     * A 1 clue can never have any v shape pointing
                     * at it.
                     */
                    done_something |=
                        vbitmap_clear(w, h, sc, x-1, y-1, 0x5);
                    done_something |=
                        vbitmap_clear(w, h, sc, x-1, y, 0x2);
                    done_something |=
                        vbitmap_clear(w, h, sc, x, y-1, 0x8);
                } else if (c == 3) {
                    /*
                     * A 3 clue can never have any v shape pointing
                     * away from it.
                     */
                    done_something |=
                        vbitmap_clear(w, h, sc, x-1, y-1, 0xA);
                    done_something |=
                        vbitmap_clear(w, h, sc, x-1, y, 0x1);
                    done_something |=
                        vbitmap_clear(w, h, sc, x, y-1, 0x4);
                } else if (c == 2) {
                    /*
                     * If a 2 clue has any kind of v ruled out on
                     * one side of it, the same v is ruled out on
                     * the other side.
                     */
                    done_something |=
                        vbitmap_clear(w, h, sc, x-1, y-1,
                                      (sc.vbitmap[(y  )*w+(x-1)] & 0x3) ^ 0x3);
                    done_something |=
                        vbitmap_clear(w, h, sc, x-1, y-1,
                                      (sc.vbitmap[(y-1)*w+(x  )] & 0xC) ^ 0xC);
                    done_something |=
                        vbitmap_clear(w, h, sc, x-1, y,
                                      (sc.vbitmap[(y-1)*w+(x-1)] & 0x3) ^ 0x3);
                    done_something |=
                        vbitmap_clear(w, h, sc, x, y-1,
                                      (sc.vbitmap[(y-1)*w+(x-1)] & 0xC) ^ 0xC);
                }


            }

    } while (done_something);

    /*
     * Solver can make no more progress. See if the grid is full.
     */
    for (i = 0; i < w*h; i++)
	if (soln[i]==0)
	    return 2;		       /* failed to converge */
    return 1;			       /* success */
}

/*
 * Filled-grid generator.
 */
static void slant_generate(int w, int h, sbyte[] soln, Random rs)
{
    int W = w+1, H = h+1;
    int x, y, i;
    int[] connected, indices;

    /*
     * Clear the output.
     */
    soln.SetAll((sbyte)0);

    /*
     * Establish a disjoint set forest for tracking connectedness
     * between grid points.
     */
    connected = Dsf.snew_dsf(W*H);

    /*
     * Prepare a list of the squares in the grid, and fill them in
     * in a random order.
     */
    indices = new  int[w*h];
    for (i = 0; i < w*h; i++)
	indices[i] = i;
    indices.Shuffle(w*h, rs);

    /*
     * Fill in each one in turn.
     */
    for (i = 0; i < w*h; i++) {
        bool fs, bs;
        int v;

	y = indices[i] / w;
	x = indices[i] % w;

	fs = (Dsf.dsf_canonify(connected, y*W+x) ==
	      Dsf.dsf_canonify(connected, (y+1)*W+(x+1)));
	bs = (Dsf.dsf_canonify(connected, (y+1)*W+x) ==
	      Dsf.dsf_canonify(connected, y*W+(x+1)));

	/*
	 * It isn't possible to get into a situation where we
	 * aren't allowed to place _either_ type of slash in a
	 * square. Thus, filled-grid generation never has to
	 * backtrack.
	 * 
	 * Proof (thanks to Gareth Taylor):
	 * 
	 * If it were possible, it would have to be because there
	 * was an existing path (not using this square) between the
	 * top-left and bottom-right corners of this square, and
	 * another between the other two. These two paths would
	 * have to cross at some point.
	 * 
	 * Obviously they can't cross in the middle of a square, so
	 * they must cross by sharing a point in common. But this
	 * isn't possible either: if you chessboard-colour all the
	 * points on the grid, you find that any continuous
	 * diagonal path is entirely composed of points of the same
	 * colour. And one of our two hypothetical paths is between
	 * two black points, and the other is between two white
	 * points - therefore they can have no point in common. []
	 */
	Debug.Assert(!(fs && bs));

	v = fs ? +1 : bs ? -1 : 2 * rs.Next(0, 2) - 1;
	fill_square(w, h, x, y, v, soln, connected, null);
    }

}
public override string GenerateNewGameDescription(SlantSettings @params, Random rs, out string aux, int interactive)
{
    int w = @params.w, h = @params.h, W = w+1, H = h+1;
    sbyte[] soln, tmpsoln, clues;
    int[] clueindices;
    SlantSolverScratch sc;
    int x, y, v, i, j;

    soln = new  sbyte[w*h];
    tmpsoln = new  sbyte[w*h];
    clues = new  sbyte[W*H];
    clueindices = new  int[W*H];
    sc = new_scratch(w, h);

    do {
	/*
	 * Create the filled grid.
	 */
	slant_generate(w, h, soln, rs);

	/*
	 * Fill in the complete set of clues.
	 */
	for (y = 0; y < H; y++)
	    for (x = 0; x < W; x++) {
		v = 0;

		if (x > 0 && y > 0 && soln[(y-1)*w+(x-1)] == -1) v++;
		if (x > 0 && y < h && soln[y*w+(x-1)] == +1) v++;
		if (x < w && y > 0 && soln[(y-1)*w+x] == +1) v++;
		if (x < w && y < h && soln[y*w+x] == -1) v++;

        clues[y * W + x] = (sbyte)v;
	    }

	/*
	 * With all clue points filled in, all puzzles are easy: we can
	 * simply process the clue points in lexicographic order, and
	 * at each clue point we will always have at most one square
	 * undecided, which we can then fill in uniquely.
	 */
	Debug.Assert(slant_solve(w, h, clues, tmpsoln, sc, DIFF_EASY) == 1);

	/*
	 * Remove as many clues as possible while retaining solubility.
	 *
	 * In DIFF_HARD mode, we prioritise the removal of obvious
	 * starting points (4s, 0s, border 2s and corner 1s), on
	 * the grounds that having as few of these as possible
	 * seems like a good thing. In particular, we can often get
	 * away without _any_ completely obvious starting points,
	 * which is even better.
	 */
	for (i = 0; i < W*H; i++)
	    clueindices[i] = i;
	clueindices.Shuffle( W*H, rs);
	for (j = 0; j < 2; j++) {
	    for (i = 0; i < W*H; i++) {
		int pass;
        bool yb, xb;

		y = clueindices[i] / W;
		x = clueindices[i] % W;
		v = clues[y*W+x];

		/*
		 * Identify which pass we should process this point
		 * in. If it's an obvious start point, _or_ we're
		 * in DIFF_EASY, then it goes in pass 0; otherwise
		 * pass 1.
		 */
		xb = (x == 0 || x == W-1);
		yb = (y == 0 || y == H-1);
		if (@params.diff == DIFF_EASY || v == 4 || v == 0 ||
		    (v == 2 && (xb||yb)) || (v == 1 && xb && yb))
		    pass = 0;
		else
		    pass = 1;

		if (pass == j) {
		    clues[y*W+x] = -1;
		    if (slant_solve(w, h, clues, tmpsoln, sc,
				    @params.diff) != 1)
                clues[y * W + x] = (sbyte)v;	       /* put it back */
		}
	    }
	}

	/*
	 * And finally, verify that the grid is of _at least_ the
	 * requested difficulty, by running the solver one level
	 * down and verifying that it can't manage it.
	 */
    } while (@params.diff > 0 &&
	     slant_solve(w, h, clues, tmpsoln, sc, @params.diff - 1) <= 1);

    /*
     * Now we have the clue set as it will be presented to the
     * user. Encode it in a game desc.
     */
    StringBuilder desc = new StringBuilder();
    {
	int run;
        int i2;

	run = 0;
	for (i2 = 0; i2 <= W*H; i2++) {
	    int n = (i2 < W*H ? clues[i2] : -2);

	    if (n == -1)
		run++;
	    else {
		if (run!=0) {
		    while (run > 0) {
			int c = 'a' - 1 + run;
			if (run > 26)
			    c = 'z';
			desc.Append((char)c);
			run -= c - ('a' - 1);
		    }
		}
		if (n >= 0)
		    desc.Append((char)('0' + n));
		run = 0;
	    }
	}
    }

    ///*
    // * Encode the solution as an aux_info.
    // */
    //{
    //char *auxbuf;
    //*aux = auxbuf = new  char[w*h+1];
    //for (i = 0; i < w*h; i++)
    //    auxbuf[i] = soln[i] < 0 ? '\\' : '/';
    //auxbuf[w*h] = '\0';
    //}
    aux = null;

    return desc.ToString();
}

static string validate_desc(SlantSettings @params, string desc)
{
    int w = @params.w, h = @params.h, W = w+1, H = h+1;
    int area = W*H;
    int squares = 0;
    int descPos = 0;
    while (descPos <desc.Length) {
        char n = desc[descPos++];
        if (n >= 'a' && n <= 'z') {
            squares += n - 'a' + 1;
        } else if (n >= '0' && n <= '4') {
            squares++;
        } else
            return "Invalid character in game description";
    }

    if (squares < area)
        return "Not enough data to fill grid";

    if (squares > area)
        return "Too much data to fit in grid";

    return null;
}

public override SlantState CreateNewGameFromDescription(SlantSettings @params, string desc)
{

    int w = @params.w, h = @params.h, W = w+1, H = h+1;
    SlantState state = new SlantState();
    int area = W*H;
    int squares = 0;

    state.p = @params;
    state.soln = new  sbyte[w*h];
    state.completed = state.used_solve = false;
    state.errors = new  byte[W*H];

    state.clues = new SlantClues();
    state.clues.w = w;
    state.clues.h = h;
    state.clues.clues = new  sbyte[W*H];
    state.clues.refcount = 1;
    state.clues.tmpdsf = new  int[W*H*2+W+H];
    state.clues.clues.SetAll((sbyte)-1);

    int descPos = 0;
    while (descPos < desc.Length) {
        char n = desc[descPos++];
        if (n >= 'a' && n <= 'z') {
            squares += n - 'a' + 1;
        } else if (n >= '0' && n <= '4') {
            state.clues.clues[squares++] = (sbyte)(n - '0');
        } else
	    Debug.Assert(false, "can't get here");
    }
    Debug.Assert(squares == area);

    return state;
}

static SlantState dup_game(SlantState state)
{
    int w = state.p.w, h = state.p.h, W = w+1, H = h+1;
    SlantState ret = new SlantState();

    ret.p = state.p;
    ret.clues = state.clues;
    ret.clues.refcount++;
    ret.completed = state.completed;
    ret.used_solve = state.used_solve;

    ret.soln = new  sbyte[w*h];
    Array.Copy(state.soln, ret.soln, w * h);

    ret.errors = new  byte[W*H];
    Array.Copy(state.errors, ret.errors, W * H);

    return ret;
}


/*
 * Utility function to return the current degree of a vertex. If
 * `anti' is set, it returns the number of filled-in edges
 * surrounding the point which _don't_ connect to it; thus 4 minus
 * its anti-degree is the maximum degree it could have if all the
 * empty spaces around it were filled in.
 * 
 * (Yes, _4_ minus its anti-degree even if it's a border vertex.)
 * 
 * If ret > 0, *sx and *sy are set to the coordinates of one of the
 * squares that contributed to it.
 */
static int vertex_degree(int w, int h, sbyte[]soln, int x, int y,
                         bool anti, ref int sx, ref int sy)
{
    int ret = 0;

    Debug.Assert(x >= 0 && x <= w && y >= 0 && y <= h);
    if (x > 0 && y > 0 && soln[(y-1)*w+(x-1)] - (anti?1:0) < 0) {
        sx = x-1;
        sy = y-1;
        ret++;
    }
    if (x > 0 && y < h && soln[y * w + (x - 1)] + (anti ? 1 : 0) > 0)
    {
        sx = x-1;
        sy = y;
        ret++;
    }
    if (x < w && y > 0 && soln[(y - 1) * w + x] + (anti ? 1 : 0) > 0)
    {
        sx = x;
        sy = y-1;
        ret++;
    }
    if (x < w && y < h && soln[y * w + x] - (anti ? 1 : 0) < 0)
    {
        sx = x;
        sy = y;
        ret++;
    }

    return anti ? 4 - ret : ret;
}

static bool check_completion(SlantState state)
{
    int w = state.p.w, h = state.p.h, W = w+1, H = h+1;
    int x, y;
    bool err = false;
    int[]dsf;

    state.errors.SetAll((byte)0);

    /*
     * To detect loops in the grid, we iterate through each edge
     * building up a dsf of connected components of the space
     * around the edges; if there's more than one such component,
     * we have a loop, and in particular we can then easily
     * identify and highlight every edge forming part of a loop
     * because it separates two nonequivalent regions.
     *
     * We use the `tmpdsf' scratch space in the shared clues
     * structure, to avoid mallocing too often.
     *
     * For these purposes, the grid is considered to be divided
     * into diamond-shaped regions surrounding an orthogonal edge.
     * This means we have W*h vertical edges and w*H horizontal
     * ones; so our vertical edges are indexed in the dsf as
     * (y*W+x) (0<=y<h, 0<=x<W), and the horizontal ones as (W*h +
     * y*w+x) (0<=y<H, 0<=x<w), where (x,y) is the topmost or
     * leftmost point on the edge.
     */
    dsf = state.clues.tmpdsf;
    Dsf.dsf_init(dsf, W*h + w*H);
    /* Start by identifying all the outer edges with each other. */
    for (y = 0; y < h; y++) {
	Dsf.dsf_merge(dsf, 0, y*W+0);
	Dsf.dsf_merge(dsf, 0, y*W+w);
    }
    for (x = 0; x < w; x++) {
	Dsf.dsf_merge(dsf, 0, W*h + 0*w+x);
	Dsf.dsf_merge(dsf, 0, W*h + h*w+x);
    }
    /* Now go through the actual grid. */
    for (y = 0; y < h; y++)
        for (x = 0; x < w; x++) {
            if (state.soln[y*w+x] >= 0) {
		/*
		 * There isn't a \ in this square, so we can unify
		 * the top edge with the left, and the bottom with
		 * the right.
		 */
		Dsf.dsf_merge(dsf, y*W+x, W*h + y*w+x);
		Dsf.dsf_merge(dsf, y*W+(x+1), W*h + (y+1)*w+x);
	    }
            if (state.soln[y*w+x] <= 0) {
		/*
		 * There isn't a / in this square, so we can unify
		 * the top edge with the right, and the bottom
		 * with the left.
		 */
		Dsf.dsf_merge(dsf, y*W+x, W*h + (y+1)*w+x);
		Dsf.dsf_merge(dsf, y*W+(x+1), W*h + y*w+x);
	    }
        }
    /* Now go through again and mark the appropriate edges as erroneous. */
    for (y = 0; y < h; y++)
        for (x = 0; x < w; x++) {
	    bool erroneous = false;
            if (state.soln[y*w+x] > 0) {
		/*
		 * A / separates the top and left edges (which
		 * must already have been identified with each
		 * other) from the bottom and right (likewise).
		 * Hence it is erroneous if and only if the top
		 * and right edges are nonequivalent.
		 */
		erroneous = (Dsf.dsf_canonify(dsf, y*W+(x+1)) !=
			     Dsf.dsf_canonify(dsf, W*h + y*w+x));
	    } else if (state.soln[y*w+x] < 0) {
		/*
		 * A \ separates the top and right edges (which
		 * must already have been identified with each
		 * other) from the bottom and left (likewise).
		 * Hence it is erroneous if and only if the top
		 * and left edges are nonequivalent.
		 */
		erroneous = (Dsf.dsf_canonify(dsf, y*W+x) !=
			     Dsf.dsf_canonify(dsf, W*h + y*w+x));
	    }
	    if (erroneous) {
		state.errors[y*W+x] |= ERR_SQUARE;
		err = true;
	    }
        }

    /*
     * Now go through and check the degree of each clue vertex, and
     * mark it with ERR_VERTEX if it cannot be fulfilled.
     */
    for (y = 0; y < H; y++)
        for (x = 0; x < W; x++) {
            int c;

	    if ((c = state.clues.clues[y*W+x]) < 0)
		continue;
            int temp1=0, temp2=0;
            /*
             * Check to see if there are too many connections to
             * this vertex _or_ too many non-connections. Either is
             * grounds for marking the vertex as erroneous.
             */
            if (vertex_degree(w, h, state.soln, x, y,
                              false, ref temp1, ref temp2) > c ||
                vertex_degree(w, h, state.soln, x, y,
                              true, ref temp1, ref temp2) > 4-c) {
                state.errors[y*W+x] |= ERR_VERTEX;
                err = true;
            }
        }

    /*
     * Now our actual victory condition is that (a) none of the
     * above code marked anything as erroneous, and (b) every
     * square has an edge in it.
     */

    if (err)
        return false;

    for (y = 0; y < h; y++)
	for (x = 0; x < w; x++)
	    if (state.soln[y*w+x] == 0)
		return false;

    return true;
}

public override SlantMove CreateSolveGameMove(SlantState state, SlantState currstate, SlantMove aux, out string error)
{
    int w = state.p.w, h = state.p.h;
    sbyte[] soln;
    int bs, ret;
    bool  free_soln = false;
    //char *move, buf[80];
    int movelen, movesize;
    int x, y;

    //if (aux) {
    ///*
    // * If we already have the solution, save ourselves some
    // * time.
    // */
    //soln = (sbyte *)aux;
    //bs = (sbyte)'\\';
    //free_soln = false;
    //} else {
    SlantSolverScratch sc = new_scratch(w, h);
    soln = new sbyte[w * h];
    bs = -1;
    ret = slant_solve(w, h, state.clues.clues, soln, sc, DIFF_HARD);
    if (ret != 1)
    {
        if (ret == 0)
            error = "This puzzle is not self-consistent";
        else
            error = "Unable to find a unique solution for this puzzle";
        return null;
    }
    //free_soln = true;
    //}
    error = null;
    /*
     * Construct a move string which turns the current state into
     * the solved state.
     */
    var move = new SlantMove();
    move.IsSolve = true;
    for (y = 0; y < h; y++)
	for (x = 0; x < w; x++) {
	    int v = (soln[y*w+x] == bs ? -1 : +1);
	    if (state.soln[y*w+x] != v) {
            move.Points.Add(new SlantMovePoint() { x = x, y = y, n = (sbyte)v });
	    }
	}

    return move;
}

//static int game_can_format_as_text_now(SlantSettings @params)
//{
//    return true;
//}

//static char *game_text_format(SlantState state)
//{
//    int w = state.p.w, h = state.p.h, W = w+1, H = h+1;
//    int x, y, len;
//    char *ret, *p;

//    /*
//     * There are h+H rows of w+W columns.
//     */
//    len = (h+H) * (w+W+1) + 1;
//    ret = new  char[len];
//    p = ret;

//    for (y = 0; y < H; y++) {
//    for (x = 0; x < W; x++) {
//        if (state.clues.clues[y*W+x] >= 0)
//        *p++ = state.clues.clues[y*W+x] + '0';
//        else
//        *p++ = '+';
//        if (x < w)
//        *p++ = '-';
//    }
//    *p++ = '\n';
//    if (y < h) {
//        for (x = 0; x < W; x++) {
//        *p++ = '|';
//        if (x < w) {
//            if (state.soln[y*w+x] != 0)
//            *p++ = (state.soln[y*w+x] < 0 ? '\\' : '/');
//            else
//            *p++ = ' ';
//        }
//        }
//        *p++ = '\n';
//    }
//    }
//    *p++ = '\0';

//    Debug.Assert(p - ret == len);
//    return ret;
//}

public override SlantUI CreateUI(SlantState state)
{
    SlantUI ui = new SlantUI();
    ui.cur_x = ui.cur_y = 0;
    ui.cur_visible = false;
    return ui;
}

private static int BORDER(SlantDrawState ds) { return (ds.tilesize);}
private static int CLUE_RADIUS(SlantDrawState ds) { return ((ds.tilesize) / 3); }
private static int CLUE_TEXTSIZE(SlantDrawState ds)  { return ((ds.tilesize) / 2); }
private static int COORD(SlantDrawState ds, int x)  { return  ( (x) * (ds.tilesize) + BORDER(ds) ); }
private static int FROMCOORD(SlantDrawState ds, int x)  { return  ( ((x) - BORDER(ds) + (ds.tilesize)) / (ds.tilesize) - 1 ); }

const float FLASH_TIME =0.30F;

public override float CompletedFlashDuration(SlantSettings settings)
{
    return FLASH_TIME;
}

/*
 * Bit fields in the `grid' and `todraw' elements of the drawstate.
 */
const long BACKSLASH =0x00000001L;
const long FORWSLASH =0x00000002L;
const long L_T       =0x00000004L;
const long ERR_L_T   =0x00000008L;
const long L_B       =0x00000010L;
const long ERR_L_B   =0x00000020L;
const long T_L       =0x00000040L;
const long ERR_T_L   =0x00000080L;
const long T_R       =0x00000100L;
const long ERR_T_R   =0x00000200L;
const long C_TL      =0x00000400L;
const long ERR_C_TL  =0x00000800L;
const long FLASH     =0x00001000L;
const long ERRSLASH  =0x00002000L;
const long ERR_TL    =0x00004000L;
const long ERR_TR    =0x00008000L;
const long ERR_BL    =0x00010000L;
const long ERR_BR    =0x00020000L;
const long CURSOR    =0x00040000L;

public override SlantMove InterpretMove(SlantState state, SlantUI ui, SlantDrawState ds, int x, int y, Buttons button, bool isTouchOrStylus)
{
    int w = state.p.w, h = state.p.h;
    int v;
    //char buf[80];
     //enum { CLOCKWISE, ANTICLOCKWISE, NONE } action = NONE;
	const int CLOCKWISE = 0;
	const int ANTICLOCKWISE = 1;
	const int NONE = 2;	
	int action = NONE;

	
    if (button == Buttons.LEFT_BUTTON || button == Buttons.RIGHT_BUTTON) {
    ///*
    // * This is an utterly awful hack which I should really sort out
    // * by means of a proper configuration mechanism. One Slant
    // * player has observed that they prefer the mouse buttons to
    // * function exactly the opposite way round, so here's a
    // * mechanism for environment-based configuration. I cache the
    // * result in a global variable - yuck! - to avoid repeated
    // * lookups.
    // */
    //{
    //    const bool swap_buttons = false;

    //    if (swap_buttons) {
    //    if (button == LEFT_BUTTON)
    //        button = RIGHT_BUTTON;
    //    else
    //        button = LEFT_BUTTON;
    //    }
    //}
        action = (button == Buttons.LEFT_BUTTON) ? CLOCKWISE : ANTICLOCKWISE;

        x = FROMCOORD(ds,x);
        y = FROMCOORD(ds,y);
        if (x < 0 || y < 0 || x >= w || y >= h)
            return null;
    } else if (Misc.IS_CURSOR_SELECT(button)) {
        if (!ui.cur_visible) {
            ui.cur_visible = true;
            return null;
        }
        x = ui.cur_x;
        y = ui.cur_y;

        action = (button == Buttons.CURSOR_SELECT2) ? ANTICLOCKWISE : CLOCKWISE;
    }
    else if (Misc.IS_CURSOR_MOVE(button))
    {
        Misc.move_cursor(button, ref ui.cur_x, ref ui.cur_y, w, h, false);
        ui.cur_visible = true;
        return null;
    }

    if (action != NONE) {
        if (action == CLOCKWISE) {
            /*
             * Left-clicking cycles blank . \ . / . blank.
             */
            v = state.soln[y*w+x] - 1;
            if (v == -2)
                v = +1;
        } else {
            /*
             * Right-clicking cycles blank . / . \ . blank.
             */
            v = state.soln[y*w+x] + 1;
            if (v == +2)
                v = -1;
        }

        var move = new SlantMove();
        move.Points.Add(new SlantMovePoint() { x = x, y = y, n = (sbyte)v });
        return move;
    }

    return null;
}
public override SlantMove ParseMove(SlantSettings settings, string moveString)
{
    return SlantMove.Parse(settings, moveString);
}

public override SlantState ExecuteMove(SlantState state, SlantMove move)
{
    int w = state.p.w, h = state.p.h;
    char c;
    int  n;
    SlantState ret = dup_game(state);

    if (move.IsSolve)
    {
        ret.used_solve = true;
    }
    foreach (SlantMovePoint point in move.Points)
    {
        if (point.x < 0 || point.y < 0 || point.x >= w || point.y >= h)
        {
            return null;
        }
        ret.soln[point.y * w + point.x] = point.n;
    }


    /*
     * We never clear the `completed' flag, but we must always
     * re-run the completion check because it also highlights
     * errors in the grid.
     */
    ret.completed = check_completion(ret) || ret.completed;

    return ret;
}

/* ----------------------------------------------------------------------
 * Drawing routines.
 */
public override void ComputeSize(SlantSettings @params, int tilesize, out int x, out int y)
{
    /* fool the macros */
    SlantDrawState ds = new SlantDrawState(){tilesize=tilesize};

    x = 2 * BORDER(ds) + @params.w * (ds.tilesize) + 1;
    y = 2 * BORDER(ds) + @params.h * (ds.tilesize) + 1;
}

public override void SetTileSize(Drawing dr, SlantDrawState ds, SlantSettings @params, int tilesize)
{
    ds.tilesize = tilesize;
}
public override float[] GetColours(Frontend fe, out int ncolours)
{
    float[]ret = new  float[3 * NCOLOURS];

    /* CURSOR colour is a background highlight. */
    Misc.game_mkhighlight(fe, ret, COL_CURSOR, COL_BACKGROUND, -1);

    ret[COL_FILLEDSQUARE * 3 + 0] = ret[COL_BACKGROUND * 3 + 0];
    ret[COL_FILLEDSQUARE * 3 + 1] = ret[COL_BACKGROUND * 3 + 1];
    ret[COL_FILLEDSQUARE * 3 + 2] = ret[COL_BACKGROUND * 3 + 2];

    ret[COL_GRID * 3 + 0] = ret[COL_BACKGROUND * 3 + 0] * 0.7F;
    ret[COL_GRID * 3 + 1] = ret[COL_BACKGROUND * 3 + 1] * 0.7F;
    ret[COL_GRID * 3 + 2] = ret[COL_BACKGROUND * 3 + 2] * 0.7F;

    ret[COL_INK * 3 + 0] = 0.0F;
    ret[COL_INK * 3 + 1] = 0.0F;
    ret[COL_INK * 3 + 2] = 0.0F;

    ret[COL_SLANT1 * 3 + 0] = 0.0F;
    ret[COL_SLANT1 * 3 + 1] = 0.0F;
    ret[COL_SLANT1 * 3 + 2] = 0.0F;

    ret[COL_SLANT2 * 3 + 0] = 0.0F;
    ret[COL_SLANT2 * 3 + 1] = 0.0F;
    ret[COL_SLANT2 * 3 + 2] = 0.0F;

    ret[COL_ERROR * 3 + 0] = 1.0F;
    ret[COL_ERROR * 3 + 1] = 0.0F;
    ret[COL_ERROR * 3 + 2] = 0.0F;

    ncolours = NCOLOURS;
    return ret;
}
public override SlantDrawState CreateDrawState(Drawing dr, SlantState state)
{
    int w = state.p.w, h = state.p.h;
    int i;
    SlantDrawState ds = new SlantDrawState();

    ds.tilesize = 0;
    ds.started = false;
    ds.grid = new  long[(w+2)*(h+2)];
    ds.todraw = new  long[(w+2)*(h+2)];
    for (i = 0; i < (w+2)*(h+2); i++)
	ds.grid[i] = ds.todraw[i] = -1;

    return ds;
}

static void draw_clue(Drawing dr, SlantDrawState ds,
		      int x, int y, long v, long err, int bg, int colour)
{
    int ccol = colour >= 0 ? colour : ((x ^ y) & 1) != 0 ? COL_SLANT1 : COL_SLANT2;
    int tcol = colour >= 0 ? colour : err != 0 ? COL_ERROR : COL_INK;

    if (v < 0)
	return;

    dr.draw_circle( COORD(ds,x), COORD(ds,y), CLUE_RADIUS(ds),
		bg >= 0 ? bg : COL_BACKGROUND, ccol);
    dr.draw_text( COORD(ds,x), COORD(ds,y), Drawing.FONT_VARIABLE,
          CLUE_TEXTSIZE(ds), Drawing.ALIGN_VCENTRE | Drawing.ALIGN_HCENTRE, tcol, v.ToString());
}

static void draw_tile(Drawing dr, SlantDrawState ds, SlantClues clues,
		      int x, int y, long v)
{
    int w = clues.w, h = clues.h, W = w+1 /*, H = h+1 */;
    int chesscolour = (x ^ y) & 1;
    int fscol = chesscolour!=0 ? COL_SLANT2 : COL_SLANT1;
    int bscol = chesscolour!=0 ? COL_SLANT1 : COL_SLANT2;

    dr.clip(COORD(ds,x), COORD(ds,y), (ds.tilesize), (ds.tilesize));

    dr.draw_rect( COORD(ds,x), COORD(ds,y), (ds.tilesize), (ds.tilesize),
          (v & FLASH) != 0 ? COL_GRID :
              (v & CURSOR) != 0? COL_CURSOR :
	      (v & (BACKSLASH | FORWSLASH))!=0 ? COL_FILLEDSQUARE :
	      COL_BACKGROUND);

    /*
     * Draw the grid lines.
     */
    if (x >= 0 && x < w && y >= 0)
        dr.draw_rect( COORD(ds,x), COORD(ds,y), (ds.tilesize)+1, 1, COL_GRID);
    if (x >= 0 && x < w && y < h)
        dr.draw_rect( COORD(ds,x), COORD(ds,y+1), (ds.tilesize)+1, 1, COL_GRID);
    if (y >= 0 && y < h && x >= 0)
        dr.draw_rect( COORD(ds,x), COORD(ds,y), 1, (ds.tilesize)+1, COL_GRID);
    if (y >= 0 && y < h && x < w)
        dr.draw_rect( COORD(ds,x+1), COORD(ds,y), 1, (ds.tilesize)+1, COL_GRID);
    if (x == -1 && y == -1)
        dr.draw_rect( COORD(ds,x+1), COORD(ds,y+1), 1, 1, COL_GRID);
    if (x == -1 && y == h)
        dr.draw_rect( COORD(ds,x+1), COORD(ds,y), 1, 1, COL_GRID);
    if (x == w && y == -1)
        dr.draw_rect( COORD(ds,x), COORD(ds,y+1), 1, 1, COL_GRID);
    if (x == w && y == h)
        dr.draw_rect( COORD(ds,x), COORD(ds,y), 1, 1, COL_GRID);

    /*
     * Draw the slash.
     */
    if ((v & BACKSLASH)!= 0) {
        int scol = (v & ERRSLASH) != 0 ? COL_ERROR : bscol;
	dr.draw_line( COORD(ds,x), COORD(ds,y), COORD(ds,x+1), COORD(ds,y+1), scol);
	dr.draw_line( COORD(ds,x)+1, COORD(ds,y), COORD(ds,x+1), COORD(ds,y+1)-1,
		  scol);
	dr.draw_line( COORD(ds,x), COORD(ds,y)+1, COORD(ds,x+1)-1, COORD(ds,y+1),
		  scol);
    } else if ((v & FORWSLASH)!= 0) {
        int scol = (v & ERRSLASH) != 0 ? COL_ERROR : fscol;
	dr.draw_line( COORD(ds,x+1), COORD(ds,y), COORD(ds,x), COORD(ds,y+1), scol);
	dr.draw_line( COORD(ds,x+1)-1, COORD(ds,y), COORD(ds,x), COORD(ds,y+1)-1,
		  scol);
	dr.draw_line( COORD(ds,x+1), COORD(ds,y)+1, COORD(ds,x)+1, COORD(ds,y+1),
		  scol);
    }

    /*
     * Draw dots on the grid corners that appear if a slash is in a
     * neighbouring cell.
     */
    if ((v & (L_T | BACKSLASH))!= 0)
	dr.draw_rect( COORD(ds,x), COORD(ds,y)+1, 1, 1,
                  ((v & ERR_L_T)!=0 ? COL_ERROR : bscol));
    if ((v & (L_B | FORWSLASH)) != 0)
	dr.draw_rect( COORD(ds,x), COORD(ds,y+1)-1, 1, 1,
                  ((v & ERR_L_B)!=0 ? COL_ERROR : fscol));
    if ((v & (T_L | BACKSLASH)) != 0)
	dr.draw_rect( COORD(ds,x)+1, COORD(ds,y), 1, 1,
                  ((v & ERR_T_L) != 0 ? COL_ERROR : bscol));
    if ((v & (T_R | FORWSLASH)) != 0)
	dr.draw_rect( COORD(ds,x+1)-1, COORD(ds,y), 1, 1,
                  ((v & ERR_T_R)!=0 ? COL_ERROR : fscol));
    if ((v & (C_TL | BACKSLASH))!=0)
	dr.draw_rect( COORD(ds,x), COORD(ds,y), 1, 1,
                  ((v & ERR_C_TL)!=0 ? COL_ERROR : bscol));

    /*
     * And finally the clues at the corners.
     */
    if (x >= 0 && y >= 0)
        draw_clue(dr, ds, x, y, clues.clues[y*W+x], v & ERR_TL, -1, -1);
    if (x < w && y >= 0)
        draw_clue(dr, ds, x+1, y, clues.clues[y*W+(x+1)], v & ERR_TR, -1, -1);
    if (x >= 0 && y < h)
        draw_clue(dr, ds, x, y+1, clues.clues[(y+1)*W+x], v & ERR_BL, -1, -1);
    if (x < w && y < h)
        draw_clue(dr, ds, x+1, y+1, clues.clues[(y+1)*W+(x+1)], v & ERR_BR,
		  -1, -1);

    dr.unclip();
    dr.draw_update( COORD(ds,x), COORD(ds,y), (ds.tilesize), (ds.tilesize));
}
public override void Redraw(Drawing dr, SlantDrawState ds, SlantState oldstate, SlantState state, int dir, SlantUI ui, float animtime, float flashtime)
{
    int w = state.p.w, h = state.p.h, W = w+1, H = h+1;
    int x, y;
    bool flashing;

    if (flashtime > 0)
	flashing = (int)(flashtime * 3 / FLASH_TIME) != 1;
    else
	flashing = false;

    if (!ds.started) {
	int ww, wh;
    ComputeSize(state.p, (ds.tilesize), out ww, out wh);
	dr.draw_rect( 0, 0, ww, wh, COL_BACKGROUND);
	dr.draw_update( 0, 0, ww, wh);
	ds.started = true;
    }

    /*
     * Loop over the grid and work out where all the slashes are.
     * We need to do this because a slash in one square affects the
     * drawing of the next one along.
     */
    for (y = -1; y <= h; y++)
	for (x = -1; x <= w; x++) {
            if (x >= 0 && x < w && y >= 0 && y < h)
                ds.todraw[(y+1)*(w+2)+(x+1)] = flashing ? FLASH : 0;
            else
                ds.todraw[(y+1)*(w+2)+(x+1)] = 0;
        }

    for (y = 0; y < h; y++) {
	for (x = 0; x < w; x++) {
            int err = state.errors[y*W+x] & ERR_SQUARE;

	    if (state.soln[y*w+x] < 0) {
		ds.todraw[(y+1)*(w+2)+(x+1)] |= BACKSLASH;
                ds.todraw[(y+2)*(w+2)+(x+1)] |= T_R;
                ds.todraw[(y+1)*(w+2)+(x+2)] |= L_B;
                ds.todraw[(y+2)*(w+2)+(x+2)] |= C_TL;
                if (err!=0) {
                    ds.todraw[(y+1)*(w+2)+(x+1)] |= ERRSLASH | 
			ERR_T_L | ERR_L_T | ERR_C_TL;
                    ds.todraw[(y+2)*(w+2)+(x+1)] |= ERR_T_R;
                    ds.todraw[(y+1)*(w+2)+(x+2)] |= ERR_L_B;
                    ds.todraw[(y+2)*(w+2)+(x+2)] |= ERR_C_TL;
                }
	    } else if (state.soln[y*w+x] > 0) {
		ds.todraw[(y+1)*(w+2)+(x+1)] |= FORWSLASH;
                ds.todraw[(y+1)*(w+2)+(x+2)] |= L_T | C_TL;
                ds.todraw[(y+2)*(w+2)+(x+1)] |= T_L | C_TL;
                if (err!=0) {
                    ds.todraw[(y+1)*(w+2)+(x+1)] |= ERRSLASH |
			ERR_L_B | ERR_T_R;
                    ds.todraw[(y+1)*(w+2)+(x+2)] |= ERR_L_T | ERR_C_TL;
                    ds.todraw[(y+2)*(w+2)+(x+1)] |= ERR_T_L | ERR_C_TL;
                }
	    }
            if (ui.cur_visible && ui.cur_x == x && ui.cur_y == y)
                ds.todraw[(y+1)*(w+2)+(x+1)] |= CURSOR;
	}
    }

    for (y = 0; y < H; y++)
        for (x = 0; x < W; x++)
            if ((state.errors[y * W + x] & ERR_VERTEX) != 0)
            {
                ds.todraw[y*(w+2)+x] |= ERR_BR;
                ds.todraw[y*(w+2)+(x+1)] |= ERR_BL;
                ds.todraw[(y+1)*(w+2)+x] |= ERR_TR;
                ds.todraw[(y+1)*(w+2)+(x+1)] |= ERR_TL;
            }

    /*
     * Now go through and draw the grid squares.
     */
    for (y = -1; y <= h; y++) {
	for (x = -1; x <= w; x++) {
	    if (ds.todraw[(y+1)*(w+2)+(x+1)] != ds.grid[(y+1)*(w+2)+(x+1)]) {
		draw_tile(dr, ds, state.clues, x, y,
                          ds.todraw[(y+1)*(w+2)+(x+1)]);
		ds.grid[(y+1)*(w+2)+(x+1)] = ds.todraw[(y+1)*(w+2)+(x+1)];
	    }
	}
    }
}

internal override void SetKeyboardCursorVisible(SlantUI ui, int tileSize, bool value)
{
    ui.cur_visible = value;
}

    }
}
