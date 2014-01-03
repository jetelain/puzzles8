using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Bridges
{
    public class BridgesGame : GameBase<BridgesSettings, BridgesState, BridgesMove, BridgesDrawState, BridgesUI>
    {
        const int MAX_BRIDGES    =4;

        const int PREFERRED_TILE_SIZE =24;
        private static int TILE_SIZE (BridgesDrawState ds) { return    (ds.tilesize); }
        private static int BORDER (BridgesDrawState ds) { return    (TILE_SIZE(ds) / 2); }

        private static int COORD(BridgesDrawState ds, int x) { return ((x) * TILE_SIZE(ds) + BORDER(ds)); }
        private static int FROMCOORD(BridgesDrawState ds, int x) { return (((x) - BORDER(ds) + TILE_SIZE(ds)) / TILE_SIZE(ds) - 1); }

        const float FLASH_TIME =0.50F;

        const int COL_BACKGROUND=0;
        const int COL_FOREGROUND=1;
        const int COL_HIGHLIGHT=2; 
        const int COL_LOWLIGHT=3;
        const int COL_SELECTED=4;
        const int COL_MARK=5;
        const int COL_HINT=6;
        const int COL_GRID=7;
        const int COL_WARNING=8;
        const int COL_CURSOR=9;
        const int NCOLOURS = 10;

        /* general flags used by all structs */
        const uint G_ISLAND = 0x0001;
        const uint G_LINEV         =0x0002;     /* contains a vert. line */
        const uint G_LINEH         =0x0004;     /* contains a horiz. line (mutex with LINEV) */
        const uint G_LINE          =(G_LINEV|G_LINEH);
        const uint G_MARKV         =0x0008;
        const uint G_MARKH         =0x0010;
        const uint G_MARK          =(G_MARKV|G_MARKH);
        const uint G_NOLINEV       =0x0020;
        const uint G_NOLINEH       =0x0040;
        const uint G_NOLINE        =(G_NOLINEV|G_NOLINEH);

        /* flags used by the drawstate */
        const uint G_ISSEL         =0x0080;
        const uint G_REDRAW        =0x0100;
        const uint G_FLASH         =0x0200;
        const uint G_WARN          =0x0400;
        const uint G_CURSOR        =0x0800;

        /* flags used by the solver etc. */
        const uint G_SWEEP         =0x1000;

        const uint G_FLAGSH        =(G_LINEH|G_MARKH|G_NOLINEH);
        const uint G_FLAGSV        =(G_LINEV|G_MARKV|G_NOLINEV);

        private static int  GRIDSZ(BridgesSettings s) { return ((s).w * (s).h); }
        private static int  GRIDSZ(BridgesState s) { return ((s).w * (s).h); }

        private static bool  INGRID(BridgesSettings s, int x, int y) { return  ((x) >= 0 && (x) < (s).w && (y) >= 0 && (y) < (s).h); }
        private static bool  INGRID(BridgesState s, int x, int y) { return  ((x) >= 0 && (x) < (s).w && (y) >= 0 && (y) < (s).h); }

        private static int  DINDEX(BridgesState state, int x, int y) { return  ((y)*state.w + (x)); }

        //private static int  s.g[x,y] { return  ((s).g[(y)*((s).w) + (x)]); }
        //private static int  IDX(s,g,i) { return  ((s).g[(i)]); }
        private static uint GRID(BridgesState s, int x, int y) { return s.grid[x, y]; }
        private static uint SCRATCH(BridgesState s, int x, int y) { return s.scratch[x, y]; }
        private static int POSSIBLES(BridgesState s, int dx, int x, int y) { return ((dx != 0) ? (s.possh[x, y]) : (s.possv[x, y])); }
        private static int MAXIMUM(BridgesState s, int dx, int x, int y) { return ((dx != 0) ? (s.maxh[x, y]) : (s.maxv[x, y])); }

        private static int GRIDCOUNT(BridgesState s, int x, int y, uint f) { return ((s.grid[ x, y] & (f)) != 0 ? (s.lines[x, y]) : 0); }

        private static bool  WITHIN2(int x,int min, int max) { return  (((x) < (min)) ? false : (((x) > (max)) ? false : true)); }
        private static bool  WITHIN(int x, int min, int max) { return  ((min) > (max) ? 
                                   WITHIN2(x,max,min) : WITHIN2(x,min,max)); }

        /* --- island struct and tree support functions --- */
        private static int  ISLAND_ORTHX(BridgesIsland isld, int j) {  
            return (isld.x + (isld.adj.points[(j)].off*isld.adj.points[(j)].dx));
        }
        private static int  ISLAND_ORTHY(BridgesIsland isld, int j) {  
            return (isld.y + (isld.adj.points[(j)].off*isld.adj.points[(j)].dy));
        }

        private static void ADDPOINT(BridgesIsland isld, bool cond, int ddx,int ddy)  
        {
            if (cond) { 
                isld.adj.points[isld.adj.npoints].x = isld.x+(ddx); 
                isld.adj.points[isld.adj.npoints].y = isld.y+(ddy); 
                isld.adj.points[isld.adj.npoints].dx = (ddx); 
                isld.adj.points[isld.adj.npoints].dy = (ddy); 
                isld.adj.points[isld.adj.npoints].off = 0; 
                isld.adj.npoints++; 
            } 
        }
        static void fixup_islands_for_realloc(BridgesState state) // XXX: Be carefull, state does not looks immutable...
        {
             for (int x = 0; x < state.w; x++)
                 for (int y = 0; y < state.h; y++) 
                     state.gridi[x,y] = null;

             for (int i = 0; i < state.islands.Count; i++)
             {
                var isld = state.islands[i];
                isld.state = state;
                state.gridi[isld.x, isld.y] = isld;
            }
        }
        
static string game_text_format(BridgesState state)
{
    int x, y, len, nl;
    BridgesIsland isld;
    uint grid;

    var ret = new StringBuilder();

    for (y = 0; y < state.h; y++) {
        for (x = 0; x < state.w; x++) {
            grid = GRID(state,x,y);
            nl = state.lines[x,y];
            isld = state.gridi[x, y];
            if (isld != null) {
                ret.Append((char)( '0' + isld.count));
            }
            else if ((grid & G_LINEV) != 0)
            {
                ret.Append((nl > 1) ? '"' : (nl == 1) ? '|' : '!'); /* gaah, want a double-bar. */
            } else if ((grid & G_LINEH)!=0) {
                ret.Append((nl > 1) ? '=' : (nl == 1) ? '-' : '~');
            }
            else if ((grid & G_NOLINE) != 0)
            {
                ret.Append('x');
            }
            else
            {
                ret.Append('.');
            }
        }
        ret.AppendLine();
    }
    return ret.ToString();
}

static void debug_state(BridgesState state)
{
    Debug.WriteLine(game_text_format(state));
}

        static void island_set_surrounds(BridgesIsland isld)
        {
            Debug.Assert(INGRID(isld.state,isld.x,isld.y));
            isld.adj.npoints = isld.adj.nislands = 0;

            ADDPOINT(isld, isld.x > 0,                -1,  0);
            ADDPOINT(isld,isld.x < (isld.state.w-1), +1,  0);
            ADDPOINT(isld,isld.y > 0,                 0, -1);
            ADDPOINT(isld,isld.y < (isld.state.h-1),  0, +1);
        }
        static void island_find_orthogonal(BridgesIsland isld)
{
    /* fills in the rest of the 'surrounds' structure, assuming
     * all other islands are now in place. */
    int i, x, y, dx, dy, off;

    isld.adj.nislands = 0;
    for (i = 0; i < isld.adj.npoints; i++) {
        dx = isld.adj.points[i].dx;
        dy = isld.adj.points[i].dy;
        x = isld.x + dx;
        y = isld.y + dy;
        off = 1;
        isld.adj.points[i].off = 0;
        while (INGRID(isld.state, x, y)) {
            if ((isld.state.grid[ x, y] & G_ISLAND)!=0) {
                isld.adj.points[i].off = off;
                isld.adj.nislands++;
                /*Debug.WriteLine(("island (%d,%d) has orth isld. %d*(%d,%d) away at (%d,%d).",
                       isld.x, isld.y, off, dx, dy,
                       ISLAND_ORTHX(isld,i), ISLAND_ORTHY(isld,i)));*/
                goto foundisland;
            }
            off++; x += dx; y += dy;
        }
foundisland:
        ;
    }
}

static bool island_hasbridge(BridgesIsland isld, int direction)
{
    int x = isld.adj.points[direction].x;
    int y = isld.adj.points[direction].y;
    uint gline = isld.adj.points[direction].dx !=0? G_LINEH : G_LINEV;

    if ((isld.state.grid[ x, y] & gline)!=0) return true;
    return false;
}

static BridgesIsland island_find_connection(BridgesIsland isld, int adjpt)
{
    BridgesIsland is_r;

    Debug.Assert(adjpt < isld.adj.npoints);
    if (isld.adj.points[adjpt].off==0) return null;
    if (!island_hasbridge(isld, adjpt)) return null;

    is_r = isld.state.gridi[ISLAND_ORTHX(isld, adjpt), ISLAND_ORTHY(isld, adjpt)];
    Debug.Assert(is_r != null);

    return is_r;
}

static BridgesIsland island_add(BridgesState state, int x, int y, int count)
{
    BridgesIsland isld;

    Debug.Assert((state.grid[x,y] & G_ISLAND)==0);
    state.grid[x,y] |= G_ISLAND;

    isld = new BridgesIsland();
    state.islands.Add(isld);

    //memset(isld, 0, sizeof(struct island));
    isld.state = state;
    isld.x = x;
    isld.y = y;
    isld.count = count;
    island_set_surrounds(isld);

    state.gridi[x, y] = isld;

    return isld;
}


/* n = -1 means 'flip NOLINE flags [and set line to 0].' */
static void island_join(BridgesIsland i1, BridgesIsland i2, int n, bool is_max)
{
    BridgesState state = i1.state;
    int s, e, x, y;

    Debug.Assert(i1.state == i2.state);
    Debug.Assert(n >= -1 && n <= i1.state.maxb);

    if (i1.x == i2.x) {
        x = i1.x;
        if (i1.y < i2.y) {
            s = i1.y+1; e = i2.y-1;
        } else {
            s = i2.y+1; e = i1.y-1;
        }
        for (y = s; y <= e; y++) {
            if (is_max) {
                state.maxv[x, y] = (sbyte)n;
            } else {
                if (n < 0) {
                    state.grid[x,y] ^= G_NOLINEV;
                } else if (n == 0) {
                    state.grid[x,y] &= ~G_LINEV;
                } else {
                    state.grid[x,y] |= G_LINEV;
                    state.lines[x, y] = (sbyte)n;
                }
            }
        }
    } else if (i1.y == i2.y) {
        y = i1.y;
        if (i1.x < i2.x) {
            s = i1.x+1; e = i2.x-1;
        } else {
            s = i2.x+1; e = i1.x-1;
        }
        for (x = s; x <= e; x++) {
            if (is_max) {
                state.maxh[x, y] = (sbyte)n;
            } else {
                if (n < 0) {
                    state.grid[x,y] ^= G_NOLINEH;
                } else if (n == 0) {
                    state.grid[x,y] &= ~G_LINEH;
                } else {
                    state.grid[x,y] |= G_LINEH;
                    state.lines[x,y] = (sbyte)n;
                }
            }
        }
    } else {
        Debug.Assert(false, "island_join: islands not orthogonal.");
    }
}

/* Counts the number of bridges currently attached to the island. */
static int island_countbridges(BridgesIsland isld)
{
    int i, c = 0;

    for (i = 0; i < isld.adj.npoints; i++) {
        c += GRIDCOUNT(isld.state,
                       isld.adj.points[i].x, isld.adj.points[i].y,
                       isld.adj.points[i].dx != 0 ? G_LINEH : G_LINEV);
    }
    /*Debug.WriteLine(("island count for (%d,%d) isld %d.", isld.x, isld.y, c));*/
    return c;
}

static int island_adjspace(BridgesIsland isld, bool marks, int missing,
                           int direction)
{
    int x, y, poss, curr, dx;
    uint gline, mline;

    x = isld.adj.points[direction].x;
    y = isld.adj.points[direction].y;
    dx = isld.adj.points[direction].dx;
    gline = dx !=0? G_LINEH : G_LINEV;

    if (marks) {
        mline = dx !=0? G_MARKH : G_MARKV;
        if ((isld.state.grid[x,y] & mline) !=0) return 0;
    }
    poss = POSSIBLES(isld.state, dx, x, y);
    poss = Math.Min(poss, missing);

    curr = GRIDCOUNT(isld.state, x, y, gline);
    poss = Math.Min(poss, MAXIMUM(isld.state, dx, x, y) - curr);

    return poss;
}

/* Counts the number of bridge spaces left around the island;
 * expects the possibles to be up-to-date. */
static int island_countspaces(BridgesIsland isld, bool marks)
{
    int i, c = 0, missing;

    missing = isld.count - island_countbridges(isld);
    if (missing < 0) return 0;

    for (i = 0; i < isld.adj.npoints; i++) {
        c += island_adjspace(isld, marks, missing, i);
    }
    return c;
}

static int island_isadj(BridgesIsland isld, int direction)
{
    int x, y;
    uint gline, mline;

    x = isld.adj.points[direction].x;
    y = isld.adj.points[direction].y;

    mline = isld.adj.points[direction].dx != 0 ? G_MARKH : G_MARKV;
    gline = isld.adj.points[direction].dx != 0? G_LINEH : G_LINEV;
    if ((isld.state.grid[ x, y] & mline) != 0) {
        /* If we're marked (i.e. the thing to attach to isld complete)
         * only count an adjacency if we're already attached. */
        return GRIDCOUNT(isld.state, x, y, gline);
    } else {
        /* If we're unmarked, count possible adjacency iff it's
         * flagged as POSSIBLE. */
        return POSSIBLES(isld.state, isld.adj.points[direction].dx, x, y);
    }
    return 0;
}

/* Counts the no. of possible adjacent islands (including islands
 * we're already connected to). */
static int island_countadj(BridgesIsland isld)
{
    int i, nadj = 0;

    for (i = 0; i < isld.adj.npoints; i++) {
        if (island_isadj(isld, i)!=0) nadj++;
    }
    return nadj;
}

static void island_togglemark(BridgesIsland isld)
{
    int i, j, x, y, o;
    BridgesIsland is_loop;

    /* mark the island... */
    isld.state.grid[ isld.x, isld.y] ^= G_MARK;

    /* ...remove all marks on non-island squares... */
    for (x = 0; x < isld.state.w; x++) {
        for (y = 0; y < isld.state.h; y++) {
            if ((isld.state.grid[ x, y] & G_ISLAND)==0)
                isld.state.grid[ x, y] &= ~G_MARK;
        }
    }

    /* ...and add marks to squares around marked islands. */
    for (i = 0; i < isld.state.islands.Count; i++) {
        is_loop = isld.state.islands[i];
        if ((is_loop.state.grid[ is_loop.x, is_loop.y] & G_MARK)==0)
            continue;

        for (j = 0; j < is_loop.adj.npoints; j++) {
            /* if this direction takes us to another island, mark all
             * squares between the two islands. */
            if (is_loop.adj.points[j].off==0) continue;
            Debug.Assert(is_loop.adj.points[j].off > 1);
            for (o = 1; o < is_loop.adj.points[j].off; o++) {
                is_loop.state.grid[
                     is_loop.x + is_loop.adj.points[j].dx*o,
                     is_loop.y + is_loop.adj.points[j].dy*o] |=
                    is_loop.adj.points[j].dy != 0 ? G_MARKV : G_MARKH;
            }
        }
    }
}

static bool island_impossible(BridgesIsland isld, bool strict)
{
    int curr = island_countbridges(isld), nspc = isld.count - curr, nsurrspc;
    int i, poss;
    BridgesIsland is_orth;

    if (nspc < 0) {
        Debug.WriteLine("island at ({0},{1}) impossible because full.", isld.x, isld.y);
        return true;        /* too many bridges */
    } else if ((curr + island_countspaces(isld, false)) < isld.count) {
        Debug.WriteLine("island at ({0},{1}) impossible because not enough spaces.", isld.x, isld.y);
        return true;        /* impossible to create enough bridges */
    } else if (strict && curr < isld.count) {
        Debug.WriteLine("island at ({0},{1}) impossible because locked.", isld.x, isld.y);
        return true;        /* not enough bridges and island isld locked */
    }

    /* Count spaces in surrounding islands. */
    nsurrspc = 0;
    for (i = 0; i < isld.adj.npoints; i++) {
        int ifree, dx = isld.adj.points[i].dx;

        if (isld.adj.points[i].off==0) continue;
        poss = POSSIBLES(isld.state, dx,
                         isld.adj.points[i].x, isld.adj.points[i].y);
        if (poss == 0) continue;
        is_orth = isld.state.gridi[ISLAND_ORTHX(isld,i), ISLAND_ORTHY(isld,i)];
        Debug.Assert(is_orth != null);

        ifree = is_orth.count - island_countbridges(is_orth);
        if (ifree > 0) {
	    /*
	     * ifree isld the number of bridges unfilled in the other
	     * island, which isld clearly an upper bound on the number
	     * of extra bridges this island may run to it.
	     *
	     * Another upper bound isld the number of bridges unfilled
	     * on the specific line between here and there. We must
	     * take the minimum of both.
	     */
	    int bmax = MAXIMUM(isld.state, dx,
			       isld.adj.points[i].x, isld.adj.points[i].y);
	    int bcurr = GRIDCOUNT(isld.state,
				  isld.adj.points[i].x, isld.adj.points[i].y,
				  dx != 0 ? G_LINEH : G_LINEV);
	    Debug.Assert(bcurr <= bmax);
            nsurrspc += Math.Min(ifree, bmax - bcurr);
	}
    }
    if (nsurrspc < nspc) {
        Debug.WriteLine("island at ({0},{1}) impossible: surr. islands {2} spc, need {3}.",
               isld.x, isld.y, nsurrspc, nspc);
        return true;       /* not enough spaces around surrounding islands to fill this one. */
    }

    return false;
}
        private static readonly BridgesSettings[] presets = new [] {
          new BridgesSettings( 7, 7, 2, 30, 10, true, 0 ),
          new BridgesSettings( 7, 7, 2, 30, 10, true, 1 ),
          new BridgesSettings( 7, 7, 2, 30, 10, true, 2 ),
          new BridgesSettings( 10, 10, 2, 30, 10, true, 0 ),
          new BridgesSettings( 10, 10, 2, 30, 10, true, 1 ),
          new BridgesSettings( 10, 10, 2, 30, 10, true, 2 ),
          new BridgesSettings( 15, 15, 2, 30, 10, true, 0 ),
          new BridgesSettings( 15, 15, 2, 30, 10, true, 1 ),
          new BridgesSettings( 15, 15, 2, 30, 10, true, 2 ),
        };
        public override IEnumerable<BridgesSettings> PresetsSettings
        {
            get { return presets; }
        }
        public override BridgesSettings DefaultSettings
        {
            get { return presets[0]; }
        }
        public override BridgesSettings ParseSettings(string settingsString)
        {
            return BridgesSettings.Parse(settingsString);
        }
        /* --- Game encoding and differences --- */

static string encode_game(BridgesState state)
{
    StringBuilder ret = new StringBuilder();
    int wh = state.w*state.h, run, x, y;
    BridgesIsland isld;

    run = 0;
    for (y = 0; y < state.h; y++) {
        for (x = 0; x < state.w; x++) {
            isld = state.gridi[ x, y];
            if (isld != null) {
                if (run != 0)
                {
                    ret.Append((char)(('a' - 1) + run)); 
                    run = 0;
                }
                if (isld.count < 10)
                    ret.Append((char)('0' + isld.count));
                else
                    ret.Append((char)('A' + (isld.count - 10)));
            } else {
                if (run == 26) {
                    ret.Append((char)(('a'-1) + run));
                    run = 0;
                }
                run++;
            }
        }
    }
    if (run!=0) {
        ret.Append((char)(('a' - 1) + run)); 
        run = 0;
    }

    return ret.ToString();
}

        
/* --- Game setup and solving utilities --- */

/* This function isld optimised; a Quantify showed that lots of grid-generation time
 * (>50%) was spent in here. Hence the IDX() stuff. */

static void map_update_possibles(BridgesState state)
{
    int x, y, s, e, bl, i, np, maxb, w = state.w, idx;
    BridgesIsland is_s = null, is_f = null;

    /* Run down vertical stripes [un]setting possv... */
    for (x = 0; x < state.w; x++) {
        idx = x;
        s = e = -1;
        bl = 0;
        maxb = state.@params.maxb;     /* placate optimiser */
        /* Unset possible flags until we find an island. */
        for (y = 0; y < state.h; y++) {
            Debug.Assert(idx ==  (y * w)+x);
            is_s = state.gridi[x,y];
            if (is_s != null) {
                maxb = is_s.count;
                break;
            }

            state.possv[x,y] = 0;
            idx += w;
        }
        for (; y < state.h; y++) {
            Debug.Assert(idx == (y * w) + x);
            maxb = Math.Min(maxb, state.maxv[x, y]);
            is_f = state.gridi[x,y];
            if (is_f!= null) {
                Debug.Assert(is_s != null);
                np = Math.Min(maxb, is_f.count);

                if (s != -1) {
                    for (i = s; i <= e; i++) {
                        state.possv[ x, i] = bl != 0 ? (sbyte)0 : (sbyte)np;
                    }
                }
                s = y+1;
                bl = 0;
                is_s = is_f;
                maxb = is_s.count;
            } else {
                e = y;
                if ((state.grid[x,y] & (G_LINEH|G_NOLINEV))!=0) bl = 1;
            }
            idx += w;
        }
        if (s != -1) {
            for (i = s; i <= e; i++)
                state.possv[ x, i] = 0;
        }
    }

    /* ...and now do horizontal stripes [un]setting possh. */
    /* can we lose this clone'n'hack? */
    for (y = 0; y < state.h; y++) {
        idx = y*w;
        s = e = -1;
        bl = 0;
        maxb = state.@params.maxb;     /* placate optimiser */
        for (x = 0; x < state.w; x++) {
            Debug.Assert(idx == (y * w) + x);
            is_s = state.gridi[x, y];
            if (is_s != null) {
                maxb = is_s.count;
                break;
            }

            state.possh[x,y] = 0;
            idx += 1;
        }
        for (; x < state.w; x++) {
            Debug.Assert(idx == (y * w) + x);
            maxb = Math.Min(maxb, state.maxh[x, y]);
            is_f = state.gridi[x,y];
            if (is_f != null) {
                Debug.Assert(is_s != null);
                np = Math.Min(maxb, is_f.count);

                if (s != -1) {
                    for (i = s; i <= e; i++) {
                        state. possh[ i, y] = bl != 0 ? (sbyte)0 : (sbyte)np;
                    }
                }
                s = x+1;
                bl = 0;
                is_s = is_f;
                maxb = is_s.count;
            } else {
                e = x;
                if ((state.grid[x,y] & (G_LINEV|G_NOLINEH))!=0) bl = 1;
            }
            idx += 1;
        }
        if (s != -1) {
            for (i = s; i <= e; i++)
                state. possh[ i, y] = 0;
        }
    }
}

static void map_count(BridgesState state)
{
    int i, n, ax, ay;
    uint flag, grid;
    BridgesIsland isld;

    for (i = 0; i < state.islands.Count; i++) {
        isld = state.islands[i];
        isld.count = 0;
        for (n = 0; n < isld.adj.npoints; n++) {
            ax = isld.adj.points[n].x;
            ay = isld.adj.points[n].y;
            flag = (ax == isld.x) ? G_LINEV : G_LINEH;
            grid = GRID(state,ax,ay);
            if ((grid & flag)!=0) {
                isld.count += state.lines[ax,ay];
            }
        }
    }
}

static void map_find_orthogonal(BridgesState state)
{
    int i;

    for (i = 0; i < state.islands.Count; i++) {
        island_find_orthogonal(state.islands[i]);
    }
}

static int grid_degree(BridgesState state, int x, int y, ref int nx_r, ref int ny_r)
{
    uint grid = SCRATCH(state, x, y), gline = grid & G_LINE;
    BridgesIsland isld;
    int x1, y1, x2, y2, c = 0, i, nx, ny;

    nx = ny = -1; /* placate optimiser */
    isld = state. gridi[ x, y];
    if (isld!=null) {
        for (i = 0; i < isld.adj.npoints; i++) {
            gline = isld.adj.points[i].dx!=0 ? G_LINEH : G_LINEV;
            if ((SCRATCH(state,
                        isld.adj.points[i].x,
                        isld.adj.points[i].y) & gline)!=0) {
                nx = isld.adj.points[i].x;
                ny = isld.adj.points[i].y;
                c++;
            }
        }
    } else if (gline != 0) {
        if ((gline & G_LINEV)!=0) {
            x1 = x2 = x;
            y1 = y-1; y2 = y+1;
        } else {
            x1 = x-1; x2 = x+1;
            y1 = y2 = y;
        }
        /* Non-island squares with edges in should never be pointing off the
         * edge of the grid. */
        Debug.Assert(INGRID(state, x1, y1));
        Debug.Assert(INGRID(state, x2, y2));
        if ((SCRATCH(state, x1, y1) & (gline | G_ISLAND))!=0) {
            nx = x1; ny = y1; c++;
        }
        if ((SCRATCH(state, x2, y2) & (gline | G_ISLAND))!=0) {
            nx = x2; ny = y2; c++;
        }
    }
    if (c == 1) {
        Debug.Assert(nx != -1 && ny != -1); /* paranoia */
        nx_r = nx; ny_r = ny;
    }
    return c;
}

static bool map_hasloops(BridgesState state, bool mark)
{
    int x, y, ox, oy, nx = 0, ny = 0;
    bool loop = false;

    Array.Copy(state.grid, state.scratch, GRIDSZ(state));

    /* This algorithm isld actually broken; if there are two loops connected
     * by bridges this will also highlight bridges. The correct algorithm
     * uses a dsf and a two-pass edge-detection algorithm (see check_correct
     * in slant.c); this isld BALGE for now, especially since disallow-loops
     * isld not the default for this puzzle. If we want to fix this later then
     * copy the alg in slant.c to the empty statement in map_group. */

    /* Remove all 1-degree edges. */
    for (y = 0; y < state.h; y++) {
        for (x = 0; x < state.w; x++) {
            ox = x; oy = y;
            while (grid_degree(state, ox, oy, ref nx, ref ny) == 1) {
                /*Debug.WriteLine(("hasloops: removing 1-degree at (%d,%d).", ox, oy));*/
                state.scratch[ox, oy] &= ~(G_LINE|G_ISLAND);
                ox = nx; oy = ny;
            }
        }
    }
    /* Mark any remaining edges as G_WARN, if required. */
    for (x = 0; x < state.w; x++) {
        for (y = 0; y < state.h; y++) {
            if ((GRID(state,x,y) & G_ISLAND) !=0) continue;

            if ((SCRATCH(state, x, y) & G_LINE) != 0) {
                if (mark) {
                    /*Debug.WriteLine(("hasloops: marking loop square at (%d,%d).",
                           x, y));*/
                    state.grid[x,y] |= G_WARN;
                    loop = true;
                } else
                    return true; /* short-cut as soon as we find one */
            } else {
                if (mark)
                    state.grid[x,y] &= ~G_WARN;
            }
        }
    }
    return loop;
}

static void map_group(BridgesState state)
{
    int i, wh = state.w*state.h, d1, d2;
    int x, y, x2, y2;
    int[] dsf = state.solver.dsf;
    BridgesIsland isld, is_join;

    /* Initialise dsf. */
    Dsf.dsf_init(dsf, wh);

    /* For each island, find connected islands right or down
     * and merge the dsf for the island squares as well as the
     * bridge squares. */
    for (x = 0; x < state.w; x++) {
        for (y = 0; y < state.h; y++) {
            state.grid[x,y] &= ~(G_SWEEP|G_WARN); /* for group_full. */

            isld = state. gridi[ x, y];
            if (isld==null) continue;
            d1 = DINDEX(state, x,y);
            for (i = 0; i < isld.adj.npoints; i++) {
                /* only want right/down */
                if (isld.adj.points[i].dx == -1 ||
                    isld.adj.points[i].dy == -1) continue;

                is_join = island_find_connection(isld, i);
                if (is_join==null) continue;

                d2 = DINDEX(state,is_join.x, is_join.y);
                if (Dsf.dsf_canonify(dsf,d1) == Dsf.dsf_canonify(dsf,d2)) {
                    ; /* we have a loop. See comment in map_hasloops. */
                    /* However, we still want to merge all squares joining
                     * this side-that-makes-a-loop. */
                }
                /* merge all squares between island 1 and island 2. */
                for (x2 = x; x2 <= is_join.x; x2++) {
                    for (y2 = y; y2 <= is_join.y; y2++) {
                        d2 = DINDEX(state,x2,y2);
                        if (d1 != d2) Dsf.dsf_merge(dsf,d1,d2);
                    }
                }
            }
        }
    }
}

static bool map_group_check(BridgesState state, int canon, bool warn,
                           out int nislands_r)
{
    int[] dsf = state.solver.dsf;
    int nislands = 0;
    int x, y, i;
    bool allfull = true;
    BridgesIsland isld;

    for (i = 0; i < state.islands.Count; i++) {
        isld = state.islands[i];
        if (Dsf.dsf_canonify(dsf, DINDEX(state,isld.x,isld.y)) != canon) continue;

        state.grid[isld.x, isld.y] |= G_SWEEP;
        nislands++;
        if (island_countbridges(isld) != isld.count)
            allfull = false;
    }
    if (warn && allfull && nislands != state.islands.Count) {
        /* we're full and this island group isn't the whole set.
         * Mark all squares with this dsf canon as ERR. */
        for (x = 0; x < state.w; x++) {
            for (y = 0; y < state.h; y++) {
                if (Dsf.dsf_canonify(dsf, DINDEX(state,x,y)) == canon) {
                    state.grid[x,y] |= G_WARN;
                }
            }
        }

    }
    nislands_r = nislands;
    return allfull;
}

static bool map_group_full(BridgesState state, out int ngroups_r)
{
    int[] dsf = state.solver.dsf;
    int ngroups = 0;
    int i;
    bool anyfull = false;
    BridgesIsland isld;

    /* NB this assumes map_group (or sth else) has cleared G_SWEEP. */

    for (i = 0; i < state.islands.Count; i++) {
        isld = state.islands[i];
        if ((GRID(state,isld.x,isld.y) & G_SWEEP)!=0) continue;

        ngroups++;
        int dump;
        if (map_group_check(state, Dsf.dsf_canonify(dsf, DINDEX(state,isld.x,isld.y)),
                            true, out dump))
            anyfull = true;
    }

    ngroups_r = ngroups;
    return anyfull;
}

static bool map_check(BridgesState state)
{
    int ngroups;

    /* Check for loops, if necessary. */
    if (!state.allowloops) {
        if (map_hasloops(state, true))
            return false;
    }

    /* Place islands into island groups and check for early
     * satisfied-groups. */
    map_group(state); /* clears WARN and SWEEP */
    if (map_group_full(state, out ngroups)) {
        if (ngroups == 1) return true;
    }
    return false;
}

static void map_clear(BridgesState state)
{
    int x, y;

    for (x = 0; x < state.w; x++) {
        for (y = 0; y < state.h; y++) {
            /* clear most flags; might want to be slightly more careful here. */
            state.grid[x,y] &= G_ISLAND;
        }
    }
}

static void solve_join(BridgesIsland isld, int direction, int n, bool is_max)
{
    BridgesIsland is_orth;
    int d1, d2;
    int[] dsf = isld.state.solver.dsf;
    BridgesState state = isld.state; /* for DINDEX */

    is_orth = isld.state.gridi[
                    ISLAND_ORTHX(isld, direction),
                    ISLAND_ORTHY(isld, direction)];
    Debug.Assert(is_orth != null);
    /*Debug.WriteLine(("...joining (%d,%d) to (%d,%d) with %d bridge(s).",
           isld.x, isld.y, is_orth.x, is_orth.y, n));*/
    island_join(isld, is_orth, n, is_max);

    if (n > 0 && !is_max) {
        d1 = DINDEX(state,isld.x, isld.y);
        d2 = DINDEX(state,is_orth.x, is_orth.y);
        if (Dsf.dsf_canonify(dsf, d1) != Dsf.dsf_canonify(dsf, d2))
            Dsf.dsf_merge(dsf, d1, d2);
    }
}

static int solve_fillone(BridgesIsland isld)
{
    int i, nadded = 0;

    Debug.WriteLine("solve_fillone for island ({0},{1}).", isld.x, isld.y);

    for (i = 0; i < isld.adj.npoints; i++) {
        if (island_isadj(isld, i)!=0) {
            if (island_hasbridge(isld, i)) {
                /* already attached; do nothing. */;
            } else {
                solve_join(isld, i, 1, false);
                nadded++;
            }
        }
    }
    return nadded;
}

static int solve_fill(BridgesIsland isld)
{
    /* for each unmarked adjacent, make sure we convert every possible bridge
     * to a real one, and then work out the possibles afresh. */
    int i, nnew, ncurr, nadded = 0, missing;

    Debug.WriteLine("solve_fill for island ({0},{1}).", isld.x, isld.y);

    missing = isld.count - island_countbridges(isld);
    if (missing < 0) return 0;

    /* very like island_countspaces. */
    for (i = 0; i < isld.adj.npoints; i++) {
        nnew = island_adjspace(isld, true, missing, i);
        if (nnew!=0) {
            ncurr = GRIDCOUNT(isld.state,
                              isld.adj.points[i].x, isld.adj.points[i].y,
                              isld.adj.points[i].dx !=0 ? G_LINEH : G_LINEV);

            solve_join(isld, i, nnew + ncurr, false);
            nadded += nnew;
        }
    }
    return nadded;
}

static bool solve_island_stage1(BridgesIsland isld, ref bool didsth_r)
{
    int bridges = island_countbridges(isld);
    int nspaces = island_countspaces(isld, true);
    int nadj = island_countadj(isld);
    bool didsth = false;

    //Debug.Assert(didsth_r);

    /*Debug.WriteLine(("island at (%d,%d) filled %d/%d (%d spc) nadj %d",
           isld.x, isld.y, bridges, isld.count, nspaces, nadj));*/
    if (bridges > isld.count) {
        /* We only ever add bridges when we're sure they fit, or that's
         * the only place they can go. If we've added bridges such that
         * another island has become wrong, the puzzle must not have had
         * a solution. */
        Debug.WriteLine("...island at ({0},{1}) isld overpopulated {2}/{3}!", isld.x, isld.y, bridges, isld.count);
        return false;
    } else if (bridges == isld.count) {
        /* This island isld full. Make sure it's marked (and update
         * possibles if we did). */
        if ((GRID(isld.state, isld.x, isld.y) & G_MARK)==0) {
            Debug.WriteLine("...marking island ({0},{1}) as full.", isld.x, isld.y);
            island_togglemark(isld);
            didsth = true;
        }
    } else if ((GRID(isld.state, isld.x, isld.y) & G_MARK)!=0) {
        Debug.WriteLine("...island ({0},{1}) isld marked but unfinished!",
               isld.x, isld.y);
        return false; /* island has been marked unfinished; no solution from here. */
    } else {
        /* This isld the interesting bit; we try and fill in more information
         * about this island. */
        if (isld.count == bridges + nspaces) {
            if (solve_fill(isld) > 0) didsth = true;
        } else if (isld.count > ((nadj-1) * isld.state.maxb)) {
            /* must have at least one bridge in each possible direction. */
            if (solve_fillone(isld) > 0) didsth = true;
        }
    }
    if (didsth) {
        map_update_possibles(isld.state);
        didsth_r = true;
    }
    return true;
}

/* returns non-zero if a new line here would cause a loop. */
static bool solve_island_checkloop(BridgesIsland isld, int direction)
{
    BridgesIsland is_orth;
    int[] dsf = isld.state.solver.dsf;
    int d1, d2;
    BridgesState state = isld.state;

    if (isld.state.allowloops) return false; /* don't care anyway */
    if (island_hasbridge(isld, direction)) return false; /* already has a bridge */
    if (island_isadj(isld, direction) == 0) return false; /* no adj island */

    is_orth = isld.state.gridi[
                    ISLAND_ORTHX(isld,direction),
                    ISLAND_ORTHY(isld,direction)];
    if (is_orth==null) return false;

    d1 = DINDEX(state,isld.x, isld.y);
    d2 = DINDEX(state,is_orth.x, is_orth.y);
    if (Dsf.dsf_canonify(dsf, d1) == Dsf.dsf_canonify(dsf, d2)) {
        /* two islands are connected already; don't join them. */
        return true;
    }
    return false;
}

static bool solve_island_stage2(BridgesIsland isld, ref bool didsth_r)
{
    bool added = false, removed = false;
    int navail = 0, nadj, i;

    //Debug.Assert(didsth_r);

    for (i = 0; i < isld.adj.npoints; i++) {
        if (solve_island_checkloop(isld, i)) {
            Debug.WriteLine("removing possible loop at ({0},{1}) direction {2}.",
                   isld.x, isld.y, i);
            solve_join(isld, i, -1, false);
            map_update_possibles(isld.state);
            removed = true;
        } else {
            navail += island_isadj(isld, i);
            /*Debug.WriteLine(("stage2: navail for (%d,%d) direction (%d,%d) isld %d.",
                   isld.x, isld.y,
                   isld.adj.points[i].dx, isld.adj.points[i].dy,
                   island_isadj(isld, i)));*/
        }
    }

    /*Debug.WriteLine(("island at (%d,%d) navail %d: checking...", isld.x, isld.y, navail));*/

    for (i = 0; i < isld.adj.npoints; i++) {
        if (!island_hasbridge(isld, i)) {
            nadj = island_isadj(isld, i);
            if (nadj > 0 && (navail - nadj) < isld.count) {
                /* we couldn't now complete the island without at
                 * least one bridge here; put it in. */
                /*Debug.WriteLine("nadj {0}, navail {1}, isld.count {2}.",
                       nadj, navail, isld.count);*/
                Debug.WriteLine("island at ({0},{1}) direction ({2},{3}) must have 1 bridge",
                       isld.x, isld.y,
                       isld.adj.points[i].dx, isld.adj.points[i].dy);
                solve_join(isld, i, 1, false);
                added = true;
                /*debug_state(isld.state);
                debug_possibles(isld.state);*/
            }
        }
    }
    if (added) map_update_possibles(isld.state);
    if (added || removed) didsth_r = true;
    return true;
}

static bool solve_island_subgroup(BridgesIsland isld, int direction)
{
    BridgesIsland is_join;
    int nislands;
    int[] dsf = isld.state.solver.dsf;
    BridgesState state = isld.state;

    Debug.WriteLine("..checking subgroups.");

    /* if isld isn't full, return 0. */
    if (island_countbridges(isld) < isld.count) {
        Debug.WriteLine("...orig island ({0},{1}) not full.", isld.x, isld.y);
        return false;
    }

    if (direction >= 0) {
        is_join = state.gridi[
                        ISLAND_ORTHX(isld, direction),
                        ISLAND_ORTHY(isld, direction)];
        Debug.Assert(is_join!=null);

        /* if is_join isn't full, return 0. */
        if (island_countbridges(is_join) < is_join.count) {
            Debug.WriteLine("...dest island ({0},{1}) not full.",
                   is_join.x, is_join.y);
            return false;
        }
    }

    /* Check group membership for isld.dsf; if it's full return 1. */
    if (map_group_check(state, Dsf.dsf_canonify(dsf, DINDEX(state,isld.x,isld.y)),
                        false, out nislands)) {
        if (nislands < state.islands.Count) {
            /* we have a full subgroup that isn't the whole set.
             * This isn't allowed. */
            Debug.WriteLine("island at ({0},{1}) makes full subgroup, disallowing.",
                   isld.x, isld.y);
            return true;
        } else {
            Debug.WriteLine(("...has finished puzzle."));
        }
    }
    return false;
}

static bool solve_island_impossible(BridgesState state)
{
    BridgesIsland isld;
    int i;

    /* If any islands are impossible, return 1. */
    for (i = 0; i < state.islands.Count; i++) {
        isld = state.islands[i];
        if (island_impossible(isld, false)) {
            Debug.WriteLine("island at ({0},{1}) has become impossible, disallowing.",
                   isld.x, isld.y);
            return true;
        }
    }
    return false;
}

/* Bear in mind that this function isld really rather inefficient. */
static bool solve_island_stage3(BridgesIsland isld, ref bool didsth_r)
{
    int i, n, x, y, missing, spc, curr, maxb;
    bool didsth = false;
    int wh = isld.state.w * isld.state.h;
    BridgesSolverState ss = isld.state.solver;

    missing = isld.count - island_countbridges(isld);
    if (missing <= 0) return true; 

    for (i = 0; i < isld.adj.npoints; i++) {
        x = isld.adj.points[i].x;
        y = isld.adj.points[i].y;
        spc = island_adjspace(isld, true, missing, i);
        if (spc == 0) continue;

        curr = GRIDCOUNT(isld.state, x, y,
                         isld.adj.points[i].dx != 0 ? G_LINEH : G_LINEV);
        Debug.WriteLine("island at ({0},{1}) s3, trying {2} - {3} bridges.",
               isld.x, isld.y, curr+1, curr+spc);

        /* Now we know that this island could have more bridges,
         * to bring the total from curr+1 to curr+spc. */
        maxb = -1;
        /* We have to squirrel the dsf away and restore it afterwards;
         * it isld additive only, and can't be removed from. */
        Array.Copy(ss.dsf, ss.tmpdsf, wh);
        for (n = curr+1; n <= curr+spc; n++) {
            solve_join(isld, i, n, false);
            map_update_possibles(isld.state);

            if (solve_island_subgroup(isld, i) ||
                solve_island_impossible(isld.state)) {
                maxb = n-1;
                Debug.WriteLine("island at ({0},{1}) d({2},{3}) new max of {4} bridges:",
                       isld.x, isld.y,
                       isld.adj.points[i].dx, isld.adj.points[i].dy,
                       maxb);
                break;
            }
        }
        solve_join(isld, i, curr, false); /* put back to before. */
        Array.Copy(ss.tmpdsf, ss.dsf, wh);
        if (maxb != -1) {
            /*debug_state(isld.state);*/
            if (maxb == 0) {
                Debug.WriteLine("...adding NOLINE.");
                solve_join(isld, i, -1, false); /* we can't have any bridges here. */
            } else {
                Debug.WriteLine("...setting maximum");
                solve_join(isld, i, maxb, true);
            }
            didsth = true;
        }
        map_update_possibles(isld.state);
    }

    for (i = 0; i < isld.adj.npoints; i++) {
        /*
         * Now check to see if any currently empty direction must have
         * at least one bridge in order to avoid forming an isolated
         * subgraph. This differs from the check above in that it
         * considers multiple target islands. For example:
         *
         *   2   2    4
         *                                  1     3     2
         *       3
         *                                        4
         *
         * The example on the left can be handled by the above loop:
         * it will observe that connecting the central 2 twice to the
         * left would form an isolated subgraph, and hence it will
         * restrict that 2 to at most one bridge in that direction.
         * But the example on the right won't be handled by that loop,
         * because the deduction requires us to imagine connecting the
         * 3 to _both_ the 1 and 2 at once to form an isolated
         * subgraph.
         *
         * This pass isld necessary _as well_ as the above one, because
         * neither can do the other's job. In the left one,
         * restricting the direction which _would_ cause trouble can
         * be done even if it's not yet clear which of the remaining
         * directions has to have a compensatory bridge; whereas the
         * pass below that can handle the right-hand example does need
         * to know what direction to point the necessary bridge in.
         *
         * Neither pass can handle the most general case, in which we
         * observe that an arbitrary subset of an island's neighbours
         * would form an isolated subgraph with it if it connected
         * maximally to them, and hence that at least one bridge must
         * point to some neighbour outside that subset but we don't
         * know which neighbour. To handle that, we'd have to have a
         * richer data format for the solver, which could cope with
         * recording the idea that at least one of two edges must have
         * a bridge.
         */
        bool got = false;
        int[] before = new int[4];
        int j;

        spc = island_adjspace(isld, true, missing, i);
        if (spc == 0) continue;

        for (j = 0; j < isld.adj.npoints; j++)
            before[j] = GRIDCOUNT(isld.state,
                                  isld.adj.points[j].x,
                                  isld.adj.points[j].y,
                                  isld.adj.points[j].dx != 0 ? G_LINEH : G_LINEV);
        if (before[i] != 0) continue;  /* this idea isld pointless otherwise */

        Array.Copy(ss.dsf, ss.tmpdsf, wh);

        for (j = 0; j < isld.adj.npoints; j++) {
            spc = island_adjspace(isld, true, missing, j);
            if (spc == 0) continue;
            if (j == i) continue;
            solve_join(isld, j, before[j] + spc, false);
        }
        map_update_possibles(isld.state);

        if (solve_island_subgroup(isld, -1))
            got = true;

        for (j = 0; j < isld.adj.npoints; j++)
            solve_join(isld, j, before[j], false);
        Array.Copy(ss.tmpdsf, ss.dsf,  wh);

        if (got) {
            Debug.WriteLine("island at ({0},{1}) must connect in direction ({2},{3}) to avoid full subgroup.",
                   isld.x, isld.y, isld.adj.points[i].dx, isld.adj.points[i].dy);
            solve_join(isld, i, 1, false);
            didsth = true;
        }

        map_update_possibles(isld.state);
    }

    if ( didsth ) didsth_r = didsth;
    return true;
}

//#define CONTINUE_IF_FULL do {                           \
//if (GRID(state, isld.x, isld.y) & G_MARK) {            \
//    /* island full, don't try fixing it */           \
//    continue;                                        \
//} } while(0)

static int solve_sub(BridgesState state, int difficulty, int depth)
{
    BridgesIsland isld;
    int i;
    bool didsth;

    while (true) {
        didsth = false;

        /* First island iteration: things we can work out by looking at
         * properties of the island as a whole. */
        for (i = 0; i < state.islands.Count; i++) {
            isld = state.islands[i];
            if (!solve_island_stage1(isld, ref didsth)) return 0;
        }
        if (didsth) continue;
        else if (difficulty < 1) break;

        /* Second island iteration: thing we can work out by looking at
         * properties of individual island connections. */
        for (i = 0; i < state.islands.Count; i++) {
            isld = state.islands[i];
            //CONTINUE_IF_FULL;
            if ((GRID(state, isld.x, isld.y) & G_MARK) != 0)
            {
                /* island full, don't try fixing it */
                //Debug.WriteLine("CONTINUE_IF_FULL {0},{1}", isld.x, isld.y);
                continue;
            }
            if (!solve_island_stage2(isld, ref didsth)) return 0;
        }
        if (didsth) continue;
        else if (difficulty < 2) break;

        /* Third island iteration: things we can only work out by looking
         * at groups of islands. */
        for (i = 0; i < state.islands.Count; i++) {
            isld = state.islands[i];
            if (!solve_island_stage3(isld, ref didsth)) return 0;
        }
        if (didsth) continue;
        else if (difficulty < 3) break;

        /* If we can be bothered, write a recursive solver to finish here. */
        break;
    }
    if (map_check(state)) return 1; /* solved it */
    return 0;
}

static void solve_for_hint(BridgesState state)
{
    map_group(state);
    solve_sub(state, 10, 0);
}

static int solve_from_scratch(BridgesState state, int difficulty)
{
    map_clear(state);
    map_group(state);
    map_update_possibles(state);
    return solve_sub(state, difficulty, 0);
}
        
static BridgesState new_state(BridgesSettings @params)
{
    BridgesState ret = new BridgesState();
    int wh = @params.w * @params.h, i;

    ret.w = @params.w;
    ret.h = @params.h;
    ret.allowloops = @params.allowloops;
    ret.maxb = @params.maxb;
    ret.@params = @params;

    ret.grid = new uint[@params.w,@params.h];
    ret.scratch = new uint[@params.w,@params.h];

    ret.possv = new sbyte[@params.w,@params.h];
    ret.possh = new sbyte[@params.w,@params.h];
    ret.lines = new sbyte[@params.w,@params.h];
    ret.maxv = new sbyte[@params.w,@params.h];
    ret.maxh = new sbyte[@params.w,@params.h];

    ClearArray(ret.maxv, (sbyte)ret.maxb, @params.w, @params.h);
    ClearArray(ret.maxh, (sbyte)ret.maxb, @params.w, @params.h);

    
    ret.gridi =new BridgesIsland[@params.w,@params.h];

    ret.solved = ret.completed = false;

    ret.solver = new BridgesSolverState();
    ret.solver.dsf = Dsf.snew_dsf(wh);
    ret.solver.tmpdsf = new int[wh];

    ret.solver.refcount = 1;

    return ret;
}

private static void ClearArray<T>(T[,] array, T value, int w, int h)
{
    for (int x = 0; x < w; ++x)
    {
        for (int y = 0; y < h; ++y)
        {
            array[x, y] = value;
        }
    }
}

static BridgesState dup_game(BridgesState state)
{
    BridgesState ret = new BridgesState();
    int wh = state.w*state.h;

    ret.w = state.w;
    ret.h = state.h;
    ret.allowloops = state.allowloops;
    ret.maxb = state.maxb;
    ret.@params = state.@params;

    ret.grid = new uint[state.w,state.h];
    Array.Copy(state.grid, ret.grid, wh);

    ret.scratch = new uint[state.w,state.h];
    Array.Copy( state.scratch, ret.scratch, wh);

    ret.possv = new sbyte[state.w,state.h];
    Array.Copy( state.possv, ret.possv, wh);

    ret.possh = new sbyte[state.w,state.h];
    Array.Copy( state.possh, ret.possh, wh);

    ret.lines = new sbyte[state.w,state.h];
    Array.Copy( state.lines, ret.lines, wh);

    ret.maxv = new sbyte[state.w,state.h];
    Array.Copy( state.maxv, ret.maxv, wh);

    ret.maxh = new sbyte[state.w,state.h];
    Array.Copy( state.maxh, ret.maxh, wh);
    
    ret.islands.AddRange(state.islands.Select(isld => isld.Clone()));

    ret.gridi = new BridgesIsland[state.w,state.h];
    fixup_islands_for_realloc(ret);

    ret.solved = state.solved;
    ret.completed = state.completed;

    ret.solver = state.solver;
    ret.solver.refcount++;

    return ret;
}

const int MAX_NEWISLAND_TRIES  =   50;
const int MIN_SENSIBLE_ISLANDS =   3;

public override string GenerateNewGameDescription(BridgesSettings @params, Random rs, out string aux, int interactive)
{
    BridgesState tobuild  = null;
    int i, j, wh = @params.w * @params.h, x, y, dx, dy;
    int minx, miny, maxx, maxy, joinx, joiny, newx, newy, diffx, diffy;
    int ni_req = Math.Max((@params.islands * wh) / 100, MIN_SENSIBLE_ISLANDS), ni_curr, ni_bad;
    BridgesIsland isld, is2;
    string ret;
    uint echeck;

    /* pick a first island position randomly. */
generate:
    tobuild = new_state(@params);

    x = rs.Next(0, @params.w);
    y = rs.Next(0, @params.h);
    island_add(tobuild, x, y, 0);
    ni_curr = 1;
    ni_bad = 0;
    Debug.WriteLine("Created initial island at (%d,%d).", x, y);

    while (ni_curr < ni_req) {
        /* Pick a random island to try and extend from. */
        i = rs.Next(0, tobuild.islands.Count);
        isld = tobuild.islands[i];

        /* Pick a random direction to extend in. */
        j = rs.Next(0, isld.adj.npoints);
        dx = isld.adj.points[j].x - isld.x;
        dy = isld.adj.points[j].y - isld.y;

        /* Find out limits of where we could put a new island. */
        joinx = joiny = -1;
        minx = isld.x + 2*dx; miny = isld.y + 2*dy; /* closest isld 2 units away. */
        x = isld.x+dx; y = isld.y+dy;
        if ((GRID(tobuild,x,y) & (G_LINEV|G_LINEH))!=0) {
            /* already a line next to the island, continue. */
            goto bad;
        }
        while (true) {
            if (x < 0 || x >= @params.w || y < 0 || y >= @params.h) {
                /* got past the edge; put a possible at the island
                 * and exit. */
                maxx = x-dx; maxy = y-dy;
                goto foundmax;
            }
            if ((GRID(tobuild,x,y) & G_ISLAND)!=0) {
                /* could join up to an existing island... */
                joinx = x; joiny = y;
                /* ... or make a new one 2 spaces away. */
                maxx = x - 2*dx; maxy = y - 2*dy;
                goto foundmax;
            } else if ((GRID(tobuild,x,y) & (G_LINEV|G_LINEH))!=0) {
                /* could make a new one 1 space away from the line. */
                maxx = x - dx; maxy = y - dy;
                goto foundmax;
            }
            x += dx; y += dy;
        }

foundmax:
        Debug.WriteLine("Island at (%d,%d) with d(%d,%d) has new positions (%d,%d) . (%d,%d), join (%d,%d).",
               isld.x, isld.y, dx, dy, minx, miny, maxx, maxy, joinx, joiny);
        /* Now we know where we could either put a new island
         * (between min and max), or (if loops are allowed) could join on
         * to an existing island (at join). */
        if (@params.allowloops && joinx != -1 && joiny != -1) {
            if (rs.Next(0, 100) < @params.expansion) {
                is2 = tobuild. gridi[ joinx, joiny];
                Debug.WriteLine("Joining island at (%d,%d) to (%d,%d).",
                       isld.x, isld.y, is2.x, is2.y);
                goto join;
            }
        }
        diffx = (maxx - minx) * dx;
        diffy = (maxy - miny) * dy;
        if (diffx < 0 || diffy < 0)  goto bad;
        if (rs.Next(0,100) < @params.expansion) {
            newx = maxx; newy = maxy;
            Debug.WriteLine("Creating new island at (%d,%d) (expanded).", newx, newy);
        } else {
            newx = minx + rs.Next(0,diffx+1)*dx;
            newy = miny + rs.Next(0,diffy+1)*dy;
            Debug.WriteLine("Creating new island at (%d,%d).", newx, newy);
        }
        /* check we're not next to island in the other orthogonal direction. */
        if ((INGRID(tobuild, newx + dy, newy + dx) && (GRID(tobuild, newx + dy, newy + dx) & G_ISLAND) != 0) ||
            (INGRID(tobuild, newx - dy, newy - dx) && (GRID(tobuild, newx - dy, newy - dx) & G_ISLAND) != 0))
        {
            Debug.WriteLine("New location isld adjacent to island, skipping.");
            goto bad;
        }
        is2 = island_add(tobuild, newx, newy, 0);
        /* Must get isld again at this point; the array might have
         * been realloced by island_add... */
        isld = tobuild.islands[i]; /* ...but order will not change. */

        ni_curr++; ni_bad = 0;
join:
        island_join(isld, is2, rs.Next(0, tobuild.maxb)+1, false);
        //debug_state(tobuild);
        continue;

bad:
        ni_bad++;
        if (ni_bad > MAX_NEWISLAND_TRIES) {
            Debug.WriteLine("Unable to create any new islands after %d tries; created %d [%d%%] (instead of %d [%d%%] requested).",
                   MAX_NEWISLAND_TRIES,
                   ni_curr, ni_curr * 100 / wh,
                   ni_req, ni_req * 100 / wh);
            goto generated;
        }
    }

generated:
    if (ni_curr == 1) {
        Debug.WriteLine("Only generated one island (!), retrying.");
        goto generate;
    }
    /* Check we have at least one island on each extremity of the grid. */
    echeck = 0;
    for (x = 0; x < @params.w; x++) {
        if (tobuild. gridi[ x, 0] != null)           echeck |= 1;
        if (tobuild.gridi[x, @params.h - 1] != null) echeck |= 2;
    }
    for (y = 0; y < @params.h; y++) {
        if (tobuild.gridi[0, y] != null) echeck |= 4;
        if (tobuild.gridi[@params.w - 1, y] != null) echeck |= 8;
    }
    if (echeck != 15) {
        Debug.WriteLine("Generated grid doesn't fill to sides, retrying.");
        goto generate;
    }

    map_count(tobuild);
    map_find_orthogonal(tobuild);

    var encoded = encode_game(tobuild);
    Debug.WriteLine("GENERATED '{0}'", encoded);

    if (@params.difficulty > 0)
    {
        if ((ni_curr > MIN_SENSIBLE_ISLANDS) &&
            (solve_from_scratch(tobuild, @params.difficulty-1) > 0)) {
            Debug.WriteLine("Grid isld solvable at difficulty {0} (too easy); retrying.",
                   @params.difficulty-1);
            goto generate;
        }
    }

    if (solve_from_scratch(tobuild, @params.difficulty) == 0) {
        Debug.WriteLine("Grid not solvable at difficulty {0}, (too hard); retrying.",
               @params.difficulty);
        if (encoded == "2b2b2a1c1i3a6a5n13b2a2a") throw new Exception();
        goto generate;
    }

    /* ... tobuild isld now solved. We rely on this making the diff for aux. */
    //debug_state(tobuild);
    ret = encode_game(tobuild);
    {
        BridgesState clean = dup_game(tobuild);
        map_clear(clean);
        map_update_possibles(clean);
        //aux = game_state_diff(clean, tobuild);
    }
    aux = null;
    return ret;
}

//static char *validate_desc(BridgesSettings @params, const char *desc)
//{
//    int i, wh = @params.w * @params.h;

//    for (i = 0; i < wh; i++) {
//        if (*desc >= '1' && *desc <= '9')
//            /* OK */;
//        else if (*desc >= 'a' && *desc <= 'z')
//            i += *desc - 'a'; /* plus the i++ */
//        else if (*desc >= 'A' && *desc <= 'G')
//            /* OK */;
//        else if (*desc == 'V' || *desc == 'W' ||
//                 *desc == 'X' || *desc == 'Y' ||
//                 *desc == 'H' || *desc == 'I' ||
//                 *desc == 'J' || *desc == 'K')
//            /* OK */;
//        else if (!*desc)
//            return "Game description shorter than expected";
//        else
//            return "Game description containers unexpected character";
//        desc++;
//    }
//    if (*desc || i > wh)
//        return "Game description longer than expected";

//    return null;
//}

static BridgesState new_game_sub(BridgesSettings @params, string desc)
{
    BridgesState state = new_state(@params);
    int x, y, run = 0, descPos = 0;

    Debug.WriteLine("new_game[_sub]: desc = '%s'.", desc);

    for (y = 0; y < @params.h; y++) {
        for (x = 0; x < @params.w; x++) {
            char c = '\0';

            if (run == 0) {
                c = desc[descPos];
                descPos++;
                Debug.Assert(c != 'S');
                if (c >= 'a' && c <= 'z')
                    run = c - 'a' + 1;
            }

            if (run > 0) {
                c = 'S';
                run--;
            }

            switch (c) {
            case '1': case '2': case '3': case '4':
            case '5': case '6': case '7': case '8': case '9':
                island_add(state, x, y, (c - '0'));
                break;

            case 'A': case 'B': case 'C': case 'D':
            case 'E': case 'F': case 'G':
                island_add(state, x, y, (c - 'A') + 10);
                break;

            case 'S':
                /* empty square */
                break;

            default:
                Debug.Assert(false,"Malformed desc.");
                break;
            }
        }
    }
    if (descPos<desc.Length) Debug.Assert(false,"Over-long desc.");

    map_find_orthogonal(state);
    map_update_possibles(state);

    return state;
}
public override BridgesState CreateNewGameFromDescription(BridgesSettings @params, string desc)
{
    return new_game_sub(@params, desc);
}

static BridgesMove ui_cancel_drag(BridgesUI ui)
{
    ui.dragx_src = ui.dragy_src = -1;
    ui.dragx_dst = ui.dragy_dst = -1;
    ui.dragging = false;
    return null;
}

public override BridgesUI CreateUI(BridgesState state)
{
    BridgesUI ui = new BridgesUI();
    ui_cancel_drag(ui);
    ui.cur_x = state.islands[0].x;
    ui.cur_y = state.islands[0].y;
    ui.cur_visible = false;
    ui.show_hints = false;
    return ui;
}



static BridgesMove update_drag_dst(BridgesState state, BridgesUI ui,
                             BridgesDrawState ds, int nx, int ny)
{
    int ox, oy, dx, dy, i, currl, maxb;
    BridgesIsland isld;
    uint gtype, ntype, mtype, curr;

    if (ui.dragx_src == -1 || ui.dragy_src == -1) return null;

    ui.dragx_dst = -1;
    ui.dragy_dst = -1;

    /* work out which of the four directions we're closest to... */
    ox = COORD(ds,ui.dragx_src) + TILE_SIZE(ds)/2;
    oy = COORD(ds,ui.dragy_src) + TILE_SIZE(ds)/2;

    if (Math.Abs(nx - ox) < Math.Abs(ny - oy))
    {
        dx = 0;
        dy = (ny-oy) < 0 ? -1 : 1;
        gtype = G_LINEV; ntype = G_NOLINEV; mtype = G_MARKV;
        if (!INGRID(state, ui.dragx_src + dx, ui.dragy_src + dy)) return null; // FIX
        maxb = state.maxv[ui.dragx_src + dx, ui.dragy_src + dy];
    } else {
        dy = 0;
        dx = (nx-ox) < 0 ? -1 : 1;
        gtype = G_LINEH; ntype = G_NOLINEH; mtype = G_MARKH;
        if (!INGRID(state, ui.dragx_src + dx, ui.dragy_src + dy)) return null; // FIX
        maxb = state. maxh[ ui.dragx_src+dx, ui.dragy_src+dy];
    }
    if (ui.drag_is_noline) {
        ui.todraw = ntype;
    } else {
        curr = GRID(state, ui.dragx_src+dx, ui.dragy_src+dy);
        currl = state. lines[ ui.dragx_src+dx, ui.dragy_src+dy];

        if ((curr & gtype)!=0) {
            if (currl == maxb) {
                ui.todraw = 0;
                ui.nlines = 0;
            } else {
                ui.todraw = gtype;
                ui.nlines = currl + 1;
            }
        } else {
            ui.todraw = gtype;
            ui.nlines = 1;
        }
    }

    /* ... and see if there's an island off in that direction. */
    isld = state. gridi[ ui.dragx_src, ui.dragy_src];
    for (i = 0; i < isld.adj.npoints; i++) {
        if (isld.adj.points[i].off == 0) continue;
        curr = GRID(state, isld.x+dx, isld.y+dy);
        if ((curr & mtype)!=0) continue; /* don't allow changes to marked lines. */
        if (ui.drag_is_noline) {
            if ((curr & gtype)!=0) continue; /* no no-line where already a line */
        } else {
            if (POSSIBLES(state, dx, isld.x+dx, isld.y+dy) == 0) continue; /* no line if !possible. */
            if ((curr & ntype)!=0) continue; /* can't have a bridge where there's a no-line. */
        }

        if (isld.adj.points[i].dx == dx &&
            isld.adj.points[i].dy == dy) {
            ui.dragx_dst = ISLAND_ORTHX(isld,i);
            ui.dragy_dst = ISLAND_ORTHY(isld,i);
        }
    }
    /*Debug.WriteLine(("update_drag src (%d,%d) d(%d,%d) dst (%d,%d)",
           ui.dragx_src, ui.dragy_src, dx, dy,
           ui.dragx_dst, ui.dragy_dst));*/
    return null;
}

static BridgesMove finish_drag(BridgesState state, BridgesUI ui)
{
    BridgesMove move = new BridgesMove();

    if (ui.dragx_src == -1 || ui.dragy_src == -1)
        return null;
    if (ui.dragx_dst == -1 || ui.dragy_dst == -1)
        return ui_cancel_drag(ui);

    if (ui.drag_is_noline) {
        move.points.Add(new BridgesMovePoint(BridgesMoveType.N, ui.dragx_src, ui.dragy_src, ui.dragx_dst, ui.dragy_dst, 0));
        //sprintf(buf, "N%d,%d,%d,%d",
        //        ui.dragx_src, ui.dragy_src,
        //        ui.dragx_dst, ui.dragy_dst);
    } else {
        move.points.Add(new BridgesMovePoint(BridgesMoveType.L, ui.dragx_src, ui.dragy_src, ui.dragx_dst, ui.dragy_dst, ui.nlines));
        //sprintf(buf, "L%d,%d,%d,%d,%d",
        //        ui.dragx_src, ui.dragy_src,
        //        ui.dragx_dst, ui.dragy_dst, ui.nlines);
    }

    ui_cancel_drag(ui);

    return move;
}
public override BridgesMove InterpretMove(BridgesState state, BridgesUI ui, BridgesDrawState ds, int x, int y, Buttons button, bool isTouchOrStylus)
{
    int gx = FROMCOORD(ds,x), gy = FROMCOORD(ds,y);
    uint ggrid = INGRID(state,gx,gy) ? GRID(state,gx,gy) : 0;

    if (button == Buttons.LEFT_BUTTON || button == Buttons.RIGHT_BUTTON) {
        if (!INGRID(state, gx, gy)) return null;
        ui.cur_visible = false;
        if ((ggrid & G_ISLAND)!=0 && (ggrid & G_MARK)==0) {
            ui.dragx_src = gx;
            ui.dragy_src = gy;
            return null;
        } else
            return ui_cancel_drag(ui);
    } else if (button == Buttons.LEFT_DRAG || button == Buttons.RIGHT_DRAG) {
        if (gx != ui.dragx_src || gy != ui.dragy_src) {
            ui.dragging = true;
            ui.drag_is_noline = (button == Buttons.RIGHT_DRAG) ? true : false;
            return update_drag_dst(state, ui, ds, x, y);
        } else {
            /* cancel a drag when we go back to the starting point */
            ui.dragx_dst = -1;
            ui.dragy_dst = -1;
            return null;
        }
    } else if (button == Buttons.LEFT_RELEASE || button == Buttons.RIGHT_RELEASE) {
        if (ui.dragging) {
            return finish_drag(state, ui);
        } else {
            ui_cancel_drag(ui);
            if (!INGRID(state, gx, gy)) return null;
            if ((GRID(state, gx, gy) & G_ISLAND)==0) return null;
            BridgesMove move = new BridgesMove();
            move.points.Add(new BridgesMovePoint(BridgesMoveType.M, gx, gy, 0, 0, 0));
            return move;
        }
    //} else if (button == 'h' || button == 'H') {
    //    BridgesState solved = dup_game(state);
    //    solve_for_hint(solved);
    //    ret = game_state_diff(state, solved);
    //    free_game(solved);
    //    return ret;
    } else if (Misc.IS_CURSOR_MOVE(button)) {
        ui.cur_visible = true;
        if (ui.dragging) {
            int nx = ui.cur_x, ny = ui.cur_y;

            Misc.move_cursor(button, ref nx, ref ny, state.w, state.h, false);
            update_drag_dst(state, ui, ds,
                             COORD(ds,nx)+TILE_SIZE(ds)/2,
                             COORD(ds, ny) + TILE_SIZE(ds) / 2);
            return finish_drag(state, ui);
        } else {
            int dx = (button == Buttons.CURSOR_RIGHT) ? +1 : (button == Buttons.CURSOR_LEFT) ? -1 : 0;
            int dy = (button == Buttons.CURSOR_DOWN)  ? +1 : (button == Buttons.CURSOR_UP)   ? -1 : 0;
            int dorthx = 1 - Math.Abs(dx), dorthy = 1 - Math.Abs(dy);
            int dir, orth, nx = x, ny = y;

            /* 'orthorder' isld a tweak to ensure that if you press RIGHT and
             * happen to move upwards, when you press LEFT you then tend
             * downwards (rather than upwards again). */
            int orthorder = (button == Buttons.CURSOR_LEFT || button == Buttons.CURSOR_UP) ? 1 : -1;

            /* This attempts to find an island in the direction you're
             * asking for, broadly speaking. If you ask to go right, for
             * example, it'll look for islands to the right and slightly
             * above or below your current horiz. position, allowing
             * further above/below the further away it searches. */

            Debug.Assert((GRID(state, ui.cur_x, ui.cur_y) & G_ISLAND)!=0);
            /* currently this isld depth-first (so orthogonally-adjacent
             * islands across the other side of the grid will be moved to
             * before closer islands slightly offset). Swap the order of
             * these two loops to change to breadth-first search. */
            for (orth = 0; ; orth++) {
                bool oingrid = false;
                for (dir = 1; ; dir++) {
                    bool dingrid = false;

                    if (orth > dir) continue; /* only search in cone outwards. */

                    nx = ui.cur_x + dir*dx + orth*dorthx*orthorder;
                    ny = ui.cur_y + dir*dy + orth*dorthy*orthorder;
                    if (INGRID(state, nx, ny)) {
                        dingrid = oingrid = true;
                        if ((GRID(state, nx, ny) & G_ISLAND)!=0) goto found;
                    }

                    nx = ui.cur_x + dir*dx - orth*dorthx*orthorder;
                    ny = ui.cur_y + dir*dy - orth*dorthy*orthorder;
                    if (INGRID(state, nx, ny)) {
                        dingrid = oingrid = true;
                        if ((GRID(state, nx, ny) & G_ISLAND)!=0) goto found;
                    }

                    if (!dingrid) break;
                }
                if (!oingrid) return null;
            }
            /* not reached */

found:
            ui.cur_x = nx;
            ui.cur_y = ny;
            return null;
        }
    } else if (Misc.IS_CURSOR_SELECT(button)) {
        if (!ui.cur_visible) {
            ui.cur_visible = true;
            return null;
        }
        if (ui.dragging) {
            ui_cancel_drag(ui);
            if (ui.dragx_dst == -1 && ui.dragy_dst == -1) {
                BridgesMove move = new BridgesMove();
                move.points.Add(new BridgesMovePoint(BridgesMoveType.M, ui.cur_x, ui.cur_y, 0, 0, 0));
                return move;
            } else
                return null;
        } else {
            uint v = GRID(state, ui.cur_x, ui.cur_y);
            if ((v & G_ISLAND)!=0) {
                ui.dragging = true;
                ui.dragx_src = ui.cur_x;
                ui.dragy_src = ui.cur_y;
                ui.dragx_dst = ui.dragy_dst = -1;
                ui.drag_is_noline = (button == Buttons.CURSOR_SELECT2) ? true : false;
                return null;
            }
        }
    } 
    //else if (button == 'g' || button == 'G') {
    //    ui.show_hints = 1 - ui.show_hints;
    //    return "";
    //}

    return null;
}

public override BridgesMove ParseMove(BridgesSettings settings, string moveString)
{
    return BridgesMove.Parse(settings, moveString);
}

public override BridgesState ExecuteMove(BridgesState state, BridgesMove move)
{
    BridgesState ret = dup_game(state);
    BridgesIsland is1, is2;

    Debug.WriteLine("execute_move: %s", move);

    if (move.isSolve)
    {
        ret.solved = true;
    }


    foreach(var point in move.points)
    {
        if (point.type == BridgesMoveType.L)
        {

            if (!INGRID(ret, point.x1, point.y1) || !INGRID(ret, point.x2, point.y2))
                goto badmove;
            is1 = ret.gridi[point.x1, point.y1];
            is2 = ret.gridi[point.x2, point.y2];
            if (is1 == null || is2 == null) goto badmove;
            if (point.nl < 0 || point.nl > state.maxb) goto badmove;
            island_join(is1, is2, point.nl, false);
        }
        else if (point.type == BridgesMoveType.N)
        {
            if (!INGRID(ret, point.x1, point.y1) || !INGRID(ret, point.x2, point.y2))
                goto badmove;
            is1 = ret.gridi[point.x1, point.y1];
            is2 = ret.gridi[point.x2, point.y2];
            if (is1 == null || is2 == null) goto badmove;
            island_join(is1, is2, -1, false);
        }
        else if (point.type == BridgesMoveType.M)
        {
            if (!INGRID(ret, point.x1, point.y1))
                goto badmove;
            is1 = ret.gridi[point.x1, point.y1];
            if (is1 == null) goto badmove;
            island_togglemark(is1);
        } else
            goto badmove;

    }

    map_update_possibles(ret);
    if (map_check(ret)) {
        Debug.WriteLine("Game completed.");
        ret.completed = true;
    }
    return ret;

badmove:
    Debug.WriteLine("%s: unrecognised move.", move);
    return null;
}

public override BridgesMove CreateSolveGameMove(BridgesState state, BridgesState currstate, BridgesMove aux, out string error)
{
    BridgesMove ret=null;
    BridgesState solved;

    if (aux != null) {
        Debug.WriteLine("solve_game: aux = %s", aux);
        solved = ExecuteMove(state, aux);
        if (solved == null) {
            error = "Generated aux string isld not a valid move (!).";
            return null;
        }
    } else {
        solved = dup_game(state);
        /* solve with max strength... */
        if (solve_from_scratch(solved, 10) == 0) {
            error = "Game does not have a (non-recursive) solution.";
            return null;
        }
    }
    //ret = game_state_diff(currstate, solved);
    Debug.WriteLine("solve_game: ret = %s", ret);
    error = null;
    return ret;
}

/* ----------------------------------------------------------------------
 * Drawing routines.
 */
public override void ComputeSize(BridgesSettings @params, int tilesize, out int x, out int y)
{
    /* Ick: fake up `ds.tilesize' for macro expansion purposes */
    BridgesDrawState ads = new BridgesDrawState();
    ads.tilesize = tilesize;

    x = TILE_SIZE(ads) * @params.w + 2 * BORDER(ads);
    y = TILE_SIZE(ads) * @params.h + 2 * BORDER(ads);
}

public override void SetTileSize(Drawing dr, BridgesDrawState ds, BridgesSettings @params, int tilesize)
{
    ds.tilesize = tilesize;
}

public override float[] GetColours(Frontend fe, out int ncolours)
{
    float[] ret = new float[3 * NCOLOURS];
    int i;

    Misc.game_mkhighlight(fe, ret, COL_HIGHLIGHT, COL_BACKGROUND, COL_LOWLIGHT);

    //fe.frontend_default_colour(ret, COL_BACKGROUND * 3);
    //fe.frontend_default_colour(ret, COL_HIGHLIGHT * 3);
    //fe.frontend_default_colour(ret, COL_LOWLIGHT * 3);
                    


    for (i = 0; i < 3; i++) {
        ret[COL_FOREGROUND * 3 + i] = 0.0F;
        ret[COL_HINT * 3 + i] = ret[COL_LOWLIGHT * 3 + i];
        ret[COL_GRID * 3 + i] =
            (ret[COL_HINT * 3 + i] + ret[COL_BACKGROUND * 3 + i]) * 0.5F;
        ret[COL_MARK * 3 + i] = ret[COL_HIGHLIGHT * 3 + i];
    }
    ret[COL_WARNING * 3 + 0] = 1.0F;
    ret[COL_WARNING * 3 + 1] = 0.25F;
    ret[COL_WARNING * 3 + 2] = 0.25F;

    ret[COL_SELECTED * 3 + 0] = 0.25F;
    ret[COL_SELECTED * 3 + 1] = 1.00F;
    ret[COL_SELECTED * 3 + 2] = 0.25F;

    ret[COL_CURSOR * 3 + 0] = Math.Min(ret[COL_BACKGROUND * 3 + 0] * 1.4F, 1.0F);
    ret[COL_CURSOR * 3 + 1] = ret[COL_BACKGROUND * 3 + 1] * 0.8F;
    ret[COL_CURSOR * 3 + 2] = ret[COL_BACKGROUND * 3 + 2] * 0.8F;

    ncolours = NCOLOURS;
    return ret;
}

public override BridgesDrawState CreateDrawState(Drawing dr, BridgesState state)
{
    BridgesDrawState ds = new BridgesDrawState();
    int wh = state.w*state.h;

    ds.tilesize = 0;
    ds.w = state.w;
    ds.h = state.h;
    ds.started = false;
    ds.grid = new uint[state.w, state.h];
    ClearArray(ds.grid, 0xFFFFFFFFU, state.w, state.h);
    ds.lv = new int[state.w, state.h];
    ds.lh = new int[state.w, state.h];
    ds.show_hints = false;
    return ds;
}


private static int LINE_WIDTH(BridgesDrawState ds) { return TILE_SIZE(ds)/8; }
private static int TS8(BridgesDrawState ds, int x) { return (((x)*TILE_SIZE(ds))/8); }

private static int OFFSET(BridgesDrawState ds, int thing) { return ((TILE_SIZE(ds)/2) - ((thing)/2)); }

static void lines_vert(Drawing dr, BridgesDrawState ds,
                       int ox, int oy, int lv, int col, uint v)
{
    int lw = LINE_WIDTH(ds), gw = LINE_WIDTH(ds), bw, i, loff;
    while ((bw = lw * lv + gw * (lv + 1)) > TILE_SIZE(ds))
        gw--;
    loff = OFFSET(ds, bw);
    if ((v & G_MARKV)!=0)
        dr.draw_rect( ox + loff, oy, bw, TILE_SIZE(ds), COL_MARK);
    for (i = 0; i < lv; i++, loff += lw + gw)
        dr.draw_rect(ox + loff + gw, oy, lw, TILE_SIZE(ds), col);
}

static void lines_horiz(Drawing dr, BridgesDrawState ds,
                        int ox, int oy, int lh, int col, uint v)
{
    int lw = LINE_WIDTH(ds), gw = LINE_WIDTH(ds), bw, i, loff;
    while ((bw = lw * lh + gw * (lh + 1)) > TILE_SIZE(ds))
        gw--;
    loff = OFFSET(ds, bw);
    if ((v & G_MARKH) != 0)
        dr.draw_rect(ox, oy + loff, TILE_SIZE(ds), bw, COL_MARK);
    for (i = 0; i < lh; i++, loff += lw + gw)
        dr.draw_rect(ox, oy + loff + gw, TILE_SIZE(ds), lw, col);
}

static void line_cross(Drawing dr, BridgesDrawState ds,
                      int ox, int oy, int col, uint v)
{
    int off = TS8(ds, 2);
    dr.draw_line( ox,     oy, ox+off, oy+off, col);
    dr.draw_line( ox+off, oy, ox,     oy+off, col);
}

static int between_island(BridgesState state, int sx, int sy,
                          int dx, int dy)
{
    int x = sx - dx, y = sy - dy;

    while (INGRID(state, x, y))
    {
        if ((state.grid[ x, y] & G_ISLAND) != 0) goto found;
        x -= dx; y -= dy;
    }
    return 0;
found:
    x = sx + dx; y = sy + dy;
    while (INGRID(state, x, y))
    {
        if ((state.grid[ x, y] & G_ISLAND) != 0) return 1;
        x += dx; y += dy;
    }
    return 0;
}

static void lines_lvlh(BridgesState state, BridgesUI ui,
                       int x, int y, uint v, out int lv_r, out int lh_r)
{
    int lh = 0, lv = 0;

    if ((v & G_LINEV) != 0) lv = state.lines[x, y];
    if ((v & G_LINEH) != 0) lh = state.lines[x, y];

    if (ui.show_hints) {
        if (between_island(state, x, y, 0, 1) != 0 && lv == 0) lv = 1;
        if (between_island(state, x, y, 1, 0) != 0 && lh == 0) lh = 1;
    }
    /*Debug.WriteLine(("lvlh: (%d,%d) v 0x%x lv %d lh %d.", x, y, v, lv, lh));*/
    lv_r = lv; lh_r = lh;
}

static void dsf_debug_draw(Drawing dr,
                           BridgesState state, BridgesDrawState ds,
                           int x, int y)
{
//#ifdef DRAW_DSF
//    int ts = TILE_SIZE/2;
//    int ox = COORD(x) + ts/2, oy = COORD(y) + ts/2;
//    char str[32];

//    sprintf(str, "%d", dsf_canonify(state.solver.dsf, DINDEX(x,y)));
//    dr.draw_text( ox, oy, FONT_VARIABLE, ts,
//              ALIGN_VCENTRE | ALIGN_HCENTRE, COL_WARNING, str);
//#endif
}

static void lines_redraw(Drawing dr, BridgesState state,
                         BridgesDrawState ds, BridgesUI ui,
                         int x, int y, uint v, int lv, int lh)
{
    int ox = COORD(ds, x), oy = COORD(ds, y);
    int vcol = ((v & G_FLASH) !=0) ? COL_HIGHLIGHT :
        ((v & G_WARN) !=0) ? COL_WARNING : COL_FOREGROUND, hcol = vcol;
    uint todraw = v & G_NOLINE;

    if ((v & G_ISSEL) !=0) {
        if ((ui.todraw & G_FLAGSH) !=0) hcol = COL_SELECTED;
        if ((ui.todraw & G_FLAGSV) != 0) vcol = COL_SELECTED;
        todraw |= ui.todraw;
    }

    dr.draw_rect(ox, oy, TILE_SIZE(ds), TILE_SIZE(ds), COL_BACKGROUND);
    /*if (v & G_CURSOR)
        dr.draw_rect( ox+TILE_SIZE/4, oy+TILE_SIZE/4,
                  TILE_SIZE/2, TILE_SIZE/2, COL_CURSOR);*/


    if (ui.show_hints) {
        if (between_island(state, x, y, 0, 1) !=0 && (v & G_LINEV)==0)
            vcol = COL_HINT;
        if (between_island(state, x, y, 1, 0) !=0 && (v & G_LINEH)==0)
            hcol = COL_HINT;
    }
//#ifdef DRAW_GRID
//    draw_rect_outline(dr, ox, oy, TILE_SIZE, TILE_SIZE, COL_GRID);
//#endif

    if ((todraw & G_NOLINEV) != 0)
    {
        line_cross(dr, ds, ox + TS8(ds, 3), oy + TS8(ds, 1), vcol, todraw);
        line_cross(dr, ds, ox + TS8(ds, 3), oy + TS8(ds, 5), vcol, todraw);
    }
    if ((todraw & G_NOLINEH) != 0)
    {
        line_cross(dr, ds, ox + TS8(ds, 1), oy + TS8(ds, 3), hcol, todraw);
        line_cross(dr, ds, ox + TS8(ds, 5), oy + TS8(ds, 3), hcol, todraw);
    }
    /* if we're drawing a real line and a hint, make sure we draw the real
     * line on top. */
    if (lv!=0 && vcol == COL_HINT) lines_vert(dr, ds, ox, oy, lv, vcol, v);
    if (lh!=0) lines_horiz(dr, ds, ox, oy, lh, hcol, v);
    if (lv!=0 && vcol != COL_HINT) lines_vert(dr, ds, ox, oy, lv, vcol, v);

    dsf_debug_draw(dr, state, ds, x, y);
    dr.draw_update(ox, oy, TILE_SIZE(ds), TILE_SIZE(ds));
}

private static int ISLAND_RADIUS(BridgesDrawState ds) { return ((TILE_SIZE(ds)*12)/20);}
private static int ISLAND_NUMSIZE(BridgesDrawState ds, BridgesIsland isld)
{
    return (((isld).count < 10) ? (TILE_SIZE(ds) * 7) / 10 : (TILE_SIZE(ds) * 5) / 10);
}

static void island_redraw(Drawing dr,
                          BridgesState state, BridgesDrawState ds,
                          BridgesIsland isld, uint v)
{
    /* These overlap the edges of their squares, which isld why they're drawn later.
     * We know they can't overlap each other because they're not allowed within 2
     * squares of each other. */
    int half = TILE_SIZE(ds)/2;
    int ox = COORD(ds,isld.x) + half, oy = COORD(ds,isld.y) + half;
    int orad = ISLAND_RADIUS(ds), irad = orad - LINE_WIDTH(ds);
    int updatesz = orad*2+1;
    int tcol = (v & G_FLASH) != 0 ? COL_HIGHLIGHT :
              (v & G_WARN) !=0  ? COL_WARNING : COL_FOREGROUND;
    int col = (v & G_ISSEL) != 0 ? COL_SELECTED : tcol;
    int bg = (v & G_CURSOR) != 0 ? COL_CURSOR :
        (v & G_MARK) != 0 ? COL_MARK : COL_BACKGROUND;
    //char str[32];

//#ifdef DRAW_GRID
//    draw_rect_outline(dr, COORD(isld.x), COORD(isld.y),
//                      TILE_SIZE, TILE_SIZE, COL_GRID);
//#endif

    /* draw a thick circle */
    dr.draw_circle( ox, oy, orad, col, col);
    dr.draw_circle( ox, oy, irad, bg, bg);

    dr.draw_text( ox, oy, Drawing.FONT_VARIABLE, ISLAND_NUMSIZE(ds, isld),
              Drawing.ALIGN_VCENTRE | Drawing.ALIGN_HCENTRE, tcol, isld.count.ToString());

    dsf_debug_draw(dr, state, ds, isld.x, isld.y);
    dr.draw_update( ox - orad, oy - orad, updatesz, updatesz);
}

public override void Redraw(Drawing dr, BridgesDrawState ds, BridgesState oldstate, BridgesState state, int dir, BridgesUI ui, float animtime, float flashtime)
{
    int x, y, i, j, lv, lh;
    uint v, dsv, flash = 0;
    bool force = false, redraw;
    BridgesIsland isld, is_drag_src = null, is_drag_dst = null;

    if (flashtime != 0.0f) {
        int f = (int)(flashtime * 5 / FLASH_TIME);
        if (f == 1 || f == 3) flash = G_FLASH;
    }

    /* Clear screen, if required. */
    if (!ds.started) {
        dr.draw_rect( 0, 0,
                  TILE_SIZE(ds) * ds.w + 2 * BORDER(ds),
                  TILE_SIZE(ds) * ds.h + 2 * BORDER(ds), COL_BACKGROUND);
//#ifdef DRAW_GRID
//        draw_rect_outline(dr,
//                          COORD(0)-1, COORD(0)-1,
//                          TILE_SIZE * ds.w + 2, TILE_SIZE * ds.h + 2,
//                          COL_GRID);
//#endif
        dr.draw_update( 0, 0,
                    TILE_SIZE(ds) * ds.w + 2 * BORDER(ds),
                    TILE_SIZE(ds) * ds.h + 2 * BORDER(ds));
        ds.started = true;
        force = true;
    }

    if (ui.dragx_src != -1 && ui.dragy_src != -1) {
        ds.dragging = true;
        is_drag_src = state. gridi[ ui.dragx_src, ui.dragy_src];
        Debug.Assert(is_drag_src != null);
        if (ui.dragx_dst != -1 && ui.dragy_dst != -1) {
            is_drag_dst = state. gridi[ ui.dragx_dst, ui.dragy_dst];
            Debug.Assert(is_drag_dst != null);
        }
    } else
        ds.dragging = false;

    if (ui.show_hints != ds.show_hints) {
        force = true;
        ds.show_hints = ui.show_hints;
    }

    /* Draw all lines (and hints, if we want), but *not* islands. */
    for (x = 0; x < ds.w; x++) {
        for (y = 0; y < ds.h; y++) {
            v = state.grid[ x, y] | flash;
            dsv = ds.grid[x,y] & ~G_REDRAW;

            if ((v & G_ISLAND) !=0)  continue;

            if (is_drag_dst != null)
            {
                if (WITHIN(x, is_drag_src.x, is_drag_dst.x) &&
                    WITHIN(y, is_drag_src.y, is_drag_dst.y))
                    v |= G_ISSEL;
            }
            lines_lvlh(state, ui, x, y, v, out lv, out lh);

            /*if (ui.cur_visible && ui.cur_x == x && ui.cur_y == y)
                v |= G_CURSOR;*/

            if (v != dsv ||
                lv != ds.lv[x,y] ||
                lh != ds.lh[x,y] ||
                force) {
                ds.grid[ x, y] = v | G_REDRAW;
                ds.lv[x,y] = lv;
                ds.lh[x,y] = lh;
                lines_redraw(dr, state, ds, ui, x, y, v, lv, lh);
            } else
                ds.grid[x,y] &= ~G_REDRAW;
        }
    }

    /* Draw islands. */
    for (i = 0; i < state.islands.Count; i++) {
        isld = state.islands[i];
        v = state.grid[ isld.x, isld.y] | flash;

        redraw = false;
        for (j = 0; j < isld.adj.npoints; j++) {
            if ((ds.grid[isld.adj.points[j].x,isld.adj.points[j].y] & G_REDRAW)!=0) {
                redraw = true;
            }
        }

        if (is_drag_src != null) {
            if (isld == is_drag_src)
                v |= G_ISSEL;
            else if (is_drag_dst != null && isld == is_drag_dst)
                v |= G_ISSEL;
        }

        if (island_impossible(isld, (v & G_MARK)!=0)) v |= G_WARN;

        if (ui.cur_visible && ui.cur_x == isld.x && ui.cur_y == isld.y)
            v |= G_CURSOR;

        if ((v != ds.grid[ isld.x, isld.y]) || force || redraw) {
            ds.grid[isld.x,isld.y] = v;
            island_redraw(dr, state, ds, isld, v);
        }
    }
}

static float game_anim_length(BridgesState oldstate,
                              BridgesState newstate, int dir, BridgesUI ui)
{
    return 0.0F;
}

static float game_flash_length(BridgesState oldstate,
                               BridgesState newstate, int dir, BridgesUI ui)
{
    if (!oldstate.completed && newstate.completed &&
        !oldstate.solved && !newstate.solved)
        return FLASH_TIME;

    return 0.0F;
}

public override float CompletedFlashDuration(BridgesSettings settings)
{
    return FLASH_TIME;
}

static int game_status(BridgesState state)
{
    return state.completed ? +1 : 0;
}

static int game_timing_state(BridgesState state, BridgesUI ui)
{
    return 1;
}
internal override void SetKeyboardCursorVisible(BridgesUI ui, int tileSize, bool value)
{
    ui.cur_visible = value;
}
    }
}
