/* At some stage we should put these into a real options struct.
 * Note that tile_redraw has no #iffery; it relies on tile_flags not
 * to put those flags in. */
#define HINT_LIGHTS 
#define HINT_OVERLAPS
#define HINT_NUMBERS

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Lightup
{
    public sealed class LightupGame : GameBase<LightupSettings, LightupState, LightupMove, LightupDrawState, LightupUI>
    {
        const int PREFERRED_TILE_SIZE = 32;

        private static int TILE_SIZE(LightupDrawState ds) { return (ds.tilesize);}
        private static int BORDER(LightupDrawState ds) { return           (TILE_SIZE(ds) / 2);}
        private static int TILE_RADIUS(LightupDrawState ds) { return      (ds.crad);}
        private static int COORD(LightupDrawState ds, int x)  { return       ( (x) * TILE_SIZE(ds) + BORDER(ds) );}
        private static int FROMCOORD(LightupDrawState ds, int x)  { return       ( ((x) - BORDER(ds) + TILE_SIZE(ds)) / TILE_SIZE(ds) - 1 );}

        const int COL_BACKGROUND = 0;
        const int COL_GRID = 1;
        const int COL_BLACK = 2;			       /* black */
        const int COL_LIGHT = 3;			       /* white */
        const int COL_LIT = 4;		       /* yellow */
        const int COL_ERROR = 5;			       /* red */
        const int COL_CURSOR = 6;
        const int NCOLOURS = 7;

        const int SYMM_NONE = 0;
        const int  SYMM_REF2= 1;
        const int  SYMM_ROT2= 2;
        const int  SYMM_REF4= 3;
        const int  SYMM_ROT4= 4;
        const int  SYMM_MAX = 5;

        const int DIFFCOUNT =2;

        const uint F_BLACK        =1;

        /* flags for black squares */
        const uint  F_NUMBERED     =2;       /* it has a number attached */
        const uint  F_NUMBERUSED   =4;       /* this number was useful for solving */

        /* flags for non-black squares */
        internal const uint  F_IMPOSSIBLE   =8;       /* can't put a light here */
        internal const uint  F_LIGHT        =16;

        const uint F_MARK          =32;

        const float FLASH_TIME = 0.30F;

        //private static int GRIDLights(LightupState gs, int x, int y) { return (gs.lights[(y)*((gs).w) + (x)]); }
        //private static uint GRIDFlags(LightupState gs, int x, int y) { return (gs.flags[(y) * ((gs).w) + (x)]); }

        private static void FOREACHLIT(LlData lld, Action<int,int> block) {                        
            int lx,ly;                                                    
            ly = (lld).oy;                                               
            for (lx = (lld).minx; lx <= (lld).maxx; lx++) {             
                if (lx == (lld).ox) continue;                              
                block(lx,ly);                                                      
            }                                                             
            lx = (lld).ox;                                               
            for (ly = (lld).miny; ly <= (lld).maxy; ly++) {             
                if (!(lld).include_origin && ly == (lld).oy) continue;
                block(lx,ly);                                                       
            }                                                             
        }

        private static void ADDPOINT(LightupSurrounds s, bool cond, int nx, int ny) 
        {
            if (cond)
            {
                s.points[s.npoints].x = (nx);
                s.points[s.npoints].y = (ny);
                s.points[s.npoints].f = 0;
                s.npoints++;
            }
        }

        /* Fills in (doesn't allocate) a surrounds structure with the grid locations
         * around a given square, taking account of the edges. */
        static void get_surrounds(LightupState state, int ox, int oy,
                                  LightupSurrounds s)
        {
            Debug.Assert(ox >= 0 && ox < state.w && oy >= 0 && oy < state.h);
            s.npoints = 0;
            ADDPOINT(s, ox > 0,            ox-1, oy);
            ADDPOINT(s, ox < (state.w - 1), ox + 1, oy);
            ADDPOINT(s, oy > 0, ox, oy - 1);
            ADDPOINT(s, oy < (state.h - 1), ox, oy + 1);
        }

        private static LightupSettings[] presets = new [] {
        
            new LightupSettings( w : 7, h : 7, blackpc : 20, symm : SYMM_ROT4, difficulty : 0 ),
            new LightupSettings( w : 7, h : 7, blackpc : 20, symm : SYMM_ROT4, difficulty : 1 ),
            new LightupSettings( w : 7, h : 7, blackpc : 20, symm : SYMM_ROT4, difficulty : 2 ),

            new LightupSettings( w : 10, h : 10, blackpc : 20, symm : SYMM_ROT2, difficulty : 0 ),
            new LightupSettings( w : 10, h : 10, blackpc : 20, symm : SYMM_ROT2, difficulty : 1 ),
            new LightupSettings( w : 10, h : 10, blackpc : 20, symm : SYMM_ROT2, difficulty : 2 ),

            new LightupSettings( w : 14, h : 14, blackpc : 20, symm : SYMM_ROT2, difficulty : 0 ),
            new LightupSettings( w : 14, h : 14, blackpc : 20, symm : SYMM_ROT2, difficulty : 1 ),
            new LightupSettings( w : 14, h : 14, blackpc : 20, symm : SYMM_ROT2, difficulty : 2 )
        };

        public override LightupSettings DefaultSettings
        {
            get { return presets[0]; }
        }

        public override IEnumerable<LightupSettings> PresetsSettings
        {
            get { return presets; }
        }

        public override LightupMove ParseMove(LightupSettings settings, string moveString)
        {
            return LightupMove.Parse(settings, moveString);
        }

        public override LightupSettings ParseSettings(string settingsString)
        {
            return LightupSettings.Parse(settingsString);
        }

        /* --- Game state construction/freeing helper functions --- */

static LightupState new_state(LightupSettings @params)
{
    LightupState ret = new LightupState();

    ret.w = @params.w;
    ret.h = @params.h;
    ret.lights = new int[ret.w,ret.h];
    ret.nlights = 0;
    //memset(ret.lights, 0, ret.w * ret.h * sizeof(int));
    ret.flags = new uint[ret.w,ret.h];
    //memset(ret.flags, 0, ret.w * ret.h * sizeof(uint));
    ret.completed = ret.used_solve = false;
    return ret;
}

static LightupState dup_game(LightupState state)
{
    LightupState ret = new LightupState();

    ret.w = state.w;
    ret.h = state.h;

    ret.lights = new int[ret.w,ret.h];
    Array.Copy(state.lights,ret.lights,  ret.w * ret.h);
    ret.nlights = state.nlights;

    ret.flags = new uint[ret.w,ret.h];
    Array.Copy(state.flags, ret.flags, ret.w * ret.h);

    ret.completed = state.completed;
    ret.used_solve = state.used_solve;

    return ret;
}


//static void debug_state(LightupState state)
//{
//    int x, y;
//    char c = '?';

//    for (y = 0; y < state.h; y++) {
//        for (x = 0; x < state.w; x++) {
//            c = '.';
//            if (state.flags[ x, y] & F_BLACK) {
//                if (state.flags[ x, y] & F_NUMBERED)
//                    c = state.lights[ x, y] + '0';
//                else
//                    c = '#';
//            } else {
//                if (state.flags[ x, y] & F_LIGHT)
//                    c = 'O';
//                else if (state.flags[ x, y] & F_IMPOSSIBLE)
//                    c = 'X';
//            }
//            debug(("%c", (int)c));
//        }
//        debug(("     "));
//        for (x = 0; x < state.w; x++) {
//            if (state.flags[ x, y] & F_BLACK)
//                c = '#';
//            else {
//                c = (state.flags[ x, y] & F_LIGHT) ? 'A' : 'a';
//                c += state.lights[ x, y];
//            }
//            debug(("%c", (int)c));
//        }
//        debug(("\n"));
//    }
//}

/* --- Game completion test routines. --- */

/* These are split up because occasionally functions are only
 * interested in one particular aspect. */

/* Returns non-zero if all grid spaces are lit. */
static bool grid_lit(LightupState state)
{
    int x, y;

    for (x = 0; x < state.w; x++) {
        for (y = 0; y < state.h; y++) {
            if ((state.flags[x,y] & F_BLACK) != 0) continue;
            if (state.lights[x,y] == 0)
                return false;
        }
    }
    return true;
}

/* Returns non-zero if any lights are lit by other lights. */
static bool grid_overlap(LightupState state)
{
    int x, y;

    for (x = 0; x < state.w; x++) {
        for (y = 0; y < state.h; y++) {
            if ((state.flags[ x, y] & F_LIGHT) == 0) continue;
            if (state.lights[ x, y] > 1)
                return true;
        }
    }
    return false;
}

static bool number_wrong(LightupState state, int x, int y)
{
    LightupSurrounds s = new LightupSurrounds();
    int i, n, empty, lights = state.lights[ x, y];

    /*
     * This function computes the display hint for a number: we
     * turn the number red if it is definitely wrong. This means
     * that either
     * 
     *  (a) it has too many lights around it, or
     * 	(b) it would have too few lights around it even if all the
     * 	    plausible squares (not black, lit or F_IMPOSSIBLE) were
     * 	    filled with lights.
     */

    Debug.Assert((state.flags[x, y] & F_NUMBERED) != 0);
    get_surrounds(state, x, y, s);

    empty = n = 0;
    for (i = 0; i < s.npoints; i++) {
        if ((state.flags[s.points[i].x, s.points[i].y] & F_LIGHT) != 0)
        {
	    n++;
	    continue;
	}
    if ((state.flags[s.points[i].x, s.points[i].y] & F_BLACK) != 0)
	    continue;
    if ((state.flags[s.points[i].x, s.points[i].y] & F_IMPOSSIBLE) != 0)
	    continue;
	if (state.lights[s.points[i].x,s.points[i].y] != 0)
	    continue;
	empty++;
    }
    return (n > lights || (n + empty < lights));
}

static bool number_correct(LightupState state, int x, int y)
{
    LightupSurrounds s = new LightupSurrounds();
    int n = 0, i, lights = state.lights[ x, y];

    Debug.Assert((state.flags[ x, y] & F_NUMBERED) != 0);
    get_surrounds(state, x, y, s);
    for (i = 0; i < s.npoints; i++) {
        if ((state.flags[s.points[i].x,s.points[i].y] & F_LIGHT) != 0)
            n++;
    }
    return (n == lights) ? true : false;
}

/* Returns non-zero if any numbers add up incorrectly. */
static bool grid_addsup(LightupState state)
{
    int x, y;

    for (x = 0; x < state.w; x++) {
        for (y = 0; y < state.h; y++) {
            if ((state.flags[ x, y] & F_NUMBERED) == 0) continue;
            if (!number_correct(state, x, y)) return false;
        }
    }
    return true;
}

static bool grid_correct(LightupState state)
{
    if (grid_lit(state) &&
        !grid_overlap(state) &&
        grid_addsup(state)) return true;
    return false;
}

/* --- Board initial setup (blacks, lights, numbers) --- */

static void clean_board(LightupState state, bool leave_blacks)
{
    int x,y;
    for (x = 0; x < state.w; x++) {
        for (y = 0; y < state.h; y++) {
            if (leave_blacks)
                state.flags[ x, y] &= F_BLACK;
            else
                state.flags[ x, y] = 0;
            state.lights[ x, y] = 0;
        }
    }
    state.nlights = 0;
}

static void set_blacks(LightupState state, LightupSettings @params,
                       Random rs)
{
    int x, y, degree = 0, nblack;
    bool rotate = false;
    int rh, rw, i;
    int wodd = (state.w % 2 != 0) ? 1 : 0;
    int hodd = (state.h % 2 != 0) ? 1 : 0;
    int[] xs = new int[4], ys = new int[4];

    switch (@params.symm) {
        case SYMM_NONE: degree = 1; rotate = false; break;
        case SYMM_ROT2: degree = 2; rotate = true; break;
        case SYMM_REF2: degree = 2; rotate = false; break;
        case SYMM_ROT4: degree = 4; rotate = true; break;
        case SYMM_REF4: degree = 4; rotate = false; break;
        default: Debug.Assert(false, "Unknown symmetry type"); break;
    }
    if (@params.symm == SYMM_ROT4 && (state.h != state.w))
        Debug.Assert(false, "4-fold symmetry unavailable without square grid");

    if (degree == 4) {
        rw = state.w/2;
        rh = state.h/2;
        if (!rotate) rw += wodd; /* ... but see below. */
        rh += hodd;
    } else if (degree == 2) {
        rw = state.w;
        rh = state.h/2;
        rh += hodd;
    } else {
        rw = state.w;
        rh = state.h;
    }

    /* clear, then randomise, required region. */
    clean_board(state, false);
    nblack = (rw * rh * @params.blackpc) / 100;
    for (i = 0; i < nblack; i++) {
        do {
            x = rs.Next(0, rw);
            y = rs.Next(0, rh);
        } while ((state.flags[x,y] & F_BLACK) != 0);
        state.flags[ x, y] |= F_BLACK;
    }

    /* Copy required region. */
    if (@params.symm == SYMM_NONE) return;

    for (x = 0; x < rw; x++) {
        for (y = 0; y < rh; y++) {
            if (degree == 4) {
                xs[0] = x;
                ys[0] = y;
                xs[1] = state.w - 1 - (rotate ? y : x);
                ys[1] = rotate ? x : y;
                xs[2] = rotate ? (state.w - 1 - x) : x;
                ys[2] = state.h - 1 - y;
                xs[3] = rotate ? y : (state.w - 1 - x);
                ys[3] = state.h - 1 - (rotate ? x : y);
            } else {
                xs[0] = x;
                ys[0] = y;
                xs[1] = rotate ? (state.w - 1 - x) : x;
                ys[1] = state.h - 1 - y;
            }
            for (i = 1; i < degree; i++) {
                state.flags[ xs[i], ys[i]] =
                    state.flags[ xs[0], ys[0]];
            }
        }
    }
    /* SYMM_ROT4 misses the middle square above; fix that here. */
    if (degree == 4 && rotate && wodd != 0 &&
        (rs.Next(0, 100) <= (uint)@params.blackpc))
        state.flags[
             state.w/2 + wodd - 1, state.h/2 + hodd - 1] |= F_BLACK;

#if SOLVER_DIAGNOSTICS
    if (verbose) debug_state(state);
#endif
}

/* Fills in (does not allocate) a LlData with all the tiles that would
 * be illuminated by a light at point (ox,oy). If origin=1 then the
 * origin is included in this list. */
static void list_lights(LightupState state, int ox, int oy, bool origin,
                        LlData lld)
{
    int x,y;

    lld.ox = lld.minx = lld.maxx = ox;
    lld.oy = lld.miny = lld.maxy = oy;
    lld.include_origin = origin;

    y = oy;
    for (x = ox-1; x >= 0; x--) {
        if ((state.flags[ x, y] & F_BLACK) != 0) break;
        if (x < lld.minx) lld.minx = x;
    }
    for (x = ox+1; x < state.w; x++) {
        if ((state.flags[ x, y] & F_BLACK) != 0) break;
        if (x > lld.maxx) lld.maxx = x;
    }

    x = ox;
    for (y = oy-1; y >= 0; y--) {
        if ((state.flags[ x, y] & F_BLACK) != 0) break;
        if (y < lld.miny) lld.miny = y;
    }
    for (y = oy+1; y < state.h; y++) {
        if ((state.flags[x, y] & F_BLACK) != 0) break;
        if (y > lld.maxy) lld.maxy = y;
    }
}

/* Makes sure a light is the given state, editing the lights table to suit the
 * new state if necessary. */
static void set_light(LightupState state, int ox, int oy, bool on)
{
    LlData lld = new LlData();
    int diff = 0;

    Debug.Assert((state.flags[ox,oy] & F_BLACK) == 0);

    if (!on && (state.flags[ox,oy] & F_LIGHT) != 0) {
        diff = -1;
        state.flags[ox,oy] &= ~F_LIGHT;
        state.nlights--;
    } else if (on && (state.flags[ox,oy] & F_LIGHT) == 0) {
        diff = 1;
        state.flags[ox,oy] |= F_LIGHT;
        state.nlights++;
    }

    if (diff != 0) {
        list_lights(state, ox, oy, true, lld);
        FOREACHLIT(lld, (lx, ly) => { state.lights[lx, ly] += diff; });
    }
}

/* Returns 1 if removing a light at (x,y) would cause a square to go dark. */
static int check_dark(LightupState state, int x, int y)
{
    LlData lld = new LlData();

    list_lights(state, x, y, true, lld);
    FOREACHLIT(lld, (lx, ly ) => { if (state.lights[lx,ly] == 1) { return/* 1*/; } });
    return 0;
}

/* Sets up an initial random correct position (i.e. every
 * space lit, and no lights lit by other lights) by filling the
 * grid with lights and then removing lights one by one at random. */
static void place_lights(LightupState state, Random rs)
{
    int i, x, y, n;
    int [] numindices;
    int wh = state.w*state.h;
    LlData lld = new LlData();

    numindices = new int[wh];
    for (i = 0; i < wh; i++) numindices[i] = i;
    numindices.Shuffle(wh, rs);

    /* Place a light on all grid squares without lights. */
    for (x = 0; x < state.w; x++) {
        for (y = 0; y < state.h; y++) {
            state.flags[ x, y] &= ~F_MARK; /* we use this later. */
            if ((state.flags[ x, y] & F_BLACK) != 0) continue;
            set_light(state, x, y, true);
        }
    }

    for (i = 0; i < wh; i++) {
        y = numindices[i] / state.w;
        x = numindices[i] % state.w;
        if ((state.flags[ x, y] & F_LIGHT)==0) continue;
        if ((state.flags[ x, y] & F_MARK)!=0) continue;
        list_lights(state, x, y, false, lld);

        /* If we're not lighting any lights ourself, don't remove anything. */
        n = 0;
        FOREACHLIT(lld, (lx,ly) => {if ((state.flags[lx,ly] & F_LIGHT)!=0) { n += 1; }} );
        if (n == 0) continue; /* [1] */

        /* Check whether removing lights we're lighting would cause anything
         * to go dark. */
        n = 0;
        FOREACHLIT(lld, (lx,ly) => { if ((state.flags[lx,ly] & F_LIGHT)!=0) { n += check_dark(state,lx,ly); }} );
        if (n == 0) {
            /* No, it wouldn't, so we can remove them all. */
            FOREACHLIT(lld, (lx,ly) => set_light(state,lx,ly, false) );
            state.flags[x,y] |= F_MARK;
        }

        if (!grid_overlap(state)) {
            return; /* we're done. */
        }
        Debug.Assert(grid_lit(state));
    }
    /* could get here if the line at [1] continue'd out of the loop. */
    if (grid_overlap(state)) {
        //debug_state(state);
        Debug.Assert(false, "place_lights failed to resolve overlapping lights!");
    }
}

/* Fills in all black squares with numbers of adjacent lights. */
static void place_numbers(LightupState state)
{
    int x, y, i, n;
    LightupSurrounds s = new LightupSurrounds();

    for (x = 0; x < state.w; x++) {
        for (y = 0; y < state.h; y++) {
            if ((state.flags[x,y] & F_BLACK)==0) continue;
            get_surrounds(state, x, y, s);
            n = 0;
            for (i = 0; i < s.npoints; i++) {
                if ((state.flags[s.points[i].x, s.points[i].y] & F_LIGHT)!=0)
                    n++;
            }
            state.flags[x,y] |= F_NUMBERED;
            state.lights[x,y] = n;
        }
    }
}

/* --- Actual solver, with helper subroutines. --- */

static void tsl_callback(LightupState state,
                         int lx, int ly, ref int x, ref int y, ref int n)
{
    if ((state.flags[lx,ly] & F_IMPOSSIBLE)!=0) return;
    if (state.lights[lx,ly] > 0) return;
    x = lx; y = ly; (n)++;
}

static bool try_solve_light(LightupState state, int ox, int oy,
                           uint flags, int lights)
{
    LlData lld = new LlData();
    int sx = 0, sy = 0, n = 0;

    if (lights > 0) return false;
    if ((flags & F_BLACK) != 0) return false;

    /* We have an unlit square; count how many ways there are left to
     * place a light that lights us (including this square); if only
     * one, we must put a light there. Squares that could light us
     * are, of course, the same as the squares we would light... */
    list_lights(state, ox, oy, true, lld);
    FOREACHLIT(lld, (lx,ly) => { tsl_callback(state, lx, ly, ref sx, ref sy, ref n); });
    if (n == 1) {
        set_light(state, sx, sy, true);
#if SOLVER_DIAGNOSTICS
        debug(("(%d,%d) can only be lit from (%d,%d); setting to LIGHT\n",
                ox,oy,sx,sy));
        if (verbose) debug_state(state);
#endif
        return true;
    }

    return false;
}

static bool could_place_light(uint flags, int lights)
{
    if ((flags & (F_BLACK | F_IMPOSSIBLE)) != 0) return false;
    return (lights > 0) ? false : true;
}

static bool could_place_light_xy(LightupState state, int x, int y)
{
    int lights = state.lights[x,y];
    uint flags = state.flags[x,y];
    return (could_place_light(flags, lights)) ? true : false;
}

/* For a given number square, determine whether we have enough info
 * to unambiguously place its lights. */
static bool try_solve_number(LightupState state, int nx, int ny,
                            uint nflags, int nlights)
{
    LightupSurrounds s = new LightupSurrounds();
    int x, y, nl, ns, i, lights;
    bool ret = false;
    uint flags;

    if ((nflags & F_NUMBERED) == 0) return false;
    nl = nlights;
    get_surrounds(state,nx,ny,s);
    ns = s.npoints;

    /* nl is no. of lights we need to place, ns is no. of spaces we
     * have to place them in. Try and narrow these down, and mark
     * points we can ignore later. */
    for (i = 0; i < s.npoints; i++) {
        x = s.points[i].x; y = s.points[i].y;
        flags = state.flags[x,y];
        lights = state.lights[x,y];
        if ((flags & F_LIGHT) != 0) {
            /* light here already; one less light for one less place. */
            nl--; ns--;
            s.points[i].f |= F_MARK;
        } else if (!could_place_light(flags, lights)) {
            ns--;
            s.points[i].f |= F_MARK;
        }
    }
    if (ns == 0) return false; /* nowhere to put anything. */
    if (nl == 0) {
        /* we have placed all lights we need to around here; all remaining
         * surrounds are therefore IMPOSSIBLE. */
        state.flags[nx,ny] |= F_NUMBERUSED;
        for (i = 0; i < s.npoints; i++) {
            if ((s.points[i].f & F_MARK)==0) {
                state.flags[s.points[i].x,s.points[i].y] |= F_IMPOSSIBLE;
                ret = true;
            }
        }
#if SOLVER_DIAGNOSTICS
        printf("Clue at (%d,%d) full; setting unlit to IMPOSSIBLE.\n",
               nx,ny);
        if (verbose) debug_state(state);
#endif
    } else if (nl == ns) {
        /* we have as many lights to place as spaces; fill them all. */
        state.flags[nx,ny] |= F_NUMBERUSED;
        for (i = 0; i < s.npoints; i++) {
            if ((s.points[i].f & F_MARK)==0) {
                set_light(state, s.points[i].x,s.points[i].y, true);
                ret = true;
            }
        }
#if SOLVER_DIAGNOSTICS
        printf("Clue at (%d,%d) trivial; setting unlit to LIGHT.\n",
               nx,ny);
        if (verbose) debug_state(state);
#endif
    }
    return ret;
}

class setscratch {
    internal int x, y;
    internal int n;
};

private static int SCRATCHSZ(LightupState state) { return (state.w+state.h); }

/* New solver algorithm: overlapping sets can add IMPOSSIBLE flags.
 * Algorithm thanks to Simon:
 *
 * (a) Any square where you can place a light has a set of squares
 *     which would become non-lights as a result. (This includes
 *     squares lit by the first square, and can also include squares
 *     adjacent to the same clue square if the new light is the last
 *     one around that clue.) Call this MAKESDARK(x,y) with (x,y) being
 *     the square you place a light.

 * (b) Any unlit square has a set of squares on which you could place
 *     a light to illuminate it. (Possibly including itself, of
 *     course.) This set of squares has the property that _at least
 *     one_ of them must contain a light. Sets of this type also arise
 *     from clue squares. Call this MAKESLIGHT(x,y), again with (x,y)
 *     the square you would place a light.

 * (c) If there exists (dx,dy) and (lx,ly) such that MAKESDARK(dx,dy) is
 *     a superset of MAKESLIGHT(lx,ly), this implies that placing a light at
 *     (dx,dy) would either leave no remaining way to illuminate a certain
 *     square, or would leave no remaining way to fulfill a certain clue
 *     (at lx,ly). In either case, a light can be ruled out at that position.
 *
 * So, we construct all possible MAKESLIGHT sets, both from unlit squares
 * and clue squares, and then we look for plausible MAKESDARK sets that include
 * our (lx,ly) to see if we can find a (dx,dy) to rule out. By the time we have
 * constructed the MAKESLIGHT set we don't care about (lx,ly), just the set
 * members.
 *
 * Once we have such a set, Simon came up with a Cunning Plan to find
 * the most sensible MAKESDARK candidate:
 *
 * (a) for each square S in your set X, find all the squares which _would_
 *     rule it out. That means any square which would light S, plus
 *     any square adjacent to the same clue square as S (provided
 *     that clue square has only one remaining light to be placed).
 *     It's not hard to make this list. Don't do anything with this
 *     data at the moment except _count_ the squares.

 * (b) Find the square S_min in the original set which has the
 *     _smallest_ number of other squares which would rule it out.

 * (c) Find all the squares that rule out S_min (it's probably
 *     better to recompute this than to have stored it during step
 *     (a), since the CPU requirement is modest but the storage
 *     cost would get ugly.) For each of these squares, see if it
 *     rules out everything else in the set X. Any which does can
 *     be marked as not-a-light.
 *
 */

//typedef void (*trl_cb)(LightupState state, int dx, int dy,
//                       setscratch scratch, int n, object ctx);

//static void try_rule_out(LightupState state, int x, int y,
//                         setscratch scratch, int n,
//                         Action<LightupState,int,int,setscratch,int,object> cb, object ctx);

static void trl_callback_search(LightupState state, int dx, int dy,
                       setscratch[] scratch, int n, object ignored)
{
    int i;

#if SOLVER_DIAGNOSTICS
    if (verbose) debug(("discount cb: light at (%d,%d)\n", dx, dy));
#endif

    for (i = 0; i < n; i++) {
        if (dx == scratch[i].x && dy == scratch[i].y) {
            scratch[i].n = 1;
            return;
        }
    }
}

class didsomething
{
    internal bool didsth;
}

static void trl_callback_discount(LightupState state, int dx, int dy,
                       setscratch[] scratch, int n, didsomething ctx)
{
    int i;

    if ((state.flags[dx,dy] & F_IMPOSSIBLE) != 0) {
#if SOLVER_DIAGNOSTICS
        debug(("Square at (%d,%d) already impossible.\n", dx,dy));
#endif
        return;
    }

    /* Check whether a light at (dx,dy) rules out everything
     * in scratch, and mark (dx,dy) as IMPOSSIBLE if it does.
     * We can use try_rule_out for this as well, as the set of
     * squares which would rule out (x,y) is the same as the
     * set of squares which (x,y) would rule out. */

#if SOLVER_DIAGNOSTICS
    if (verbose) debug(("Checking whether light at (%d,%d) rules out everything in scratch.\n", dx, dy));
#endif

    for (i = 0; i < n; i++)
        scratch[i].n = 0;
    try_rule_out<object>(state, dx, dy, scratch, n, trl_callback_search, null);
    for (i = 0; i < n; i++) {
        if (scratch[i].n == 0) return;
    }
    /* The light ruled out everything in scratch. Yay. */
    state.flags[dx,dy] |= F_IMPOSSIBLE;
#if SOLVER_DIAGNOSTICS
    debug(("Set reduction discounted square at (%d,%d):\n", dx,dy));
    if (verbose) debug_state(state);
#endif

    ctx.didsth = true;
}

static void trl_callback_incn(LightupState state, int dx, int dy,
                       setscratch[] scratch, int n, setscratch s)
{
    s.n++;
}

static void try_rule_out<T>(LightupState state, int x, int y,
                         setscratch[] scratch, int n,
                         Action<LightupState,int,int,setscratch[],int,T> cb, T ctx)
{
    /* XXX Find all the squares which would rule out (x,y); anything
     * that would light it as well as squares adjacent to same clues
     * as X assuming that clue only has one remaining light.
     * Call the callback with each square. */
    LlData lld = new LlData();
    LightupSurrounds s = new LightupSurrounds(), ss = new LightupSurrounds();
    int i, j, curr_lights, tot_lights;

    /* Find all squares that would rule out a light at (x,y) and call trl_cb
     * with them: anything that would light (x,y)... */

    list_lights(state, x, y, false, lld);
    FOREACHLIT(lld, (lx,ly) => { if (could_place_light_xy(state, lx, ly)) { cb(state, lx, ly, scratch, n, ctx); } });

    /* ... as well as any empty space (that isn't x,y) next to any clue square
     * next to (x,y) that only has one light left to place. */

    get_surrounds(state, x, y, s);
    for (i = 0; i < s.npoints; i++) {
        if ((state.flags[s.points[i].x,s.points[i].y] & F_NUMBERED)==0)
            continue;
        /* we have an adjacent clue square; find /its/ surrounds
         * and count the remaining lights it needs. */
        get_surrounds(state,s.points[i].x,s.points[i].y,ss);
        curr_lights = 0;
        for (j = 0; j < ss.npoints; j++) {
            if ((state.flags[ss.points[j].x,ss.points[j].y] & F_LIGHT)!=0)
                curr_lights++;
        }
        tot_lights = state.lights[ s.points[i].x, s.points[i].y];
        /* We have a clue with tot_lights to fill, and curr_lights currently
         * around it. If adding a light at (x,y) fills up the clue (i.e.
         * curr_lights + 1 = tot_lights) then we need to discount all other
         * unlit squares around the clue. */
        if ((curr_lights + 1) == tot_lights) {
            for (j = 0; j < ss.npoints; j++) {
                int lx = ss.points[j].x, ly = ss.points[j].y;
                if (lx == x && ly == y) continue;
                if (could_place_light_xy(state, lx, ly))
                    cb(state, lx, ly, scratch, n, ctx);
            }
        }
    }
}

#if SOLVER_DIAGNOSTICS
static void debug_scratch(string msg, setscratch scratch, int n)
{
    int i;
    debug(("%s scratch (%d elements):\n", msg, n));
    for (i = 0; i < n; i++) {
        debug(("  (%d,%d) n%d\n", scratch[i].x, scratch[i].y, scratch[i].n));
    }
}
#endif

static bool discount_set(LightupState state,
                        setscratch[] scratch, int n)
{
    int i, besti, bestn;
    didsomething didsth = new didsomething();

#if SOLVER_DIAGNOSTICS
    if (verbose > 1) debug_scratch("discount_set", scratch, n);
#endif
    if (n == 0) return false;

    for (i = 0; i < n; i++) {
        try_rule_out(state, scratch[i].x, scratch[i].y, scratch, n,
                     trl_callback_incn, scratch[i]);
    }
#if SOLVER_DIAGNOSTICS
    if (verbose > 1) debug_scratch("discount_set after count", scratch, n);
#endif

    besti = -1; bestn = SCRATCHSZ(state);
    for (i = 0; i < n; i++) {
        if (scratch[i].n < bestn) {
            bestn = scratch[i].n;
            besti = i;
        }
    }
#if SOLVER_DIAGNOSTICS
    if (verbose > 1) debug(("best square (%d,%d) with n%d.\n",
           scratch[besti].x, scratch[besti].y, scratch[besti].n));
#endif
    try_rule_out(state, scratch[besti].x, scratch[besti].y, scratch, n,
                 trl_callback_discount, didsth);
#if SOLVER_DIAGNOSTICS
    if (didsth) debug((" [from square (%d,%d)]\n",
                       scratch[besti].x, scratch[besti].y));
#endif

    return didsth.didsth;
}

static void discount_clear(LightupState state, setscratch[] scratch, out int n)
{
    n = 0;
    //Array.Clear(scratch, 0, SCRATCHSZ(state));
    for(int i = 0;i<scratch.Length;++i)
    {
        scratch[i] = new setscratch();
    }
}

static void unlit_cb(LightupState state, int lx, int ly,
                     setscratch[] scratch, ref int n)
{
    if (could_place_light_xy(state, lx, ly)) {
        scratch[n].x = lx; scratch[n].y = ly; (n)++;
    }
}

/* Construct a MAKESLIGHT set from an unlit square. */
static bool discount_unlit(LightupState state, int x, int y,
                          setscratch[] scratch)
{
    LlData lld = new LlData();
    int n;
    bool didsth;

#if SOLVER_DIAGNOSTICS
    if (verbose) debug(("Trying to discount for unlit square at (%d,%d).\n", x, y));
    if (verbose > 1) debug_state(state);
#endif

    discount_clear(state, scratch, out n);

    list_lights(state, x, y, true , lld);
    FOREACHLIT(lld, (lx, ly) => { unlit_cb(state, lx, ly, scratch, ref n); });
    didsth = discount_set(state, scratch, n);
#if SOLVER_DIAGNOSTICS
    if (didsth) debug(("  [from unlit square at (%d,%d)].\n", x, y));
#endif
    return didsth;

}

/* Construct a series of MAKESLIGHT sets from a clue square.
 *  for a clue square with N remaining spaces that must contain M lights, every
 *  subset of size N-M+1 of those N spaces forms such a set.
 */

static bool discount_clue(LightupState state, int x, int y,
                          setscratch[] scratch)
{
    int slen, m = state.lights[ x, y], n, i, lights;
    uint flags;
    bool didsth = false;
    LightupSurrounds s = new LightupSurrounds(), sempty = new LightupSurrounds();
    Combi combi;

    if (m == 0) return false;

#if SOLVER_DIAGNOSTICS
    if (verbose) debug(("Trying to discount for sets at clue (%d,%d).\n", x, y));
    if (verbose > 1) debug_state(state);
#endif

    /* m is no. of lights still to place; starts off at the clue value
     * and decreases when we find a light already down.
     * n is no. of spaces left; starts off at 0 and goes up when we find
     * a plausible space. */

    get_surrounds(state, x, y, s);
    //memset(&sempty, 0, sizeof(surrounds));
    for (i = 0; i < s.npoints; i++) {
        int lx = s.points[i].x, ly = s.points[i].y;
        flags = state.flags[lx,ly];
        lights = state.lights[lx,ly];

        if ((flags & F_LIGHT) != 0) m--;

        if (could_place_light(flags, lights)) {
            sempty.points[sempty.npoints].x = lx;
            sempty.points[sempty.npoints].y = ly;
            sempty.npoints++;
        }
    }
    n = sempty.npoints; /* sempty is now a surrounds of only blank squares. */
    if (n == 0) return false; /* clue is full already. */

    if (m < 0 || m > n) return false; /* become impossible. */

    combi = new Combi(n - m + 1, n);
    while (combi.Next())
    {
        discount_clear(state, scratch, out slen);
        for (i = 0; i < combi.r; i++) {
            scratch[slen].x = sempty.points[combi.a[i]].x;
            scratch[slen].y = sempty.points[combi.a[i]].y;
            slen++;
        }
        if (discount_set(state, scratch, slen)) didsth = true;
    }
#if SOLVER_DIAGNOSTICS
    if (didsth) debug(("  [from clue at (%d,%d)].\n", x, y));
#endif
    return didsth;
}

const int F_SOLVE_FORCEUNIQUE    =1;
const int F_SOLVE_DISCOUNTSETS   =2;
const int F_SOLVE_ALLOWRECURSE   =4;

static uint flags_from_difficulty(int difficulty)
{
    uint sflags = F_SOLVE_FORCEUNIQUE;
    Debug.Assert(difficulty <= DIFFCOUNT);
    if (difficulty >= 1) sflags |= F_SOLVE_DISCOUNTSETS;
    if (difficulty >= 2) sflags |= F_SOLVE_ALLOWRECURSE;
    return sflags;
}

const int MAXRECURSE= 5;

static int solve_sub(LightupState state,
                     uint solve_flags, int depth,
                     ref int maxdepth)
{
    uint flags;
    int x, y, ncanplace, lights;
    bool didstuff;
    int bestx, besty, n, bestn, copy_soluble, self_soluble, ret, maxrecurse = 0;
    LightupState scopy;
    LlData lld = new LlData();
    setscratch[] sscratch = null;

#if SOLVER_DIAGNOSTICS
    printf("solve_sub: depth = %d\n", depth);
#endif
    if (maxdepth < depth) maxdepth = depth;
    if ((solve_flags & F_SOLVE_ALLOWRECURSE) != 0) maxrecurse = MAXRECURSE;

    while (true) {
        if (grid_overlap(state)) {
            /* Our own solver, from scratch, should never cause this to happen
             * (assuming a soluble grid). However, if we're trying to solve
             * from a half-completed *incorrect* grid this might occur; we
             * just return the 'no solutions' code in this case. */
            ret = 0; goto done;
        }

        if (grid_correct(state)) { ret = 1; goto done; }

        ncanplace = 0;
        didstuff = false;
        /* These 2 loops, and the functions they call, are the critical loops
         * for timing; any optimisations should look here first. */
        for (x = 0; x < state.w; x++) {
            for (y = 0; y < state.h; y++) {
                flags = state.flags[x,y];
                lights = state.lights[x,y];
                ncanplace += could_place_light(flags, lights) ? 1 : 0;

                if (try_solve_light(state, x, y, flags, lights)) didstuff = true;
                if (try_solve_number(state, x, y, flags, lights)) didstuff = true;
            }
        }
        if (didstuff) continue;
        if (ncanplace == 0) {
            /* nowhere to put a light, puzzle is unsoluble. */
            ret = 0; goto done;
        }

        if ((solve_flags & F_SOLVE_DISCOUNTSETS) != 0) {

            if (sscratch == null) sscratch = Enumerable.Range(0, SCRATCHSZ(state)).Select(i => new setscratch()).ToArray(); //new setscratch[SCRATCHSZ(state)];
            /* Try a more cunning (and more involved) way... more details above. */
            for (x = 0; x < state.w; x++) {
                for (y = 0; y < state.h; y++) {
                    flags = state.flags[x,y];
                    lights = state.lights[x,y];

                    if ((flags & F_BLACK) == 0 && lights == 0) {
                        if (discount_unlit(state, x, y, sscratch)) {
                            didstuff = true;
                            goto reduction_success;
                        }
                    } else if ((flags & F_NUMBERED)!=0) {
                        if (discount_clue(state, x, y, sscratch)) {
                            didstuff = true;
                            goto reduction_success;
                        }
                    }
                }
            }
        }
reduction_success:
        if (didstuff) continue;

        /* We now have to make a guess; we have places to put lights but
         * no definite idea about where they can go. */
        if (depth >= maxrecurse) {
            /* mustn't delve any deeper. */
            ret = -1; goto done;
        }
        /* Of all the squares that we could place a light, pick the one
         * that would light the most currently unlit squares. */
        /* This heuristic was just plucked from the air; there may well be
         * a more efficient way of choosing a square to flip to minimise
         * recursion. */
        bestn = 0;
        bestx = besty = -1; /* suyb */
        for (x = 0; x < state.w; x++) {
            for (y = 0; y < state.h; y++) {
                flags = state.flags[x,y];
                lights = state.lights[x,y];
                if (!could_place_light(flags, lights)) continue;

                n = 0;
                list_lights(state, x, y, true, lld);
                FOREACHLIT(lld, (lx, ly) => { if (state.lights[lx,ly] == 0) n++; });
                if (n > bestn) {
                    bestn = n; bestx = x; besty = y;
                }
            }
        }
        Debug.Assert(bestn > 0);
	Debug.Assert(bestx >= 0 && besty >= 0);

        /* Now we've chosen a plausible (x,y), try to solve it once as 'lit'
         * and once as 'impossible'; we need to make one copy to do this. */

        scopy = dup_game(state);
#if SOLVER_DIAGNOSTICS
        debug(("Recursing #1: trying (%d,%d) as IMPOSSIBLE\n", bestx, besty));
#endif
        state.flags[bestx,besty] |= F_IMPOSSIBLE;
        self_soluble = solve_sub(state, solve_flags,  depth+1, ref maxdepth);

        if ((solve_flags & F_SOLVE_FORCEUNIQUE) == 0 && self_soluble > 0) {
            /* we didn't care about finding all solutions, and we just
             * found one; return with it immediately. */
            ret = self_soluble;
            goto done;
        }

#if SOLVER_DIAGNOSTICS
        debug(("Recursing #2: trying (%d,%d) as LIGHT\n", bestx, besty));
#endif
        set_light(scopy, bestx, besty, true);
        copy_soluble = solve_sub(scopy, solve_flags, depth + 1, ref maxdepth);

        /* If we wanted a unique solution but we hit our recursion limit
         * (on either branch) then we have to assume we didn't find possible
         * extra solutions, and return 'not soluble'. */
        if (((solve_flags & F_SOLVE_FORCEUNIQUE)!=0) &&
            ((copy_soluble < 0) || (self_soluble < 0))) {
            ret = -1;
        /* Make sure that whether or not it was self or copy (or both) that
         * were soluble, that we return a solved state in self. */
        } else if (copy_soluble <= 0) {
            /* copy wasn't soluble; keep self state and return that result. */
            ret = self_soluble;
        } else if (self_soluble <= 0) {
            /* copy solved and we didn't, so copy in copy's (now solved)
             * flags and light state. */
            Array.Copy(scopy.lights, state.lights, scopy.w * scopy.h);
            Array.Copy(scopy.flags, state.flags, scopy.w * scopy.h);
            ret = copy_soluble;
        } else {
            ret = copy_soluble + self_soluble;
        }
        goto done;
    }
done:
#if SOLVER_DIAGNOSTICS
    if (ret < 0)
        debug(("solve_sub: depth = %d returning, ran out of recursion.\n",
               depth));
    else
        debug(("solve_sub: depth = %d returning, %d solutions.\n",
               depth, ret));
#endif
    return ret;
}

/* Fills in the (possibly partially-complete) game_state as far as it can,
 * returning the number of possible solutions. If it returns >0 then the
 * game_state will be in a solved state, but you won't know which one. */
static int dosolve(LightupState state, uint solve_flags, ref int maxdepth)
{
    int x, y, nsol;

    for (x = 0; x < state.w; x++) {
        for (y = 0; y < state.h; y++) {
            state.flags[x,y] &= ~F_NUMBERUSED;
        }
    }
    nsol = solve_sub(state, solve_flags, 0, ref maxdepth);
    return nsol;
}

static int strip_unused_nums(LightupState state)
{
    int x,y,n=0;
    for (x = 0; x < state.w; x++) {
        for (y = 0; y < state.h; y++) {
            if (((state.flags[x,y] & F_NUMBERED) != 0) &&
                (state.flags[x,y] & F_NUMBERUSED) == 0) {
                state.flags[x,y] &= ~F_NUMBERED;
                state.lights[x,y] = 0;
                n++;
            }
        }
    }
    Debug.WriteLine("Stripped {0} unused numbers.", n);
    return n;
}

static void unplace_lights(LightupState state)
{
    int x,y;
    for (x = 0; x < state.w; x++) {
        for (y = 0; y < state.h; y++) {
            if ((state.flags[x,y] & F_LIGHT) != 0)
                set_light(state,x,y,false);
            state.flags[x,y] &= ~F_IMPOSSIBLE;
            state.flags[x,y] &= ~F_NUMBERUSED;
        }
    }
}

static bool puzzle_is_good(LightupState state, int difficulty)
{
    int nsol, mdepth = 0;
    uint sflags = flags_from_difficulty(difficulty);

    unplace_lights(state);

#if SOLVER_DIAGNOSTICS
    debug(("Trying to solve with difficulty %d (0x%x):\n",
           difficulty, sflags));
    if (verbose) debug_state(state);
#endif

    nsol = dosolve(state, sflags, ref mdepth);
    /* if we wanted an easy puzzle, make sure we didn't need recursion. */
    if ((sflags & F_SOLVE_ALLOWRECURSE) == 0 && mdepth > 0) {
        Debug.WriteLine("Ignoring recursive puzzle.");
        return false;
    }

    Debug.WriteLine("{0} solutions found.", nsol);
    if (nsol <= 0) return false;
    if (nsol > 1) return false;
    return true;
}

/* --- New game creation and user input code. --- */

/* The basic algorithm here is to generate the most complex grid possible
 * while honouring two restrictions:
 *
 *  * we require a unique solution, and
 *  * either we require solubility with no recursion (!@params.recurse)
 *  * or we require some recursion. (@params.recurse).
 *
 * The solver helpfully keeps track of the numbers it needed to use to
 * get its solution, so we use that to remove an initial set of numbers
 * and check we still satsify our requirements (on uniqueness and
 * non-recursiveness, if applicable; we don't check explicit recursiveness
 * until the end).
 *
 * Then we try to remove all numbers in a random order, and see if we
 * still satisfy requirements (putting them back if we didn't).
 *
 * Removing numbers will always, in general terms, make a puzzle require
 * more recursion but it may also mean a puzzle becomes non-unique.
 *
 * Once we're done, if we wanted a recursive puzzle but the most difficult
 * puzzle we could come up with was non-recursive, we give up and try a new
 * grid. */

const int MAX_GRIDGEN_TRIES =20;

public override string GenerateNewGameDescription(LightupSettings @params_in, Random rs, out string aux, int interactive)
{
    LightupSettings @params = @params_in.Clone();
    LightupState news = new_state(@params), copys;
    int i, j, x, y, wh = @params.w*@params.h, num;
    int [] numindices;
    int run;
    StringBuilder ret;

    /* Construct a shuffled list of grid positions; we only
     * do this once, because if it gets used more than once it'll
     * be on a different grid layout. */
    numindices = new int[wh];
    for (j = 0; j < wh; j++) numindices[j] = j;
    numindices.Shuffle( wh, rs);

    while (true) {
        for (i = 0; i < MAX_GRIDGEN_TRIES; i++) {
            set_blacks(news, @params, rs); /* also cleans board. */

            /* set up lights and then the numbers, and remove the lights */
            place_lights(news, rs);
            Debug.WriteLine("Generating initial grid.");
            place_numbers(news);
            if (!puzzle_is_good(news, @params.difficulty)) continue;

            /* Take a copy, remove numbers we didn't use and check there's
             * still a unique solution; if so, use the copy subsequently. */
            copys = dup_game(news);
            strip_unused_nums(copys);
            if (!puzzle_is_good(copys, @params.difficulty)) {
                Debug.WriteLine("Stripped grid is not good, reverting.");
            } else {
                news = copys;
            }

            /* Go through grid removing numbers at random one-by-one and
             * trying to solve again; if it ceases to be good put the number back. */
            for (j = 0; j < wh; j++) {
                y = numindices[j] / @params.w;
                x = numindices[j] % @params.w;
                if ((news.flags[ x, y] & F_NUMBERED) == 0) continue;
                num = news.lights[ x, y];
                news.lights[ x, y] = 0;
                news.flags[ x, y] &= ~F_NUMBERED;
                if (!puzzle_is_good(news, @params.difficulty)) {
                    news.lights[ x, y] = num;
                    news.flags[ x, y] |= F_NUMBERED;
                } else
                    Debug.WriteLine("Removed ({0},{1}) still soluble.", x, y);
            }
            if (@params.difficulty > 0) {
                /* Was the maximally-difficult puzzle difficult enough?
                 * Check we can't solve it with a more simplistic solver. */
                if (puzzle_is_good(news, @params.difficulty-1)) {
                    Debug.WriteLine("Maximally-hard puzzle still not hard enough, skipping.");
                    continue;
                }
            }

            goto goodpuzzle;
        }
        /* Couldn't generate a good puzzle in however many goes. Ramp up the
         * %age of black squares (if we didn't already have lots; in which case
         * why couldn't we generate a puzzle?) and try again. */
        if (@params.blackpc < 90) @params.blackpc += 5;
        Debug.WriteLine("New black layout {0}%.\n", @params.blackpc);
    }
goodpuzzle:
    /* Game is encoded as a long string one character per square;
     * 'S' is a space
     * 'B' is a black square with no number
     * '0', '1', '2', '3', '4' is a black square with a number. */
    ret = new StringBuilder();
    run = 0;
    for (y = 0; y < @params.h; y++) {
	for (x = 0; x < @params.w; x++) {
            if ((news.flags[x,y] & F_BLACK)!=0) {
		if (run != 0 ) {
		    ret.Append((char)(('a'-1) + run));
		    run = 0;
		}
                if ((news.flags[x,y] & F_NUMBERED)!=0)
                    ret.Append((char)('0' + news.lights[x,y]));
                else
                    ret.Append('B');
            } else {
		if (run == 26) {
		    ret.Append((char)(('a'-1) + run));
		    run = 0;
		}
		run++;
	    }
        }
    }
    if (run != 0) {
	ret.Append((char)(('a'-1) + run));
	run = 0;
    }
    //Debug.Assert(p - ret <= @params.w * @params.h);
    aux = null;
    return ret.ToString();
}

static string validate_desc(LightupSettings @params, string desc)
{
    int i;
    int pos = 0;
    for (i = 0; i < @params.w*@params.h; i++) {
        if (pos >= desc.Length)
            return "Game description shorter than expected";
        else if (desc[pos] >= '0' && desc[pos] <= '4')
            /* OK */;
        else if (desc[pos] == 'B')
            /* OK */;
        else if (desc[pos] >= 'a' && desc[pos] <= 'z')
            i += desc[pos] - 'a';	       /* and the i++ will add another one */
        else 
            return "Game description contained unexpected character";
        pos++;
    }
    if (desc.Length > pos || i > @params.w * @params.h)
        return "Game description longer than expected";

    return null;
}

public override LightupState CreateNewGameFromDescription(LightupSettings @params, string desc)
{
    LightupState ret = new_state(@params);
    int x,y;
    int run = 0;
    int pos = 0;

    for (y = 0; y < @params.h; y++) {
	for (x = 0; x < @params.w; x++) {
            char c = '\0';

	    if (run == 0) {
		c = desc[pos++];
		Debug.Assert(c != 'S');
		if (c >= 'a' && c <= 'z')
		    run = c - 'a' + 1;
	    }

	    if (run > 0) {
		c = 'S';
		run--;
	    }

            switch (c) {
	      case '0': case '1': case '2': case '3': case '4':
                ret.flags[x,y] |= F_NUMBERED;
                ret.lights[x,y] = (c - '0');
                /* run-on... */
                ret.flags[x,y] |= F_BLACK;
                break;
	      case 'B':
                ret.flags[x,y] |= F_BLACK;
                break;

	      case 'S':
		/* empty square */
                break;

	      default:
                Debug.Assert(false, "Malformed desc.");
		break;
            }
        }
    }
    if (desc.Length > pos) Debug.Assert(false,"Over-long desc.");

    return ret;
}

public override LightupMove CreateSolveGameMove(LightupState state, LightupState currstate, LightupMove ai, out string error)
{
    LightupState solved;
    LightupMove move = new LightupMove();
    int x, y;
    uint oldflags, solvedflags, sflags;
    error = null;
    /* We don't care here about non-unique puzzles; if the
     * user entered one themself then I doubt they care. */

    sflags = F_SOLVE_ALLOWRECURSE | F_SOLVE_DISCOUNTSETS;
    int ignored = 0;

    /* Try and solve from where we are now (for non-unique
     * puzzles this may produce a different answer). */
    solved = dup_game(currstate);
    if (dosolve(solved, sflags, ref ignored) > 0) goto solved;

    /* That didn't work; try solving from the clean puzzle. */
    solved = dup_game(state);
    if (dosolve(solved, sflags, ref ignored) > 0) goto solved;
    error = "Unable to find a solution to this puzzle.";
    goto done;

solved:
    move.isSolve = true;
    for (x = 0; x < currstate.w; x++) {
        for (y = 0; y < currstate.h; y++) {
            oldflags = currstate.flags[x, y];
            solvedflags = solved.flags[x, y];
            if ((oldflags & F_LIGHT) != (solvedflags & F_LIGHT))
                move.points.Add(new LightupPoint(){  f =F_LIGHT, x = x, y = y });
            else if ((oldflags & F_IMPOSSIBLE) != (solvedflags & F_IMPOSSIBLE))
                move.points.Add(new LightupPoint(){  f =F_IMPOSSIBLE, x = x, y = y });
        }
    }

done:
    return move;
}

//static int game_can_format_as_text_now(LightupSettings @params)
//{
//    return TRUE;
//}

///* 'borrowed' from slant.c, mainly. I could have printed it one
// * character per cell (like debug_state) but that comes out tiny.
// * 'L' is used for 'light here' because 'O' looks too much like '0'
// * (black square with no surrounding lights). */
//static string game_text_format(LightupState state)
//{
//    int w = state.w, h = state.h, W = w+1, H = h+1;
//    int x, y, len, lights;
//    uint flags;
//    string ret, *p;

//    len = (h+H) * (w+W+1) + 1;
//    ret = snewn(len, char);
//    p = ret;

//    for (y = 0; y < H; y++) {
//        for (x = 0; x < W; x++) {
//            *p++ = '+';
//            if (x < w)
//                *p++ = '-';
//        }
//        *p++ = '\n';
//        if (y < h) {
//            for (x = 0; x < W; x++) {
//                *p++ = '|';
//                if (x < w) {
//                    /* actual interesting bit. */
//                    flags = state.flags[ x, y];
//                    lights = state.lights[ x, y];
//                    if (flags & F_BLACK) {
//                        if (flags & F_NUMBERED)
//                            *p++ = '0' + lights;
//                        else
//                            *p++ = '#';
//                    } else {
//                        if (flags & F_LIGHT)
//                            *p++ = 'L';
//                        else if (flags & F_IMPOSSIBLE)
//                            *p++ = 'x';
//                        else if (lights > 0)
//                            *p++ = '.';
//                        else
//                            *p++ = ' ';
//                    }
//                }
//            }
//            *p++ = '\n';
//        }
//    }
//    *p++ = '\0';

//    Debug.Assert(p - ret == len);
//    return ret;
//}

public override LightupUI CreateUI(LightupState state)
{
    LightupUI ui = new LightupUI();
    ui.cur_x = ui.cur_y = 0;
    ui.cur_visible = false;
    return ui;
}

static void game_changed_state(LightupUI ui, LightupState oldstate,
                               LightupState newstate)
{
    if (newstate.completed)
        ui.cur_visible = false;
}

const int DF_BLACK        =1;       /* black square */
const int DF_NUMBERED     =2;       /* black square with number */
const int DF_LIT          =4;       /* display (white) square lit up */
const int DF_LIGHT        =8;       /* display light in square */
const int DF_OVERLAP      =16;      /* display light as overlapped */
const int DF_CURSOR       =32;      /* display cursor */
const int DF_NUMBERWRONG  =64;      /* display black numbered square as error. */
const int DF_FLASH        =128;     /* background flash is on. */
const int DF_IMPOSSIBLE   =256;     /* display non-light little square */


/* Believe it or not, this empty = "" hack is needed to get around a bug in
 * the prc-tools gcc when optimisation is turned on; before, it produced:
    lightup-sect.c: In function `interpret_move':
    lightup-sect.c:1416: internal error--unrecognizable insn:
    (insn 582 580 583 (set (reg:SI 134)
            (pc)) -1 (nil)
        (nil))
 */

internal override void SetKeyboardCursorVisible(LightupUI ui, int tileSize, bool value)
{
    ui.cur_visible = value;
}
public override LightupMove InterpretMove(LightupState state, LightupUI ui, LightupDrawState ds, int x, int y, Buttons button, bool isTouchOrStylus)
{
    const int NONE = 0;
    const int FLIP_LIGHT = 1;
    const int FLIP_IMPOSSIBLE = 2;
    //enum { NONE, FLIP_LIGHT, FLIP_IMPOSSIBLE } action = NONE;
    int action = NONE;
    int cx = -1, cy = -1;
    uint flags;

    if (button == Buttons.LEFT_BUTTON || button == Buttons.RIGHT_BUTTON) {
        ui.cur_visible = false;
        cx = FROMCOORD(ds,x);
        cy = FROMCOORD(ds,y);
        action = (button == Buttons.LEFT_BUTTON) ? FLIP_LIGHT : FLIP_IMPOSSIBLE;
    }
    else if (Misc.IS_CURSOR_SELECT(button)) {
        if (ui.cur_visible) {
            /* Only allow cursor-effect operations if the cursor is visible
             * (otherwise you have no idea which square it might be affecting) */
            cx = ui.cur_x;
            cy = ui.cur_y;
            action = (button == Buttons.CURSOR_SELECT2) ?
                FLIP_IMPOSSIBLE : FLIP_LIGHT;
        }
        ui.cur_visible = true;
               }
    else if (Misc.IS_CURSOR_MOVE(button))
    {
        Misc.move_cursor(button, ref ui.cur_x, ref ui.cur_y, state.w, state.h, false);
        ui.cur_visible = true;
    } else
        return null;

    char c;
    switch (action) {
    case FLIP_LIGHT:
    case FLIP_IMPOSSIBLE:
        if (cx < 0 || cy < 0 || cx >= state.w || cy >= state.h)
            return null;
        flags = state.flags[ cx, cy];
        if ((flags & F_BLACK)!=0)
            return null;
        if (action == FLIP_LIGHT) {
            if (isTouchOrStylus){
                if ((flags & F_IMPOSSIBLE) != 0 || (flags & F_LIGHT) !=0 ) c = 'I'; else c = 'L';
            }else{
                if ((flags & F_IMPOSSIBLE)!=0) return null;
                c = 'L';
            }
        } else {
            if (isTouchOrStylus){
                if ((flags & F_IMPOSSIBLE) != 0 || (flags & F_LIGHT) != 0) c = 'L'; else c = 'I';
            }else{
            if ((flags & F_LIGHT)!=0) return null;
            c = 'I';
            }
        }
        LightupMove move = new LightupMove();
        move.points.Add(new LightupPoint() {
            x = cx,
            y = cy,
            f = c == 'L' ? F_LIGHT : F_IMPOSSIBLE
        });
        return move;

    case NONE:
        return null;

    //default:
    //    Debug.Assert(false,"Shouldn't get here!");
    }
    return null;
}

public override LightupState ExecuteMove(LightupState state, LightupMove move)
{
    LightupState ret = dup_game(state);
    int x, y;
    uint flags;

    if (move.isSolve)
    {
        ret.used_solve = true;
    }

    foreach (var point in move.points)
    {
        x = point.x;
        y = point.y; 
        flags = ret.flags[x, y];
        if ((flags & F_BLACK) != 0) goto badmove;

        /* LIGHT and IMPOSSIBLE are mutually exclusive. */
        if (point.f == F_LIGHT)
        {
            ret.flags[x, y] &= ~F_IMPOSSIBLE;
            set_light(ret, x, y, (flags & F_LIGHT) != 0 ? false : true);
        }
        else
        {
            set_light(ret, x, y, false);
            ret.flags[x, y] ^= F_IMPOSSIBLE;
        }
    }

    if (grid_correct(ret)) ret.completed = true;
    return ret;

badmove:
    return null;
}

/* ----------------------------------------------------------------------
 * Drawing routines.
 */

/* XXX entirely cloned from fifteen.c; separate out? */

public override void ComputeSize(LightupSettings @params, int tilesize, out int x, out int y)
{
    /* Ick: fake up `ds.tilesize' for macro expansion purposes */
    LightupDrawState ds = new LightupDrawState(){ tilesize = tilesize };

    x = TILE_SIZE(ds) * @params.w + 2 * BORDER(ds);
    y = TILE_SIZE(ds) * @params.h + 2 * BORDER(ds);
}


public override void SetTileSize(Drawing dr, LightupDrawState ds, LightupSettings @params, int tilesize)
{
    ds.tilesize = tilesize;
    ds.crad = 3*(tilesize-1)/8;
}

public override float[] GetColours(Frontend fe, out int ncolours)
{
    float[] ret = new float[3 * NCOLOURS];
    int i;

    fe.frontend_default_colour(ret, COL_BACKGROUND * 3);

    for (i = 0; i < 3; i++) {
        ret[COL_BLACK * 3 + i] = 0.0F;
        ret[COL_LIGHT * 3 + i] = 1.0F;
        ret[COL_CURSOR * 3 + i] = ret[COL_BACKGROUND * 3 + i] / 2.0F;
        ret[COL_GRID * 3 + i] = ret[COL_BACKGROUND * 3 + i] / 1.5F;

    }

    ret[COL_ERROR * 3 + 0] = 1.0F;
    ret[COL_ERROR * 3 + 1] = 0.25F;
    ret[COL_ERROR * 3 + 2] = 0.25F;

    ret[COL_LIT * 3 + 0] = 1.0F;
    ret[COL_LIT * 3 + 1] = 1.0F;
    ret[COL_LIT * 3 + 2] = 0.0F;

    ncolours = NCOLOURS;
    return ret;
}

public override LightupDrawState CreateDrawState(Drawing dr, LightupState state)
{
    LightupDrawState ds = new LightupDrawState();

    ds.tilesize = ds.crad = 0;
    ds.w = state.w; ds.h = state.h;

    ds.flags = new uint[ds.w,ds.h];
    for (int i = 0; i < ds.w; i++)
        for (int j = 0; j < ds.h; j++)
            ds.flags[i,j] = uint.MaxValue;

    ds.started = false;

    return ds;
}

static uint tile_flags(LightupDrawState ds, LightupState state,
                               LightupUI ui, int x, int y, bool flashing)
{
    uint flags = state.flags[ x, y];
    int lights = state.lights[ x, y];
    uint ret = 0;

    if (flashing) ret |= DF_FLASH;
    if (ui != null && ui.cur_visible && x == ui.cur_x && y == ui.cur_y)
        ret |= DF_CURSOR;

    if ((flags & F_BLACK)!=0) {
        ret |= DF_BLACK;
        if ((flags & F_NUMBERED)!=0) {
#if HINT_NUMBERS
            if (number_wrong(state, x, y))
		ret |= DF_NUMBERWRONG;
#endif
            ret |= DF_NUMBERED;
        }
    } else {
#if HINT_LIGHTS
        if (lights > 0) ret |= DF_LIT;
#endif
        if ((flags & F_LIGHT)!=0) {
            ret |= DF_LIGHT;
#if HINT_OVERLAPS
            if (lights > 1) ret |= DF_OVERLAP;
#endif
        }
        if ((flags & F_IMPOSSIBLE)!=0) ret |= DF_IMPOSSIBLE;
    }
    return ret;
}

static void tile_redraw(Drawing dr, LightupDrawState ds,
                        LightupState state, int x, int y)
{
    uint ds_flags = ds.flags[ x, y];
    int dx = COORD(ds,x), dy = COORD(ds,y);
    int lit = (ds_flags & DF_FLASH) != 0 ? COL_GRID : COL_LIT;

    if ((ds_flags & DF_BLACK)!=0) {
        dr.draw_rect(dx, dy, TILE_SIZE(ds), TILE_SIZE(ds), COL_BLACK);
        if ((ds_flags & DF_NUMBERED) !=0) {
            int ccol = (ds_flags & DF_NUMBERWRONG) !=0 ? COL_ERROR : COL_LIGHT;

            /* We know that this won't change over the course of the game
             * so it's OK to ignore this when calculating whether or not
             * to redraw the tile. */
            dr.draw_text(dx + TILE_SIZE(ds)/2, dy + TILE_SIZE(ds)/2,
                      Drawing.FONT_VARIABLE, TILE_SIZE(ds)*3/5,
              Drawing.ALIGN_VCENTRE | Drawing.ALIGN_HCENTRE, ccol, state.lights[x, y].ToString());
        }
    } else {
        dr.draw_rect(dx, dy, TILE_SIZE(ds), TILE_SIZE(ds),
                  (ds_flags & DF_LIT) !=0 ? lit : COL_BACKGROUND);
        dr.draw_rect_outline(dx, dy, TILE_SIZE(ds), TILE_SIZE(ds), COL_GRID);
        if ((ds_flags & DF_LIGHT) != 0) {
            int lcol = (ds_flags & DF_OVERLAP) != 0 ? COL_ERROR : COL_LIGHT;
            dr.draw_circle(dx + TILE_SIZE(ds)/2, dy + TILE_SIZE(ds)/2, TILE_RADIUS(ds),
                        lcol, COL_BLACK);
        } else if ((ds_flags & DF_IMPOSSIBLE) != 0) {
            bool draw_blobs_when_lit = true;
        //    if (draw_blobs_when_lit < 0) {
        //string env = getenv("LIGHTUP_LIT_BLOBS");
        //draw_blobs_when_lit = (!env || (env[0] == 'y' ||
        //                                        env[0] == 'Y'));
        //    }
            if ((ds_flags & DF_LIT) == 0 || draw_blobs_when_lit) {
                int rlen = TILE_SIZE(ds) / 4;
                dr.draw_rect(dx + TILE_SIZE(ds)/2 - rlen/2,
                          dy + TILE_SIZE(ds)/2 - rlen/2,
                          rlen, rlen, COL_BLACK);
            }
        }
    }

    if ((ds_flags & DF_CURSOR)!=0) {
        int coff = TILE_SIZE(ds)/8;
        dr.draw_rect_outline(dx + coff, dy + coff,
                          TILE_SIZE(ds) - coff*2, TILE_SIZE(ds) - coff*2, COL_CURSOR);
    }

    dr.draw_update(dx, dy, TILE_SIZE(ds), TILE_SIZE(ds));
}

public override void Redraw(Drawing dr, LightupDrawState ds, LightupState oldstate, LightupState state, int dir, LightupUI ui, float animtime, float flashtime)
{
    bool flashing = false;
    int x,y;

    if (flashtime != 0) flashing = (int)(flashtime * 3 / FLASH_TIME) != 1;

    if (!ds.started) {
        dr.draw_rect(0, 0,
                  TILE_SIZE(ds) * ds.w + 2 * BORDER(ds),
                  TILE_SIZE(ds) * ds.h + 2 * BORDER(ds), COL_BACKGROUND);

        dr.draw_rect_outline(COORD(ds,0)-1, COORD(ds,0)-1,
                          TILE_SIZE(ds) * ds.w + 2,
                          TILE_SIZE(ds) * ds.h + 2,
                          COL_GRID);

        dr.draw_update(0, 0,
                    TILE_SIZE(ds) * ds.w + 2 * BORDER(ds),
                    TILE_SIZE(ds) * ds.h + 2 * BORDER(ds));
        ds.started = true;
    }

    for (x = 0; x < ds.w; x++) {
        for (y = 0; y < ds.h; y++) {
            uint ds_flags = tile_flags(ds, state, ui, x, y, flashing);
            if (ds_flags != ds.flags[x, y]) {
                ds.flags[ x, y] = ds_flags;
                tile_redraw(dr, ds, state, x, y);
            }
        }
    }
}

public override float CompletedFlashDuration(LightupSettings settings)
{

        return FLASH_TIME;
    
}

//static float game_anim_length(LightupState oldstate,
//                              LightupState newstate, int dir, LightupUI ui)
//{
//    return 0.0F;
//}

//static float game_flash_length(LightupState oldstate,
//                               LightupState newstate, int dir, LightupUI ui)
//{
//    if (!oldstate.completed && newstate.completed &&
//        !oldstate.used_solve && !newstate.used_solve)
//        return FLASH_TIME;
//    return 0.0F;
//}

//static int game_status(LightupState state)
//{
//    return state.completed ? +1 : 0;
//}

//static int game_timing_state(LightupState state, LightupUI ui)
//{
//    return TRUE;
//}





    }
}
