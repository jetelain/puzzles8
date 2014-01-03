using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.SignPost
{
    public sealed class SignPostGame : GameBase<SignPostSettings,SignPostState,SignPostMove,SignPostDrawState,SignPostUI>
    {
        private const int  PREFERRED_TILE_SIZE= 48;
private static int TILE_SIZE(SignPostDrawState ds) { return (ds.tilesize);}
private static int BLITTER_SIZE(SignPostDrawState ds){ return  TILE_SIZE(ds);}
private static int BORDER(SignPostDrawState ds) { return    (TILE_SIZE(ds) / 2);}

private static int COORD(SignPostDrawState ds, int x) { return  ( (x) * TILE_SIZE(ds) + BORDER(ds) );}
private static int FROMCOORD(SignPostDrawState ds,int x) { return  ( ((x) - BORDER(ds) + TILE_SIZE(ds)) / TILE_SIZE(ds) - 1 );}

private static bool INGRID(SignPostState s,int x,int y){ return  ((x) >= 0 && (x) < (s).w && (y) >= 0 && (y) < (s).h);}

private const float FLASH_SPIN= 0.7F;

private const int NBACKGROUNDS= 16;
private const int COL_BACKGROUND=0; 
private const int COL_HIGHLIGHT=1;
private const int COL_LOWLIGHT=2;
private const int COL_GRID=3;
private const int COL_CURSOR=4; 
private const int COL_ERROR=5;
private const int COL_DRAG_ORIGIN=6;
private const int COL_ARROW=7;
private const int COL_ARROW_BG_DIM=8;
private const int COL_NUMBER=9;
private const int COL_NUMBER_SET=10; 
private const int COL_NUMBER_SET_MID=11;
private const int COL_B0=12;                             /* background colours */
private const int COL_M0 =   COL_B0 + 1*NBACKGROUNDS; /* mid arrow colours */
private const int COL_D0 =   COL_B0 + 2*NBACKGROUNDS; /* dim arrow colours */
private const int COL_X0 =   COL_B0 + 3*NBACKGROUNDS; /* dim arrow colours */
private const int NCOLOURS = COL_B0 + 4*NBACKGROUNDS;


static string[] dirstrings = new[]{ "N ", "NE", "E ", "SE", "S ", "SW", "W ", "NW" };

static readonly int[] dxs = new int[]{  0,  1, 1, 1, 0, -1, -1, -1 };
static readonly int[] dys = new int[]{ -1, -1, 0, 1, 1,  1,  0, -1 };

private static SignPostDir SignPostDir_OPPOSITE( SignPostDir d) { return (SignPostDir)(((int)d+4)%8);}

const int FLAG_IMMUTABLE = 1;
const int  FLAG_ERROR    =  2;

/* --- Generally useful functions --- */

private static bool ISREALNUM(SignPostState state, int num) { return ((num) > 0 && (num) <= (state).n); }

static int whichdir(int fromx, int fromy, int tox, int toy)
{
    int i, dx, dy;

    dx = tox - fromx;
    dy = toy - fromy;

    if (dx != 0 && dy != 0 && Math.Abs(dx) != Math.Abs(dy)) return -1;

    if (dx != 0) dx = dx / Math.Abs(dx); /* limit to (-1, 0, 1) */
    if (dy != 0) dy = dy / Math.Abs(dy); /* ditto */

    for (i = 0; i < (int)SignPostDir.MAX; i++) {
        if (dx == dxs[i] && dy == dys[i]) return i;
    }
    return -1;
}

static int whichdiri(SignPostState state, int fromi, int toi)
{
    int w = state.w;
    return whichdir(fromi%w, fromi/w, toi%w, toi/w);
}

static bool ispointing(SignPostState state, int fromx, int fromy,
                      int tox, int toy)
{
    int w = state.w, dir = state.dirs[fromy*w+fromx];

    /* (by convention) squares do not point to themselves. */
    if (fromx == tox && fromy == toy) return false;

    /* the final number points to nothing. */
    if (state.nums[fromy*w + fromx] == state.n) return false;

    while (true) {
        if (!INGRID(state, fromx, fromy)) return false;
        if (fromx == tox && fromy == toy) return true;
        fromx += dxs[dir]; fromy += dys[dir];
    }
    return false; /* not reached */
}

static bool ispointingi(SignPostState state, int fromi, int toi)
{
    int w = state.w;
    return ispointing(state, fromi%w, fromi/w, toi%w, toi/w);
}

/* Taking the number 'num', work out the gap between it and the next
 * available number up or down (depending on d). Return 1 if the region
 * at (x,y) will fit in that gap, or 0 otherwise. */
static bool move_couldfit(SignPostState state, int num, int d, int x, int y)
{
    int n, gap, i = y*state.w+x, sz;

    Debug.Assert(d != 0);
    /* The 'gap' is the number of missing numbers in the grid between
     * our number and the next one in the sequence (up or down), or
     * the end of the sequence (if we happen not to have 1/n present) */
    for (n = num + d, gap = 0;
         ISREALNUM(state, n) && state.numsi[n] == -1;
         n += d, gap++) ; /* empty loop */

    if (gap == 0) {
        /* no gap, so the only allowable move is that that directly
         * links the two numbers. */
        n = state.nums[i];
        return (n == num+d) ? false : true;
    }
    if (state.prev[i] == -1 && state.next[i] == -1)
        return true; /* single unconnected square, always OK */

    sz = Dsf.dsf_size(state.dsf, i);
    return (sz > gap) ? false : true;
}

static bool isvalidmove(SignPostState state, bool clever,
                       int fromx, int fromy, int tox, int toy)
{
    int w = state.w, from = fromy*w+fromx, to = toy*w+tox;
    int nfrom, nto;

    if (!INGRID(state, fromx, fromy) || !INGRID(state, tox, toy))
        return false;

    /* can only move where we point */
    if (!ispointing(state, fromx, fromy, tox, toy))
        return false;

    nfrom = state.nums[from]; nto = state.nums[to];

    /* can't move _from_ the preset final number, or _to_ the preset 1. */
    if (((nfrom == state.n) && (state.flags[from] & FLAG_IMMUTABLE) !=0) ||
        ((nto == 1) && (state.flags[to] & FLAG_IMMUTABLE) != 0))
        return false;

    /* can't create a new connection between cells in the same region
     * as that would create a loop. */
    if (Dsf.dsf_canonify(state.dsf, from) == Dsf.dsf_canonify(state.dsf, to))
        return false;

    /* if both cells are actual numbers, can't drag if we're not
     * one digit apart. */
    if (ISREALNUM(state, nfrom) && ISREALNUM(state, nto)) {
        if (nfrom != nto-1)
            return false;
    } else if (clever && ISREALNUM(state, nfrom)) {
        if (!move_couldfit(state, nfrom, +1, tox, toy))
            return false;
    } else if (clever && ISREALNUM(state, nto)) {
        if (!move_couldfit(state, nto, -1, fromx, fromy))
            return false;
    }

    return true;
}

static void makelink(SignPostState state, int from, int to)
{
    if (state.next[from] != -1)
        state.prev[state.next[from]] = -1;
    state.next[from] = to;

    if (state.prev[to] != -1)
        state.next[state.prev[to]] = -1;
    state.prev[to] = from;
}

static int game_can_format_as_text_now(SignPostSettings @params)
{
    if (@params.w * @params.h >= 100) return 0;
    return 1;
}

//static string game_text_format(SignPostState state)
//{
//    int len = state.h * 2 * (4*state.w + 1) + state.h + 2;
//    int x, y, i, num, n, set;
//    string ret, p;

//    p = ret = new  char[len];

//    for (y = 0; y < state.h; y++) {
//        for (x = 0; x < state.h; x++) {
//            i = y*state.w+x;
//            *p++ = dirstrings[state.dirs[i]][0];
//            *p++ = dirstrings[state.dirs[i]][1];
//            *p++ = (state.flags[i] & FLAG_IMMUTABLE) ? 'I' : ' ';
//            *p++ = ' ';
//        }
//        *p++ = '\n';
//        for (x = 0; x < state.h; x++) {
//            i = y*state.w+x;
//            num = state.nums[i];
//            if (num == 0) {
//                *p++ = ' ';
//                *p++ = ' ';
//                *p++ = ' ';
//            } else {
//                n = num % (state.n+1);
//                set = num / (state.n+1);

//                Debug.Assert(n <= 99); /* two digits only! */

//                if (set != 0)
//                    *p++ = set+'a'-1;

//                *p++ = (n >= 10) ? ('0' + (n/10)) : ' ';
//                *p++ = '0' + (n%10);

//                if (set == 0)
//                    *p++ = ' ';
//            }
//            *p++ = ' ';
//        }
//        *p++ = '\n';
//        *p++ = '\n';
//    }
//    *p++ = '\0';

//    return ret;
//}

static void debug_state(string desc, SignPostState state)
{
#if DEBUGGING
    string dbg;
    if (state.n >= 100) {
        Debug.WriteLine("[ no game_text_format for this size ]"));
        return;
    }
    dbg = game_text_format(state);
    Debug.WriteLine("%s\n%s", desc, dbg));
    sfree(dbg);
#endif
}


static void strip_nums(SignPostState state) {
    int i;
    for (i = 0; i < state.n; i++) {
        if ((state.flags[i] & FLAG_IMMUTABLE)==0)
            state.nums[i] = 0;
    }
    state.next.SetAll(-1);
    state.prev.SetAll(-1);
    state.numsi.SetAll(-1);
    Dsf.dsf_init(state.dsf, state.n);
}

static int check_nums(SignPostState orig, SignPostState copy, bool only_immutable)
{
    int i, ret = 1;
    Debug.Assert(copy.n == orig.n);
    for (i = 0; i < copy.n; i++) {
        if (only_immutable && (copy.flags[i] & FLAG_IMMUTABLE)==0) continue;
        Debug.Assert(copy.nums[i] >= 0);
        Debug.Assert(copy.nums[i] <= copy.n);
        if (copy.nums[i] != orig.nums[i]) {
            Debug.WriteLine("check_nums: (%d,%d) copy=%d, orig=%d.",
                   i%orig.w, i/orig.w, copy.nums[i], orig.nums[i]);
            ret = 0;
        }
    }
    return ret;
}

/* --- Game parameter/presets functions --- */


static readonly SignPostSettings[] signpost_presets = {
  new SignPostSettings( 3, 3, true ),
  new SignPostSettings( 4, 4, true ), // <= Default !
  new SignPostSettings( 4, 4, false ),
  new SignPostSettings( 5, 5, true ),
  new SignPostSettings( 5, 5, false ),
  new SignPostSettings( 6, 6, true ),
  new SignPostSettings( 7, 7, true )
};

public override IEnumerable<SignPostSettings> PresetsSettings
{
    get { return signpost_presets; }
}

public override SignPostSettings DefaultSettings
{
    get { return signpost_presets[1]; }
}

public override SignPostSettings ParseSettings(string settingsString)
{
    return SignPostSettings.Parse(settingsString);
}
/* --- Game description string generation and unpicking --- */

static void blank_game_into(SignPostState state)
{
    state.dirs.SetAll(0);
    state.nums.SetAll(0);
    state.flags.SetAll((uint)0);
    state.next.SetAll(-1);
    state.prev.SetAll(-1);
    state.numsi.SetAll(-1);
}

static SignPostState blank_game(int w, int h)
{
    SignPostState state = new SignPostState();

    state.w = w;
    state.h = h;
    state.n = w*h;

    state.dirs  = new  int[state.n];
    state.nums  = new  int[state.n];
    state.flags = new  uint[state.n];
    state.next  = new  int[state.n];
    state.prev  = new  int[state.n];
    state.dsf = Dsf.snew_dsf(state.n);
    state.numsi  = new  int[state.n+1];

    blank_game_into(state);

    return state;
}

static void dup_game_to(SignPostState to, SignPostState from)
{
    to.completed = from.completed;
    to.used_solve = from.used_solve;
    to.impossible = from.impossible;

    Array.Copy( from.dirs,to.dirs, to.n);
    Array.Copy( from.flags,to.flags, to.n);
    Array.Copy( from.nums,to.nums, to.n);

    Array.Copy( from.next,to.next, to.n);
    Array.Copy( from.prev,to.prev, to.n);

    Array.Copy( from.dsf,to.dsf, to.n);
    Array.Copy( from.numsi,to.numsi, (to.n+1));
}

static SignPostState dup_game(SignPostState state)
{
    SignPostState ret = blank_game(state.w, state.h);
    dup_game_to(ret, state);
    return ret;
}


static void unpick_desc(SignPostSettings @params, string desc,
                        out SignPostState sout, out string mout)
{
    SignPostState state = blank_game(@params.w, @params.h);
    string msg = null;
        char c;
    int num = 0, i = 0;
    int descPos=0;

    while (descPos < desc.Length) {
        if (i >= state.n) {
            msg = "Game description longer than expected";
            goto done;
        }

        c = desc[descPos];
        if (char.IsDigit((char)c)) {
            num = (num*10) + (int)(c-'0');
            if (num > state.n) {
                msg = "Number too large";
                goto done;
            }
        } else if ((c-'a') >= 0 && (c-'a') < (int)SignPostDir.MAX) {
            state.nums[i] = num;
            state.flags[i] = num!=0 ? FLAG_IMMUTABLE : (uint)0;
            num = 0;

            state.dirs[i] = c - 'a';
            i++;
        //} else if (!*desc) {
        //    msg = "Game description shorter than expected";
        //    goto done;
        } else {
            msg = "Game description contains unexpected characters";
            goto done;
        }
        descPos++;
    }
    if (i < state.n) {
        msg = "Game description shorter than expected";
        goto done;
    }

done:
    if (msg!=null) { /* sth went wrong. */
        mout = msg;
		sout = null;
    } else {
        mout = null;
        sout = state;
    }
}

static string generate_desc(SignPostState state, bool issolve)
{
    StringBuilder ret = new StringBuilder();
    int i;

    if (issolve) {
        ret.Append('S');
    }
    for (i = 0; i < state.n; i++) {
        if (state.nums[i]!=0)
            ret.Append(state.nums[i]).Append((char)(state.dirs[i]+'a'));
        else
            ret.Append((char)(state.dirs[i]+'a'));
    }
    return ret.ToString();
}

/* --- Game generation --- */

/* Fills in preallocated arrays ai (indices) and ad (directions)
 * showing all non-numbered cells adjacent to index i, returns length */
/* This function has been somewhat optimised... */
static int cell_adj(SignPostState state, int i, int[] ai, int[] ad)
{
    int n = 0, a, x, y, sx, sy, dx, dy, newi;
    int w = state.w, h = state.h;

    sx = i % w; sy = i / w;

    for (a = 0; a < (int)SignPostDir.MAX; a++) {
        x = sx; y = sy;
        dx = dxs[a]; dy = dys[a];
        while (true) {
            x += dx; y += dy;
            if (x < 0 || y < 0 || x >= w || y >= h) break;

            newi = y*w + x;
            if (state.nums[newi] == 0) {
                ai[n] = newi;
                ad[n] = a;
                n++;
            }
        }
    }
    return n;
}

static int new_game_fill(SignPostState state, Random rs,
                         int headi, int taili)
{
    int nfilled, an, ret = 0, j;
    int[] aidx, adir;

    aidx = new  int[state.n];
    adir = new  int[state.n];

    Debug.WriteLine("new_game_fill: headi=%d, taili=%d.", headi, taili);

    state.nums.SetAll( 0);

    state.nums[headi] = 1;
    state.nums[taili] = state.n;

    state.dirs[taili] = 0;
    nfilled = 2;

    while (nfilled < state.n) {
        /* Try and expand _from_ headi; keep going if there's only one
         * place to go to. */
        an = cell_adj(state, headi, aidx, adir);
        do {
            if (an == 0) goto done;
            j = rs.Next(0, an);
            state.dirs[headi] = adir[j];
            state.nums[aidx[j]] = state.nums[headi] + 1;
            nfilled++;
            headi = aidx[j];
            an = cell_adj(state, headi, aidx, adir);
        } while (an == 1);

        /* Try and expand _to_ taili; keep going if there's only one
         * place to go to. */
        an = cell_adj(state, taili, aidx, adir);
        do {
            if (an == 0) goto done;
            j = rs.Next(0, an);
            state.dirs[aidx[j]] = (int)SignPostDir_OPPOSITE((SignPostDir)adir[j]);
            state.nums[aidx[j]] = state.nums[taili] - 1;
            nfilled++;
            taili = aidx[j];
            an = cell_adj(state, taili, aidx, adir);
        } while (an == 1);
    }
    /* If we get here we have headi and taili set but unconnected
     * by direction: we need to set headi's direction so as to point
     * at taili. */
    state.dirs[headi] = whichdiri(state, headi, taili);

    /* it could happen that our last two weren't in line; if that's the
     * case, we have to start again. */
    if (state.dirs[headi] != -1) ret = 1;

done:
    return ret;
}

/* Better generator: with the 'generate, sprinkle numbers, solve,
 * repeat' algorithm we're _never_ generating anything greater than
 * 6x6, and spending all of our time in new_game_fill (and very little
 * in solve_state).
 *
 * So, new generator steps:
   * generate the grid, at random (same as now). Numbers 1 and N get
      immutable flag immediately.
   * squirrel that away for the solved state.
   *
   * (solve:) Try and solve it.
   * If we solved it, we're done:
     * generate the description from current immutable numbers,
     * free stuff that needs freeing,
     * return description + solved state.
   * If we didn't solve it:
     * count #tiles in state we've made deductions about.
     * while (1):
       * randomise a scratch array.
       * for each index in scratch (in turn):
         * if the cell isn't empty, continue (through scratch array)
         * set number + immutable in state.
         * try and solve state.
         * if we've solved it, we're done.
         * otherwise, count #tiles. If it's more than we had before:
           * good, break from this loop and re-randomise.
         * otherwise (number didn't help):
           * remove number and try next in scratch array.
       * if we've got to the end of the scratch array, no luck:
          free everything we need to, and go back to regenerate the grid.
   */


static void debug_desc(string what, SignPostState state)
{
#if DEBUGGING
    {
        string desc = generate_desc(state, 0);
        Debug.WriteLine("%s game state: %dx%d:%s", what, state.w, state.h, desc));
        sfree(desc);
    }
#endif
}

/* Expects a fully-numbered game_state on input, and makes sure
 * FLAG_IMMUTABLE is only set on those numbers we need to solve
 * (as for a real new-game); returns 1 if it managed
 * this (such that it could solve it), or 0 if not. */
static bool new_game_strip(SignPostState state, Random rs)
{
    int[]scratch;
    int i, j;
    bool ret = true;
    SignPostState copy = dup_game(state);

    Debug.WriteLine("new_game_strip.");

    strip_nums(copy);
    debug_desc("Stripped", copy);

    if (solve_state(copy) > 0) {
        Debug.WriteLine("new_game_strip: soluble immediately after strip.");
        return true;
    }

    scratch = new  int[state.n];
    for (i = 0; i < state.n; i++) scratch[i] = i;
    scratch.Shuffle(state.n, rs);

    /* This is scungy. It might just be quick enough.
     * It goes through, adding set numbers in empty squares
     * until either we run out of empty squares (in the one
     * we're half-solving) or else we solve it properly.
     * NB that we run the entire solver each time, which
     * strips the grid beforehand; we will save time if we
     * avoid that. */
    for (i = 0; i < state.n; i++) {
        j = scratch[i];
        if (copy.nums[j] > 0 && copy.nums[j] <= state.n)
            continue; /* already solved to a real number here. */
        Debug.Assert(state.nums[j] <= state.n);
        Debug.WriteLine("new_game_strip: testing add IMMUTABLE number %d at square (%d,%d).",
               state.nums[j], j%state.w, j/state.w);
        copy.nums[j] = state.nums[j];
        copy.flags[j] |= FLAG_IMMUTABLE;
        state.flags[j] |= FLAG_IMMUTABLE;
        debug_state("Copy of state: ", copy);
        strip_nums(copy);
        if (solve_state(copy) > 0) goto solved;
        Debug.Assert(check_nums(state, copy, true) != 0);
    }
    ret = false;
    goto done;

solved:
    Debug.WriteLine("new_game_strip: now solved.");
    /* Since we added basically at random, try now to remove numbers
     * and see if we can still solve it; if we can (still), really
     * remove the number. Make sure we don't remove the anchor numbers
     * 1 and N. */
    for (i = 0; i < state.n; i++) {
        j = scratch[i];
        if ((state.flags[j] & FLAG_IMMUTABLE) != 0 &&
            (state.nums[j] != 1 && state.nums[j] != state.n)) {
            Debug.WriteLine("new_game_strip: testing remove IMMUTABLE number %d at square (%d,%d).",
                  state.nums[j], j%state.w, j/state.w);
            state.flags[j] = (uint)((int)state.flags[j] & ~FLAG_IMMUTABLE);
            dup_game_to(copy, state);
            strip_nums(copy);
            if (solve_state(copy) > 0) {
                Debug.Assert(check_nums(state, copy, false) != 0);
                Debug.WriteLine("new_game_strip: OK, removing number");
            } else {
                Debug.Assert(state.nums[j] <= state.n);
                Debug.WriteLine("new_game_strip: cannot solve, putting IMMUTABLE back.");
                copy.nums[j] = state.nums[j];
                state.flags[j] |= FLAG_IMMUTABLE;
            }
        }
    }

done:
    Debug.WriteLine("new_game_strip: %ssuccessful.", ret ? "" : "not ");
    return ret;
}

public override string GenerateNewGameDescription(SignPostSettings @params, Random rs, out string aux, int interactive)
{
    SignPostState state = blank_game(@params.w, @params.h);
    string ret;
    int headi, taili;

generate:
    blank_game_into(state);

    /* keep trying until we fill successfully. */
    do {
        if (@params.force_corner_start) {
            headi = 0;
            taili = state.n-1;
        } else {
            do {
                headi = rs.Next(0, state.n);
                taili = rs.Next(0, state.n);
            } while (headi == taili);
        }
    } while (new_game_fill(state, rs, headi, taili) == 0);

    debug_state("Filled game:", state);

    Debug.Assert(state.nums[headi] <= state.n);
    Debug.Assert(state.nums[taili] <= state.n);

    state.flags[headi] |= FLAG_IMMUTABLE;
    state.flags[taili] |= FLAG_IMMUTABLE;

    /* This will have filled in directions and _all_ numbers.
     * Store the game definition for this, as the solved-state. */
    if (!new_game_strip(state, rs)) {
        goto generate;
    }
    strip_nums(state);
    {
        SignPostState tosolve = dup_game(state);
        Debug.Assert(solve_state(tosolve) > 0);
    }
    ret = generate_desc(state, false);
    aux = null;
    return ret;
}

static string validate_desc(SignPostSettings @params, string desc)
{
    string ret = null;
    SignPostState temp;
    unpick_desc(@params, desc, out temp, out ret);
    return ret;
}

/* --- Linked-list and numbers array --- */

/* Assuming numbers are always up-to-date, there are only four possibilities
 * for regions changing after a single valid move:
 *
 * 1) two differently-coloured regions being combined (the resulting colouring
 *     should be based on the larger of the two regions)
 * 2) a numbered region having a single number added to the start (the
 *     region's colour will remain, and the numbers will shift by 1)
 * 3) a numbered region having a single number added to the end (the
 *     region's colour and numbering remains as-is)
 * 4) two unnumbered squares being joined (will pick the smallest unused set
 *     of colours to use for the new region).
 *
 * There should never be any complications with regions containing 3 colours
 * being combined, since two of those colours should have been merged on a
 * previous move.
 *
 * Most of the complications are in ensuring we don't accidentally set two
 * regions with the same colour (e.g. if a region was split). If this happens
 * we always try and give the largest original portion the original colour.
 */

private static int COLOUR(SignPostState state,int a) { return ((a) / (state.n+1)); }
private static int START(SignPostState state, int c) {return ((c) * (state.n+1)); }

class head_meta {
    internal int i;      /* position */
    internal int sz;     /* size of region */
    internal int start;  /* region start number preferred, or 0 if !preference */
    internal int preference; /* 0 if we have no preference (and should just pick one) */
    internal string why;
}

static void head_number(SignPostState state, int i, head_meta head)
{
    int off = 0, ss, j = i, c, n, sz;

    /* Insist we really were passed the head of a chain. */
    Debug.Assert(state.prev[i] == -1 && state.next[i] != -1);

    head.i = i;
    head.sz = Dsf.dsf_size(state.dsf, i);
    head.why = null;

    /* Search through this chain looking for real numbers, checking that
     * they match up (if there are more than one). */
    head.preference = 0;
    while (j != -1) {
        if ((state.flags[j] & FLAG_IMMUTABLE)!=0) {
            ss = state.nums[j] - off;
            if (head.preference==0) {
                head.start = ss;
                head.preference = 1;
                head.why = "contains cell with immutable number";
            } else if (head.start != ss) {
                Debug.WriteLine("head_number: chain with non-sequential numbers!");
                state.impossible = true;
            }
        }
        off++;
        j = state.next[j];
        Debug.Assert(j != i); /* we have created a loop, obviously wrong */
    }
    if (head.preference!=0) goto done;

    if (state.nums[i] == 0 && state.nums[state.next[i]] > state.n) {
        /* (probably) empty cell onto the head of a coloured region:
         * make sure we start at a 0 offset. */
        head.start = START(state,COLOUR(state,state.nums[state.next[i]]));
        head.preference = 1;
        head.why = "adding blank cell to head of numbered region";
    } else if (state.nums[i] <= state.n) {
        /* if we're 0 we're probably just blank -- but even if we're a
         * (real) numbered region, we don't have an immutable number
         * in it (any more) otherwise it'd have been caught above, so
         * reassign the colour. */
        head.start = 0;
        head.preference = 0;
        head.why = "lowest available colour group";
    } else {
        c = COLOUR(state,state.nums[i]);
        n = 1;
        sz = Dsf.dsf_size(state.dsf, i);
        j = i;
        while (state.next[j] != -1) {
            j = state.next[j];
            if (state.nums[j] == 0 && state.next[j] == -1) {
                head.start = START(state,c);
                head.preference = 1;
                head.why = "adding blank cell to end of numbered region";
                goto done;
            }
            if (COLOUR(state,state.nums[j]) == c)
                n++;
            else {
                int start_alternate = START(state,COLOUR(state,state.nums[j]));
                if (n < (sz - n)) {
                    head.start = start_alternate;
                    head.preference = 1;
                    head.why = "joining two coloured regions, swapping to larger colour";
                } else {
                    head.start = START(state,c);
                    head.preference = 1;
                    head.why = "joining two coloured regions, taking largest";
                }
                goto done;
            }
        }
        /* If we got here then we may have split a region into
         * two; make sure we don't assign a colour we've already used. */
        if (c == 0) {
            /* not convinced this shouldn't be an assertion failure here. */
            head.start = 0;
            head.preference = 0;
        } else {
            head.start = START(state,c);
            head.preference = 1;
        }
        head.why = "got to end of coloured region";
    }

done:
    Debug.Assert(head.why != null);
    if (head.preference!=0)
        Debug.WriteLine("Chain at (%d,%d) numbered for preference at %d (colour %d): %s.",
               head.i%state.w, head.i/state.w,
               head.start, COLOUR(state,head.start), head.why);
    else
        Debug.WriteLine("Chain at (%d,%d) using next available colour: %s.",
               head.i%state.w, head.i/state.w,
               head.why);
}

static void connect_numbers(SignPostState state)
{
    int i, di, dni;

    Dsf.dsf_init(state.dsf, state.n);
    for (i = 0; i < state.n; i++) {
        if (state.next[i] != -1) {
            Debug.Assert(state.prev[state.next[i]] == i);
            di = Dsf.dsf_canonify(state.dsf, i);
            dni = Dsf.dsf_canonify(state.dsf, state.next[i]);
            if (di == dni) {
                Debug.WriteLine("connect_numbers: chain forms a loop.");
                state.impossible = true;
            }
            Dsf.dsf_merge(state.dsf, di, dni);
        }
    }
}

static int compare_heads(head_meta ha,  head_meta hb)
{
    
    /* Heads with preferred colours first... */
    if (ha.preference!=0 && hb.preference==0) return -1;
    if (hb.preference!=0 && ha.preference==0) return 1;

    /* ...then heads with low colours first... */
    if (ha.start < hb.start) return -1;
    if (ha.start > hb.start) return 1;

    /* ... then large regions first... */
    if (ha.sz > hb.sz) return -1;
    if (ha.sz < hb.sz) return 1;

    /* ... then position. */
    if (ha.i > hb.i) return -1;
    if (ha.i < hb.i) return 1;

    return 0;
}

static int lowest_start(SignPostState state, head_meta[] heads, int nheads)
{
    int n, c;

    /* NB start at 1: colour 0 is real numbers */
    for (c = 1; c < state.n; c++) {
        for (n = 0; n < nheads; n++) {
            if (COLOUR(state,heads[n].start) == c)
                goto used;
        }
        return c;
used:
        ;
    }
    Debug.Assert(false, "No available colours!");
    return 0;
}

static void update_numbers(SignPostState state)
{
    int i, j, n, nnum, nheads;
    head_meta[] heads = Enumerable.Range(0, state.n).Select(x => new head_meta()).ToArray();//new head_meta[state.n]; // FIXME: fill with instances !

    for (n = 0; n < state.n; n++)
        state.numsi[n] = -1;

    for (i = 0; i < state.n; i++) {
        if ((state.flags[i] & FLAG_IMMUTABLE)!=0) {
            Debug.Assert(state.nums[i] > 0);
            Debug.Assert(state.nums[i] <= state.n);
            state.numsi[state.nums[i]] = i;
        }
        else if (state.prev[i] == -1 && state.next[i] == -1)
            state.nums[i] = 0;
    }
    connect_numbers(state);

    /* Construct an array of the heads of all current regions, together
     * with their preferred colours. */
    nheads = 0;
    for (i = 0; i < state.n; i++) {
        /* Look for a cell that is the start of a chain
         * (has a next but no prev). */
        if (state.prev[i] != -1 || state.next[i] == -1) continue;

        head_number(state, i, heads[nheads++]);
    }

    /* Sort that array:
     * - heads with preferred colours first, then
     * - heads with low colours first, then
     * - large regions first
     */
    Array.Sort(heads, 0, nheads, Comparer<head_meta>.Create(compare_heads));

    /* Remove duplicate-coloured regions. */
    for (n = nheads-1; n >= 0; n--) { /* order is important! */
        if ((n != 0) && (heads[n].start == heads[n-1].start)) {
            /* We have a duplicate-coloured region: since we're
             * sorted in size order and this is not the first
             * of its colour it's not the largest: recolour it. */
            heads[n].start = START(state,lowest_start(state, heads, nheads));
            heads[n].preference = -1; /* '-1' means 'was duplicate' */
        }
        else if (heads[n].preference==0) {
            Debug.Assert(heads[n].start == 0);
            heads[n].start = START(state,lowest_start(state, heads, nheads));
        }
    }

    Debug.WriteLine("Region colouring after duplicate removal:");

    for (n = 0; n < nheads; n++) {
        Debug.WriteLine("  Chain at (%d,%d) sz %d numbered at %d (colour %d): %s%s",
               heads[n].i % state.w, heads[n].i / state.w, heads[n].sz,
               heads[n].start, COLOUR(state,heads[n].start), heads[n].why,
               heads[n].preference == 0 ? " (next available)" :
               heads[n].preference < 0 ? " (duplicate, next available)" : "");

        nnum = heads[n].start;
        j = heads[n].i;
        while (j != -1) {
            if ((state.flags[j] & FLAG_IMMUTABLE)==0) {
                if (nnum > 0 && nnum <= state.n)
                    state.numsi[nnum] = j;
                state.nums[j] = nnum;
            }
            nnum++;
            j = state.next[j];
            Debug.Assert(j != heads[n].i); /* loop?! */
        }
    }
    /*debug_numbers(state);*/
}

static bool check_completion(SignPostState state, bool mark_errors)
{
    int n, j, k;
    bool error = false, complete;

    /* NB This only marks errors that are possible to perpetrate with
     * the current UI in interpret_move. Things like forming loops in
     * linked sections and having numbers not add up should be forbidden
     * by the code elsewhere, so we don't bother marking those (because
     * it would add lots of tricky drawing code for very little gain). */
    if (mark_errors) {
        for (j = 0; j < state.n; j++)
            state.flags[j] = (uint)((int)state.flags[j] & ~FLAG_ERROR);
    }

    /* Search for repeated numbers. */
    for (j = 0; j < state.n; j++) {
        if (state.nums[j] > 0 && state.nums[j] <= state.n) {
            for (k = j+1; k < state.n; k++) {
                if (state.nums[k] == state.nums[j]) {
                    if (mark_errors) {
                        state.flags[j] |= FLAG_ERROR;
                        state.flags[k] |= FLAG_ERROR;
                    }
                    error = true;
                }
            }
        }
    }

    /* Search and mark numbers n not pointing to n+1; if any numbers
     * are missing we know we've not completed. */
    complete = true;
    for (n = 1; n < state.n; n++) {
        if (state.numsi[n] == -1 || state.numsi[n+1] == -1)
            complete = false;
        else if (!ispointingi(state, state.numsi[n], state.numsi[n+1])) {
            if (mark_errors) {
                state.flags[state.numsi[n]] |= FLAG_ERROR;
                state.flags[state.numsi[n+1]] |= FLAG_ERROR;
            }
            error = true;
        } else {
            /* make sure the link is explicitly made here; for instance, this
             * is nice if the user drags from 2 out (making 3) and a 4 is also
             * visible; this ensures that the link from 3 to 4 is also made. */
            if (mark_errors)
                makelink(state, state.numsi[n], state.numsi[n+1]);
        }
    }

    /* Search and mark numbers less than 0, or 0 with links. */
    for (n = 1; n < state.n; n++) {
        if ((state.nums[n] < 0) ||
            (state.nums[n] == 0 &&
             (state.next[n] != -1 || state.prev[n] != -1))) {
            error = true;
            if (mark_errors)
                state.flags[n] |= FLAG_ERROR;
        }
    }

    if (error) return false;
    return complete;
}
public override SignPostState CreateNewGameFromDescription(SignPostSettings @params, string desc)
{

    SignPostState state = null;
    string temp;
    unpick_desc(@params, desc, out state, out temp);
    if (state == null) Debug.Assert(false,"new_game failed to unpick");

    update_numbers(state);
    check_completion(state, true); /* update any auto-links */

    return state;
}

/* --- Solver --- */

/* If a tile has a single tile it can link _to_, or there's only a single
 * location that can link to a given tile, fill that link in. */
static int solve_single(SignPostState state, SignPostState copy, int[] from)
{
    int i, j, sx, sy, x, y, d, poss, w=state.w, nlinks = 0;

    /* The from array is a list of 'which square can link _to_ us';
     * we start off with from as '-1' (meaning 'not found'); if we find
     * something that can link to us it is set to that index, and then if
     * we find another we set it to -2. */

    from.SetAll(-1);

    /* poss is 'can I link to anything' with the same meanings. */

    for (i = 0; i < state.n; i++) {
        if (state.next[i] != -1) continue;
        if (state.nums[i] == state.n) continue; /* no next from last no. */

        d = state.dirs[i];
        poss = -1;
        sx = x = i%w; sy = y = i/w;
        while (true) {
            x += dxs[d]; y += dys[d];
            if (!INGRID(state, x, y)) break;
            if (!isvalidmove(state, true, sx, sy, x, y)) continue;

            /* can't link to somewhere with a back-link we would have to
             * break (the solver just doesn't work like this). */
            j = y*w+x;
            if (state.prev[j] != -1) continue;

            if (state.nums[i] > 0 && state.nums[j] > 0 &&
                state.nums[i] <= state.n && state.nums[j] <= state.n &&
                state.nums[j] == state.nums[i]+1) {
                Debug.WriteLine("Solver: forcing link through existing consecutive numbers.");
                poss = j;
                from[j] = i;
                break;
            }

            /* if there's been a valid move already, we have to move on;
             * we can't make any deductions here. */
            poss = (poss == -1) ? j : -2;

            /* Modify the from array as described above (which is enumerating
             * what points to 'j' in a similar way). */
            from[j] = (from[j] == -1) ? i : -2;
        }
        if (poss == -2) {
            /*Debug.WriteLine("Solver: (%d,%d) has multiple possible next squares.", sx, sy));*/
            ;
        } else if (poss == -1) {
            Debug.WriteLine("Solver: nowhere possible for (%d,%d) to link to.", sx, sy);
            copy.impossible = true;
            return -1;
        } else {
            Debug.WriteLine("Solver: linking (%d,%d) to only possible next (%d,%d).",
                   sx, sy, poss%w, poss/w);
            makelink(copy, i, poss);
            nlinks++;
        }
    }

    for (i = 0; i < state.n; i++) {
        if (state.prev[i] != -1) continue;
        if (state.nums[i] == 1) continue; /* no prev from 1st no. */

        x = i%w; y = i/w;
        if (from[i] == -1) {
            Debug.WriteLine("Solver: nowhere possible to link to (%d,%d)", x, y);
            copy.impossible = true;
            return -1;
        } else if (from[i] == -2) {
            /*Debug.WriteLine("Solver: (%d,%d) has multiple possible prev squares.", x, y));*/
            ;
        } else {
            Debug.WriteLine("Solver: linking only possible prev (%d,%d) to (%d,%d).",
                   from[i]%w, from[i]/w, x, y);
            makelink(copy, from[i], i);
            nlinks++;
        }
    }

    return nlinks;
}

/* Returns 1 if we managed to solve it, 0 otherwise. */
static int solve_state(SignPostState state)
{
    SignPostState copy = dup_game(state);
    int[] scratch = new int[state.n];
    int ret;

    debug_state("Before solver: ", state);

    while (true) {
        update_numbers(state);

        if (solve_single(state, copy, scratch) != 0) {
            dup_game_to(state, copy);
            if (state.impossible) break; else continue;
        }
        break;
    }

    update_numbers(state);
    ret = state.impossible ? -1 : check_completion(state, false) ? 1 : 0;
    Debug.WriteLine("Solver finished: %s",
           ret < 0 ? "impossible" : ret > 0 ? "solved" : "not solved");
    debug_state("After solver: ", state);
    return ret;
}

public override SignPostMove CreateSolveGameMove(SignPostState state, SignPostState currstate, SignPostMove ai, out string error)
{
    SignPostState tosolve;
    string ret = null;
    int result;
    error = null;
    tosolve = dup_game(currstate);
    result = solve_state(tosolve);
    if (result > 0)
        ret = generate_desc(tosolve, true);
    if (ret != null) return new SignPostMove() { Type = SignPostMoveType.Solve, SolveData = ret.Substring(1) };

    tosolve = dup_game(state);
    result = solve_state(tosolve);
    if (result < 0)
        error = "Puzzle is impossible.";
    else if (result == 0)
        error = "Unable to solve puzzle.";
    else
        ret = generate_desc(tosolve, true);


    return new SignPostMove() { Type = SignPostMoveType.Solve, SolveData = ret.Substring(1) }; 
}

/* --- UI and move routines. --- */


public override SignPostUI CreateUI(SignPostState state)
{
    SignPostUI ui = new SignPostUI();

    /* NB: if this is ever changed to as to require more than a structure
     * copy to clone, there's code that needs fixing in game_redraw too. */

    ui.cx = ui.cy = 0;
    ui.cshow = false;

    ui.dragging = false;
    ui.sx = ui.sy = ui.dx = ui.dy = 0;

    return ui;
}


static string encode_ui(SignPostUI ui)
{
    return null;
}

static void decode_ui(SignPostUI ui, string encoding)
{
}

public override void GameStateChanged(SignPostUI ui, SignPostState oldstate, SignPostState newstate)
{
    if (!oldstate.completed && newstate.completed)
        ui.cshow = ui.dragging = false;
}

public override SignPostMove InterpretMove(SignPostState state, SignPostUI ui, SignPostDrawState ds, int mx, int my, Buttons button, bool isTouchOrStylus)
{
    int x = FROMCOORD(ds,mx), y = FROMCOORD(ds,my), w = state.w;

    if (Misc.IS_CURSOR_MOVE(button)) {
        Misc.move_cursor(button, ref ui.cx, ref ui.cy, state.w, state.h, false);
        ui.cshow = true;
        if (ui.dragging) {
            ui.dx = COORD(ds,ui.cx) + TILE_SIZE(ds)/2;
            ui.dy = COORD(ds,ui.cy) + TILE_SIZE(ds)/2;
        }
        return null;
    }
    else if (Misc.IS_CURSOR_SELECT(button))
    {
        if (!ui.cshow)
            ui.cshow = true;
        else if (ui.dragging) {
            ui.dragging = false;
            if (ui.sx == ui.cx && ui.sy == ui.cy) return null;
            if (ui.drag_is_from) {
                if (!isvalidmove(state, false, ui.sx, ui.sy, ui.cx, ui.cy)) return null;
                return new SignPostMove() { Type= SignPostMoveType.Link, sx = ui.sx, sy = ui.sy, ex = ui.cx, ey = ui.cy };
            } else {
                if (!isvalidmove(state, false, ui.cx, ui.cy, ui.sx, ui.sy)) return null;
                return new SignPostMove() { Type = SignPostMoveType.Link, sx = ui.cx, sy = ui.cy, ex = ui.sx, ey = ui.sy };
            }
        } else {
            ui.dragging = true;
            ui.sx = ui.cx;
            ui.sy = ui.cy;
            ui.dx = COORD(ds,ui.cx) + TILE_SIZE(ds)/2;
            ui.dy = COORD(ds,ui.cy) + TILE_SIZE(ds)/2;
            ui.drag_is_from = (button == Buttons.CURSOR_SELECT) ? true : false;
        }
        return null;
    }
    if (Misc.IS_MOUSE_DOWN(button)) {
        if (ui.cshow) {
            ui.cshow = ui.dragging = false;
        }
        Debug.Assert(!ui.dragging);
        if (!INGRID(state, x, y)) return null;

        if (button == Buttons.LEFT_BUTTON) {
            /* disallow dragging from the final number. */
            if ((state.nums[y*w+x] == state.n) &&
                (state.flags[y*w+x] & FLAG_IMMUTABLE) != 0)
                return null;
        } else if (button == Buttons.RIGHT_BUTTON) {
            /* disallow dragging to the first number. */
            if ((state.nums[y*w+x] == 1) &&
                (state.flags[y * w + x] & FLAG_IMMUTABLE) != 0)
                return null;
        }

        ui.dragging = true;
        ui.drag_is_from = (button == Buttons.LEFT_BUTTON) ? true : false;
        ui.sx = x;
        ui.sy = y;
        ui.dx = mx;
        ui.dy = my;
        ui.cshow = false;
        return null;
    } else if (Misc.IS_MOUSE_DRAG(button) && ui.dragging) {
        ui.dx = mx;
        ui.dy = my;
        return null;
    } else if (Misc.IS_MOUSE_RELEASE(button) && ui.dragging) {
        ui.dragging = false;
        if (ui.sx == x && ui.sy == y) return null; /* single click */

        if (!INGRID(state, x, y)) {
            int si = ui.sy*w+ui.sx;
            if (state.prev[si] == -1 && state.next[si] == -1)
                return null;
            return new SignPostMove() { Type = ui.drag_is_from ? SignPostMoveType.MarkC : SignPostMoveType.MarkX, sx = ui.sx, sy = ui.sy };
        }

        if (ui.drag_is_from) {
            if (!isvalidmove(state, false, ui.sx, ui.sy, x, y)) return null;
            return new SignPostMove() { Type = SignPostMoveType.Link, sx = ui.sx, sy = ui.sy, ex = x, ey = y };

        } else {
            if (!isvalidmove(state, false, x, y, ui.sx, ui.sy)) return null;
            return new SignPostMove() { Type = SignPostMoveType.Link, sx = x, sy = y, ex = ui.sx, ey = ui.sy };

        }
    } /* else if (button == 'H' || button == 'h')
        return dupstr("H"); */
    //else if ((button == 'x' || button == 'X') && ui.cshow) {
    //    int si = ui.cy*w + ui.cx;
    //    if (state.prev[si] == -1 && state.next[si] == -1)
    //        return "";
    //    sprintf(buf, "%c%d,%d",
    //            (int)((button == 'x') ? 'C' : 'X'), ui.cx, ui.cy);
    //    return dupstr(buf);
    //}

    return null;
}

static void unlink_cell(SignPostState state, int si)
{
    Debug.WriteLine("Unlinking (%d,%d).", si%state.w, si/state.w);
    if (state.prev[si] != -1) {
        Debug.WriteLine(" ... removing prev link from (%d,%d).",
               state.prev[si]%state.w, state.prev[si]/state.w);
        state.next[state.prev[si]] = -1;
        state.prev[si] = -1;
    }
    if (state.next[si] != -1) {
        Debug.WriteLine(" ... removing next link to (%d,%d).",
               state.next[si]%state.w, state.next[si]/state.w);
        state.prev[state.next[si]] = -1;
        state.next[si] = -1;
    }
}

public override SignPostMove ParseMove(SignPostSettings settings, string moveString)
{
    return SignPostMove.Parse(settings, moveString);
}

public override SignPostState ExecuteMove(SignPostState state, SignPostMove move)
{
    SignPostState ret = null;
    int sx, sy, ex, ey, si, ei, w = state.w;
    char c;

    Debug.WriteLine("move: %s", move);

    if (move.Type == SignPostMoveType.Solve) {
        SignPostSettings p = new SignPostSettings(state.w,state.h, false);
	SignPostState tmp;
        string valid;
	int i;

        valid = validate_desc(p, move.SolveData);
        if (valid != null) {
            Debug.WriteLine("execute_move: move not valid: %s", valid);
            return null;
        }
	ret = dup_game(state);
    tmp = CreateNewGameFromDescription(p, move.SolveData);
	for (i = 0; i < state.n; i++) {
	    ret.prev[i] = tmp.prev[i];
	    ret.next[i] = tmp.next[i];
	}
        ret.used_solve = true;
    } else if (move.Type == SignPostMoveType.Link) {
        sx = move.sx;
        sy = move.sy;
        ex = move.ex;
        ey = move.ey;
        if (!isvalidmove(state, false, sx, sy, ex, ey)) return null;

        ret = dup_game(state);

        si = sy*w+sx; ei = ey*w+ex;
        makelink(ret, si, ei);
    } else if (move.Type == SignPostMoveType.MarkC ||  move.Type == SignPostMoveType.MarkX) {
        sx = move.sx;
        sy = move.sy;
        if (!INGRID(state, sx, sy)) return null;
        si = sy*w+sx;
        if (state.prev[si] == -1 && state.next[si] == -1)
            return null;

        ret = dup_game(state);

        if (move.Type == SignPostMoveType.MarkC) {
            /* Unlink the single cell we dragged from the board. */
            unlink_cell(ret, si);
        } else {
            int i, set, sset = state.nums[si] / (state.n+1);
            for (i = 0; i < state.n; i++) {
                /* Unlink all cells in the same set as the one we dragged
                 * from the board. */

                if (state.nums[i] == 0) continue;
                set = state.nums[i] / (state.n+1);
                if (set != sset) continue;

                unlink_cell(ret, i);
            }
        }
    //} else if (strcmp(move, "H") == 0) {
    //    ret = dup_game(state);
    //    solve_state(ret);
    }
    if (ret != null) {
        update_numbers(ret);
        if (check_completion(ret, true)) ret.completed = true;
    }

    return ret;
}

/* ----------------------------------------------------------------------
 * Drawing routines.
 */
public override void ComputeSize(SignPostSettings @params, int tilesize, out int x, out int y)
{
    /* Ick: fake up `ds.tilesize' for macro expansion purposes */
    SignPostDrawState ds = new SignPostDrawState(){tilesize = tilesize};
	
    x = TILE_SIZE(ds) * @params.w + 2 * BORDER(ds);
    y = TILE_SIZE(ds) * @params.h + 2 * BORDER(ds);
}

public override void SetTileSize(Drawing dr, SignPostDrawState ds, SignPostSettings @params, int tilesize)
{
    ds.tilesize = tilesize;
    Debug.Assert(TILE_SIZE(ds) > 0);

}

/* Colours chosen from the webby palette to work as a background to black text,
 * W then some plausible approximation to pastelly ROYGBIV; we then interpolate
 * between consecutive pairs to give another 8 (and then the drawing routine
 * will reuse backgrounds). */
static readonly ulong[] bgcols = new ulong[]{
    0xffffff, /* white */
    0xffa07a, /* lightsalmon */
    0x98fb98, /* green */
    0x7fffd4, /* aquamarine */
    0x9370db, /* medium purple */
    0xffa500, /* orange */
    0x87cefa, /* lightskyblue */
    0xffff00, /* yellow */
};

private static void  average(float[] ret, int r, int a,int b,float w)  { 
    for (int i = 0; i < 3; i++) 
	ret[(r)*3+i] = ret[(a)*3+i] + w * (ret[(b)*3+i] - ret[(a)*3+i]); 
}

public override float[] GetColours(Frontend fe, out int ncolours)
{
    float[] ret = new  float[3 * NCOLOURS];
    int c, i;

    Misc.game_mkhighlight(fe, ret, COL_BACKGROUND, COL_HIGHLIGHT, COL_LOWLIGHT);

    for (i = 0; i < 3; i++) {
        ret[COL_NUMBER * 3 + i] = 0.0F;
        ret[COL_ARROW * 3 + i] = 0.0F;
        ret[COL_CURSOR * 3 + i] = ret[COL_BACKGROUND * 3 + i] / 2.0F;
        ret[COL_GRID * 3 + i] = ret[COL_BACKGROUND * 3 + i] / 1.3F;
    }
    ret[COL_NUMBER_SET * 3 + 0] = 0.0F;
    ret[COL_NUMBER_SET * 3 + 1] = 0.0F;
    ret[COL_NUMBER_SET * 3 + 2] = 0.9F;

    ret[COL_ERROR * 3 + 0] = 1.0F;
    ret[COL_ERROR * 3 + 1] = 0.0F;
    ret[COL_ERROR * 3 + 2] = 0.0F;

    ret[COL_DRAG_ORIGIN * 3 + 0] = 0.2F;
    ret[COL_DRAG_ORIGIN * 3 + 1] = 1.0F;
    ret[COL_DRAG_ORIGIN * 3 + 2] = 0.2F;

    for (c = 0; c < 8; c++) {
         ret[(COL_B0 + c) * 3 + 0] = (float)((bgcols[c] & 0xff0000) >> 16) / 256.0F;
         ret[(COL_B0 + c) * 3 + 1] = (float)((bgcols[c] & 0xff00) >> 8) / 256.0F;
         ret[(COL_B0 + c) * 3 + 2] = (float)((bgcols[c] & 0xff)) / 256.0F;
    }
    for (c = 0; c < 8; c++) {
        for (i = 0; i < 3; i++) {
           ret[(COL_B0 + 8 + c) * 3 + i] =
               (ret[(COL_B0 + c) * 3 + i] + ret[(COL_B0 + c + 1) * 3 + i]) / 2.0F;
        }
    }


    average(ret,COL_ARROW_BG_DIM, COL_BACKGROUND, COL_ARROW, 0.1F);
    average(ret,COL_NUMBER_SET_MID, COL_B0, COL_NUMBER_SET, 0.3F);
    for (c = 0; c < NBACKGROUNDS; c++) {
	/* I assume here that COL_ARROW and COL_NUMBER are the same.
	 * Otherwise I'd need two sets of COL_M*. */
	average(ret,COL_M0 + c, COL_B0 + c, COL_NUMBER, 0.3F);
	average(ret,COL_D0 + c, COL_B0 + c, COL_NUMBER, 0.1F);
	average(ret,COL_X0 + c, COL_BACKGROUND, COL_B0 + c, 0.5F);
    }

    ncolours = NCOLOURS;
    return ret;
}

public override SignPostDrawState CreateDrawState(Drawing dr, SignPostState state)
{
    SignPostDrawState ds = new SignPostDrawState();
    int i;

    ds.tilesize = 0;
    ds.started = ds.solved = false;
    ds.w = state.w;
    ds.h = state.h;
    ds.n = state.n;

    ds.nums = new  int[state.n];
    ds.dirp = new  int[state.n];
    ds.f = new  uint[state.n];
    for (i = 0; i < state.n; i++) {
        ds.nums[i] = 0;
        ds.dirp[i] = -1;
        ds.f[i] = 0;
    }

    ds.angle_offset = 0.0F;

    ds.dragging = false;
    ds.dx = ds.dy = 0;

    return ds;
}


/* cx, cy are top-left corner. sz is the 'radius' of the arrow.
 * ang is in radians, clockwise from 0 == straight up. */
static void draw_arrow(Drawing dr, int cx, int cy, int sz, double ang,
                       int cfill, int cout)
{
    int[] coords = new int[14];
    int xdx, ydx, xdy, ydy, xdx3, xdy3;
    double s = Math.Sin(ang), c = Math.Cos(ang);

    xdx3 = (int)(sz * (c/3 + 1) + 0.5) - sz;
    xdy3 = (int)(sz * (s/3 + 1) + 0.5) - sz;
    xdx = (int)(sz * (c + 1) + 0.5) - sz;
    xdy = (int)(sz * (s + 1) + 0.5) - sz;
    ydx = -xdy;
    ydy = xdx;


    coords[2*0 + 0] = cx - ydx;
    coords[2*0 + 1] = cy - ydy;
    coords[2*1 + 0] = cx + xdx;
    coords[2*1 + 1] = cy + xdy;
    coords[2*2 + 0] = cx + xdx3;
    coords[2*2 + 1] = cy + xdy3;
    coords[2*3 + 0] = cx + xdx3 + ydx;
    coords[2*3 + 1] = cy + xdy3 + ydy;
    coords[2*4 + 0] = cx - xdx3 + ydx;
    coords[2*4 + 1] = cy - xdy3 + ydy;
    coords[2*5 + 0] = cx - xdx3;
    coords[2*5 + 1] = cy - xdy3;
    coords[2*6 + 0] = cx - xdx;
    coords[2*6 + 1] = cy - xdy;

    dr.draw_polygon( coords, 7, cfill, cout);
}

static void draw_arrow_dir(Drawing dr, int cx, int cy, int sz, int dir,
                           int cfill, int cout, double angle_offset)
{
    double ang = 2.0 * Math.PI * (double)dir / 8.0 + angle_offset;
    draw_arrow(dr, cx, cy, sz, ang, cfill, cout);
}

/* cx, cy are centre coordinates.. */
static void draw_star(Drawing dr, int cx, int cy, int rad, int npoints,
                      int cfill, int cout, double angle_offset)
{
    int[] coords;
    int n;
    double a, r;

    Debug.Assert(npoints > 0);

    coords = new  int[npoints * 2 * 2];

    for (n = 0; n < npoints * 2; n++) {
        a = 2.0 * Math.PI * ((double)n / ((double)npoints * 2.0)) + angle_offset;
        r = (n % 2) != 0 ? (double)rad/2.0 : (double)rad;

        /* We're rotating the point at (0, -r) by a degrees */
        coords[2*n+0] = cx + (int)( r * Math.Sin(a));
        coords[2*n+1] = cy + (int)(-r * Math.Cos(a));
    }
    dr.draw_polygon( coords, npoints*2, cfill, cout);
}

static int num2col(SignPostDrawState ds, int num)
{
    int set = num / (ds.n+1);

    if (num <= 0 || set == 0) return COL_B0;
    return COL_B0 + 1 + ((set-1) % 15);
}

private static int ARROW_HALFSZ(SignPostDrawState ds) { return (7 * TILE_SIZE(ds) / 32); }

const int F_CUR           =0x001;   /* Cursor on this tile. */
const int F_DRAG_SRC      =0x002;   /* Tile is source of a drag. */
const int F_ERROR         =0x004;   /* Tile marked in error. */
const int F_IMMUTABLE     =0x008;   /* Tile (number) is immutable. */
const int F_ARROW_POINT   =0x010;   /* Tile points to other tile */
const int F_ARROW_INPOINT =0x020;   /* Other tile points in here. */
const int F_DIM           =0x040;   /* Tile is dim */

private static int dim(int fg, int bg) {
      return (bg)==COL_BACKGROUND ? COL_ARROW_BG_DIM : 
      (bg) + COL_D0 - COL_B0 ;
}

private static int mid(int fg, int bg) {
      return (fg)==COL_NUMBER_SET ? COL_NUMBER_SET_MID : 
      (bg) + COL_M0 - COL_B0 ;
}

private static int dimbg(int bg) {
      return (bg)==COL_BACKGROUND ? COL_BACKGROUND : 
      (bg) + COL_X0 - COL_B0 ;
}

static void tile_redraw(Drawing dr, SignPostDrawState ds, int tx, int ty,
                        int dir, int dirp, int num, uint f,
                        double angle_offset, int print_ink)
{
    int cb = TILE_SIZE(ds) / 16, textsz;
    int arrowcol, sarrowcol, setcol, textcol;
    int acx, acy, asz;
    bool empty = false;

    if (num == 0 && (f & F_ARROW_POINT) == 0 && (f & F_ARROW_INPOINT) == 0)
    {
        empty =true;
        /*
         * We don't display text in empty cells: typically these are
         * signified by num=0. However, in some cases a cell could
         * have had the number 0 assigned to it if the user made an
         * error (e.g. tried to connect a chain of length 5 to the
         * immutable number 4) so we _do_ display the 0 if the cell
         * has a link in or a link out.
         */
    }

    /* Calculate colours. */

    if (print_ink >= 0) {
	/*
	 * We're printing, so just do everything in black.
	 */
	arrowcol = textcol = print_ink;
	setcol = sarrowcol = -1;       /* placate optimiser */
    } else {

	setcol = empty ? COL_BACKGROUND : num2col(ds, num);



	if ((f & F_DRAG_SRC)!=0) arrowcol = COL_DRAG_ORIGIN;
	else if ((f & F_DIM)!=0) arrowcol = dim(COL_ARROW, setcol);
    else if ((f & F_ARROW_POINT) != 0) arrowcol = mid(COL_ARROW, setcol);
	else arrowcol = COL_ARROW;

	if ((f & F_ERROR)!=0 && (f & F_IMMUTABLE)==0) textcol = COL_ERROR;
	else {
        if ((f & F_IMMUTABLE) != 0) textcol = COL_NUMBER_SET;
	    else textcol = COL_NUMBER;

	    if ((f & F_DIM)!=0) textcol = dim(textcol, setcol);
	    else if (((f & F_ARROW_POINT)!=0 || num==ds.n) &&
		     ((f & F_ARROW_INPOINT)!=0 || num==1))
		textcol = mid(textcol, setcol);
	}

    if ((f & F_DIM) != 0) sarrowcol = dim(COL_ARROW, setcol);
	else sarrowcol = COL_ARROW;
    }

    /* Clear tile background */

    if (print_ink < 0) {
	dr.draw_rect( tx, ty, TILE_SIZE(ds), TILE_SIZE(ds),
		  (f & F_DIM)!=0 ? dimbg(setcol) : setcol);
    }

    /* Draw large (outwards-pointing) arrow. */

    asz = ARROW_HALFSZ(ds);         /* 'radius' of arrow/star. */
    acx = tx+TILE_SIZE(ds)/2+asz;   /* centre x */
    acy = ty+TILE_SIZE(ds)/2+asz;   /* centre y */

    if (num == ds.n && (f & F_IMMUTABLE) != 0)
        draw_star(dr, acx, acy, asz, 5, arrowcol, arrowcol, angle_offset);
    else
        draw_arrow_dir(dr, acx, acy, asz, dir, arrowcol, arrowcol, angle_offset);
    if (print_ink < 0 && (f & F_CUR) != 0)
        dr.draw_rect_corners( acx, acy, asz+1, COL_CURSOR);

    /* Draw dot iff this tile requires a predecessor and doesn't have one. */

    if (print_ink < 0) {
	acx = tx+TILE_SIZE(ds)/2-asz;
	acy = ty+TILE_SIZE(ds)/2+asz;

	if ((f & F_ARROW_INPOINT)==0 && num != 1) {
	    dr.draw_circle(acx, acy, asz / 4, sarrowcol, sarrowcol);
	}
    }

    /* Draw text (number or set). */

    if (!empty) {
        int set = (num <= 0) ? 0 : num / (ds.n+1);

        string p = null;
        if (set == 0 || num <= 0) {
            p = num.ToString();
        } else {
            int n = num % (ds.n+1);
            StringBuilder buf = new StringBuilder();

            if (n != 0) {
                buf.Append('+').Append(n);
            }
            do {
                set--;
                buf.Insert(0, (char)((set % 26)+'a'));
                set /= 26;
            } while (set != 0); 
            p = buf.ToString();
        }
        textsz = Math.Min(2*asz, (TILE_SIZE(ds) - 2 * cb) / p.Length);
        dr.draw_text(tx + cb, ty + TILE_SIZE(ds) / 4, Drawing.FONT_VARIABLE, textsz,
                  Drawing.ALIGN_VCENTRE | Drawing.ALIGN_HLEFT, textcol, p);
    }

    if (print_ink < 0) {
	dr.draw_rect_outline( tx, ty, TILE_SIZE(ds), TILE_SIZE(ds), COL_GRID);
	dr.draw_update( tx, ty, TILE_SIZE(ds), TILE_SIZE(ds));
    }
}

static void draw_drag_indicator(Drawing dr, SignPostDrawState ds,
                                SignPostState state, SignPostUI ui,
                                bool validdrag)
{
    int dir, w = ds.w, acol = COL_ARROW;
    int fx = FROMCOORD(ds,ui.dx), fy = FROMCOORD(ds,ui.dy);
    double ang;

    if (validdrag) {
        // fx or fy can be outside of grid, cap to grid size
        fx = Math.Min(Math.Max(0, fx), ds.w - 1);
        fy = Math.Min(Math.Max(0, fy), ds.h - 1);

        /* If we could move here, lock the arrow to the appropriate direction. */
        dir = ui.drag_is_from ? state.dirs[ui.sy*w+ui.sx] : state.dirs[fy*w+fx];

        ang = (2.0 * Math.PI * dir) / 8.0; /* similar to calculation in draw_arrow_dir. */
    } else {
        /* Draw an arrow pointing away from/towards the origin cell. */
        int ox = COORD(ds,ui.sx) + TILE_SIZE(ds)/2, oy = COORD(ds,ui.sy) + TILE_SIZE(ds)/2;
        double tana, offset;
        double xdiff = Math.Abs(ox - ui.dx), ydiff = Math.Abs(oy - ui.dy);

        if (xdiff == 0) {
            ang = (oy > ui.dy) ? 0.0F : Math.PI;
        } else if (ydiff == 0) {
            ang = (ox > ui.dx) ? 3.0F * Math.PI / 2.0F : Math.PI / 2.0F;
        } else {
            if (ui.dx > ox && ui.dy < oy) {
                tana = xdiff / ydiff;
                offset = 0.0F;
            } else if (ui.dx > ox && ui.dy > oy) {
                tana = ydiff / xdiff;
                offset = Math.PI / 2.0F;
            } else if (ui.dx < ox && ui.dy > oy) {
                tana = xdiff / ydiff;
                offset = Math.PI;
            } else {
                tana = ydiff / xdiff;
                offset = 3.0F * Math.PI / 2.0F;
            }
            ang = Math.Atan(tana) + offset;
        }

        if (!ui.drag_is_from) ang += Math.PI; /* point to origin, not away from. */

    }
    draw_arrow(dr, ui.dx, ui.dy, ARROW_HALFSZ(ds), ang, acol, acol);
}
public override void Redraw(Drawing dr, SignPostDrawState ds, SignPostState oldstate, SignPostState state, int dir, SignPostUI ui, float animtime, float flashtime)
{
 
    int x, y, i, w = ds.w, dirp;
    bool force = false;
    uint f;
    double angle_offset = 0.0;
    SignPostState postdrop = null;

    if (flashtime > 0.0F)
        angle_offset = 2.0 * Math.PI * (flashtime / FLASH_SPIN);
    if (angle_offset != ds.angle_offset) {
        ds.angle_offset = angle_offset;
        force = true;
    }

    //if (ds.dragging) {
    //    Debug.Assert(ds.dragb);
    //    blitter_load(dr, ds.dragb, ds.dx, ds.dy);
    //    dr.draw_update( ds.dx, ds.dy, BLITTER_SIZE(ds), BLITTER_SIZE(ds));
    //    ds.dragging = FALSE;
    //}

    /* If an in-progress drag would make a valid move if finished, we
     * reflect that move in the board display. We let interpret_move do
     * most of the heavy lifting for us: we have to copy the game_ui so
     * as not to stomp on the real UI's drag state. */
    if (ui.dragging) {
        SignPostUI uicopy = new SignPostUI(ui);
        var movestr = InterpretMove(state, uicopy, ds, ui.dx, ui.dy, Buttons.LEFT_RELEASE, false);

        if (movestr != null ) {
            postdrop = ExecuteMove(state, movestr);
            state = postdrop;
        }
    }

    if (!ds.started) {
        int aw = TILE_SIZE(ds) * state.w;
        int ah = TILE_SIZE(ds) * state.h;
        dr.draw_rect(0, 0, aw + 2 * BORDER(ds), ah + 2 * BORDER(ds), COL_HIGHLIGHT);
        dr.draw_rect_outline( BORDER(ds) - 1, BORDER(ds) - 1, aw + 2, ah + 2, COL_GRID);
        dr.draw_update( 0, 0, aw + 2 * BORDER(ds), ah + 2 * BORDER(ds));
    }
    for (x = 0; x < state.w; x++) {
        for (y = 0; y < state.h; y++) {
            i = y*w + x;
            f = 0;
            dirp = -1;

            if (ui.cshow && x == ui.cx && y == ui.cy)
                f |= F_CUR;

            if (ui.dragging) {
                if (x == ui.sx && y == ui.sy)
                    f |= F_DRAG_SRC;
                else if (ui.drag_is_from) {
                    if (!ispointing(state, ui.sx, ui.sy, x, y))
                        f |= F_DIM;
                } else {
                    if (!ispointing(state, x, y, ui.sx, ui.sy))
                        f |= F_DIM;
                }
            }

            if (state.impossible ||
                state.nums[i] < 0 || (state.flags[i] & FLAG_ERROR)!=0)
                f |= F_ERROR;
            if ((state.flags[i] & FLAG_IMMUTABLE)!=0)
                f |= F_IMMUTABLE;

            if (state.next[i] != -1)
                f |= F_ARROW_POINT;

            if (state.prev[i] != -1) {
                /* Currently the direction here is from our square _back_
                 * to its previous. We could change this to give the opposite
                 * sense to the direction. */
                f |= F_ARROW_INPOINT;
                dirp = whichdir(x, y, state.prev[i]%w, state.prev[i]/w);
            }

            if (state.nums[i] != ds.nums[i] ||
                f != ds.f[i] || dirp != ds.dirp[i] ||
                force || !ds.started) {
                int sign;
                {
                    /*
                     * Trivial and foolish configurable option done on
                     * purest whim. With this option enabled, the
                     * victory flash is done by rotating each square
                     * in the opposite direction from its immediate
                     * neighbours, so that they behave like a field of
                     * interlocking gears. With it disabled, they all
                     * rotate in the same direction. Choose for
                     * yourself which is more brain-twisting :-)
                     */
                    bool gear_mode = true;
                    //if (gear_mode < 0) {
                    //    string env = getenv("SIGNPOST_GEARS");
                    //    gear_mode = (env && (env[0] == 'y' || env[0] == 'Y'));
                    //}
                    if (gear_mode)
                        sign = 1 - 2 * ((x ^ y) & 1);
                    else
                        sign = 1;
                }
                tile_redraw(dr, ds,
                            BORDER(ds) + x * TILE_SIZE(ds),
                            BORDER(ds) + y * TILE_SIZE(ds),
                            state.dirs[i], dirp, state.nums[i], f,
                            sign * angle_offset, -1);
                ds.nums[i] = state.nums[i];
                ds.f[i] = f;
                ds.dirp[i] = dirp;
            }
        }
    }
    if (ui.dragging) {
        ds.dragging = true;
        ds.dx = ui.dx - BLITTER_SIZE(ds)/2;
        ds.dy = ui.dy - BLITTER_SIZE(ds)/2;
        //blitter_save(dr, ds.dragb, ds.dx, ds.dy);

        draw_drag_indicator(dr, ds, state, ui, postdrop != null ? true : false);
    }
    if (!ds.started) ds.started = true;
}

static float game_anim_length(SignPostState oldstate,
                              SignPostState newstate, int dir, SignPostUI ui)
{
    return 0.0F;
}

static float game_flash_length(SignPostState oldstate,
                               SignPostState newstate, int dir, SignPostUI ui)
{
    if (!oldstate.completed &&
        newstate.completed && !newstate.used_solve)
        return FLASH_SPIN;
    else
        return 0.0F;
}

static int game_status(SignPostState state)
{
    return state.completed ? +1 : 0;
}
internal override void SetKeyboardCursorVisible(SignPostUI ui, int tileSize, bool value)
{
    ui.cshow = value;
}
public override float CompletedFlashDuration(SignPostSettings settings)
{
    return FLASH_SPIN;
}

internal override bool HoldToRightMouse
{
    get
    {
        return true;
    }
}
    }
}
