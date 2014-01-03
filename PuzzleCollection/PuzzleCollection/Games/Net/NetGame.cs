using PuzzleCollection.Games.Structures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Net
{
    public class NetGame : GameBase<NetSettings, NetState, NetMove, NetDrawState, NetUI>
    {
        
        private static void MATMUL(out float rx, out float ry,float[] mat,float xx, float yy)
        {
            rx = mat[0] * xx + mat[2] * yy; 
            ry = mat[1] * xx + mat[3] * yy; 
        }

/* Direction and other bitfields */
private const int R =0x01;
private const int U =0x02;
private const int L =0x04;
private const int D =0x08;
private const int LOCKED =0x10;
private const int ACTIVE =0x20;

/* Rotations: Anticlockwise, Clockwise, Flip, general rotate */
private static int A(int x) { return ( (((x) & 0x07) << 1) | (((x) & 0x08) >> 3) );}
private static int C(int x) { return ( (((x) & 0x0E) >> 1) | (((x) & 0x01) << 3) );}
private static int F(int x) { return ( (((x) & 0x0C) >> 2) | (((x) & 0x03) << 2) );}
private static int ROT(int x, int n) { return ( ((n)&3) == 0 ? (x) : 
		    ((n)&3) == 1 ? A(x) : 
		    ((n)&3) == 2 ? F(x) : C(x) ); }

/* X and Y displacements */
private static int X(int x) { return ( (x) == R ? +1 : (x) == L ? -1 : 0 ); }
private static int Y(int x) { return ( (x) == D ? +1 : (x) == U ? -1 : 0 ); }

/* Bit count */
private static int COUNT(int x) { return  ( (((x) & 0x08) >> 3) + (((x) & 0x04) >> 2) + 
		   (((x) & 0x02) >> 1) + ((x) & 0x01) ); }

private const int PREFERRED_TILE_SIZE =32;
private static int TILE_SIZE(NetDrawState ds) { return (ds.tilesize); }
private const int TILE_BORDER =1;
private const int WINDOW_OFFSET =16;

private const float ROTATE_TIME= 0.13F;
private const float FLASH_FRAME= 0.07F;

/* Transform physical coords to game coords using game_drawstate ds */
private static int GX(NetDrawState ds, int x) { return (((x) + ds.org_x) % ds.width); }
private static int GY(NetDrawState ds, int y) { return (((y) + ds.org_y) % ds.height); }
/* ...and game coords to physical coords */
private static int RX(NetDrawState ds, int x) { return (((x) + ds.width - ds.org_x) % ds.width); }
private static int RY(NetDrawState ds, int y) { return (((y) + ds.height - ds.org_y) % ds.height); }


private const int COL_BACKGROUND=0;
private const int COL_LOCKED=1;
private const int COL_BORDER=2;
private const int COL_WIRE=3;
private const int COL_ENDPOINT=4;
private const int COL_POWERED=5;
private const int COL_BARRIER=6;
private const int NCOLOURS=7;


private static void OFFSETWH(out int x2, out int y2, int x1,int y1,int dir,int width,int height) 
{
    (x2) = ((x1) + width + X((dir))) % width;
      (y2) = ((y1) + height + Y((dir))) % height;
}

private static void OFFSET(out int x2,out int y2,int x1,int y1, int dir, NetState state) 
{
	OFFSETWH(out x2, out y2,x1,y1,dir,(state).width,(state).height);
}
private static void OFFSET(out int x2, out int y2, int x1, int y1, int dir, NetSettings state)
{
    OFFSETWH(out x2, out y2, x1, y1, dir, (state).width, (state).height);
}
//private static byte  byte[] a[ int x, int y] { return ( a[(y) * (state).width + (x)] ); }
//private static byte  byte[] a[ int x, int y] { return (a[(y) * (state).width + (x)]); }
//private static byte NetState state.tiles[int x,int y] { return state.tiles[x, y]; }
//private static byte NetState state.barriers[int x,int y] { return state.barriers[x, y]; }
//private static byte  byte[[] a, int x, int y] { return a[x, y]; }
//private static byte  byte[[] a, int x, int y] { return a[x, y]; }

private static void IndexToXY(int w, int i, out int x, out int y)
{
    y = i / w;
    x = i % w;
}

static int xyd_cmp(NetPoint a, NetPoint b)
{
    if (a.x < b.x)
	return -1;
    if (a.x > b.x)
	return +1;
    if (a.y < b.y)
	return -1;
    if (a.y > b.y)
	return +1;
    if (a.direction < b.direction)
	return -1;
    if (a.direction > b.direction)
	return +1;
    return 0;
}



static NetPoint new_xyd(int x, int y, int direction)
{
    NetPoint xyd = new NetPoint();
    xyd.x = x;
    xyd.y = y;
    xyd.direction = direction;
    return xyd;
}

///* ----------------------------------------------------------------------
// * Manage game parameters.
// */
//static NetSettings default_@params(void)
//{
//    NetSettings ret = snew(game_@params);

//    ret.width = 5;
//    ret.height = 5;
//    ret.wrapping = false;
//    ret.unique = true;
//    ret.barrier_probability = 0.0;

//    return ret;
//}
        private static NetSettings[] presets = new[] {
            new NetSettings(5, 5, false, true, 0.0f),
            new NetSettings(7, 7, false, true, 0.0f),
            new NetSettings(9, 9, false, true, 0.0f),
            new NetSettings(11, 11, false, true, 0.0f),
            new NetSettings(13, 11, false, true, 0.0f),
            new NetSettings(5, 5, true, true, 0.0f),
            new NetSettings(7, 7, true, true, 0.0f),
            new NetSettings(9, 9, true, true, 0.0f),
            new NetSettings(11, 11, true, true, 0.0f),
            new NetSettings(13, 11, true, true, 0.0f)
        };

        public override NetSettings DefaultSettings
        {
            get { return presets[0]; }
        }

        public override IEnumerable<NetSettings> PresetsSettings
        {
            get { return presets; }
        }

        public override NetMove ParseMove(NetSettings settings, string moveString)
        {
            return NetMove.Parse(settings, moveString);
        }

        public override NetSettings ParseSettings(string settingsString)
        {
            return NetSettings.Parse(settingsString);
        }
//static void decode_@params(NetSettings ret, char const *string)
//{
//    char const *p = string;

//    ret.width = atoi(p);
//    while (*p && isdigit((byte)*p)) p++;
//    if (*p == 'x') {
//        p++;
//        ret.height = atoi(p);
//        while (*p && isdigit((byte)*p)) p++;
//    } else {
//        ret.height = ret.width;
//    }

//    while (*p) {
//        if (*p == 'w') {
//            p++;
//        ret.wrapping = true;
//    } else if (*p == 'b') {
//        p++;
//            ret.barrier_probability = (float)atof(p);
//        while (*p && (*p == '.' || isdigit((byte)*p))) p++;
//    } else if (*p == 'a') {
//            p++;
//        ret.unique = false;
//    } else
//        p++;		       /* skip any other gunk */
//    }
//}

//static char *encode_@params(NetSettings @params, int full)
//{
//    char ret[400];
//    int len;

//    len = sprintf(ret, "%dx%d", @params.width, @params.height);
//    if (@params.wrapping)
//        ret[len++] = 'w';
//    if (full && @params.barrier_probability)
//        len += sprintf(ret+len, "b%g", @params.barrier_probability);
//    if (full && !@params.unique)
//        ret[len++] = 'a';
//    Debug.Assert(len < lenof(ret));
//    ret[len] = '\0';

//    return dupstr(ret);
//}

//static config_item *game_configure(NetSettings @params)
//{
//    config_item *ret;
//    char buf[80];

//    ret = snewn(6, config_item);

//    ret[0].name = "Width";
//    ret[0].type = C_STRING;
//    sprintf(buf, "%d", @params.width);
//    ret[0].sval = dupstr(buf);
//    ret[0].ival = 0;

//    ret[1].name = "Height";
//    ret[1].type = C_STRING;
//    sprintf(buf, "%d", @params.height);
//    ret[1].sval = dupstr(buf);
//    ret[1].ival = 0;

//    ret[2].name = "Walls wrap around";
//    ret[2].type = C_BOOLEAN;
//    ret[2].sval = null;
//    ret[2].ival = @params.wrapping;

//    ret[3].name = "Barrier probability";
//    ret[3].type = C_STRING;
//    sprintf(buf, "%g", @params.barrier_probability);
//    ret[3].sval = dupstr(buf);
//    ret[3].ival = 0;

//    ret[4].name = "Ensure unique solution";
//    ret[4].type = C_BOOLEAN;
//    ret[4].sval = null;
//    ret[4].ival = @params.unique;

//    ret[5].name = null;
//    ret[5].type = C_END;
//    ret[5].sval = null;
//    ret[5].ival = 0;

//    return ret;
//}

//static NetSettings custom_@params(const config_item *cfg)
//{
//    NetSettings ret = snew(game_@params);

//    ret.width = atoi(cfg[0].sval);
//    ret.height = atoi(cfg[1].sval);
//    ret.wrapping = cfg[2].ival;
//    ret.barrier_probability = (float)atof(cfg[3].sval);
//    ret.unique = cfg[4].ival;

//    return ret;
//}

//static char *validate_@params(NetSettings @params, int full)
//{
//    if (@params.width <= 0 || @params.height <= 0)
//    return "Width and height must both be greater than zero";
//    if (@params.width <= 1 && @params.height <= 1)
//    return "At least one of width and height must be greater than one";
//    if (@params.barrier_probability < 0)
//    return "Barrier probability may not be negative";
//    if (@params.barrier_probability > 1)
//    return "Barrier probability may not be greater than 1";

//    /*
//     * Specifying either grid dimension as 2 in a wrapping puzzle
//     * makes it actually impossible to ensure a unique puzzle
//     * solution.
//     * 
//     * Proof:
//     * 
//     * Without loss of generality, let us assume the puzzle _width_
//     * is 2, so we can conveniently discuss rows without having to
//     * say `rows/columns' all the time. (The height may be 2 as
//     * well, but that doesn't matter.)
//     * 
//     * In each row, there are two edges between tiles: the inner
//     * edge (running down the centre of the grid) and the outer
//     * edge (the identified left and right edges of the grid).
//     * 
//     * Lemma: In any valid 2xn puzzle there must be at least one
//     * row in which _exactly one_ of the inner edge and outer edge
//     * is connected.
//     * 
//     *   Proof: No row can have _both_ inner and outer edges
//     *   connected, because this would yield a loop. So the only
//     *   other way to falsify the lemma is for every row to have
//     *   _neither_ the inner nor outer edge connected. But this
//     *   means there is no connection at all between the left and
//     *   right columns of the puzzle, so there are two disjoint
//     *   subgraphs, which is also disallowed. []
//     * 
//     * Given such a row, it is always possible to make the
//     * disconnected edge connected and the connected edge
//     * disconnected without changing the state of any other edge.
//     * (This is easily seen by case analysis on the various tiles:
//     * left-pointing and right-pointing endpoints can be exchanged,
//     * likewise T-pieces, and a corner piece can select its
//     * horizontal connectivity independently of its vertical.) This
//     * yields a distinct valid solution.
//     * 
//     * Thus, for _every_ row in which exactly one of the inner and
//     * outer edge is connected, there are two valid states for that
//     * row, and hence the total number of solutions of the puzzle
//     * is at least 2^(number of such rows), and in particular is at
//     * least 2 since there must be at least one such row. []
//     */
//    if (full && @params.unique && @params.wrapping &&
//        (@params.width == 2 || @params.height == 2))
//        return "No wrapping puzzle with a width or height of 2 can have"
//        " a unique solution";

//    return null;
//}

/* ----------------------------------------------------------------------
 * Solver used to assure solution uniqueness during generation. 
 */

/*
 * Test cases I used while debugging all this were
 * 
 *   ./net --generate 1 13x11w#12300
 * which expands under the non-unique grid generation rules to
 *   13x11w:5eaade1bd222664436d5e2965c12656b1129dd825219e3274d558d5eb2dab5da18898e571d5a2987be79746bd95726c597447d6da96188c513add829da7681da954db113d3cd244
 * and has two ambiguous areas.
 * 
 * An even better one is
 *   13x11w#507896411361192
 * which expands to
 *   13x11w:b7125b1aec598eb31bd58d82572bc11494e5dee4e8db2bdd29b88d41a16bdd996d2996ddec8c83741a1e8674e78328ba71737b8894a9271b1cd1399453d1952e43951d9b712822e
 * and has an ambiguous area _and_ a situation where loop avoidance
 * is a necessary deductive technique.
 * 
 * Then there's
 *   48x25w#820543338195187
 * becoming
 *   48x25w:255989d14cdd185deaa753a93821a12edc1ab97943ac127e2685d7b8b3c48861b2192416139212b316eddd35de43714ebc7628d753db32e596284d9ec52c5a7dc1b4c811a655117d16dc28921b2b4161352cab1d89d18bc836b8b891d55ea4622a1251861b5bc9a8aa3e5bcd745c95229ca6c3b5e21d5832d397e917325793d7eb442dc351b2db2a52ba8e1651642275842d8871d5534aabc6d5b741aaa2d48ed2a7dbbb3151ddb49d5b9a7ed1ab98ee75d613d656dbba347bc514c84556b43a9bc65a3256ead792488b862a9d2a8a39b4255a4949ed7dbd79443292521265896b4399c95ede89d7c8c797a6a57791a849adea489359a158aa12e5dacce862b8333b7ebea7d344d1a3c53198864b73a9dedde7b663abb1b539e1e8853b1b7edb14a2a17ebaae4dbe63598a2e7e9a2dbdad415bc1d8cb88cbab5a8c82925732cd282e641ea3bd7d2c6e776de9117a26be86deb7c82c89524b122cb9397cd1acd2284e744ea62b9279bae85479ababe315c3ac29c431333395b24e6a1e3c43a2da42d4dce84aadd5b154aea555eaddcbd6e527d228c19388d9b424d94214555a7edbdeebe569d4a56dc51a86bd9963e377bb74752bd5eaa5761ba545e297b62a1bda46ab4aee423ad6c661311783cc18786d4289236563cb4a75ec67d481c14814994464cd1b87396dee63e5ab6e952cc584baa1d4c47cb557ec84dbb63d487c8728118673a166846dd3a4ebc23d6cb9c5827d96b4556e91899db32b517eda815ae271a8911bd745447121dc8d321557bc2a435ebec1bbac35b1a291669451174e6aa2218a4a9c5a6ca31ebc45d84e3a82c121e9ced7d55e9a
 * which has a spot (far right) where slightly more complex loop
 * avoidance is required.
 */

class todo {
    internal bool[] marked;
    internal int[] buffer;
    internal int buflen;
    internal int head, tail;
}

static todo todo_new(int maxsize)
{
    todo todo = new todo();
    todo.marked = new bool[maxsize];
    todo.buflen = maxsize + 1;
    todo.buffer = new int[todo.buflen];
    todo.head = todo.tail = 0;
    return todo;
}


static void todo_add(todo todo, int index)
{
    if (todo.marked[index])
	return;			       /* already on the list */
    todo.marked[index] = true;
    todo.buffer[todo.tail++] = index;
    if (todo.tail == todo.buflen)
	todo.tail = 0;
}

static int todo_get(todo todo) {
    int ret;

    if (todo.head == todo.tail)
	return -1;		       /* list is empty */
    ret = todo.buffer[todo.head++];
    if (todo.head == todo.buflen)
	todo.head = 0;
    todo.marked[ret] = false;

    return ret;
}

static bool net_solver(int w, int h, byte[,] tiles,
		      byte[,] barriers, bool wrapping)
{
    byte[] tilestate;
    byte[] edgestate;
    int[] deadends;
    IList<int> equivalence;
    todo todo;
    int i, j, x, y;
    int area;
    bool done_something;

    /*
     * Set up the solver's data structures.
     */
    
    /*
     * tilestate stores the possible orientations of each tile.
     * There are up to four of these, so we'll index the array in
     * fours. tilestate[(y * w + x) * 4] and its three successive
     * members give the possible orientations, clearing to 255 from
     * the end as things are ruled out.
     * 
     * In this loop we also count up the area of the grid (which is
     * not _necessarily_ equal to w*h, because there might be one
     * or more blank squares present. This will never happen in a
     * grid generated _by_ this program, but it's worth keeping the
     * solver as general as possible.)
     */
    tilestate = new byte[w * h * 4];
    area = 0;
    for (i = 0; i < w*h; i++) {
        IndexToXY(w, i, out x, out y);
	    tilestate[i * 4] = (byte)(tiles[x,y] & 0xF);
	    for (j = 1; j < 4; j++) {
	        if (tilestate[i * 4 + j - 1] == 255 ||
		    A(tilestate[i * 4 + j - 1]) == tilestate[i * 4])
		    tilestate[i * 4 + j] = 255;
	        else
		    tilestate[i * 4 + j] = (byte)A(tilestate[i * 4 + j - 1]);
	    }
        if (tiles[x, y] != 0)
	        area++;
    }
    x = 0; y = 0;
        /*
         * edgestate stores the known state of each edge. It is 0 for
         * unknown, 1 for open (connected) and 2 for closed (not
         * connected).
         * 
         * In principle we need only worry about each edge once each,
         * but in fact it's easier to track each edge twice so that we
         * can reference it from either side conveniently. Also I'm
         * going to allocate _five_ bytes per tile, rather than the
         * obvious four, so that I can index edgestate[(y*w+x) * 5 + d]
         * where d is 1,2,4,8 and they never overlap.
         */
       edgestate = new byte[(w * h - 1) * 5 + 9];
    //memset(edgestate, 0, (w * h - 1) * 5 + 9);

    /*
     * deadends tracks which edges have dead ends on them. It is
     * indexed by tile and direction: deadends[(y*w+x) * 5 + d]
     * tells you whether heading out of tile (x,y) in direction d
     * can reach a limited amount of the grid. Values are area+1
     * (no dead end known) or less than that (can reach _at most_
     * this many other tiles by heading this way out of this tile).
     */
    deadends = new int[(w * h - 1) * 5 + 9];
    for (i = 0; i < (w * h - 1) * 5 + 9; i++)
	deadends[i] = area+1;

    /*
     * equivalence tracks which sets of tiles are known to be
     * connected to one another, so we can avoid creating loops by
     * linking together tiles which are already linked through
     * another route.
     * 
     * This is a disjoint set forest structure: equivalence[i]
     * contains the index of another member of the equivalence
     * class containing i, or contains i itself for precisely one
     * member in each such class. To find a representative member
     * of the equivalence class containing i, you keep replacing i
     * with equivalence[i] until it stops changing; then you go
     * _back_ along the same path and point everything on it
     * directly at the representative member so as to speed up
     * future searches. Then you test equivalence between tiles by
     * finding the representative of each tile and seeing if
     * they're the same; and you create new equivalence (merge
     * classes) by finding the representative of each tile and
     * setting equivalence[one]=the_other.
     */
    equivalence = Dsf.snew_dsf(w * h);

    /*
     * On a non-wrapping grid, we instantly know that all the edges
     * round the edge are closed.
     */
    if (!wrapping) {
	for (i = 0; i < w; i++) {
	    edgestate[i * 5 + 2] = edgestate[((h-1) * w + i) * 5 + 8] = 2;
	}
	for (i = 0; i < h; i++) {
	    edgestate[(i * w + w-1) * 5 + 1] = edgestate[(i * w) * 5 + 4] = 2;
	}
    }

    /*
     * If we have barriers available, we can mark those edges as
     * closed too.
     */
    if (barriers != null) {
	for (y = 0; y < h; y++) for (x = 0; x < w; x++) {
	    int d;
	    for (d = 1; d <= 8; d += d) {
		if ((barriers[x,y] & d) !=0) {
		    int x2, y2;
		    /*
		     * In principle the barrier list should already
		     * contain each barrier from each side, but
		     * let's not take chances with our internal
		     * consistency.
		     */
		    OFFSETWH(out x2, out y2, x, y, d, w, h);
		    edgestate[(y*w+x) * 5 + d] = 2;
		    edgestate[(y2*w+x2) * 5 + F(d)] = 2;
		}
	    }
	}
    }

    /*
     * Since most deductions made by this solver are local (the
     * exception is loop avoidance, where joining two tiles
     * together on one side of the grid can theoretically permit a
     * fresh deduction on the other), we can address the scaling
     * problem inherent in iterating repeatedly over the entire
     * grid by instead working with a to-do list.
     */
    todo = todo_new(w * h);

    /*
     * Main deductive loop.
     */
    done_something = true;	       /* prevent instant termination! */
    while (true) {
	int index;

	/*
	 * Take a tile index off the todo list and process it.
	 */
	index = todo_get(todo);
	if (index == -1) {
	    /*
	     * If we have run out of immediate things to do, we
	     * have no choice but to scan the whole grid for
	     * longer-range things we've missed. Hence, I now add
	     * every square on the grid back on to the to-do list.
	     * I also set `done_something' to false at this point;
	     * if we later come back here and find it still false,
	     * we will know we've scanned the entire grid without
	     * finding anything new to do, and we can terminate.
	     */
	    if (!done_something)
		break;
	    for (i = 0; i < w*h; i++)
		todo_add(todo, i);
	    done_something = false;

	    index = todo_get(todo);
	}

	y = index / w;
	x = index % w;
	{
	    int d, ourclass = Dsf.dsf_canonify(equivalence, y*w+x);
	    int[] deadendmax = new int[9];

	    deadendmax[1] = deadendmax[2] = deadendmax[4] = deadendmax[8] = 0;

	    for (i = j = 0; i < 4 && tilestate[(y*w+x) * 4 + i] != 255; i++) {
		bool valid;
		int nnondeadends, deadendtotal;
		int nequiv;
        int[] equiv = new int[5], nondeadends = new int[4];
		int val = tilestate[(y*w+x) * 4 + i];

		valid = true;
		nnondeadends = deadendtotal = 0;
		equiv[0] = ourclass;
		nequiv = 1;
		for (d = 1; d <= 8; d += d) {
		    /*
		     * Immediately rule out this orientation if it
		     * conflicts with any known edge.
		     */
		    if ((edgestate[(y*w+x) * 5 + d] == 1 && (val & d) == 0) ||
			(edgestate[(y*w+x) * 5 + d] == 2 && (val & d) != 0))
			valid = false;

		    if ((val & d) !=0) {
			/*
			 * Count up the dead-end statistics.
			 */
			if (deadends[(y*w+x) * 5 + d] <= area) {
			    deadendtotal += deadends[(y*w+x) * 5 + d];
			} else {
			    nondeadends[nnondeadends++] = d;
			}

			/*
			 * Ensure we aren't linking to any tiles,
			 * through edges not already known to be
			 * open, which create a loop.
			 */
			if (edgestate[(y*w+x) * 5 + d] == 0) {
			    int c, k, x2, y2;
			    
			    OFFSETWH(out x2, out y2, x, y, d, w, h);
			    c = Dsf.dsf_canonify(equivalence, y2*w+x2);
			    for (k = 0; k < nequiv; k++)
				if (c == equiv[k])
				    break;
			    if (k == nequiv)
				equiv[nequiv++] = c;
			    else
				valid = false;
			}
		    }
		}

		if (nnondeadends == 0) {
		    /*
		     * If this orientation links together dead-ends
		     * with a total area of less than the entire
		     * grid, it is invalid.
		     *
		     * (We add 1 to deadendtotal because of the
		     * tile itself, of course; one tile linking
		     * dead ends of size 2 and 3 forms a subnetwork
		     * with a total area of 6, not 5.)
		     */
		    if (deadendtotal > 0 && deadendtotal+1 < area)
			valid = false;
		} else if (nnondeadends == 1) {
		    /*
		     * If this orientation links together one or
		     * more dead-ends with precisely one
		     * non-dead-end, then we may have to mark that
		     * non-dead-end as a dead end going the other
		     * way. However, it depends on whether all
		     * other orientations share the same property.
		     */
		    deadendtotal++;
		    if (deadendmax[nondeadends[0]] < deadendtotal)
			deadendmax[nondeadends[0]] = deadendtotal;
		} else {
		    /*
		     * If this orientation links together two or
		     * more non-dead-ends, then we can rule out the
		     * possibility of putting in new dead-end
		     * markings in those directions.
		     */
		    int k;
		    for (k = 0; k < nnondeadends; k++)
			deadendmax[nondeadends[k]] = area+1;
		}

		if (valid)
		    tilestate[(y*w+x) * 4 + j++] = (byte)val;
#if SOLVER_DIAGNOSTICS
		else
		    printf("ruling out orientation %x at %d,%d\n", val, x, y);
#endif
	    }

	    Debug.Assert(j > 0);	       /* we can't lose _all_ possibilities! */

	    if (j < i) {
		done_something = true;

		/*
		 * We have ruled out at least one tile orientation.
		 * Make sure the rest are blanked.
		 */
		while (j < 4)
		    tilestate[(y*w+x) * 4 + j++] = 255;
	    }

	    /*
	     * Now go through the tile orientations again and see
	     * if we've deduced anything new about any edges.
	     */
	    {
		int a, o;
		a = 0xF; o = 0;

		for (i = 0; i < 4 && tilestate[(y*w+x) * 4 + i] != 255; i++) {
		    a &= tilestate[(y*w+x) * 4 + i];
		    o |= tilestate[(y*w+x) * 4 + i];
		}
		for (d = 1; d <= 8; d += d)
		    if (edgestate[(y*w+x) * 5 + d] == 0) {
			int x2, y2, d2;
			OFFSETWH(out x2, out y2, x, y, d, w, h);
			d2 = F(d);
			if ((a & d) != 0) {
			    /* This edge is open in all orientations. */
#if SOLVER_DIAGNOSTICS
			    printf("marking edge %d,%d:%d open\n", x, y, d);
#endif
			    edgestate[(y*w+x) * 5 + d] = 1;
			    edgestate[(y2*w+x2) * 5 + d2] = 1;
			    Dsf.dsf_merge(equivalence, y*w+x, y2*w+x2);
			    done_something = true;
			    todo_add(todo, y2*w+x2);
			} else if ((o & d)==0) {
			    /* This edge is closed in all orientations. */
#if SOLVER_DIAGNOSTICS
			    printf("marking edge %d,%d:%d closed\n", x, y, d);
#endif
			    edgestate[(y*w+x) * 5 + d] = 2;
			    edgestate[(y2*w+x2) * 5 + d2] = 2;
			    done_something = true;
			    todo_add(todo, y2*w+x2);
			}
		    }

	    }

	    /*
	     * Now check the dead-end markers and see if any of
	     * them has lowered from the real ones.
	     */
	    for (d = 1; d <= 8; d += d) {
		int x2, y2, d2;
		OFFSETWH(out x2, out y2, x, y, d, w, h);
		d2 = F(d);
		if (deadendmax[d] > 0 &&
		    deadends[(y2*w+x2) * 5 + d2] > deadendmax[d]) {
#if SOLVER_DIAGNOSTICS
		    printf("setting dead end value %d,%d:%d to %d\n",
			   x2, y2, d2, deadendmax[d]);
#endif
		    deadends[(y2*w+x2) * 5 + d2] = deadendmax[d];
		    done_something = true;
		    todo_add(todo, y2*w+x2);
		}
	    }

	}
    }

    /*
     * Mark all completely determined tiles as locked.
     */
    bool j2 = true;
    for (i = 0; i < w*h; i++) {
        IndexToXY(w, i, out x, out y);
	if (tilestate[i * 4 + 1] == 255) {
	    Debug.Assert(tilestate[i * 4 + 0] != 255);
	    tiles[x,y] = (byte)(tilestate[i * 4] | LOCKED);
	} else {
        tiles[x, y] = (byte)(tiles[x, y] & ~LOCKED);
	    j2 = false;
	}
    }

    return j2;
}

/* ----------------------------------------------------------------------
 * Randomly select a new game description.
 */

/*
 * Function to randomly perturb an ambiguous section in a grid, to
 * attempt to ensure unique solvability.
 */
static void perturb(int w, int h, byte[,] tiles, bool wrapping,
		    Random rs, int startx, int starty, int startd)
{
    List<NetPoint> perimeter;
    NetPoint[] perim2, looppos = new[] { new NetPoint(), new NetPoint() }; ;
    NetPoint[][] loop = new NetPoint[2][];
    int nperim;
    int[] nloop = new int[2], loopsize = new int[2];
    int x, y, d, i;

    /*
     * We know that the tile at (startx,starty) is part of an
     * ambiguous section, and we also know that its neighbour in
     * direction startd is fully specified. We begin by tracing all
     * the way round the ambiguous area.
     */
    nperim /*= perimsize*/ = 0;
    perimeter = new List<NetPoint>();
    x = startx;
    y = starty;
    d = startd;
#if PERTURB_DIAGNOSTICS
    printf("perturb %d,%d:%d\n", x, y, d);
#endif
    do {
	int x2, y2, d2;
    perimeter.Add(new NetPoint());
	perimeter[nperim].x = x;
	perimeter[nperim].y = y;
	perimeter[nperim].direction = d;
	nperim++;
#if PERTURB_DIAGNOSTICS
	printf("perimeter: %d,%d:%d\n", x, y, d);
#endif

	/*
	 * First, see if we can simply turn left from where we are
	 * and find another locked square.
	 */
	d2 = A(d);
	OFFSETWH(out x2, out y2, x, y, d2, w, h);
	if ((!wrapping && (Math.Abs(x2-x) > 1 || Math.Abs(y2-y) > 1)) ||
	    (tiles[x2,y2] & LOCKED) != 0) {
	    d = d2;
	} else {
	    /*
	     * Failing that, step left into the new square and look
	     * in front of us.
	     */
	    x = x2;
	    y = y2;
	    OFFSETWH(out x2, out y2, x, y, d, w, h);
	    if ((wrapping || (Math.Abs(x2-x) <= 1 && Math.Abs(y2-y) <= 1)) &&
		(tiles[x2,y2] & LOCKED)==0) {
		/*
		 * And failing _that_, we're going to have to step
		 * forward into _that_ square and look right at the
		 * same locked square as we started with.
		 */
		x = x2;
		y = y2;
		d = C(d);
	    }
	}

    } while (x != startx || y != starty || d != startd);

    /*
     * Our technique for perturbing this ambiguous area is to
     * search round its edge for a join we can make: that is, an
     * edge on the perimeter which is (a) not currently connected,
     * and (b) connecting it would not yield a full cross on either
     * side. Then we make that join, search round the network to
     * find the loop thus constructed, and sever the loop at a
     * randomly selected other point.
     */
    perim2 = perimeter.Take(nperim).Select(p => p.Clone()).ToArray(); 
    /* Shuffle the perimeter, so as to search it without directional bias. */
    perim2.Shuffle(nperim, rs);
    for (i = 0; i < nperim; i++) {
	int x2, y2;

	x = perim2[i].x;
	y = perim2[i].y;
	d = perim2[i].direction;

	OFFSETWH(out x2, out y2, x, y, d, w, h);
	if (!wrapping && (Math.Abs(x2-x) > 1 || Math.Abs(y2-y) > 1))
	    continue;            /* can't link across non-wrapping border */
	if ((tiles[x,y] & d)!=0)
	    continue;		       /* already linked in this direction! */
	if (((tiles[x,y] | d) & 15) == 15)
	    continue;		       /* can't turn this tile into a cross */
	if (((tiles[x2,y2] | F(d)) & 15) == 15)
	    continue;		       /* can't turn other tile into a cross */

	/*
	 * We've found the point at which we're going to make a new
	 * link.
	 */
#if PERTURB_DIAGNOSTICS	
	printf("linking %d,%d:%d\n", x, y, d);
#endif
	tiles[x, y] |= (byte) d;
    tiles[x2, y2] |= (byte) F(d);

	break;
    }

    if (i == nperim) {
	return;			       /* nothing we can do! */
    }

    /*
     * Now we've constructed a new link, we need to find the entire
     * loop of which it is a part.
     * 
     * In principle, this involves doing a complete search round
     * the network. However, I anticipate that in the vast majority
     * of cases the loop will be quite small, so what I'm going to
     * do is make _two_ searches round the network in parallel, one
     * keeping its metaphorical hand on the left-hand wall while
     * the other keeps its hand on the right. As soon as one of
     * them gets back to its starting point, I abandon the other.
     */
    for (i = 0; i < 2; i++) {
	loopsize[i] = nloop[i] = 0;
	loop[i] = null;
	looppos[i].x = x;
	looppos[i].y = y;
	looppos[i].direction = d;
    }
    while (true) {
	for (i = 0; i < 2; i++) {
	    int x2, y2, j;

	    x = looppos[i].x;
	    y = looppos[i].y;
	    d = looppos[i].direction;

        OFFSETWH(out x2, out y2, x, y, d, w, h);

	    /*
	     * Add this path segment to the loop, unless it exactly
	     * reverses the previous one on the loop in which case
	     * we take it away again.
	     */
#if PERTURB_DIAGNOSTICS
	    printf("looppos[%d] = %d,%d:%d\n", i, x, y, d);
#endif
	    if (nloop[i] > 0 &&
		loop[i][nloop[i]-1].x == x2 &&
		loop[i][nloop[i]-1].y == y2 &&
		loop[i][nloop[i]-1].direction == F(d)) {
#if PERTURB_DIAGNOSTICS
		printf("removing path segment %d,%d:%d from loop[%d]\n",
		       x2, y2, F(d), i);
#endif
		nloop[i]--;
	    } else {
		if (nloop[i] >= loopsize[i]) {
		    loopsize[i] = loopsize[i] * 3 / 2 + 32;
            Array.Resize(ref loop[i], loopsize[i]);
            loop[i] = loop[i].Select(p => p ?? new NetPoint()).ToArray(); // Fill empty slots
		}
#if PERTURB_DIAGNOSTICS
		printf("adding path segment %d,%d:%d to loop[%d]\n",
		       x, y, d, i);
#endif
		loop[i][nloop[i]++] = looppos[i].Clone();
	    }

#if PERTURB_DIAGNOSTICS
	    printf("tile at new location is %x\n", tiles[y2*w+x2] & 0xF);
#endif
	    d = F(d);
	    for (j = 0; j < 4; j++) {
		if (i == 0)
		    d = A(d);
		else
		    d = C(d);
#if PERTURB_DIAGNOSTICS
		printf("trying dir %d\n", d);
#endif
		if ((tiles[x2,y2] & d) != 0) {
		    looppos[i].x = x2;
		    looppos[i].y = y2;
		    looppos[i].direction = d;
		    break;
		}
	    }

	    Debug.Assert(j < 4);
	    Debug.Assert(nloop[i] > 0);

	    if (looppos[i].x == loop[i][0].x &&
		looppos[i].y == loop[i][0].y &&
		looppos[i].direction == loop[i][0].direction) {
#if PERTURB_DIAGNOSTICS
		printf("loop %d finished tracking\n", i);
#endif

		/*
		 * Having found our loop, we now sever it at a
		 * randomly chosen point - absolutely any will do -
		 * which is not the one we joined it at to begin
		 * with. Conveniently, the one we joined it at is
		 * loop[i][0], so we just avoid that one.
		 */
		j = rs.Next(0, nloop[i]-1) + 1;
		x = loop[i][j].x;
		y = loop[i][j].y;
		d = loop[i][j].direction;
        OFFSETWH(out x2, out y2, x, y, d, w, h);
		tiles[x,y] &= (byte)~d;
        tiles[x2,y2] &= (byte)~F(d);

		break;
	    }
	}
	if (i < 2)
	    break;
    }

    /*
     * Finally, we must mark the entire disputed section as locked,
     * to prevent the perturb function being called on it multiple
     * times.
     * 
     * To do this, we _sort_ the perimeter of the area. The
     * existing xyd_cmp function will arrange things into columns
     * for us, in such a way that each column has the edges in
     * vertical order. Then we can work down each column and fill
     * in all the squares between an up edge and a down edge.
     */
    perimeter.Sort(xyd_cmp);
    x = y = -1;
    for (i = 0; i <= nperim; i++) {
	if (i == nperim || perimeter[i].x > x) {
	    /*
	     * Fill in everything from the last Up edge to the
	     * bottom of the grid, if necessary.
	     */
	    if (x != -1) {
		while (y < h) {
#if PERTURB_DIAGNOSTICS
		    printf("resolved: locking tile %d,%d\n", x, y);
#endif
		    tiles[x,y] |= LOCKED;
		    y++;
		}
		x = y = -1;
	    }

	    if (i == nperim)
		break;

	    x = perimeter[i].x;
	    y = 0;
	}

	if (perimeter[i].direction == U) {
	    x = perimeter[i].x;
	    y = perimeter[i].y;
	} else if (perimeter[i].direction == D) {
	    /*
	     * Fill in everything from the last Up edge to here.
	     */
	    Debug.Assert(x == perimeter[i].x && y <= perimeter[i].y);
	    while (y <= perimeter[i].y) {
#if PERTURB_DIAGNOSTICS
		printf("resolved: locking tile %d,%d\n", x, y);
#endif
		tiles[x,y] |= LOCKED;
		y++;
	    }
	    x = y = -1;
	}
    }
}

public override string GenerateNewGameDescription(NetSettings @params, Random rs, out string aux, int interactive)
{
    Tree234<NetPoint> possibilities, barriertree;
    int w, h, x, y, cx, cy, nbarriers;
    byte[,] tiles, barriers;

    w = @params.width;
    h = @params.height;

    cx = w / 2;
    cy = h / 2;

    tiles = new byte[w,h];
    barriers = new byte[w,h];

    begin_generation:

    Array.Clear(tiles, 0, w * h);//memset(tiles, 0, w * h);
    Array.Clear(barriers, 0, w * h);//memset(barriers, 0, w * h);

    /*
     * Construct the unshuffled grid.
     * 
     * To do this, we simply start at the centre point, repeatedly
     * choose a random possibility out of the available ways to
     * extend a used square into an unused one, and do it. After
     * extending the third line out of a square, we remove the
     * fourth from the possibilities list to avoid any full-cross
     * squares (which would make the game too easy because they
     * only have one orientation).
     * 
     * The slightly worrying thing is the avoidance of full-cross
     * squares. Can this cause our unsophisticated construction
     * algorithm to paint itself into a corner, by getting into a
     * situation where there are some unreached squares and the
     * only way to reach any of them is to extend a T-piece into a
     * full cross?
     * 
     * Answer: no it can't, and here's a proof.
     * 
     * Any contiguous group of such unreachable squares must be
     * surrounded on _all_ sides by T-pieces pointing away from the
     * group. (If not, then there is a square which can be extended
     * into one of the `unreachable' ones, and so it wasn't
     * unreachable after all.) In particular, this implies that
     * each contiguous group of unreachable squares must be
     * rectangular in shape (any deviation from that yields a
     * non-T-piece next to an `unreachable' square).
     * 
     * So we have a rectangle of unreachable squares, with T-pieces
     * forming a solid border around the rectangle. The corners of
     * that border must be connected (since every tile connects all
     * the lines arriving in it), and therefore the border must
     * form a closed loop around the rectangle.
     * 
     * But this can't have happened in the first place, since we
     * _know_ we've avoided creating closed loops! Hence, no such
     * situation can ever arise, and the naive grid construction
     * algorithm will guaranteeably result in a complete grid
     * containing no unreached squares, no full crosses _and_ no
     * closed loops. []
     */
    possibilities = new Tree234<NetPoint>(xyd_cmp);

    if (cx+1 < w)
	possibilities.add234(new_xyd(cx, cy, R));
    if (cy-1 >= 0)
	possibilities.add234(new_xyd(cx, cy, U));
    if (cx-1 >= 0)
	possibilities.add234(new_xyd(cx, cy, L));
    if (cy+1 < h)
	possibilities.add234(new_xyd(cx, cy, D));

    while (possibilities.count234() > 0)
    {
	int i;
	NetPoint xyd;
	int x1, y1, d1, x2, y2, d2, d;

	/*
	 * Extract a randomly chosen possibility from the list.
	 */
    i = rs.Next(0, possibilities.count234());
    xyd = possibilities.delpos234(i);
	x1 = xyd.x;
	y1 = xyd.y;
	d1 = xyd.direction;

	OFFSET(out x2, out y2, x1, y1, d1, @params);
	d2 = F(d1);
#if GENERATION_DIAGNOSTICS
	printf("picked (%d,%d,%c) <. (%d,%d,%c)\n",
	       x1, y1, "0RU3L567D9abcdef"[d1], x2, y2, "0RU3L567D9abcdef"[d2]);
#endif

	/*
	 * Make the connection. (We should be moving to an as yet
	 * unused tile.)
	 */
	 tiles[ x1, y1] |= (byte)d1;
	Debug.Assert( tiles[ x2, y2] == 0);
    tiles[x2, y2] |= (byte)d2;

	/*
	 * If we have created a T-piece, remove its last
	 * possibility.
	 */
	if (COUNT( tiles[ x1, y1]) == 3) {
	    NetPoint xyd1 = new NetPoint(), xydp;

	    xyd1.x = x1;
	    xyd1.y = y1;
	    xyd1.direction = 0x0F ^  tiles[ x1, y1];

	    xydp = possibilities.find234(xyd1, null);

	    if (xydp != null) {
#if GENERATION_DIAGNOSTICS
		printf("T-piece; removing (%d,%d,%c)\n",
		       xydp.x, xydp.y, "0RU3L567D9abcdef"[xydp.direction]);
#endif
		possibilities.del234(xydp);
	    }
	}

	/*
	 * Remove all other possibilities that were pointing at the
	 * tile we've just moved into.
	 */
	for (d = 1; d < 0x10; d <<= 1) {
	    int x3, y3, d3;
	    NetPoint xyd1 = new NetPoint(), xydp;

        OFFSET(out x3, out y3, x2, y2, d, @params);
	    d3 = F(d);

	    xyd1.x = x3;
	    xyd1.y = y3;
	    xyd1.direction = d3;

	    xydp = possibilities.find234(xyd1, null);

	    if (xydp != null) {
#if GENERATION_DIAGNOSTICS
		printf("Loop avoidance; removing (%d,%d,%c)\n",
		       xydp.x, xydp.y, "0RU3L567D9abcdef"[xydp.direction]);
#endif
		possibilities.del234(xydp);
	    }
	}

	/*
	 * Add new possibilities to the list for moving _out_ of
	 * the tile we have just moved into.
	 */
	for (d = 1; d < 0x10; d <<= 1) {
	    int x3, y3;

	    if (d == d2)
		continue;	       /* we've got this one already */

	    if (!@params.wrapping) {
		if (d == U && y2 == 0)
		    continue;
		if (d == D && y2 == h-1)
		    continue;
		if (d == L && x2 == 0)
		    continue;
		if (d == R && x2 == w-1)
		    continue;
	    }

        OFFSET(out x3, out y3, x2, y2, d, @params);

	    if ( tiles[ x3, y3] != 0)
		continue;	       /* this would create a loop */

#if GENERATION_DIAGNOSTICS
	    printf("New frontier; adding (%d,%d,%c)\n",
		   x2, y2, "0RU3L567D9abcdef"[d]);
#endif
	    possibilities.add234(new_xyd(x2, y2, d));
	}
    }
    /* Having done that, we should have no possibilities remaining. */
    Debug.Assert(possibilities.count234() == 0);

    if (@params.unique) {
	int prevn = -1;

	/*
	 * Run the solver to check unique solubility.
	 */
	while (!net_solver(w, h, tiles, null, @params.wrapping)) {
	    int n = 0;

	    /*
	     * We expect (in most cases) that most of the grid will
	     * be uniquely specified already, and the remaining
	     * ambiguous sections will be small and separate. So
	     * our strategy is to find each individual such
	     * section, and perform a perturbation on the network
	     * in that area.
	     */
	    for (y = 0; y < h; y++) for (x = 0; x < w; x++) {
		if (x+1 < w && ((tiles[x,y] ^ tiles[x+1,y]) & LOCKED) != 0) {
		    n++;
		    if ((tiles[x,y] & LOCKED)!=0)
			perturb(w, h, tiles, @params.wrapping, rs, x+1, y, L);
		    else
			perturb(w, h, tiles, @params.wrapping, rs, x, y, R);
		}
        if (y + 1 < h && ((tiles[x, y] ^ tiles[x, y + 1]) & LOCKED) != 0)
        {
		    n++;
		    if ((tiles[x,y] & LOCKED)!=0)
			perturb(w, h, tiles, @params.wrapping, rs, x, y+1, U);
		    else
			perturb(w, h, tiles, @params.wrapping, rs, x, y, D);
		}
	    }

	    /*
	     * Now n counts the number of ambiguous sections we
	     * have fiddled with. If we haven't managed to decrease
	     * it from the last time we ran the solver, give up and
	     * regenerate the entire grid.
	     */
	    if (prevn != -1 && prevn <= n)
		goto begin_generation; /* (sorry) */

	    prevn = n;
	}

	/*
	 * The solver will have left a lot of LOCKED bits lying
	 * around in the tiles array. Remove them.
	 */
    for (y = 0; y < h; y++) 
	for (x = 0; x < w; x++) 
	    tiles[x,y] = (byte)(tiles[x,y] & ~LOCKED);
    
    }

    /*
     * Now compute a list of the possible barrier locations.
     */
    barriertree = new Tree234<NetPoint>(xyd_cmp);
    for (y = 0; y < h; y++) {
	for (x = 0; x < w; x++) {

	    if (( tiles[ x, y] & R) == 0 &&
                (@params.wrapping || x < w-1))
		barriertree.add234(new_xyd(x, y, R));
        if (( tiles[ x, y] & D) == 0 &&
                (@params.wrapping || y < h-1))
		barriertree.add234(new_xyd(x, y, D));
	}
    }

    ///*
    // * Save the unshuffled grid in aux.
    // */
    //{
    //char *solution;
    //    int i;

    //solution = snewn(w * h + 1, char);
    //    for (i = 0; i < w * h; i++)
    //        solution[i] = "0123456789abcdef"[tiles[i] & 0xF];
    //    solution[w*h] = '\0';

    //*aux = solution;
    //}

    /*
     * Now shuffle the grid.
     * 
     * In order to avoid accidentally generating an already-solved
     * grid, we will reshuffle as necessary to ensure that at least
     * one edge has a mismatched connection.
     *
     * This can always be done, since validate_@params() enforces a
     * grid area of at least 2 and our generator never creates
     * either type of rotationally invariant tile (cross and
     * blank). Hence there must be at least one edge separating
     * distinct tiles, and it must be possible to find orientations
     * of those tiles such that one tile is trying to connect
     * through that edge and the other is not.
     * 
     * (We could be more subtle, and allow the shuffle to generate
     * a grid in which all tiles match up locally and the only
     * criterion preventing the grid from being already solved is
     * connectedness. However, that would take more effort, and
     * it's easier to simply make sure every grid is _obviously_
     * not solved.)
     */
    while (true) {
        int mismatches;

        for (y = 0; y < h; y++) {
            for (x = 0; x < w; x++) {
                int orig =  tiles[ x, y];
                int rot = rs.Next(0, 4);
                tiles[x, y] = (byte)ROT(orig, rot);
            }
        }

        mismatches = 0;
        /*
         * I can't even be bothered to check for mismatches across
         * a wrapping edge, so I'm just going to enforce that there
         * must be a mismatch across a non-wrapping edge, which is
         * still always possible.
         */
        for (y = 0; y < h; y++) for (x = 0; x < w; x++) {
            if (x+1 < w && ((ROT( tiles[ x, y], 2) ^
                              tiles[ x + 1, y]) & L) != 0)
                mismatches++;
            if (y+1 < h && ((ROT( tiles[ x, y], 2) ^
                              tiles[ x, y + 1]) & U) != 0)
                mismatches++;
        }

        if (mismatches > 0)
            break;
    }

    /*
     * And now choose barrier locations. (We carefully do this
     * _after_ shuffling, so that changing the barrier rate in the
     * @params while keeping the random seed the same will give the
     * same shuffled grid and _only_ change the barrier locations.
     * Also the way we choose barrier locations, by repeatedly
     * choosing one possibility from the list until we have enough,
     * is designed to ensure that raising the barrier rate while
     * keeping the seed the same will provide a superset of the
     * previous barrier set - i.e. if you ask for 10 barriers, and
     * then decide that's still too hard and ask for 20, you'll get
     * the original 10 plus 10 more, rather than getting 20 new
     * ones and the chance of remembering your first 10.)
     */
    nbarriers = (int)(@params.barrier_probability * barriertree.count234());
    Debug.Assert(nbarriers >= 0 && nbarriers <= barriertree.count234());

    while (nbarriers > 0) {
	int i;
	NetPoint xyd;
	int x1, y1, d1, x2, y2, d2;

	/*
	 * Extract a randomly chosen barrier from the list.
	 */
    i = rs.Next(0, barriertree.count234());
	xyd = barriertree.delpos234(i);

	Debug.Assert(xyd != null);

	x1 = xyd.x;
	y1 = xyd.y;
	d1 = xyd.direction;
    //sfree(xyd);

	OFFSET(out x2, out y2, x1, y1, d1, @params);
	d2 = F(d1);

    barriers[x1, y1] |= (byte)d1;
    barriers[x2, y2] |= (byte)d2;

	nbarriers--;
    }

    ///*
    // * Clean up the rest of the barrier list.
    // */
    //{
    //NetPoint xyd;

    //while ( (xyd = delpos234(barriertree, 0)) != null)
    //    sfree(xyd);

    //freetree234(barriertree);
    //}

    /*
     * Finally, encode the grid into a string game description.
     * 
     * My syntax is extremely simple: each square is encoded as a
     * hex digit in which bit 0 means a connection on the right,
     * bit 1 means up, bit 2 left and bit 3 down. (i.e. the same
     * encoding as used internally). Each digit is followed by
     * optional barrier indicators: `v' means a vertical barrier to
     * the right of it, and `h' means a horizontal barrier below
     * it.
     */
    var desc = new StringBuilder();
    for (y = 0; y < h; y++) {
        for (x = 0; x < w; x++) {
            desc.Append( "0123456789abcdef"[ tiles[ x, y]]);
            if ((@params.wrapping || x < w-1) &&
                ( barriers[ x, y] & R) != 0)
                desc.Append( 'v');
            if ((@params.wrapping || y < h-1) &&
                ( barriers[ x, y] & D) != 0)
                desc.Append( 'h');
        }
    }
    aux = null;
    return desc.ToString();
}

static string validate_desc(NetSettings @params, string desc)
{
    int w = @params.width, h = @params.height;
    int i;
    int pos = 0;

    for (i = 0; i < w*h; i++) {
        if (pos >= desc.Length)
            return "Game description shorter than expected";
        else if (desc[pos] >= '0' && desc[pos] <= '9')
            /* OK */;
        else if (desc[pos] >= 'a' && desc[pos] <= 'f')
            /* OK */;
        else if (desc[pos] >= 'A' && desc[pos] <= 'F')
            /* OK */;
        else 
            return "Game description contained unexpected character";
        pos++;
        while (desc[pos] == 'h' || desc[pos] == 'v')
            pos++;
    }
    if (pos < desc.Length)
        return "Game description longer than expected";

    return null;
}

/* ----------------------------------------------------------------------
 * Construct an initial game state, given a description and parameters.
 */

public override NetState CreateNewGameFromDescription(NetSettings @params, string desc)
{
    NetState state;
    int w, h, x, y;

    Debug.Assert(@params.width > 0 && @params.height > 0);
    Debug.Assert(@params.width > 1 || @params.height > 1);

    /*
     * Create a blank game state.
     */
    state = new NetState();
    w = state.width = @params.width;
    h = state.height = @params.height;
    state.wrapping = @params.wrapping;
    state.last_rotate_dir = state.last_rotate_x = state.last_rotate_y = 0;
    state.completed = state.used_solve = false;
    state.tiles = new byte[state.width,state.height];
    //memset(state.tiles, 0, state.width * state.height);
    state.barriers = new byte[state.width,state.height];
    //memset(state.barriers, 0, state.width * state.height);

    /*
     * Parse the game description into the grid.
     */
    int pos = 0;
    for (y = 0; y < h; y++) {
        for (x = 0; x < w; x++) {
            if (desc[pos] >= '0' && desc[pos] <= '9')
                state.tiles[x, y] = (byte)(desc[pos] - '0');
            else if (desc[pos] >= 'a' && desc[pos] <= 'f')
                state.tiles[x, y] = (byte)(desc[pos] - 'a' + 10);
            else if (desc[pos] >= 'A' && desc[pos] <= 'F')
                state.tiles[x, y] = (byte)(desc[pos] - 'A' + 10);
            if (pos<desc.Length)
                pos++;
            while (pos < desc.Length && (desc[pos] == 'h' || desc[pos] == 'v'))
            {
                int x2, y2, d1, d2;
                if (desc[pos] == 'v')
                    d1 = R;
                else
                    d1 = D;

                OFFSET(out x2, out y2, x, y, d1, state);
                d2 = F(d1);

                state.barriers[x, y] |= (byte)d1;
                state.barriers[x2, y2] |= (byte)d2;

                pos++;
            }
        }
    }

    /*
     * Set up border barriers if this is a non-wrapping game.
     */
    if (!state.wrapping) {
	for (x = 0; x < state.width; x++) {
	    state.barriers[x,0] |= U;
	    state.barriers[x,state.height-1] |= D;
	}
	for (y = 0; y < state.height; y++) {
	    state.barriers[0,y] |= L;
	    state.barriers[state.width-1,y] |= R;
	}
    } else {
        /*
         * We check whether this is de-facto a non-wrapping game
         * despite the parameters, in case we were passed the
         * description of a non-wrapping game. This is so that we
         * can change some aspects of the UI behaviour.
         */
        state.wrapping = false;
        for (x = 0; x < state.width; x++)
            if ((state.barriers[x,0] & U)==0 ||
                (state.barriers[x,state.height-1] & D)==0)
                state.wrapping = true;
        for (y = 0; y < state.height; y++)
            if ((state.barriers[0,y] & L)==0 ||
                (state.barriers[state.width-1,y] & R)==0)
                state.wrapping = true;
    }

    return state;
}

static NetState dup_game(NetState state)
{
    NetState ret = new NetState();
    ret.width = state.width;
    ret.height = state.height;
    ret.wrapping = state.wrapping;
    ret.completed = state.completed;
    ret.used_solve = state.used_solve;
    ret.last_rotate_dir = state.last_rotate_dir;
    ret.last_rotate_x = state.last_rotate_x;
    ret.last_rotate_y = state.last_rotate_y;
    ret.tiles = new byte[state.width,state.height];
    Array.Copy(state.tiles, ret.tiles, state.width * state.height);
    ret.barriers = new byte[state.width, state.height];
    Array.Copy(state.barriers, ret.barriers, state.width * state.height);
    return ret;
}
public override NetMove CreateSolveGameMove(NetState state, NetState currstate, NetMove ai, out string error)
{
    byte[,] tiles;
    int i;

    tiles = new byte[state.width,state.height];

    //if (!aux) {
	/*
	 * Run the internal solver on the provided grid. This might
	 * not yield a complete solution.
	 */
	Array.Copy(state.tiles, tiles, state.width * state.height);
	net_solver(state.width, state.height, tiles,
		   state.barriers, state.wrapping);
    //} else {
    //    for (i = 0; i < state.width * state.height; i++) {
    //        int c = aux[i];

    //        if (c >= '0' && c <= '9')
    //            tiles[i] = c - '0';
    //        else if (c >= 'a' && c <= 'f')
    //            tiles[i] = c - 'a' + 10;
    //        else if (c >= 'A' && c <= 'F')
    //            tiles[i] = c - 'A' + 10;

    //    tiles[i] |= LOCKED;
    //    }
    //}

    /*
     * Now construct a string which can be passed to execute_move()
     * to transform the current grid into the solved one.
     */
    NetMove ret = new NetMove();
    ret.isSolve = true;

    for (i = 0; i < state.width * state.height; i++) {
	int x = i % state.width, y = i / state.width;
	int from = currstate.tiles[x,y], to = tiles[x,y];
	int ft = from & (R|L|U|D), tt = to & (R|L|U|D);
    NetMoveType? chr;
	if (from == to)
	    continue;		       /* nothing needs doing at all */

	/*
	 * To transform this tile into the desired tile: first
	 * unlock the tile if it's locked, then rotate it if
	 * necessary, then lock it if necessary.
	 */
	if ((from & LOCKED) != 0)
        ret.points.Add(new NetMovePoint(NetMoveType.L, x, y));


	if (tt == A(ft))
	    chr = NetMoveType.A;
	else if (tt == C(ft))
	    chr = NetMoveType.C;
	else if (tt == F(ft))
	    chr = NetMoveType.F;
	else {
	    Debug.Assert(tt == ft);
	    chr = null;
	}
	if (chr != null)
        ret.points.Add(new NetMovePoint(chr.Value,x,y));

	if ((to & LOCKED)!=0)
        ret.points.Add(new NetMovePoint(NetMoveType.L,x,y));

    }
    error = null;
    return ret;
}

//static int game_can_format_as_text_now(NetSettings @params)
//{
//    return true;
//}

//static char *game_text_format(NetState state)
//{
//    return null;
//}

/* ----------------------------------------------------------------------
 * Utility routine.
 */

/*
 * Compute which squares are reachable from the centre square, as a
 * quick visual aid to determining how close the game is to
 * completion. This is also a simple way to tell if the game _is_
 * completed - just call this function and see whether every square
 * is marked active.
 */
static byte[,] compute_active(NetState state, int cx, int cy)
{
    byte[,] active;
    Tree234<NetPoint> todo;
    NetPoint xyd;

    active = new byte[state.width,state.height];

    /*
     * We only store (x,y) pairs in todo, but it's easier to reuse
     * xyd_cmp and just store direction 0 every time.
     */
    todo = new Tree234<NetPoint>(xyd_cmp);
     active[ cx, cy] = ACTIVE;
    todo.add234(new_xyd(cx, cy, 0));

    while ( (xyd = todo.delpos234(0)) != null) {
	int x1, y1, d1, x2, y2, d2;

	x1 = xyd.x;
	y1 = xyd.y;

	for (d1 = 1; d1 < 0x10; d1 <<= 1) {
        OFFSET(out x2, out y2, x1, y1, d1, state);
	    d2 = F(d1);

	    /*
	     * If the next tile in this direction is connected to
	     * us, and there isn't a barrier in the way, and it
	     * isn't already marked active, then mark it active and
	     * add it to the to-examine list.
	     */
	    if ((state.tiles[x1,y1] & d1) != 0 &&
		(state.tiles[x2,y2] & d2) != 0 &&
		(state.barriers[x1,y1] & d1) == 0 &&
		 active[ x2, y2] == 0) {
		 active[ x2, y2] = ACTIVE;
		todo.add234(new_xyd(x2, y2, 0));
	    }
	}
    }
    /* Now we expect the todo list to have shrunk to zero size. */
    Debug.Assert(todo.count234() == 0);

    return active;
}
public override NetUI CreateUI(NetState state)
{
    NetUI ui = new NetUI();
    ui.org_x = ui.org_y = 0;
    ui.cur_x = ui.cx = state.width / 2;
    ui.cur_y = ui.cy = state.height / 2;
    ui.cur_visible = false;
    ui.rs = new Random();
    return ui;
}

//static void free_ui(NetUI ui)
//{
//    random_free(ui.rs);
//    sfree(ui);
//}

//static char *encode_ui(NetUI ui)
//{
//    char buf[120];
//    /*
//     * We preserve the origin and centre-point coordinates over a
//     * serialise.
//     */
//    sprintf(buf, "O%d,%d;C%d,%d", ui.org_x, ui.org_y, ui.cx, ui.cy);
//    return dupstr(buf);
//}

//static void decode_ui(NetUI ui, const char *encoding)
//{
//    sscanf(encoding, "O%d,%d;C%d,%d",
//       &ui.org_x, &ui.org_y, &ui.cx, &ui.cy);
//}

//static void game_changed_state(NetUI ui, NetState oldstate,
//                               NetState newstate)
//{
//}


enum NetAction {
    NONE, ROTATE_LEFT, ROTATE_180, ROTATE_RIGHT, TOGGLE_LOCK, JUMBLE,
    MOVE_ORIGIN, MOVE_SOURCE, MOVE_ORIGIN_AND_SOURCE, MOVE_CURSOR
}

internal override void SetKeyboardCursorVisible(NetUI ui, int tileSize, bool value)
{
    ui.cur_visible = value;
}

/* ----------------------------------------------------------------------
 * Process a move.
 */
public override NetMove InterpretMove(NetState state, NetUI ui, NetDrawState ds, int x, int y, Buttons button, bool isTouchOrStylus)
{
    int tx = -1, ty = -1, dir = 0;
    bool shift = (button & Buttons.MOD_SHFT)!=0, ctrl =( button & Buttons.MOD_CTRL)!=0;
    //enum {
    //    NONE, ROTATE_LEFT, ROTATE_180, ROTATE_RIGHT, TOGGLE_LOCK, JUMBLE,
    //    MOVE_ORIGIN, MOVE_SOURCE, MOVE_ORIGIN_AND_SOURCE, MOVE_CURSOR
    //} action;

    button &= ~Buttons.MOD_MASK;
    NetAction action = NetAction.NONE;

    if (button == Buttons.LEFT_BUTTON ||
	button == Buttons.MIDDLE_BUTTON ||
#if USE_DRAGGING
	button == LEFT_DRAG ||
	button == LEFT_RELEASE ||
	button == RIGHT_DRAG ||
	button == RIGHT_RELEASE ||
#endif
	button == Buttons.RIGHT_BUTTON) {

	if (ui.cur_visible) {
	    ui.cur_visible = false;
	}

	/*
	 * The button must have been clicked on a valid tile.
	 */
	x -= WINDOW_OFFSET + TILE_BORDER;
	y -= WINDOW_OFFSET + TILE_BORDER;
	if (x < 0 || y < 0)
	    return null;
	tx = x / TILE_SIZE(ds);
	ty = y / TILE_SIZE(ds);
	if (tx >= state.width || ty >= state.height)
	    return null;
        /* Transform from physical to game coords */
        tx = (tx + ui.org_x) % state.width;
        ty = (ty + ui.org_y) % state.height;
	if (x % TILE_SIZE(ds) >= TILE_SIZE(ds) - TILE_BORDER ||
	    y % TILE_SIZE(ds) >= TILE_SIZE(ds) - TILE_BORDER)
	    return null;

#if USE_DRAGGING

        if (button == MIDDLE_BUTTON
#if STYLUS_BASED
	    || button == RIGHT_BUTTON  /* with a stylus, `right-click' locks */
#endif
	    ) {
            /*
             * Middle button never drags: it only toggles the lock.
             */
            action = TOGGLE_LOCK;
        } else if (button == LEFT_BUTTON
#if ! STYLUS_BASED
                   || button == RIGHT_BUTTON /* (see above) */
#endif
                  ) {
            /*
             * Otherwise, we note down the start point for a drag.
             */
            ui.dragtilex = tx;
            ui.dragtiley = ty;
            ui.dragstartx = x % TILE_SIZE;
            ui.dragstarty = y % TILE_SIZE;
            ui.dragged = false;
            return nullret;            /* no actual action */
        } else if (button == LEFT_DRAG
#if ! STYLUS_BASED
                   || button == RIGHT_DRAG
#endif
                  ) {
            /*
             * Find the new drag point and see if it necessitates a
             * rotation.
             */
            int x0,y0, xA,yA, xC,yC, xF,yF;
            int mx, my;
            int d0, dA, dC, dF, dmin;

            tx = ui.dragtilex;
            ty = ui.dragtiley;

            mx = x - (ui.dragtilex * TILE_SIZE);
            my = y - (ui.dragtiley * TILE_SIZE);

            x0 = ui.dragstartx;
            y0 = ui.dragstarty;
            xA = ui.dragstarty;
            yA = TILE_SIZE-1 - ui.dragstartx;
            xF = TILE_SIZE-1 - ui.dragstartx;
            yF = TILE_SIZE-1 - ui.dragstarty;
            xC = TILE_SIZE-1 - ui.dragstarty;
            yC = ui.dragstartx;

            d0 = (mx-x0)*(mx-x0) + (my-y0)*(my-y0);
            dA = (mx-xA)*(mx-xA) + (my-yA)*(my-yA);
            dF = (mx-xF)*(mx-xF) + (my-yF)*(my-yF);
            dC = (mx-xC)*(mx-xC) + (my-yC)*(my-yC);

            dmin = min(min(d0,dA),min(dF,dC));

            if (d0 == dmin) {
                return nullret;
            } else if (dF == dmin) {
                action = ROTATE_180;
                ui.dragstartx = xF;
                ui.dragstarty = yF;
                ui.dragged = true;
            } else if (dA == dmin) {
                action = ROTATE_LEFT;
                ui.dragstartx = xA;
                ui.dragstarty = yA;
                ui.dragged = true;
            } else /* dC == dmin */ {
                action = ROTATE_RIGHT;
                ui.dragstartx = xC;
                ui.dragstarty = yC;
                ui.dragged = true;
            }
        } else if (button == LEFT_RELEASE
#if ! STYLUS_BASED
                   || button == RIGHT_RELEASE
#endif
                  ) {
            if (!ui.dragged) {
                /*
                 * There was a click but no perceptible drag:
                 * revert to single-click behaviour.
                 */
                tx = ui.dragtilex;
                ty = ui.dragtiley;

                if (button == LEFT_RELEASE)
                    action = ROTATE_LEFT;
                else
                    action = ROTATE_RIGHT;
            } else
                return nullret;        /* no action */
        }

#else // USE_DRAGGING */

    action = (button == Buttons.LEFT_BUTTON ? NetAction.ROTATE_LEFT :
          button == Buttons.RIGHT_BUTTON ? NetAction.ROTATE_RIGHT : NetAction.TOGGLE_LOCK);

#endif // USE_DRAGGING */

    } else if (Misc.IS_CURSOR_MOVE(button)) {
        switch (button) {
          case Buttons.CURSOR_UP:       dir = U; break;
          case Buttons.CURSOR_DOWN:     dir = D; break;
          case Buttons.CURSOR_LEFT:     dir = L; break;
          case Buttons.CURSOR_RIGHT:    dir = R; break;
          default:              return null;
        }
        if (shift && ctrl) action = NetAction.MOVE_ORIGIN_AND_SOURCE;
        else if (shift) action = NetAction.MOVE_ORIGIN;
        else if (ctrl) action = NetAction.MOVE_SOURCE;
        else action = NetAction.MOVE_CURSOR;
    } else if (Misc.IS_CURSOR_SELECT(button)) {
	tx = ui.cur_x;
	ty = ui.cur_y;
	if (button == Buttons.CURSOR_SELECT)
        action = NetAction.ROTATE_LEFT;
	else if (button == Buttons.CURSOR_SELECT2)
        action = NetAction.TOGGLE_LOCK;
        ui.cur_visible = true;
    }  else
	return null;

    /*
     * The middle button locks or unlocks a tile. (A locked tile
     * cannot be turned, and is visually marked as being locked.
     * This is a convenience for the player, so that once they are
     * sure which way round a tile goes, they can lock it and thus
     * avoid forgetting later on that they'd already done that one;
     * and the locking also prevents them turning the tile by
     * accident. If they change their mind, another middle click
     * unlocks it.)
     */
    if (action == NetAction.TOGGLE_LOCK)
    {
        var move = new NetMove();
        move.points.Add(new NetMovePoint(NetMoveType.L, tx, ty));
        return move;
    }
    else if (action == NetAction.ROTATE_LEFT || action == NetAction.ROTATE_RIGHT ||
               action == NetAction.ROTATE_180)
    {

        /*
         * The left and right buttons have no effect if clicked on a
         * locked tile.
         */
        if ((state.tiles[tx,ty] & LOCKED)!=0)
            return null;

        var move = new NetMove();
        /*
         * Otherwise, turn the tile one way or the other. Left button
         * turns anticlockwise; right button turns clockwise.
         */
        move.points.Add(new NetMovePoint((action == NetAction.ROTATE_LEFT ? NetMoveType.A :
                                          action == NetAction.ROTATE_RIGHT ? NetMoveType.C : NetMoveType.F), tx, ty));
	    return move;
    }
    else if (action == NetAction.JUMBLE)
    {
        /*
         * Jumble all unlocked tiles to random orientations.
         */

        int jx, jy;
        var ret = new NetMove();
        ret.isJumble = true;
        var types = new []{NetMoveType.A,NetMoveType.F,NetMoveType.C};

        for (jy = 0; jy < state.height; jy++) {
            for (jx = 0; jx < state.width; jx++) {
                if ((state.tiles[jx,jy] & LOCKED)==0) {
                    int rot = ui.rs.Next(0, 4);
		    if (rot != 0) {
                ret.points.Add(new NetMovePoint(types[rot - 1], jx, jy));
		    }
                }
            }
        }

	return ret;
    }
    else if (action == NetAction.MOVE_ORIGIN || action == NetAction.MOVE_SOURCE ||
               action == NetAction.MOVE_ORIGIN_AND_SOURCE || action == NetAction.MOVE_CURSOR)
    {
        Debug.Assert(dir != 0);
        if (action == NetAction.MOVE_ORIGIN || action == NetAction.MOVE_ORIGIN_AND_SOURCE)
        {
            if (state.wrapping) {
                 OFFSET(out ui.org_x, out ui.org_y, ui.org_x, ui.org_y, dir, state);
            } else return null; /* disallowed for non-wrapping grids */
        }
        if (action == NetAction.MOVE_SOURCE || action == NetAction.MOVE_ORIGIN_AND_SOURCE)
        {
            OFFSET(out ui.cx, out ui.cy, ui.cx, ui.cy, dir, state);
        }
        if (action == NetAction.MOVE_CURSOR)
        {
            OFFSET(out ui.cur_x, out ui.cur_y, ui.cur_x, ui.cur_y, dir, state);
            ui.cur_visible = true;
        }
        return null;
    } else {
	return null;
    }
}

public override NetState ExecuteMove(NetState from, NetMove move)
{
    NetState ret;
    int  orig;
    int tx = -1, ty = -1;
    bool noanim;

    ret = dup_game(from);

    if (move.isJumble || move.isSolve)
    {
        if (move.isSolve)
	    ret.used_solve = true;
	noanim = true;
    } else
	noanim = false;

    ret.last_rotate_dir = 0;	       /* suppress animation */
    ret.last_rotate_x = ret.last_rotate_y = 0;

    foreach(var point in move.points)
    {
        tx = point.x;
        ty = point.y;
        var chr = point.move;
	if ( tx >= 0 && tx < from.width && ty >= 0 && ty < from.height) {
	    orig = ret.tiles[tx,ty];
        if (chr == NetMoveType.A)
        {
		ret.tiles[tx,ty] = (byte)A(orig);
		if (!noanim)
		    ret.last_rotate_dir = +1;
        }
        else if (chr == NetMoveType.F)
        {
            ret.tiles[tx, ty] = (byte)F(orig);
		if (!noanim)
                    ret.last_rotate_dir = +2; /* + for sake of argument */
        }
        else if (chr == NetMoveType.C)
        {
            ret.tiles[tx, ty] = (byte)C(orig);
		if (!noanim)
		    ret.last_rotate_dir = -1;
	    } else {
            Debug.Assert(chr == NetMoveType.L);
		ret.tiles[tx,ty] ^= LOCKED;
	    }

	} else {
	    return null;
	}
    }
    if (!noanim) {
        if (tx == -1 || ty == -1) {return null; }
	ret.last_rotate_x = tx;
	ret.last_rotate_y = ty;
    }

    /*
     * Check whether the game has been completed.
     * 
     * For this purpose it doesn't matter where the source square
     * is, because we can start from anywhere and correctly
     * determine whether the game is completed.
     */
    {
	byte[,] active = compute_active(ret, 0, 0);
	int x1, y1;
	bool complete = true;

	for (x1 = 0; x1 < ret.width; x1++)
	    for (y1 = 0; y1 < ret.height; y1++)
		if ((ret.tiles[x1,y1] & 0xF) != 0 && active[ x1, y1] == 0) {
		    complete = false;
		    goto break_label;  /* break out of two loops at once */
		}
	break_label:


	if (complete)
	    ret.completed = true;
    }

    return ret;
}


/* ----------------------------------------------------------------------
 * Routines for drawing the game position on the screen.
 */
public override NetDrawState CreateDrawState(Drawing dr, NetState state)
{
    NetDrawState ds = new NetDrawState();

    ds.started = false;
    ds.width = state.width;
    ds.height = state.height;
    ds.org_x = ds.org_y = -1;
    ds.visible = new byte[state.width,state.height];
    ds.tilesize = 0;                  /* undecided yet */
    //memset(ds.visible, 0xFF, state.width * state.height);
    for (int x = 0; x < state.width; ++x)
        for (int y = 0; y < state.height; ++y)
            ds.visible[x, y] = 0xFF;
    return ds;
}

public override void ComputeSize(NetSettings @params, int tilesize, out int x, out int y)
{
    x = WINDOW_OFFSET * 2 + tilesize * @params.width + TILE_BORDER;
    y = WINDOW_OFFSET * 2 + tilesize * @params.height + TILE_BORDER;
}

public override void SetTileSize(Drawing dr, NetDrawState ds, NetSettings @params, int tilesize)
{
    ds.tilesize = tilesize;
}

public override float[] GetColours(Frontend fe, out int ncolours)
{
    float[]ret;

    ret = new float[NCOLOURS * 3];
    ncolours = NCOLOURS;

    /*
     * Basic background colour is whatever the front end thinks is
     * a sensible default.
     */
    fe.frontend_default_colour(ret,COL_BACKGROUND * 3);

    /*
     * Wires are black.
     */
    ret[COL_WIRE * 3 + 0] = 0.0F;
    ret[COL_WIRE * 3 + 1] = 0.0F;
    ret[COL_WIRE * 3 + 2] = 0.0F;

    /*
     * Powered wires and powered endpoints are cyan.
     */
    ret[COL_POWERED * 3 + 0] = 0.0F;
    ret[COL_POWERED * 3 + 1] = 1.0F;
    ret[COL_POWERED * 3 + 2] = 1.0F;

    /*
     * Barriers are red.
     */
    ret[COL_BARRIER * 3 + 0] = 1.0F;
    ret[COL_BARRIER * 3 + 1] = 0.0F;
    ret[COL_BARRIER * 3 + 2] = 0.0F;

    /*
     * Unpowered endpoints are blue.
     */
    ret[COL_ENDPOINT * 3 + 0] = 0.0F;
    ret[COL_ENDPOINT * 3 + 1] = 0.0F;
    ret[COL_ENDPOINT * 3 + 2] = 1.0F;

    /*
     * Tile borders are a darker grey than the background.
     */
    ret[COL_BORDER * 3 + 0] = 0.5F * ret[COL_BACKGROUND * 3 + 0];
    ret[COL_BORDER * 3 + 1] = 0.5F * ret[COL_BACKGROUND * 3 + 1];
    ret[COL_BORDER * 3 + 2] = 0.5F * ret[COL_BACKGROUND * 3 + 2];

    /*
     * Locked tiles are a grey in between those two.
     */
    ret[COL_LOCKED * 3 + 0] = 0.75F * ret[COL_BACKGROUND * 3 + 0];
    ret[COL_LOCKED * 3 + 1] = 0.75F * ret[COL_BACKGROUND * 3 + 1];
    ret[COL_LOCKED * 3 + 2] = 0.75F * ret[COL_BACKGROUND * 3 + 2];

    return ret;
}

static void draw_filled_line(Drawing dr, int x1, int y1, int x2, int y2,
			     int colour)
{
    dr.draw_line(x1-1, y1, x2-1, y2, COL_WIRE);
    dr.draw_line( x1+1, y1, x2+1, y2, COL_WIRE);
    dr.draw_line( x1, y1-1, x2, y2-1, COL_WIRE);
    dr.draw_line( x1, y1+1, x2, y2+1, COL_WIRE);
    dr.draw_line( x1, y1, x2, y2, colour);
}

static void draw_rect_coords(Drawing dr, int x1, int y1, int x2, int y2,
                             int colour)
{
    int mx = (x1 < x2 ? x1 : x2);
    int my = (y1 < y2 ? y1 : y2);
    int dx = (x2 + x1 - 2*mx + 1);
    int dy = (y2 + y1 - 2*my + 1);

    dr.draw_rect( mx, my, dx, dy, colour);
}

/*
 * draw_barrier_corner() and draw_) are passed physical coords
 */
static void draw_barrier_corner(Drawing dr, NetDrawState ds,
                                int x, int y, int dx, int dy, int phase)
{
    int bx = WINDOW_OFFSET + TILE_SIZE(ds) * x;
    int by = WINDOW_OFFSET + TILE_SIZE(ds) * y;
    int x1, y1;

    x1 = (dx > 0 ? TILE_SIZE(ds) + TILE_BORDER - 1 : 0);
    y1 = (dy > 0 ? TILE_SIZE(ds) + TILE_BORDER - 1 : 0);

    if (phase == 0) {
        draw_rect_coords(dr, bx+x1+dx, by+y1,
                         bx+x1-TILE_BORDER*dx, by+y1-(TILE_BORDER-1)*dy,
                         COL_WIRE);
        draw_rect_coords(dr, bx+x1, by+y1+dy,
                         bx+x1-(TILE_BORDER-1)*dx, by+y1-TILE_BORDER*dy,
                         COL_WIRE);
    } else {
        draw_rect_coords(dr, bx+x1, by+y1,
                         bx+x1-(TILE_BORDER-1)*dx, by+y1-(TILE_BORDER-1)*dy,
                         COL_BARRIER);
    }
}
static void draw_barrier(Drawing dr, NetDrawState ds,
                         int x, int y, int dir, int phase)
{
    int bx = WINDOW_OFFSET + TILE_SIZE(ds) * x;
    int by = WINDOW_OFFSET + TILE_SIZE(ds) * y;
    int x1, y1, w, h;

    x1 = (X(dir) > 0 ? TILE_SIZE(ds) : X(dir) == 0 ? TILE_BORDER : 0);
    y1 = (Y(dir) > 0 ? TILE_SIZE(ds) : Y(dir) == 0 ? TILE_BORDER : 0);
    w = (X(dir) != 0 ? TILE_BORDER : TILE_SIZE(ds) - TILE_BORDER);
    h = (Y(dir) != 0 ? TILE_BORDER : TILE_SIZE(ds) - TILE_BORDER);

    if (phase == 0) {
        dr.draw_rect( bx+x1-X(dir), by+y1-Y(dir), w, h, COL_WIRE);
    } else {
        dr.draw_rect( bx+x1, by+y1, w, h, COL_BARRIER);
    }
}

/*
 * draw_tile() is passed physical coordinates
 */
static void draw_tile(Drawing dr, NetState state, NetDrawState ds,
                      int x, int y, int tile, bool src, float angle, bool cursor)
{
    int bx = WINDOW_OFFSET + TILE_SIZE(ds) * x;
    int by = WINDOW_OFFSET + TILE_SIZE(ds) * y;
    float[] matrix = new float[4];
    float cx, cy, ex, ey, tx, ty;
    int dir, col, phase;

    /*
     * When we draw a single tile, we must draw everything up to
     * and including the borders around the tile. This means that
     * if the neighbouring tiles have connections to those borders,
     * we must draw those connections on the borders themselves.
     */

    dr.clip(bx, by, TILE_SIZE(ds)+TILE_BORDER, TILE_SIZE(ds)+TILE_BORDER);

    /*
     * So. First blank the tile out completely: draw a big
     * rectangle in border colour, and a smaller rectangle in
     * background colour to fill it in.
     */
    dr.draw_rect( bx, by, TILE_SIZE(ds)+TILE_BORDER, TILE_SIZE(ds)+TILE_BORDER,
              COL_BORDER);
    dr.draw_rect( bx+TILE_BORDER, by+TILE_BORDER,
              TILE_SIZE(ds)-TILE_BORDER, TILE_SIZE(ds)-TILE_BORDER,
              (tile & LOCKED) != 0 ? COL_LOCKED : COL_BACKGROUND);

    /*
     * Draw an inset outline rectangle as a cursor, in whichever of
     * COL_LOCKED and COL_BACKGROUND we aren't currently drawing
     * in.
     */
    if (cursor) {
	dr.draw_line( bx+TILE_SIZE(ds)/8, by+TILE_SIZE(ds)/8,
		  bx+TILE_SIZE(ds)/8, by+TILE_SIZE(ds)-TILE_SIZE(ds)/8,
		  (tile & LOCKED)!=0 ? COL_BACKGROUND : COL_LOCKED);
	dr.draw_line( bx+TILE_SIZE(ds)/8, by+TILE_SIZE(ds)/8,
		  bx+TILE_SIZE(ds)-TILE_SIZE(ds)/8, by+TILE_SIZE(ds)/8,
		  (tile & LOCKED)!=0 ? COL_BACKGROUND : COL_LOCKED);
	dr.draw_line( bx+TILE_SIZE(ds)-TILE_SIZE(ds)/8, by+TILE_SIZE(ds)/8,
		  bx+TILE_SIZE(ds)-TILE_SIZE(ds)/8, by+TILE_SIZE(ds)-TILE_SIZE(ds)/8,
		  (tile & LOCKED)!=0 ? COL_BACKGROUND : COL_LOCKED);
	dr.draw_line( bx+TILE_SIZE(ds)/8, by+TILE_SIZE(ds)-TILE_SIZE(ds)/8,
		  bx+TILE_SIZE(ds)-TILE_SIZE(ds)/8, by+TILE_SIZE(ds)-TILE_SIZE(ds)/8,
		  (tile & LOCKED)!=0 ? COL_BACKGROUND : COL_LOCKED);
    }

    /*
     * Set up the rotation matrix.
     */
    matrix[0] = (float)Math.Cos(angle * Math.PI / 180.0);
    matrix[1] = (float)-Math.Sin(angle * Math.PI / 180.0);
    matrix[2] = (float)Math.Sin(angle * Math.PI / 180.0);
    matrix[3] = (float)Math.Cos(angle * Math.PI / 180.0);

    /*
     * Draw the wires.
     */
    cx = cy = TILE_BORDER + (TILE_SIZE(ds)-TILE_BORDER) / 2.0F - 0.5F;
    col = ((tile & ACTIVE)!=0 ? COL_POWERED : COL_WIRE);
    for (dir = 1; dir < 0x10; dir <<= 1) {
        if ((tile & dir)!=0) {
            ex = (TILE_SIZE(ds) - TILE_BORDER - 1.0F) / 2.0F * X(dir);
            ey = (TILE_SIZE(ds) - TILE_BORDER - 1.0F) / 2.0F * Y(dir);
            MATMUL(out tx, out ty, matrix, ex, ey);
            draw_filled_line(dr, bx+(int)cx, by+(int)cy,
			     bx+(int)(cx+tx), by+(int)(cy+ty),
			     COL_WIRE);
        }
    }
    for (dir = 1; dir < 0x10; dir <<= 1) {
        if ((tile & dir)!=0) {
            ex = (TILE_SIZE(ds) - TILE_BORDER - 1.0F) / 2.0F * X(dir);
            ey = (TILE_SIZE(ds) - TILE_BORDER - 1.0F) / 2.0F * Y(dir);
            MATMUL(out tx, out ty, matrix, ex, ey);
            dr.draw_line( bx+(int)cx, by+(int)cy,
		      bx+(int)(cx+tx), by+(int)(cy+ty), col);
        }
    }

    /*
     * Draw the box in the middle. We do this in blue if the tile
     * is an unpowered endpoint, in cyan if the tile is a powered
     * endpoint, in black if the tile is the centrepiece, and
     * otherwise not at all.
     */
    col = -1;
    if (src)
        col = COL_WIRE;
    else if (COUNT(tile) == 1) {
        col = ((tile & ACTIVE)!=0 ? COL_POWERED : COL_ENDPOINT);
    }
    if (col >= 0) {
        int i;
        int[] points = new int[8];

        points[0] = +1; points[1] = +1;
        points[2] = +1; points[3] = -1;
        points[4] = -1; points[5] = -1;
        points[6] = -1; points[7] = +1;

        for (i = 0; i < 8; i += 2) {
            ex = (TILE_SIZE(ds) * 0.24F) * points[i];
            ey = (TILE_SIZE(ds) * 0.24F) * points[i + 1];
            MATMUL(out tx, out ty, matrix, ex, ey);
            points[i] = bx+(int)(cx+tx);
            points[i+1] = by+(int)(cy+ty);
        }

        dr.draw_polygon(points, 4, col, COL_WIRE);
    }

    /*
     * Draw the points on the border if other tiles are connected
     * to us.
     */
    for (dir = 1; dir < 0x10; dir <<= 1) {
        int dx, dy, px, py, lx, ly, vx, vy, ox, oy;

        dx = X(dir);
        dy = Y(dir);

        ox = x + dx;
        oy = y + dy;

        if (ox < 0 || ox >= state.width || oy < 0 || oy >= state.height)
            continue;

        if ((state.tiles[GX(ds, ox), GY(ds, oy)] & F(dir))==0)
            continue;

        px = bx + (int)(dx>0 ? TILE_SIZE(ds) + TILE_BORDER - 1 : dx<0 ? 0 : cx);
        py = by + (int)(dy>0 ? TILE_SIZE(ds) + TILE_BORDER - 1 : dy<0 ? 0 : cy);
        lx = dx * (TILE_BORDER-1);
        ly = dy * (TILE_BORDER-1);
        vx = (dy !=0 ? 1 : 0);
        vy = (dx !=0 ? 1 : 0);

        if (angle == 0.0f && (tile & dir) !=0) {
            /*
             * If we are fully connected to the other tile, we must
             * draw right across the tile border. (We can use our
             * own ACTIVE state to determine what colour to do this
             * in: if we are fully connected to the other tile then
             * the two ACTIVE states will be the same.)
             */
            draw_rect_coords(dr, px-vx, py-vy, px+lx+vx, py+ly+vy, COL_WIRE);
            draw_rect_coords(dr, px, py, px+lx, py+ly,
                             (tile & ACTIVE) !=0 ? COL_POWERED : COL_WIRE);
        } else {
            /*
             * The other tile extends into our border, but isn't
             * actually connected to us. Just draw a single black
             * dot.
             */
            draw_rect_coords(dr, px, py, px, py, COL_WIRE);
        }
    }

    /*
     * Draw barrier corners, and then barriers.
     */
    for (phase = 0; phase < 2; phase++) {
        for (dir = 1; dir < 0x10; dir <<= 1) {
            int x1, y1;
            bool corner = false;
            /*
             * If at least one barrier terminates at the corner
             * between dir and A(dir), draw a barrier corner.
             */
            if ((state.barriers[GX(ds, x), GY(ds, y)] & (dir | A(dir))) !=0)
            {
                corner = true;
            } else {
                /*
                 * Only count barriers terminating at this corner
                 * if they're physically next to the corner. (That
                 * is, if they've wrapped round from the far side
                 * of the screen, they don't count.)
                 */
                x1 = x + X(dir);
                y1 = y + Y(dir);
                if (x1 >= 0 && x1 < state.width &&
                    y1 >= 0 && y1 < state.height &&
                    (state.barriers[GX(ds, x1), GY(ds, y1)] & A(dir)) !=0)
                {
                    corner = true;
                } else {
                    x1 = x + X(A(dir));
                    y1 = y + Y(A(dir));
                    if (x1 >= 0 && x1 < state.width &&
                        y1 >= 0 && y1 < state.height &&
                        (state.barriers[GX(ds, x1), GY(ds, y1)] & dir) !=0)
                        corner = true;
                }
            }

            if (corner) {
                /*
                 * At least one barrier terminates here. Draw a
                 * corner.
                 */
                draw_barrier_corner(dr, ds, x, y,
                                    X(dir)+X(A(dir)), Y(dir)+Y(A(dir)),
                                    phase);
            }
        }

        for (dir = 1; dir < 0x10; dir <<= 1)
            if ((state.barriers[GX(ds, x), GY(ds, y)] & dir)!=0)
                draw_barrier(dr, ds, x, y, dir, phase);
    }

    dr.unclip();

    dr.draw_update(bx, by, TILE_SIZE(ds)+TILE_BORDER, TILE_SIZE(ds)+TILE_BORDER);
}

public override void Redraw(Drawing dr, NetDrawState ds, NetState oldstate, NetState state, int dir, NetUI ui, float t, float ft)
{
    int x, y, tx, ty, frame, last_rotate_dir;
    bool moved_origin = false;
    byte[,] active;
    float angle = 0.0f;

    /*
     * Clear the screen, and draw the exterior barrier lines, if
     * this is our first call or if the origin has changed.
     */
    if (!ds.started || ui.org_x != ds.org_x || ui.org_y != ds.org_y) {
        int phase;

        ds.started = true;

        dr.draw_rect( 0, 0, 
                  WINDOW_OFFSET * 2 + TILE_SIZE(ds) * state.width + TILE_BORDER,
                  WINDOW_OFFSET * 2 + TILE_SIZE(ds) * state.height + TILE_BORDER,
                  COL_BACKGROUND);

        ds.org_x = ui.org_x;
        ds.org_y = ui.org_y;
        moved_origin = true;

        dr.draw_update(0, 0, 
                    WINDOW_OFFSET*2 + TILE_SIZE(ds)*state.width + TILE_BORDER,
                    WINDOW_OFFSET*2 + TILE_SIZE(ds)*state.height + TILE_BORDER);

        for (phase = 0; phase < 2; phase++) {

            for (x = 0; x < ds.width; x++) {
                if (x+1 < ds.width) {
                    if ((state.barriers[GX(ds, x), GY(ds, 0)] & R)!=0)
                        draw_barrier_corner(dr, ds, x, -1, +1, +1, phase);
                    if ((state.barriers[GX(ds, x), GY(ds, ds.height - 1)] & R)!=0)
                        draw_barrier_corner(dr, ds, x, ds.height, +1, -1, phase);
                }
                if ((state.barriers[GX(ds, x), GY(ds, 0)] & U)!=0)
                {
                    draw_barrier_corner(dr, ds, x, -1, -1, +1, phase);
                    draw_barrier_corner(dr, ds, x, -1, +1, +1, phase);
                     draw_barrier(dr, ds, x, -1, D, phase);
                }
                if ((state.barriers[GX(ds, x), GY(ds, ds.height - 1)] & D)!=0)
                {
                    draw_barrier_corner(dr, ds, x, ds.height, -1, -1, phase);
                    draw_barrier_corner(dr, ds, x, ds.height, +1, -1, phase);
                    draw_barrier(dr, ds, x, ds.height, U, phase);
                }
            }

            for (y = 0; y < ds.height; y++) {
                if (y+1 < ds.height) {
                    if ((state.barriers[GX(ds, 0), GY(ds, y)] & D)!=0)
                        draw_barrier_corner(dr, ds, -1, y, +1, +1, phase);
                    if ((state.barriers[GX(ds, ds.width - 1), GY(ds, y)] & D)!=0)
                        draw_barrier_corner(dr, ds, ds.width, y, -1, +1, phase);
                }
                if ((state.barriers[GX(ds, 0), GY(ds, y)] & L) != 0)
                {
                    draw_barrier_corner(dr, ds, -1, y, +1, -1, phase);
                    draw_barrier_corner(dr, ds, -1, y, +1, +1, phase);
                    draw_barrier(dr, ds, -1, y, R, phase);
                }
                if ((state.barriers[GX(ds, ds.width - 1), GY(ds, y)] & R) != 0)
                {
                    draw_barrier_corner(dr, ds, ds.width, y, -1, -1, phase);
                    draw_barrier_corner(dr, ds, ds.width, y, -1, +1, phase);
                    draw_barrier(dr, ds, ds.width, y, L, phase);
                }
            }
        }
    }

    tx = ty = -1;
    last_rotate_dir = dir==-1 ? oldstate.last_rotate_dir :
                                state.last_rotate_dir;
    if (oldstate != null && (t < ROTATE_TIME) && last_rotate_dir != 0) {
        /*
         * We're animating a single tile rotation. Find the turning
         * tile.
         */
        tx = (dir==-1 ? oldstate.last_rotate_x : state.last_rotate_x);
        ty = (dir==-1 ? oldstate.last_rotate_y : state.last_rotate_y);
        angle = last_rotate_dir * dir * 90.0F * (t / ROTATE_TIME);
        state = oldstate;
    }

    frame = -1;
    if (ft > 0) {
        /*
         * We're animating a completion flash. Find which frame
         * we're at.
         */
        frame = (int)(ft / FLASH_FRAME);
    }

    /*
     * Draw any tile which differs from the way it was last drawn.
     */
    active = compute_active(state, ui.cx, ui.cy);

    for (x = 0; x < ds.width; x++)
        for (y = 0; y < ds.height; y++) {
            byte c = (byte)(state.tiles[GX(ds, x), GY(ds, y)] |
                               active[GX(ds, x), GY(ds, y)]);
            bool is_src = GX(ds, x) == ui.cx && GY(ds, y) == ui.cy;
            bool is_anim = GX(ds, x) == tx && GY(ds, y) == ty;
            bool is_cursor = ui.cur_visible &&
                            GX(ds, x) == ui.cur_x && GY(ds, y) == ui.cur_y;

            /*
             * In a completion flash, we adjust the LOCKED bit
             * depending on our distance from the centre point and
             * the frame number.
             */
            if (frame >= 0) {
                int rcx = RX(ds, ui.cx), rcy = RY(ds, ui.cy);
                int xdist, ydist, dist;
                xdist = (x < rcx ? rcx - x : x - rcx);
                ydist = (y < rcy ? rcy - y : y - rcy);
                dist = (xdist > ydist ? xdist : ydist);

                if (frame >= dist && frame < dist+4) {
                    int @lock = (frame - dist) & 1;
                    @lock = @lock != 0 ? LOCKED : 0;
                    c = (byte)((c &~ LOCKED) | @lock);
                }
            }

            if (moved_origin ||
                 ds.visible[ x, y] != c ||
                 ds.visible[ x, y] == 0xFF ||
                is_src || is_anim || is_cursor) {
                draw_tile(dr, state, ds, x, y, c,
                          is_src, (is_anim ? angle : 0.0F), is_cursor);
                if (is_src || is_anim || is_cursor)
                     ds.visible[ x, y] = 0xFF;
                else
                     ds.visible[ x, y] = c;
            }
        }

    ///*
    // * Update the status bar.
    // */
    //{
    //char statusbuf[256];
    //int i, n, n2, a;

    //n = state.width * state.height;
    //for (i = a = n2 = 0; i < n; i++) {
    //    if (active[i])
    //    a++;
    //        if (state.tiles[i] & 0xF)
    //            n2++;
    //    }

    //sprintf(statusbuf, "%sActive: %d/%d",
    //    (state.used_solve ? "Auto-solved. " :
    //     state.completed ? "COMPLETED! " : ""), a, n2);

    //status_bar(dr, statusbuf);
    //}

    //sfree(active);
}

static float game_anim_length(NetState oldstate,
                              NetState newstate, int dir, NetUI ui)
{
    int last_rotate_dir;

    /*
     * Don't animate if last_rotate_dir is zero.
     */
    last_rotate_dir = dir==-1 ? oldstate.last_rotate_dir :
                                newstate.last_rotate_dir;
    if (last_rotate_dir != 0)
        return ROTATE_TIME;

    return 0.0F;
}

public override float AnimDuration
{
    get
    {
        return ROTATE_TIME;
    }
}

public override float CompletedFlashDuration(NetSettings settings)
{
    int size = 0;
    if (size < settings.width)
        size = settings.width;
    if (size < settings.height)
        size = settings.height;
    return FLASH_FRAME * (size + 4);
}


static int game_status(NetState state)
{
    return state.completed ? +1 : 0;
}

static bool game_timing_state(NetState state, NetUI ui)
{
    return true;
}

    }
}
