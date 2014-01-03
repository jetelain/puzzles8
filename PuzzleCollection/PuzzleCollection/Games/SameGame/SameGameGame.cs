using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.SameGame
{
    public sealed class SameGameGame : GameBase<SameGameSettings,SameGameState,SameGameMove,SameGameDrawState,SameGameUI>
    {
        
private static int TILE_INNER(SameGameDrawState ds) { return (ds.tileinner);}
private static int TILE_GAP(SameGameDrawState ds) { return (ds.tilegap);}
private static int TILE_SIZE(SameGameDrawState ds) { return (TILE_INNER(ds) + TILE_GAP(ds));}
private const int PREFERRED_TILE_SIZE =32;
private static int BORDER(SameGameDrawState ds) { return (TILE_SIZE(ds) / 2);}
private const int HIGHLIGHT_WIDTH =2;

private const float FLASH_FRAME =0.13F;

private static int COORD(SameGameDrawState ds,int x)  { return ( (x) * TILE_SIZE(ds) + BORDER(ds) );}
private static int FROMCOORD(SameGameDrawState ds,int x)  { return ( ((x) - BORDER(ds) + TILE_SIZE(ds)) / TILE_SIZE(ds) - 1 );}

private static int X(SameGameState state, int i) { return ( (i) % (state).@params.w );}
private static int Y(SameGameState state, int i)  { return( (i) / (state).@params.w );}
private static int C(SameGameSettings state, int x, int y) { return ( (y) * (state).w + (x) );}

private const int COL_BACKGROUND=0;
private const int COL_1=1;
private const int  COL_2=2; 
private const int COL_3=3;
private const int COL_4=4;
private const int COL_5=5;
private const int COL_6=6;
private const int  COL_7=7;
private const int COL_8=8;
private const int COL_9=9;
private const int COL_IMPOSSIBLE=10; 
private const int COL_SEL=11;
private const int COL_HIGHLIGHT=12; 
private const int COL_LOWLIGHT=13;
private const int NCOLOURS = 14;



/* These flags must be unique across all uses; in the game_state,
 * the game_ui, and the drawstate (as they all get combined in the
 * drawstate). */
private const ushort TILE_COLMASK   = 0x00ff;
private const ushort TILE_SELECTED   =0x0100; /* used in ui and drawstate */
private const ushort TILE_JOINRIGHT  =0x0200; /* used in drawstate */
private const ushort TILE_JOINDOWN   =0x0400; /* used in drawstate */
private const ushort TILE_JOINDIAG   =0x0800; /* used in drawstate */
private const ushort TILE_HASSEL     =0x1000; /* used in drawstate */
private const ushort TILE_IMPOSSIBLE =0x2000; /* used in drawstate */

private static int TILE(SameGameState gs,int x,int y) { return gs.tiles[x,y]; }
private static int COL(SameGameState gs,int x,int y)  { return gs.tiles[x,y] & TILE_COLMASK; }
private static int ISSEL(SameGameState gs,int x,int y)  { return gs.tiles[x,y] & TILE_SELECTED; }

private static int TILE(SameGameUI gs,int x,int y) { return gs.tiles[x,y]; }
private static int COL(SameGameUI gs,int x,int y)  { return gs.tiles[x,y] & TILE_COLMASK; }
private static int ISSEL(SameGameUI gs, int x, int y) { return gs.tiles[x, y] & TILE_SELECTED; }

private static void SWAPTILE(SameGameState gs,int x1,int y1,int x2,int y2)  {   
    int t = gs.tiles[x1,y1];               
    gs.tiles[x1,y1] = gs.tiles[x2,y2];      
    gs.tiles[x2,y2] = t;

    gs.dx[x1, y1] = (sbyte)(gs.dx[x2, y2] + (x2 - x1));
    gs.dy[x1, y1] = (sbyte)(gs.dy[x2, y2] + (y2 - y1));
} 

static int npoints(SameGameSettings @params, int nsel)
{
    int sdiff = nsel - @params.scoresub;
    return (sdiff > 0) ? sdiff * sdiff : 0;
}

private static readonly  SameGameSettings[] presets = new[] {
    new SameGameSettings( 5, 5, 3, 2, true ),
    new SameGameSettings( 10, 5, 3, 2, true ),
    new SameGameSettings( 15, 10, 3, 2, true ),
    new SameGameSettings( 15, 10, 4, 2, true ),
    new SameGameSettings( 20, 15, 4, 2, true )
};

public override IEnumerable<SameGameSettings> PresetsSettings
{
    get { return presets; }
}

public override SameGameSettings DefaultSettings
{
    get { return presets[1]; }
}

public override SameGameSettings ParseSettings(string settingsString)
{
    return SameGameSettings.Parse(settingsString);
}

/*
 * Guaranteed-soluble grid generator.
 */
static void gen_grid(int w, int h, int nc, int[] grid, Random rs)
{
    int wh = w*h, tc = nc+1;
    int i, j, k, c, x, y, pos, n;
    int[] list, grid2;
    bool ok;
    int failures = 0;

    /*
     * We'll use `list' to track the possible places to put our
     * next insertion. There are up to h places to insert in each
     * column: in a column of height n there are n+1 places because
     * we can insert at the very bottom or the very top, but a
     * column of height h can't have anything at all inserted in it
     * so we have up to h in each column. Likewise, with n columns
     * present there are n+1 places to fit a new one in between but
     * we can't insert a column if there are already w; so there
     * are a maximum of w new columns too. Total is wh + w.
     */
    list = new int[wh + w];
    grid2 = new int[wh];

    do {
        /*
         * Start with two or three squares - depending on parity of w*h
         * - of a random colour.
         */
        for (i = 0; i < wh; i++)
            grid[i] = 0;
        j = 2 + (wh % 2);
        c = 1 + rs.Next(0, nc);
	if (j <= w) {
	    for (i = 0; i < j; i++)
		grid[(h-1)*w+i] = c;
	} else {
	    Debug.Assert(j <= h);
	    for (i = 0; i < j; i++)
		grid[(h-1-i)*w] = c;
	}

        /*
         * Now repeatedly insert a two-square blob in the grid, of
         * whatever colour will go at the position we chose.
         */
        while (true) {
            n = 0;

            /*
             * Build up a list of insertion points. Each point is
             * encoded as y*w+x; insertion points between columns are
             * encoded as h*w+x.
             */

            if (grid[wh - 1] == 0) {
                /*
                 * The final column is empty, so we can insert new
                 * columns.
                 */
                for (i = 0; i < w; i++) {
                    list[n++] = wh + i;
                    if (grid[(h-1)*w + i] == 0)
                        break;
                }
            }

            /*
             * Now look for places to insert within columns.
             */
            for (i = 0; i < w; i++) {
                if (grid[(h-1)*w+i] == 0)
                    break;		       /* no more columns */

                if (grid[i] != 0)
                    continue;	       /* this column is full */

                for (j = h; j-- > 0 ;) {
                    list[n++] = j*w+i;
                    if (grid[j*w+i] == 0)
                        break;	       /* this column is exhausted */
                }
            }

            if (n == 0)
                break;		       /* we're done */



            /*
             * Now go through the list one element at a time in
             * random order, and actually attempt to insert
             * something there.
             */
            while (n-- > 0) {
                int[] dirs = new int[4];
                int ndirs, dir;

                i = rs.Next(0, n+1);
                pos = list[i];
                list[i] = list[n];

                x = pos % w;
                y = pos / w;

                Array.Copy(grid, grid2, wh);

                if (y == h) {
                    /*
                     * Insert a column at position x.
                     */
                    for (i = w-1; i > x; i--)
                        for (j = 0; j < h; j++)
                            grid2[j*w+i] = grid2[j*w+(i-1)];
                    /*
                     * Clear the new column.
                     */
                    for (j = 0; j < h; j++)
                        grid2[j*w+x] = 0;
                    /*
                     * Decrement y so that our first square is actually
                     * inserted _in_ the grid rather than just below it.
                     */
                    y--;
                }

                /*
                 * Insert a square within column x at position y.
                 */
                for (i = 0; i+1 <= y; i++)
                    grid2[i*w+x] = grid2[(i+1)*w+x];


                /*
                 * Pick our square colour so that it doesn't match any
                 * of its neighbours.
                 */
                {
                    int[] wrongcol = new int[4];
                    int nwrong = 0;

                    /*
                     * List the neighbouring colours.
                     */
                    if (x > 0)
                        wrongcol[nwrong++] = grid2[y*w+(x-1)];
                    if (x+1 < w)
                        wrongcol[nwrong++] = grid2[y*w+(x+1)];
                    if (y > 0)
                        wrongcol[nwrong++] = grid2[(y-1)*w+x];
                    if (y+1 < h)
                        wrongcol[nwrong++] = grid2[(y+1)*w+x];

                    /*
                     * Eliminate duplicates. We can afford a shoddy
                     * algorithm here because the problem size is
                     * bounded.
                     */
                    for (i = j = 0 ;; i++) {
                        int pos2 = -1, min = 0;
                        if (j > 0)
                            min = wrongcol[j-1];
                        for (k = i; k < nwrong; k++)
                            if (wrongcol[k] > min &&
                                (pos2 == -1 || wrongcol[k] < wrongcol[pos2]))
                                pos2 = k;
                        if (pos2 >= 0) {
                            int v = wrongcol[pos2];
                            wrongcol[pos2] = wrongcol[j];
                            wrongcol[j++] = v;
                        } else
                            break;
                    }
                    nwrong = j;

                    /*
                     * If no colour will go here, stop trying.
                     */
                    if (nwrong == nc)
                        continue;

                    /*
                     * Otherwise, pick a colour from the remaining
                     * ones.
                     */
                    c = 1 + rs.Next(0, nc - nwrong);
                    for (i = 0; i < nwrong; i++) {
                        if (c >= wrongcol[i])
                            c++;
                        else
                            break;
                    }
                }

                /*
                 * Place the new square.
                 * 
                 * Although I've _chosen_ the new region's colour
                 * (so that we can check adjacency), I'm going to
                 * actually place it as an invalid colour (tc)
                 * until I'm sure it's viable. This is so that I
                 * can conveniently check that I really have made a
                 * _valid_ inverse move later on.
                 */
                grid2[y*w+x] = tc;

                /*
                 * Now attempt to extend it in one of three ways: left,
                 * right or up.
                 */
                ndirs = 0;
                if (x > 0 &&
                    grid2[y*w+(x-1)] != c &&
                    grid2[x-1] == 0 &&
                    (y+1 >= h || grid2[(y+1)*w+(x-1)] != c) &&
                    (y+1 >= h || grid2[(y+1)*w+(x-1)] != 0) &&
                    (x <= 1 || grid2[y*w+(x-2)] != c))
                    dirs[ndirs++] = -1;    /* left */
                if (x+1 < w &&
                    grid2[y*w+(x+1)] != c &&
                    grid2[x+1] == 0 &&
                    (y+1 >= h || grid2[(y+1)*w+(x+1)] != c) &&
                    (y+1 >= h || grid2[(y+1)*w+(x+1)] != 0) &&
                    (x+2 >= w || grid2[y*w+(x+2)] != c))
                    dirs[ndirs++] = +1;    /* right */
                if (y > 0 &&
                    grid2[x] == 0 &&
                    (x <= 0 || grid2[(y-1)*w+(x-1)] != c) &&
                    (x+1 >= w || grid2[(y-1)*w+(x+1)] != c)) {
                    /*
                     * We add this possibility _twice_, so that the
                     * probability of placing a vertical domino is
                     * about the same as that of a horizontal. This
                     * should yield less bias in the generated
                     * grids.
                     */
                    dirs[ndirs++] = 0;     /* up */
                    dirs[ndirs++] = 0;     /* up */
                }

                if (ndirs == 0)
                    continue;

                dir = dirs[rs.Next(0, ndirs)];


                /*
                 * Insert a square within column (x+dir) at position y.
                 */
                for (i = 0; i+1 <= y; i++)
                    grid2[i*w+x+dir] = grid2[(i+1)*w+x+dir];
                grid2[y*w+x+dir] = tc;

                /*
                 * See if we've divided the remaining grid squares
                 * into sub-areas. If so, we need every sub-area to
                 * have an even area or we won't be able to
                 * complete generation.
                 * 
                 * If the height is odd and not all columns are
                 * present, we can increase the area of a subarea
                 * by adding a new column in it, so in that
                 * situation we don't mind having as many odd
                 * subareas as there are spare columns.
                 * 
                 * If the height is even, we can't fix it at all.
                 */
                {
                    int nerrs = 0, nfix = 0;
                    k = 0;             /* current subarea size */
                    for (i = 0; i < w; i++) {
                        if (grid2[(h-1)*w+i] == 0) {
                            if ((h % 2)!=0)
                                nfix++;
                            continue;
                        }
                        for (j = 0; j < h && grid2[j*w+i] == 0; j++);
                        Debug.Assert(j < h);
                        if (j == 0) {
                            /*
                             * End of previous subarea.
                             */
                            if ((k % 2)!=0)
                                nerrs++;
                            k = 0;
                        } else {
                            k += j;
                        }
                    }
                    if ((k % 2)!=0)
                        nerrs++;
                    if (nerrs > nfix)
                        continue;      /* try a different placement */
                }

                /*
                 * We've made a move. Verify that it is a valid
                 * move and that if made it would indeed yield the
                 * previous grid state. The criteria are:
                 * 
                 *  (a) removing all the squares of colour tc (and
                 *      shuffling the columns up etc) from grid2
                 *      would yield grid
                 *  (b) no square of colour tc is adjacent to one
                 *      of colour c
                 *  (c) all the squares of colour tc form a single
                 *      connected component
                 * 
                 * We verify the latter property at the same time
                 * as checking that removing all the tc squares
                 * would yield the previous grid. Then we colour
                 * the tc squares in colour c by breadth-first
                 * search, which conveniently permits us to test
                 * that they're all connected.
                 */
                {
                    int x1, x2, y1, y2;
                    bool ok2 = true;
                    int fillstart = -1, ntc = 0;



                    for (x1 = x2 = 0; x2 < w; x2++) {
                        bool usedcol = false;

                        for (y1 = y2 = h-1; y2 >= 0; y2--) {
                            if (grid2[y2*w+x2] == tc) {
                                ntc++;
                                if (fillstart == -1)
                                    fillstart = y2*w+x2;
                                if ((y2+1 < h && grid2[(y2+1)*w+x2] == c) ||
                                    (y2-1 >= 0 && grid2[(y2-1)*w+x2] == c) ||
                                    (x2+1 < w && grid2[y2*w+x2+1] == c) ||
                                    (x2-1 >= 0 && grid2[y2*w+x2-1] == c)) {
                                    ok = false;
                                }
                                continue;
                            }
                            if (grid2[y2*w+x2] == 0)
                                break;
                            usedcol = true;
                            if (grid2[y2*w+x2] != grid[y1*w+x1]) {
                                ok2 = false;
                            }
                            y1--;
                        }

                        /*
                         * If we've reached the top of the column
                         * in grid2, verify that we've also reached
                         * the top of the column in `grid'.
                         */
                        if (usedcol) {
                            while (y1 >= 0) {
                                if (grid[y1*w+x1] != 0) {
                                    ok2 = false;
                                }
                                y1--;
                            }
                        }

                        if (!ok2)
                            break;

                        if (usedcol)
                            x1++;
                    }

                    if (!ok2) {
                        Debug.Assert(false, "This should never happen");

                        /*
                         * If this game is compiled NDEBUG so that
                         * the assertion doesn't bring it to a
                         * crashing halt, the only thing we can do
                         * is to give up, loop round again, and
                         * hope to randomly avoid making whatever
                         * type of move just caused this failure.
                         */
                        continue;
                    }

                    /*
                     * Now use bfs to fill in the tc section as
                     * colour c. We use `list' to store the set of
                     * squares we have to process.
                     */
                    i = j = 0;
                    Debug.Assert(fillstart >= 0);
                    list[i++] = fillstart;

                    while (j < i) {
                        k = list[j];
                        x = k % w;
                        y = k / w;

                        j++;

                        Debug.Assert(grid2[k] == tc);
                        grid2[k] = c;

                        if (x > 0 && grid2[k-1] == tc)
                            list[i++] = k-1;
                        if (x+1 < w && grid2[k+1] == tc)
                            list[i++] = k+1;
                        if (y > 0 && grid2[k-w] == tc)
                            list[i++] = k-w;
                        if (y+1 < h && grid2[k+w] == tc)
                            list[i++] = k+w;
                    }


                    /*
                     * Check that we've filled the same number of
                     * tc squares as we originally found.
                     */
                    Debug.Assert(j == ntc);
                }

                Array.Copy(grid2, grid,  wh);

                break;		       /* done it! */
            }



            if (n < 0)
                break;
        }

        ok = true;
        for (i = 0; i < wh; i++)
            if (grid[i] == 0) {
                ok = false;
                failures++;

                break;
            }

    } while (!ok);
}

/*
 * Not-guaranteed-soluble grid generator; kept as a legacy, and in
 * case someone finds the slightly odd quality of the guaranteed-
 * soluble grids to be aesthetically displeasing or finds its CPU
 * utilisation to be excessive.
 */
static void gen_grid_random(int w, int h, int nc, int[] grid, Random rs)
{
    int i, j, c;
    int n = w * h;

    for (i = 0; i < n; i++)
	grid[i] = 0;

    /*
     * Our sole concession to not gratuitously generating insoluble
     * grids is to ensure we have at least two of every colour.
     */
    for (c = 1; c <= nc; c++) {
	for (j = 0; j < 2; j++) {
	    do {
		i = (int)rs.Next(0, n);
	    } while (grid[i] != 0);
	    grid[i] = c;
	}
    }

    /*
     * Fill in the rest of the grid at random.
     */
    for (i = 0; i < n; i++) {
	if (grid[i] == 0)
	    grid[i] = (int)rs.Next(0, nc)+1;
    }
}

public override string GenerateNewGameDescription(SameGameSettings @params, Random rs, out string aux, int interactive)
{
    int n, i;
    int[] tiles;

    n = @params.w * @params.h;
    tiles = new int[n];

    if (@params.soluble)
	gen_grid(@params.w, @params.h, @params.ncols, tiles, rs);
    else
	gen_grid_random(@params.w, @params.h, @params.ncols, tiles, rs);

    var ret = new StringBuilder();

    for (i = 0; i < n; i++) {
        if ( ret.Length > 0 )
        {
            ret.Append(',');
        }
	    ret.Append(tiles[i]);
    }
    aux =null;
    return ret.ToString();
}

//static char *validate_desc(SameGameSettings @params, const char *desc)
//{
//    int area = @params.w * @params.h, i;
//    const char *p = desc;

//    for (i = 0; i < area; i++) {
//    const char *q = p;
//    int n;

//    if (!isdigit((unsigned char)*p))
//        return "Not enough numbers in string";
//    while (isdigit((unsigned char)*p)) p++;

//    if (i < area-1 && *p != ',')
//        return "Expected comma after number";
//    else if (i == area-1 && *p)
//        return "Excess junk at end of string";

//    n = atoi(q);
//    if (n < 0 || n > @params.ncols)
//        return "Colour out of range";

//    if (*p) p++; /* eat comma */
//    }
//    return null;
//}

public override SameGameState CreateNewGameFromDescription(SameGameSettings @params, string desc)
{
    SameGameState state = new SameGameState();
    int i;

    state.@params = @params; /* struct copy */
    state.n = state.@params.w * state.@params.h;
    state.tiles = new int[state.@params.w, state.@params.h];
    state.dx = new sbyte[state.@params.w, state.@params.h];
    state.dy = new sbyte[state.@params.w, state.@params.h];
    var descSplitted = desc.Split(',');
    for (i = 0; i < state.n; i++) {
	    state.tiles[X(state,i),Y(state,i)] = int.Parse(descSplitted[i]);
    }
    state.complete = state.impossible = false;
    state.score = 0;

    return state;
}

static SameGameState dup_game(SameGameState state)
{
    SameGameState ret = new SameGameState();

    ret.@params = state.@params; 
    ret.n = state.n;
    ret.complete = state.complete;
    ret.impossible = state.impossible;
    ret.score = state.score;

    ret.tiles = new int[state.@params.w, state.@params.h];
    Array.Copy( state.tiles, ret.tiles, state.n);
    ret.dx = new sbyte[state.@params.w, state.@params.h];
    ret.dy = new sbyte[state.@params.w, state.@params.h];
    
    return ret;
}

public override SameGameMove CreateSolveGameMove(SameGameState state, SameGameState currstate, SameGameMove ai, out string error)
{
    error = null;
    return null;
}

public override SameGameUI CreateUI(SameGameState state)
{
    SameGameUI ui = new SameGameUI();

    ui.@params = state.@params; /* structure copy */
    ui.tiles = new int[state.@params.w, state.@params.h];
    ui.nselected = 0;

    ui.xsel = ui.ysel = 0;
    ui.displaysel = false;

    return ui;
}


static void sel_clear(SameGameUI ui, SameGameState state)
{
    for (int x = 0; x < state.@params.w; x++)
	    for (int y = 0; y < state.@params.h; y++)
	        ui.tiles[x,y] &= ~TILE_SELECTED;
    ui.nselected = 0;
}

public override void GameStateChanged(SameGameUI ui, SameGameState oldstate, SameGameState newstate)
{
    sel_clear(ui, newstate);

    /*
     * If the game state has just changed into an unplayable one
     * (either completed or impossible), we vanish the keyboard-
     * control cursor.
     */
    if (newstate.complete || newstate.impossible)
	ui.displaysel = false;
}

static SameGameMove sel_movedesc(SameGameUI ui, SameGameState state)
{
    SameGameMove ret = new SameGameMove();
    for (int x = 0; x < state.@params.w; x++)
    {
        for (int y = 0; y < state.@params.h; y++)
        {
            if ((ui.tiles[x,y] & TILE_SELECTED)!=0)
            {
                ret.select.Add(y * state.@params.w + x);
                ui.tiles[x,y] &= ~TILE_SELECTED;
            }
        }
    }
    ui.nselected = 0;
    return ret;
}

static void sel_expand(SameGameUI ui, SameGameState state, int tx, int ty)
{
    int ns = 1, nadded, x, y, c;

    ui.tiles[tx,ty] |= TILE_SELECTED;
    do {
	nadded = 0;

	for (x = 0; x < state.@params.w; x++) {
	    for (y = 0; y < state.@params.h; y++) {
		if (x == tx && y == ty) continue;
		if (ISSEL(ui,x,y)!=0) continue;

		c = COL(state,x,y);
		if ((x > 0) &&
		    ISSEL(ui,x-1,y) !=0 && COL(state,x-1,y) == c) {
		    ui.tiles[x,y] |= TILE_SELECTED;
		    nadded++;
		    continue;
		}

		if ((x+1 < state.@params.w) &&
		    ISSEL(ui,x+1,y)!=0 && COL(state,x+1,y) == c) {
                ui.tiles[x, y] |= TILE_SELECTED;
		    nadded++;
		    continue;
		}

		if ((y > 0) &&
            ISSEL(ui, x, y - 1) != 0 && COL(state, x, y - 1) == c)
        {
                ui.tiles[x, y] |= TILE_SELECTED;
		    nadded++;
		    continue;
		}

		if ((y+1 < state.@params.h) &&
            ISSEL(ui, x, y + 1) != 0 && COL(state, x, y + 1) == c)
        {
                ui.tiles[x, y] |= TILE_SELECTED;
		    nadded++;
		    continue;
		}
	    }
	}
	ns += nadded;
    } while (nadded > 0);

    if (ns > 1) {
	ui.nselected = ns;
    } else {
	sel_clear(ui, state);
    }
}

static bool sg_emptycol(SameGameState ret, int x)
{
    int y;
    for (y = 0; y < ret.@params.h; y++) {
        if (COL(ret, x, y) != 0) return false;
    }
    return true;
}


static void sg_snuggle(SameGameState ret)
{
    int x,y, ndone;

    /* make all unsupported tiles fall down. */
    do {
	ndone = 0;
	for (x = 0; x < ret.@params.w; x++) {
	    for (y = ret.@params.h-1; y > 0; y--) {
		if (COL(ret,x,y) != 0) continue;
		if (COL(ret,x,y-1) != 0) {
		    SWAPTILE(ret,x,y,x,y-1);
		    ndone++;
		}
	    }
	}
    } while (ndone != 0);

    /* shuffle all columns as far left as they can go. */
    do {
	ndone = 0;
	for (x = 0; x < ret.@params.w-1; x++) {
	    if (sg_emptycol(ret,x) && !sg_emptycol(ret,x+1)) {
		ndone++;
		for (y = 0; y < ret.@params.h; y++) {
		    SWAPTILE(ret,x,y,x+1,y);
		}
	    }
	}
    } while (ndone!=0);
}

static void sg_check(SameGameState ret)
{
    int x,y;
    bool complete = true, impossible = true;

    for (x = 0; x < ret.@params.w; x++) {
	for (y = 0; y < ret.@params.h; y++) {
	    if (COL(ret,x,y) == 0)
		continue;
	    complete = false;
	    if (x+1 < ret.@params.w) {
		if (COL(ret,x,y) == COL(ret,x+1,y))
            impossible = false;
	    }
	    if (y+1 < ret.@params.h) {
		if (COL(ret,x,y) == COL(ret,x,y+1))
            impossible = false;
	    }
	}
    }
    ret.complete = complete;
    ret.impossible = impossible;
}

public override SameGameMove InterpretMove(SameGameState state, SameGameUI ui, SameGameDrawState ds, int x, int y, Buttons button, bool isTouchOrStylus)
{
    int tx, ty;
    SameGameMove ret = null;

    ui.displaysel = false;

    if (button == Buttons.RIGHT_BUTTON || button == Buttons.LEFT_BUTTON)
    {
        tx = FROMCOORD(ds, x); ty = FROMCOORD(ds, y);
    }
    else if (Misc.IS_CURSOR_MOVE(button))
    {
	int dx = 0, dy = 0;
    ui.displaysel = true;
    dx = (button == Buttons.CURSOR_LEFT) ? -1 : ((button == Buttons.CURSOR_RIGHT) ? +1 : 0);
    dy = (button == Buttons.CURSOR_DOWN) ? +1 : ((button == Buttons.CURSOR_UP) ? -1 : 0);
	ui.xsel = (ui.xsel + state.@params.w + dx) % state.@params.w;
	ui.ysel = (ui.ysel + state.@params.h + dy) % state.@params.h;
	return ret;
    } else if (Misc.IS_CURSOR_SELECT(button)) {
	ui.displaysel = true;
	tx = ui.xsel;
	ty = ui.ysel;
    } else
	return null;

    if (tx < 0 || tx >= state.@params.w || ty < 0 || ty >= state.@params.h)
	return null;
    if (COL(state, tx, ty) == 0) return null;

    if (ISSEL(ui, tx, ty) != 0)
    {
        if (button == Buttons.RIGHT_BUTTON || button == Buttons.CURSOR_SELECT2)
	    sel_clear(ui, state);
	else
	    ret = sel_movedesc(ui, state);
    } else {
	sel_clear(ui, state); /* might be no-op */
	sel_expand(ui, state, tx, ty);
    }

    return ret;
}

public override SameGameState ExecuteMove(SameGameState from, SameGameMove move)
{
    int n;
    SameGameState ret;


	ret = dup_game(from);

	n = 0;

	foreach(int i in move.select)
    {
	    if (i < 0 || i >= ret.n) {
		return null;
	    }
	    n++;
        ret.tiles[X(ret, i), Y(ret, i)] = 0;
	}

	ret.score += npoints(ret.@params, n);

	sg_snuggle(ret); /* shifts blanks down and to the left */
	sg_check(ret);   /* checks for completeness or impossibility */

	return ret;

}

public override SameGameMove ParseMove(SameGameSettings settings, string moveString)
{
    return SameGameMove.Parse(settings, moveString);
}

/* ----------------------------------------------------------------------
 * Drawing routines.
 */
public override void SetTileSize(Drawing dr, SameGameDrawState ds, SameGameSettings @params, int tilesize)
{
    ds.tilegap = 2;
    ds.tileinner = tilesize - ds.tilegap;
}

public override void ComputeSize(SameGameSettings @params, int tilesize, out int x, out int y)
{
    /* Ick: fake up tile size variables for macro expansion purposes */
    SameGameDrawState ds = new SameGameDrawState();
    SetTileSize(null, ds, @params, tilesize);

    x = TILE_SIZE(ds) * @params.w + 2 * BORDER(ds) - TILE_GAP(ds);
    y = TILE_SIZE(ds) * @params.h + 2 * BORDER(ds) - TILE_GAP(ds);
}

public override float[] GetColours(Frontend fe, out int ncolours)
{
    float[] ret = new float[3 * NCOLOURS];

    fe.frontend_default_colour(ret,COL_BACKGROUND * 3);

    ret[COL_1 * 3 + 0] = 0.0F;
    ret[COL_1 * 3 + 1] = 0.0F;
    ret[COL_1 * 3 + 2] = 1.0F;

    ret[COL_2 * 3 + 0] = 0.0F;
    ret[COL_2 * 3 + 1] = 0.5F;
    ret[COL_2 * 3 + 2] = 0.0F;

    ret[COL_3 * 3 + 0] = 1.0F;
    ret[COL_3 * 3 + 1] = 0.0F;
    ret[COL_3 * 3 + 2] = 0.0F;

    ret[COL_4 * 3 + 0] = 1.0F;
    ret[COL_4 * 3 + 1] = 1.0F;
    ret[COL_4 * 3 + 2] = 0.0F;

    ret[COL_5 * 3 + 0] = 1.0F;
    ret[COL_5 * 3 + 1] = 0.0F;
    ret[COL_5 * 3 + 2] = 1.0F;

    ret[COL_6 * 3 + 0] = 0.0F;
    ret[COL_6 * 3 + 1] = 1.0F;
    ret[COL_6 * 3 + 2] = 1.0F;

    ret[COL_7 * 3 + 0] = 0.5F;
    ret[COL_7 * 3 + 1] = 0.5F;
    ret[COL_7 * 3 + 2] = 1.0F;

    ret[COL_8 * 3 + 0] = 0.5F;
    ret[COL_8 * 3 + 1] = 1.0F;
    ret[COL_8 * 3 + 2] = 0.5F;

    ret[COL_9 * 3 + 0] = 1.0F;
    ret[COL_9 * 3 + 1] = 0.5F;
    ret[COL_9 * 3 + 2] = 0.5F;

    ret[COL_IMPOSSIBLE * 3 + 0] = 0.0F;
    ret[COL_IMPOSSIBLE * 3 + 1] = 0.0F;
    ret[COL_IMPOSSIBLE * 3 + 2] = 0.0F;

    ret[COL_SEL * 3 + 0] = 1.0F;
    ret[COL_SEL * 3 + 1] = 1.0F;
    ret[COL_SEL * 3 + 2] = 1.0F;

    ret[COL_HIGHLIGHT * 3 + 0] = 1.0F;
    ret[COL_HIGHLIGHT * 3 + 1] = 1.0F;
    ret[COL_HIGHLIGHT * 3 + 2] = 1.0F;

    ret[COL_LOWLIGHT * 3 + 0] = ret[COL_BACKGROUND * 3 + 0] * 2.0F / 3.0F;
    ret[COL_LOWLIGHT * 3 + 1] = ret[COL_BACKGROUND * 3 + 1] * 2.0F / 3.0F;
    ret[COL_LOWLIGHT * 3 + 2] = ret[COL_BACKGROUND * 3 + 2] * 2.0F / 3.0F;

    ncolours = NCOLOURS;
    return ret;
}

public override SameGameDrawState CreateDrawState(Drawing dr, SameGameState state)
{
    SameGameDrawState ds = new SameGameDrawState();


    ds.started = false;
    ds.tileinner = ds.tilegap = 0;   /* not decided yet */
    ds.tiles = new int[state.@params.w, state.@params.h];
    ds.bgcolour = -1;
    for (int x = 0; x < state.@params.w; x++)
        for (int y = 0; y < state.@params.h; y++)
            ds.tiles[x,y] = -1;

    return ds;
}

public override float AnimDuration
{
    get
    {
        return 0.20F;
    }
}
/* Drawing routing for the tile at (x,y) is responsible for drawing
 * itself and the gaps to its right and below. If we're the same colour
 * as the tile to our right, then we fill in the gap; ditto below, and if
 * both then we fill the teeny tiny square in the corner as well.
 */

 void tile_redraw(Drawing dr, SameGameDrawState ds,
            int x, int y, bool dright, bool dbelow,
                        int tile, int bgcolour, SameGameState oldstate, SameGameState state, float animtime)
{
    int outer = bgcolour, inner = outer, col = tile & TILE_COLMASK;
    int dx = 0, dy = 0;
    if (oldstate != null && animtime <= AnimDuration)
    {
        float f = TILE_SIZE(ds) * (float)Math.Tanh((1 - animtime / AnimDuration) * Math.PI);
        float f1 = TILE_SIZE(ds) * (1 - animtime / AnimDuration);
        dx = (int)(state.dx[x, y] * f1);
        dy = (int)(state.dy[x, y] * f);
    }

    if (col!=0) {
	if ((tile & TILE_IMPOSSIBLE)!=0) {
	    outer = col;
	    inner = COL_IMPOSSIBLE;
	} else if ((tile & TILE_SELECTED)!=0) {
	    outer = COL_SEL;
	    inner = col;
	} else {
	    outer = inner = col;
	}
    } else {
        return;
    }
    dr.draw_rect(dx+COORD(ds, x), dy+COORD(ds, y), TILE_INNER(ds), TILE_INNER(ds), outer);
    dr.draw_rect(dx + COORD(ds, x) + TILE_INNER(ds) / 4, dy + COORD(ds, y) + TILE_INNER(ds) / 4,
          TILE_INNER(ds) / 2, TILE_INNER(ds) / 2, inner);

    if (dright)
        dr.draw_rect(dx + COORD(ds, x) + TILE_INNER(ds), dy + COORD(ds, y), TILE_GAP(ds), TILE_INNER(ds),
          (tile & TILE_JOINRIGHT) != 0 ? outer : bgcolour);
    if (dbelow)
        dr.draw_rect(dx + COORD(ds, x), dy + COORD(ds, y) + TILE_INNER(ds), TILE_INNER(ds), TILE_GAP(ds),
          (tile & TILE_JOINDOWN) != 0 ? outer : bgcolour);
    if (dright && dbelow)
        dr.draw_rect(dx + COORD(ds, x) + TILE_INNER(ds), dy + COORD(ds, y) + TILE_INNER(ds), TILE_GAP(ds), TILE_GAP(ds),
          (tile & TILE_JOINDIAG) != 0 ? outer : bgcolour);

    if ((tile & TILE_HASSEL)!= 0) {
        int sx = COORD(ds, x) + 2, sy = COORD(ds, y) + 2, ssz = TILE_INNER(ds) - 5;
	int scol = (outer == COL_SEL) ? COL_LOWLIGHT : COL_HIGHLIGHT;
	dr.draw_line( sx,     sy,     sx+ssz, sy,     scol);
	dr.draw_line( sx+ssz, sy,     sx+ssz, sy+ssz, scol);
	dr.draw_line( sx+ssz, sy+ssz, sx,     sy+ssz, scol);
	dr.draw_line( sx,     sy+ssz, sx,     sy,     scol);
    }

    dr.draw_update(COORD(ds, x), COORD(ds, y), TILE_SIZE(ds), TILE_SIZE(ds));
}

public override void Redraw(Drawing dr, SameGameDrawState ds, SameGameState oldstate, SameGameState state, int dir, SameGameUI ui, float animtime, float flashtime)
{
    int bgcolour, x, y;

    /* This was entirely cloned from fifteen.c; it should probably be
     * moved into some generic 'draw-recessed-rectangle' utility fn. */
    if (!ds.started) {
	int[] coords = new int[10];

	dr.draw_rect( 0, 0,
          TILE_SIZE(ds) * state.@params.w + 2 * BORDER(ds),
          TILE_SIZE(ds) * state.@params.h + 2 * BORDER(ds), COL_BACKGROUND);
	dr.draw_update( 0, 0,
            TILE_SIZE(ds) * state.@params.w + 2 * BORDER(ds),
            TILE_SIZE(ds) * state.@params.h + 2 * BORDER(ds));

	/*
	 * Recessed area containing the whole puzzle.
	 */
    //coords[0] = COORD(ds, state.@params.w) + HIGHLIGHT_WIDTH - 1 - TILE_GAP(ds);
    //coords[1] = COORD(ds, state.@params.h) + HIGHLIGHT_WIDTH - 1 - TILE_GAP(ds);
    //coords[2] = COORD(ds, state.@params.w) + HIGHLIGHT_WIDTH - 1 - TILE_GAP(ds);
    //coords[3] = COORD(ds, 0) - HIGHLIGHT_WIDTH;
    //coords[4] = coords[2] - TILE_SIZE(ds);
    //coords[5] = coords[3] + TILE_SIZE(ds);
    //coords[8] = COORD(ds, 0) - HIGHLIGHT_WIDTH;
    //coords[9] = COORD(ds, state.@params.h) + HIGHLIGHT_WIDTH - 1 - TILE_GAP(ds);
    //coords[6] = coords[8] + TILE_SIZE(ds);
    //coords[7] = coords[9] - TILE_SIZE(ds);
    //dr.draw_polygon(coords, 5, COL_HIGHLIGHT, COL_HIGHLIGHT);

    //coords[1] = COORD(ds, 0) - HIGHLIGHT_WIDTH;
    //coords[0] = COORD(ds, 0) - HIGHLIGHT_WIDTH;
    //dr.draw_polygon(coords, 5, COL_LOWLIGHT, COL_LOWLIGHT);

	ds.started = true;
    }

    if (flashtime > 0.0) {
	int frame = (int)(flashtime / FLASH_FRAME);
    bgcolour = ((frame % 2) != 0 ? COL_LOWLIGHT : COL_HIGHLIGHT);
    } else
	bgcolour = COL_BACKGROUND;

    for (x = 0; x < state.@params.w; x++) {
	for (y = 0; y < state.@params.h; y++) {
        //int i = (state.@params.w * y) + x;
	    int col = COL(state,x,y), tile = col;
	    bool dright = (x+1 < state.@params.w);
        bool dbelow = (y + 1 < state.@params.h);

	    tile |= ISSEL(ui,x,y);
	    if (state.impossible)
		tile |= TILE_IMPOSSIBLE;
	    if (dright && COL(state,x+1,y) == col)
		tile |= TILE_JOINRIGHT;
	    if (dbelow && COL(state,x,y+1) == col)
		tile |= TILE_JOINDOWN;
        if ((tile & TILE_JOINRIGHT) != 0 && (tile & TILE_JOINDOWN) != 0 &&
		COL(state,x+1,y+1) == col)
		tile |= TILE_JOINDIAG;

	    if (ui.displaysel && ui.xsel == x && ui.ysel == y)
		tile |= TILE_HASSEL;

	    /* For now we're never expecting oldstate at all (because we have
	     * no animation); when we do we might well want to be looking
	     * at the tile colours from oldstate, not state. */
        if ((oldstate != null && COL(oldstate, x, y) != col) ||
		(ds.bgcolour != bgcolour) ||
        (tile != ds.tiles[x, y]))
        {
		tile_redraw(dr, ds, x, y, dright, dbelow, tile, bgcolour, oldstate, state, animtime);
        ds.tiles[x, y] = tile;
	    }
	}
    }
    ds.bgcolour = bgcolour;

    //{
    //char status[255], score[80];

    //sprintf(score, "Score: %d", state.score);

    //if (state.complete)
    //    sprintf(status, "COMPLETE! %s", score);
    //else if (state.impossible)
    //    sprintf(status, "Cannot move! %s", score);
    //else if (ui.nselected)
    //    sprintf(status, "%s  Selected: %d (%d)",
    //        score, ui.nselected, npoints(&state.@params, ui.nselected));
    //else
    //    sprintf(status, "%s", score);
    //status_bar(dr, status);
    //}
}

static float game_anim_length(SameGameState oldstate,
                              SameGameState newstate, int dir, SameGameUI ui)
{
    return 0.0F;
}

static float game_flash_length(SameGameState oldstate,
                               SameGameState newstate, int dir, SameGameUI ui)
{
    if ((!oldstate.complete && newstate.complete) ||
        (!oldstate.impossible && newstate.impossible))
	return 2 * FLASH_FRAME;
    else
	return 0.0F;
}

static int game_status(SameGameState state)
{
    /*
     * Dead-end situations are assumed to be rescuable by Undo, so we
     * don't bother to identify them and return -1.
     */
    return state.complete ? +1 : 0;
}

//static int game_timing_state(SameGameState state, SameGameUI ui)
//{
//    return true;
//}
internal override void SetKeyboardCursorVisible(SameGameUI ui, int tileSize, bool value)
{
    ui.displaysel = value;
}





    }
}
