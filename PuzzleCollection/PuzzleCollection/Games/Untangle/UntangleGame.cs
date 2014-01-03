using PuzzleCollection.Games.Structures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Untangle
{
    public class UntangleGame : GameBase<UntangleSettings, UntangleState, UntangleMove, UntangleDrawState, UntangleUI>
    {
        const int CIRCLE_RADIUS =16;
        const int DRAG_THRESHOLD =(CIRCLE_RADIUS * 2);
        const int PREFERRED_TILESIZE= 64;

        const float FLASH_TIME =0.30F;
        const float ANIM_TIME =0.13F;
        const float SOLVEANIM_TIME =0.50F;

        const int COL_SYSBACKGROUND=0;
        const int COL_BACKGROUND=1;
        const int COL_LINE=2;
        const int COL_OUTLINE=3;
        const int COL_POINT=4;
        const int COL_DRAGPOINT=5;
        const int COL_NEIGHBOUR=6;
        const int COL_FLASH1=7;
        const int COL_FLASH2=8;
        const int NCOLOURS = 9;

        private static bool greater64(long i,long j) { return i > j; }

        private static int sign64( long i) { return Math.Sign(i); }

        static long mulu32to64(ulong x, ulong y)
        {
            return (long)x * (long)y;
        }

        static long mul32to64(long x, long y)
        {
            return (long)x * (long)y;
        }

        static long dotprod64(long a, long b, long p, long q)
        {
            return mul32to64(a, b) + mul32to64(p, q);
        }

        private static UntangleSettings[] presets = new[]{
            new UntangleSettings(6),
            new UntangleSettings(10),
            new UntangleSettings(15),
            new UntangleSettings(20),
            new UntangleSettings(25)
        };

        public override UntangleSettings DefaultSettings
        {
            get { return presets[1]; }
        }

        public override IEnumerable<UntangleSettings> PresetsSettings
        {
            get { return presets; }
        }
        public override UntangleSettings ParseSettings(string settingsString)
        {
            return UntangleSettings.Parse(settingsString);
        }
        internal override void SetKeyboardCursorVisible(UntangleUI ui, int tileSize, bool value)
        {
            
        }
        
/*
 * Determine whether the line segments between a1 and a2, and
 * between b1 and b2, intersect. We count it as an intersection if
 * any of the endpoints lies _on_ the other line.
 */
static bool cross(UntanglePoint a1, UntanglePoint a2, UntanglePoint b1, UntanglePoint b2)
{
    long b1x, b1y, b2x, b2y, px, py;
    long d1, d2, d3;

    /*
     * The condition for crossing is that b1 and b2 are on opposite
     * sides of the line a1-a2, and vice versa. We determine this
     * by taking the dot product of b1-a1 with a vector
     * perpendicular to a2-a1, and similarly with b2-a1, and seeing
     * if they have different signs.
     */

    /*
     * Construct the vector b1-a1. We don't have to worry too much
     * about the denominator, because we're only going to check the
     * sign of this vector; we just need to get the numerator
     * right.
     */
    b1x = b1.x * a1.d - a1.x * b1.d;
    b1y = b1.y * a1.d - a1.y * b1.d;
    /* Now construct b2-a1, and a vector perpendicular to a2-a1,
     * in the same way. */
    b2x = b2.x * a1.d - a1.x * b2.d;
    b2y = b2.y * a1.d - a1.y * b2.d;
    px = a1.y * a2.d - a2.y * a1.d;
    py = a2.x * a1.d - a1.x * a2.d;
    /* Take the dot products. Here we resort to 64-bit arithmetic. */
    d1 = dotprod64(b1x, px, b1y, py);
    d2 = dotprod64(b2x, px, b2y, py);
    /* If they have the same non-zero sign, the lines do not cross. */
    if ((sign64(d1) > 0 && sign64(d2) > 0) ||
	(sign64(d1) < 0 && sign64(d2) < 0))
	return false;

    /*
     * If the dot products are both exactly zero, then the two line
     * segments are collinear. At this UntanglePoint the intersection
     * condition becomes whether or not they overlap within their
     * line.
     */
    if (sign64(d1) == 0 && sign64(d2) == 0) {
	/* Construct the vector a2-a1. */
	px = a2.x * a1.d - a1.x * a2.d;
	py = a2.y * a1.d - a1.y * a2.d;
	/* Determine the dot products of b1-a1 and b2-a1 with this. */
	d1 = dotprod64(b1x, px, b1y, py);
	d2 = dotprod64(b2x, px, b2y, py);
	/* If they're both strictly negative, the lines do not cross. */
	if (sign64(d1) < 0 && sign64(d2) < 0)
	    return false;
	/* Otherwise, take the dot product of a2-a1 with itself. If
	 * the other two dot products both exceed this, the lines do
	 * not cross. */
	d3 = dotprod64(px, px, py, py);
	if (greater64(d1, d3) && greater64(d2, d3))
	    return false;
    }

    /*
     * We've eliminated the only important special case, and we
     * have determined that b1 and b2 are on opposite sides of the
     * line a1-a2. Now do the same thing the other way round and
     * we're done.
     */
    b1x = a1.x * b1.d - b1.x * a1.d;
    b1y = a1.y * b1.d - b1.y * a1.d;
    b2x = a2.x * b1.d - b1.x * a2.d;
    b2y = a2.y * b1.d - b1.y * a2.d;
    px = b1.y * b2.d - b2.y * b1.d;
    py = b2.x * b1.d - b1.x * b2.d;
    d1 = dotprod64(b1x, px, b1y, py);
    d2 = dotprod64(b2x, px, b2y, py);
    if ((sign64(d1) > 0 && sign64(d2) > 0) ||
	(sign64(d1) < 0 && sign64(d2) < 0))
	return false;

    /*
     * The lines must cross.
     */
    return true;
}

static ulong squarert(ulong n) {
    ulong d, a, b, di;

    d = n;
    a = 0;
    b = 1L << 30;		       /* largest available power of 4 */
    do {
        a >>= 1;
        di = 2*a + b;
        if (di <= d) {
            d -= di;
            a += b;
        }
        b >>= 2;
    } while (b != 0);

    return a;
}

/*
 * Our solutions are arranged on a square grid big enough that n
 * points occupy about 1/POINTDENSITY of the grid.
 */
const int POINTDENSITY = 3;
const int MAXDEGREE= 4;
private static ulong COORDLIMIT(int n) { return squarert(((uint)n) * POINTDENSITY); }

static void addedge(Tree234<UntangleEdge> edges, int a, int b)
{
    UntangleEdge e = new UntangleEdge();

    Debug.Assert(a != b);

    e.a = Math.Min(a, b);
    e.b = Math.Max(a, b);

    edges.add234(e);
}

static bool isedge(Tree234<UntangleEdge> edges, int a, int b)
{
    UntangleEdge e = new UntangleEdge();

    Debug.Assert(a != b);

    e.a = Math.Min(a, b);
    e.b = Math.Max(a, b);

    return edges.find234(e, null) != null;
}

/*
 * Construct UntanglePoint coordinates for n points arranged in a circle,
 * within the bounding box (0,0) to (w,w).
 */
static void make_circle(UntanglePoint[] pts, int n, int w)
{
    long d, r, c, i;

    /*
     * First, decide on a denominator. Although in principle it
     * would be nice to set this really high so as to finely
     * distinguish all the points on the circle, I'm going to set
     * it at a fixed size to prevent integer overflow problems.
     */
    d = PREFERRED_TILESIZE;

    /*
     * Leave a little space outside the circle.
     */
    c = d * w / 2;
    r = d * w * 3 / 7;

    /*
     * Place the points.
     */
    for (i = 0; i < n; i++) {
	double angle = i * 2 * Math.PI / n;
	double x = r * Math.Sin(angle), y = - r * Math.Cos(angle);
    pts[i] = new UntanglePoint();
	pts[i].x = (long)(c + x + 0.5);
	pts[i].y = (long)(c + y + 0.5);
	pts[i].d = d;
    }
}

public override string GenerateNewGameDescription(UntangleSettings @params, Random rs, out string aux, int interactive)
{
    int n = @params.n, i;
    long w, h, j, k, m;
    UntanglePoint[] pts, pts2;
    long []tmp;
    Tree234<UntangleEdge> edges;
    Tree234<UntangleVertex> vertices;
    UntangleEdge e, e2;
    UntangleVertex v;
    UntangleVertex[] vs, vlist;
    StringBuilder ret;

    w = h = (long)COORDLIMIT(n);

    /*
     * Choose n points from this grid.
     */
    pts = new UntanglePoint[n];
    tmp = new long[w*h];
    for (i = 0; i < w*h; i++)
	tmp[i] = i;
    tmp.Shuffle((int)(w*h), rs);
    for (i = 0; i < n; i++) {
        pts[i] = new UntanglePoint();
	pts[i].x = tmp[i] % w;
	pts[i].y = tmp[i] / w;
	pts[i].d = 1;
    }


    /*
     * Now start adding edges between the points.
     * 
     * At all times, we attempt to add an edge to the lowest-degree
     * vertex we currently have, and we try the other vertices as
     * candidate second endpoints in order of distance from this
     * one. We stop as soon as we find an edge which
     * 
     *  (a) does not increase any vertex's degree beyond MAXDEGREE
     *  (b) does not cross any existing edges
     *  (c) does not intersect any actual point.
     */
    vs = new UntangleVertex[n];
    vertices = new Tree234<UntangleVertex>(UntangleVertex.Compare);
    for (i = 0; i < n; i++) {
	v = vs[i] = new UntangleVertex();
	v.param = 0;		       /* in this tree, param is the degree */
	v.vindex = i;
	vertices.add234(v);
    }
    edges = new Tree234<UntangleEdge>(UntangleEdge.Compare);
    vlist = new UntangleVertex[n];
    while (true) {
	bool added = false;

	for (i = 0; i < n; i++) {
	    v = vertices.index234(i);
	    j = v.vindex;

	    if (v.param >= MAXDEGREE)
		break;		       /* nothing left to add! */

	    /*
	     * Sort the other vertices into order of their distance
	     * from this one. Don't bother looking below i, because
	     * we've already tried those edges the other way round.
	     * Also here we rule out target vertices with too high
	     * a degree, and (of course) ones to which we already
	     * have an edge.
	     */
	    m = 0;
	    for (k = i+1; k < n; k++) {
		UntangleVertex kv = vertices.index234((int)k);
		int ki = kv.vindex;
		int dx, dy;

		if (kv.param >= MAXDEGREE || isedge(edges, (int)ki, (int)j))
		    continue;

        vlist[m] = new UntangleVertex();
		vlist[m].vindex = ki;
		dx = (int)(pts[ki].x - pts[j].x);
		dy = (int)(pts[ki].y - pts[j].y);
		vlist[m].param = dx*dx + dy*dy;
		m++;
	    }
        Array.Sort(vlist, 0, (int)m, Comparer<UntangleVertex>.Create(UntangleVertex.Compare));
        
	    for (k = 0; k < m; k++) {
		int p;
		int ki = vlist[k].vindex;

		/*
		 * Check to see whether this edge intersects any
		 * existing edge or point.
		 */
		for (p = 0; p < n; p++)
		    if (p != ki && p != j && cross(pts[ki], pts[j],
						   pts[p], pts[p]))
			break;
		if (p < n)
		    continue;
		for (p = 0; (e = edges.index234(p)) != null; p++)
		    if (e.a != ki && e.a != j &&
			e.b != ki && e.b != j &&
			cross(pts[ki], pts[j], pts[e.a], pts[e.b]))
			break;
		if (e != null)
		    continue;

		/*
		 * We're done! Add this edge, modify the degrees of
		 * the two vertices involved, and break.
		 */
		addedge(edges, (int)j, ki);
		added = true;
		vertices.del234(vs[j]);
		vs[j].param++;
		vertices.add234(vs[j]);
		vertices.del234(vs[ki]);
		vs[ki].param++;
		vertices.add234(vs[ki]);
		break;
	    }

	    if (k < m)
		break;
	}

	if (!added)
	    break;		       /* we're done. */
    }

    /*
     * That's our graph. Now shuffle the points, making sure that
     * they come out with at least one crossed line when arranged
     * in a circle (so that the puzzle isn't immediately solved!).
     */
    tmp = new long[n];
    for (i = 0; i < n; i++)
	tmp[i] = i;
    pts2 = new UntanglePoint[n];
    make_circle(pts2, n, (int)w);
    while (true) {
	tmp.Shuffle(n, rs);
	for (i = 0; (e = edges.index234(i)) != null; i++) {
	    for (j = i+1; (e2 = edges.index234((int)j)) != null; j++) {
		if (e2.a == e.a || e2.a == e.b ||
		    e2.b == e.a || e2.b == e.b)
		    continue;
		if (cross(pts2[tmp[e2.a]], pts2[tmp[e2.b]],
			  pts2[tmp[e.a]], pts2[tmp[e.b]]))
		    break;
	    }
	    if (e2 != null)
		break;
	}
	if (e != null)
	    break;		       /* we've found a crossing */
    }

    /*
     * We're done. Now encode the graph in a string format. Let's
     * use a comma-separated list of dash-separated vertex number
     * pairs, numbered from zero. We'll sort the list to prevent
     * side channels.
     */
    ret = new StringBuilder();
    {
	int retlen;
	UntangleEdge[] ea;

	retlen = 0;
	m = edges.count234();
	ea = new UntangleEdge[m];
	for (i = 0; (e = edges.index234(i)) != null; i++) {
	    Debug.Assert(i < m);
        ea[i] = new UntangleEdge();
	    ea[i].a = (int)Math.Min(tmp[e.a], tmp[e.b]);
	    ea[i].b = (int)Math.Max(tmp[e.a], tmp[e.b]);
	}
	Debug.Assert(i == m);
	Array.Sort(ea, UntangleEdge.Compare);

	for (i = 0; i < m; i++) {
        if ( ret.Length > 0 )
        {
            ret.Append(',');
        }
	    ret.Append(ea[i].a).Append('-').Append(ea[i].b);
	}

    }

    aux = null;
    ///*
    // * Encode the solution we started with as an aux_info string.
    // */
    //{
    //char buf[80];
    //char *auxstr;
    //int auxlen;

    //auxlen = 2;		       /* leading 'S' and trailing '\0' */
    //for (i = 0; i < n; i++) {
    //    j = tmp[i];
    //    pts2[j] = pts[i];
    //    if (pts2[j].d & 1) {
    //    pts2[j].x *= 2;
    //    pts2[j].y *= 2;
    //    pts2[j].d *= 2;
    //    }
    //    pts2[j].x += pts2[j].d / 2;
    //    pts2[j].y += pts2[j].d / 2;
    //    auxlen += sprintf(buf, ";P%d:%ld,%ld/%ld", i,
    //              pts2[j].x, pts2[j].y, pts2[j].d);
    //}
    //k = 0;
    //auxstr = snewn(auxlen, char);
    //auxstr[k++] = 'S';
    //for (i = 0; i < n; i++)
    //    k += sprintf(auxstr+k, ";P%d:%ld,%ld/%ld", i,
    //         pts2[i].x, pts2[i].y, pts2[i].d);
    //Debug.Assert(k < auxlen);
    //*aux = auxstr;
    //}
    //sfree(pts2);

    //sfree(tmp);
    //sfree(vlist);
    //freetree234(vertices);
    //sfree(vs);
    //while ((e = delpos234(edges, 0)) != null)
    //sfree(e);
    //freetree234(edges);
    //sfree(pts);

    return ret.ToString();
}

//static char *validate_desc(UntangleSettings @params, const char *desc)
//{
//    int a, b;

//    while (*desc) {
//    a = atoi(desc);
//    if (a < 0 || a >= @params.n)
//        return "Number out of range in game description";
//    while (*desc && isdigit((unsigned char)*desc)) desc++;
//    if (*desc != '-')
//        return "Expected '-' after number in game description";
//    desc++;			       /* eat dash */
//    b = atoi(desc);
//    if (b < 0 || b >= @params.n)
//        return "Number out of range in game description";
//    while (*desc && isdigit((unsigned char)*desc)) desc++;
//    if (*desc) {
//        if (*desc != ',')
//        return "Expected ',' after number in game description";
//        desc++;		       /* eat comma */
//    }
//    }

//    return null;
//}

static void mark_crossings(UntangleState state)
{
    bool ok = true;
    int i, j;
    UntangleEdge e, e2;

    /*
     * Check correctness: for every pair of edges, see whether they
     * cross.
     */
    for (i = 0; (e = state.graph.edges.index234(i)) != null; i++) {
        for (j = i + 1; (e2 = state.graph.edges.index234(j)) != null; j++)
        {
	    if (e2.a == e.a || e2.a == e.b ||
		e2.b == e.a || e2.b == e.b)
		continue;
	    if (cross(state.pts[e2.a], state.pts[e2.b],
		      state.pts[e.a], state.pts[e.b])) {
		ok = false;
		goto done;	       /* multi-level break - sorry */
	    }
	}
    }

    /*
     * e == null if we've gone through all the edge pairs
     * without finding a crossing.
     */
    done:
    if (ok)
	state.completed = true;
}
public override UntangleState CreateNewGameFromDescription(UntangleSettings @params, string desc)
{
    int n = @params.n;
    UntangleState state = new UntangleState();
    int a, b;

    state.@params = @params;
    state.w = state.h = (int)COORDLIMIT(n);
    state.pts = new UntanglePoint[n];
    make_circle(state.pts, n, state.w);
    state.graph = new UntangleGraph();
    state.graph.refcount = 1;
    state.graph.edges = new Tree234<UntangleEdge>(UntangleEdge.Compare);
    state.completed = state.cheated = state.just_solved = false;

    //int descPos = 0;
    //while (descPos< desc.Length) {
    //a = atoi(desc);
    //Debug.Assert(a >= 0 && a < @params.n);
    //while (*desc && isdigit((unsigned char)*desc)) desc++;
    //Debug.Assert(*desc == '-');
    //desc++;			       /* eat dash */
    //b = atoi(desc);
    //Debug.Assert(b >= 0 && b < @params.n);
    //while (*desc && isdigit((unsigned char)*desc)) desc++;
    //if (*desc) {
    //    Debug.Assert(*desc == ',');
    //    desc++;		       /* eat comma */
    //}
    //addedge(state.graph.edges, a, b);
    //}
    var tokens = desc.Split(',');
    foreach (var token in tokens)
    {
        var parts = token.Split('-');
        a = int.Parse(parts[0], CultureInfo.InvariantCulture);
        b = int.Parse(parts[1], CultureInfo.InvariantCulture);
        addedge(state.graph.edges, a, b);
    }

    return state;
}

static UntangleState dup_game(UntangleState state)
{
    int n = state.@params.n;
    UntangleState ret = new UntangleState();

    ret.@params = state.@params;
    ret.w = state.w;
    ret.h = state.h;
    ret.pts = state.pts.Select(p => new UntanglePoint(){ d = p.d, x = p.x, y = p.y}).ToArray();
    ret.graph = state.graph;
    ret.graph.refcount++;
    ret.completed = state.completed;
    ret.cheated = state.cheated;
    ret.just_solved = state.just_solved;

    return ret;
}

public override UntangleMove CreateSolveGameMove(UntangleState state, UntangleState currstate, UntangleMove ai, out string error)
{
    error = "Not supported";
    return null;
}

//static UntangleMove solve_game(UntangleState state, UntangleState currstate,
//                        UntangleMove aux, out string error)
//{
//    int n = state.@params.n;
//    int[] matrix = new int[4];
//    UntanglePoint pts;
//    int i, j, besti;
//    float bestd;


//    if (aux == null) {
//    error = "Solution not known for this puzzle";
//    return null;
//    }

//    /*
//     * Decode the aux_info to get the original UntanglePoint positions.
//     */
//    pts = snewn(n, point);
//    aux++;                             /* eat 'S' */
//    for (i = 0; i < n; i++) {
//        int p, k;
//        long x, y, d;
//    int ret = sscanf(aux, ";P%d:%ld,%ld/%ld%n", &p, &x, &y, &d, &k);
//        if (ret != 4 || p != i) {
//            *error = "Internal error: aux_info badly formatted";
//            sfree(pts);
//            return null;
//        }
//        pts[i].x = x;
//        pts[i].y = y;
//        pts[i].d = d;
//        aux += k;
//    }

//    /*
//     * Now go through eight possible symmetries of the UntanglePoint set.
//     * For each one, work out the sum of the Euclidean distances
//     * between the points' current positions and their new ones.
//     * 
//     * We're squaring distances here, which means we're at risk of
//     * integer overflow. Fortunately, there's no real need to be
//     * massively careful about rounding errors, since this is a
//     * non-essential bit of the code; so I'll just work in floats
//     * internally.
//     */
//    besti = -1;
//    bestd = 0.0F;

//    for (i = 0; i < 8; i++) {
//        float d;

//        matrix[0] = matrix[1] = matrix[2] = matrix[3] = 0;
//        matrix[i & 1] = (i & 2) ? +1 : -1;
//        matrix[3-(i&1)] = (i & 4) ? +1 : -1;

//        d = 0.0F;
//        for (j = 0; j < n; j++) {
//            float px = (float)pts[j].x / pts[j].d;
//            float py = (float)pts[j].y / pts[j].d;
//            float sx = (float)currstate.pts[j].x / currstate.pts[j].d;
//            float sy = (float)currstate.pts[j].y / currstate.pts[j].d;
//            float cx = (float)currstate.w / 2;
//            float cy = (float)currstate.h / 2;
//            float ox, oy, dx, dy;

//            px -= cx;
//            py -= cy;

//            ox = matrix[0] * px + matrix[1] * py;
//            oy = matrix[2] * px + matrix[3] * py;

//            ox += cx;
//            oy += cy;

//            dx = ox - sx;
//            dy = oy - sy;

//            d += dx*dx + dy*dy;
//        }

//        if (besti < 0 || bestd > d) {
//            besti = i;
//            bestd = d;
//        }
//    }

//    Debug.Assert(besti >= 0);

//    /*
//     * Now we know which symmetry is closest to the points' current
//     * positions. Use it.
//     */
//    matrix[0] = matrix[1] = matrix[2] = matrix[3] = 0;
//    matrix[besti & 1] = (besti & 2) ? +1 : -1;
//    matrix[3-(besti&1)] = (besti & 4) ? +1 : -1;

//    retsize = 256;
//    ret = snewn(retsize, char);
//    retlen = 0;
//    ret[retlen++] = 'S';
//    ret[retlen] = '\0';

//    for (i = 0; i < n; i++) {
//        float px = (float)pts[i].x / pts[i].d;
//        float py = (float)pts[i].y / pts[i].d;
//        float cx = (float)currstate.w / 2;
//        float cy = (float)currstate.h / 2;
//        float ox, oy;
//        int extra;

//        px -= cx;
//        py -= cy;

//        ox = matrix[0] * px + matrix[1] * py;
//        oy = matrix[2] * px + matrix[3] * py;

//        ox += cx;
//        oy += cy;

//        /*
//         * Use a fixed denominator of 2, because we know the
//         * original points were on an integer grid offset by 1/2.
//         */
//        pts[i].d = 2;
//        ox *= pts[i].d;
//        oy *= pts[i].d;
//        pts[i].x = (long)(ox + 0.5F);
//        pts[i].y = (long)(oy + 0.5F);

//        extra = sprintf(buf, ";P%d:%ld,%ld/%ld", i,
//                        pts[i].x, pts[i].y, pts[i].d);
//        if (retlen + extra >= retsize) {
//            retsize = retlen + extra + 256;
//            ret = sresize(ret, retsize, char);
//        }
//        strcpy(ret + retlen, buf);
//        retlen += extra;
//    }

//    sfree(pts);

//    return ret;
//}

//static int game_can_format_as_text_now(UntangleSettings @params)
//{
//    return true;
//}

//static char *game_text_format(UntangleState state)
//{
//    return null;
//}

public override UntangleUI CreateUI(UntangleState state)
{
    UntangleUI ui = new UntangleUI();
    ui.dragpoint = -1;
    ui.just_moved = ui.just_dragged = false;
    return ui;
}

//static void free_ui(UntangleUI ui)
//{
//    sfree(ui);
//}

//static char *encode_ui(UntangleUI ui)
//{
//    return null;
//}

//static void decode_ui(UntangleUI ui, const char *encoding)
//{
//}

static void game_changed_state(UntangleUI ui, UntangleState oldstate,
                               UntangleState newstate)
{
    ui.dragpoint = -1;
    ui.just_moved = ui.just_dragged;
    ui.just_dragged = false;
}


public override UntangleMove InterpretMove(UntangleState state, UntangleUI ui, UntangleDrawState ds, int x, int y, Buttons button, bool isTouchOrStylus)
{
    int n = state.@params.n;

    if (Misc.IS_MOUSE_DOWN(button)) {
	int i, best;
        long bestd;

	/*
	 * Begin drag. We drag the vertex _nearest_ to the pointer,
	 * just in case one is nearly on top of another and we want
	 * to drag the latter. However, we drag nothing at all if
	 * the nearest vertex is outside DRAG_THRESHOLD.
	 */
	best = -1;
	bestd = 0;

	for (i = 0; i < n; i++) {
	    long px = state.pts[i].x * ds.tilesize / state.pts[i].d;
	    long py = state.pts[i].y * ds.tilesize / state.pts[i].d;
	    long dx = px - x;
	    long dy = py - y;
	    long d = dx*dx + dy*dy;

	    if (best == -1 || bestd > d) {
		best = i;
		bestd = d;
	    }
	}

	if (bestd <= DRAG_THRESHOLD * DRAG_THRESHOLD) {
	    ui.dragpoint = best;
	    ui.newpoint.x = x;
	    ui.newpoint.y = y;
	    ui.newpoint.d = ds.tilesize;
	    return null;
	}

    } else if (Misc.IS_MOUSE_DRAG(button) && ui.dragpoint >= 0) {
	ui.newpoint.x = x;
	ui.newpoint.y = y;
	ui.newpoint.d = ds.tilesize;
	return null;
    } else if (Misc.IS_MOUSE_RELEASE(button) && ui.dragpoint >= 0) {
	int p = ui.dragpoint;

	ui.dragpoint = -1;	       /* terminate drag, no matter what */

	/*
	 * First, see if we're within range. The user can cancel a
	 * drag by dragging the UntanglePoint right off the window.
	 */
	if (ui.newpoint.x < 0 ||
            ui.newpoint.x >= (long)state.w*ui.newpoint.d ||
	    ui.newpoint.y < 0 ||
            ui.newpoint.y >= (long)state.h*ui.newpoint.d)
	    return null;

	/*
	 * We aren't cancelling the drag. Construct a move string
	 * indicating where this UntanglePoint is going to.
	 */
	ui.just_dragged = true;
        var move = new UntangleMove();
        move.points.Add(new UntangleMovePoint(p,ui.newpoint));
        return move;
    }

    return null;
}

public override UntangleMove ParseMove(UntangleSettings settings, string moveString)
{
    return UntangleMove.Parse(settings, moveString);
}

public override UntangleState ExecuteMove(UntangleState state, UntangleMove move)
{
    int n = state.@params.n;
    UntangleState ret = dup_game(state);

    ret.just_solved = false;

    //while (*move) {
    //if (*move == 'S') {
    //    move++;
    //    if (*move == ';') move++;
    //    ret.cheated = ret.just_solved = true;
    //}
    //if (*move == 'P' &&
    //    sscanf(move+1, "%d:%ld,%ld/%ld%n", &p, &x, &y, &d, &k) == 4 &&
    //    p >= 0 && p < n && d > 0) {
    //    ret.pts[p].x = x;
    //    ret.pts[p].y = y;
    //    ret.pts[p].d = d;

    //    move += k+1;
    //    if (*move == ';') move++;
    //} else {
    //    return null;
    //}
    //}
    if (move.isSolve)
    {
        ret.cheated = ret.just_solved = true;
    }

    foreach (var point in move.points)
    {
        ret.pts[point.p].x = point.x;
        ret.pts[point.p].y = point.y;
        ret.pts[point.p].d = point.d;
    }

    mark_crossings(ret);

    return ret;
}

/* ----------------------------------------------------------------------
 * Drawing routines.
 */
public override void ComputeSize(UntangleSettings @params, int tilesize, out int x, out int y)
{
    x = y = (int)COORDLIMIT(@params.n) * tilesize;
}

public override void SetTileSize(Drawing dr, UntangleDrawState ds, UntangleSettings @params, int tilesize)
{
    ds.tilesize = tilesize;
}

public override float[] GetColours(Frontend fe, out int ncolours)
{
    float[] ret = new float[3 * NCOLOURS];

    /*
     * COL_BACKGROUND is what we use as the normal background colour.
     * Unusually, though, it isn't colour #0: COL_SYSBACKGROUND, a bit
     * darker, takes that place. This means that if the user resizes
     * an Untangle window so as to change its aspect ratio, the
     * still-square playable area will be distinguished from the dead
     * space around it.
     */
    //Misc.game_mkhighlight(fe, ret, COL_BACKGROUND, -1, COL_SYSBACKGROUND);
    fe.frontend_default_colour(ret, COL_BACKGROUND * 3);
    fe.frontend_default_colour(ret, COL_SYSBACKGROUND * 3);


    ret[COL_LINE * 3 + 0] = 0.0F;
    ret[COL_LINE * 3 + 1] = 0.0F;
    ret[COL_LINE * 3 + 2] = 0.0F;

    ret[COL_OUTLINE * 3 + 0] = 0.0F;
    ret[COL_OUTLINE * 3 + 1] = 0.0F;
    ret[COL_OUTLINE * 3 + 2] = 0.0F;

    ret[COL_POINT * 3 + 0] = 0.0F;
    ret[COL_POINT * 3 + 1] = 0.0F;
    ret[COL_POINT * 3 + 2] = 1.0F;

    ret[COL_DRAGPOINT * 3 + 0] = 1.0F;
    ret[COL_DRAGPOINT * 3 + 1] = 1.0F;
    ret[COL_DRAGPOINT * 3 + 2] = 1.0F;

    ret[COL_NEIGHBOUR * 3 + 0] = 1.0F;
    ret[COL_NEIGHBOUR * 3 + 1] = 0.0F;
    ret[COL_NEIGHBOUR * 3 + 2] = 0.0F;

    ret[COL_FLASH1 * 3 + 0] = 0.5F;
    ret[COL_FLASH1 * 3 + 1] = 0.5F;
    ret[COL_FLASH1 * 3 + 2] = 0.5F;

    ret[COL_FLASH2 * 3 + 0] = 1.0F;
    ret[COL_FLASH2 * 3 + 1] = 1.0F;
    ret[COL_FLASH2 * 3 + 2] = 1.0F;

    ncolours = NCOLOURS;
    return ret;
}
public override UntangleDrawState CreateDrawState(Drawing dr, UntangleState state)
{
    UntangleDrawState ds = new UntangleDrawState();
    int i;

    ds.tilesize = 0;
    ds.x = new long[state.@params.n];
    ds.y = new long[state.@params.n];
    for (i = 0; i < state.@params.n; i++)
        ds.x[i] = ds.y[i] = -1;
    ds.bg = -1;
    ds.dragpoint = -1;

    return ds;
}


static UntanglePoint mix(UntanglePoint a, UntanglePoint b, float distance)
{
    UntanglePoint ret = new UntanglePoint();

    ret.d = a.d * b.d;
    ret.x = (long)(a.x * b.d + distance * (b.x * a.d - a.x * b.d));
    ret.y = (long)(a.y * b.d + distance * (b.y * a.d - a.y * b.d));

    return ret;
}
public override void Redraw(Drawing dr, UntangleDrawState ds, UntangleState oldstate, UntangleState state, int dir, UntangleUI ui, float animtime, float flashtime)
{
    int w, h;
    UntangleEdge e;
    int i, j;
    int bg;
    bool points_moved;

    /*
     * There's no terribly sensible way to do partial redraws of
     * this game, so I'm going to have to resort to redrawing the
     * whole thing every time.
     */

    if (flashtime == 0)
        bg = COL_BACKGROUND;
    else if ((int)(flashtime * 4 / FLASH_TIME) % 2 == 0)
        bg = COL_FLASH1;
    else
        bg = COL_FLASH2;

    /*
     * To prevent excessive spinning on redraw during a completion
     * flash, we first check to see if _either_ the flash
     * background colour has changed _or_ at least one UntanglePoint has
     * moved _or_ a drag has begun or ended, and abandon the redraw
     * if neither is the case.
     * 
     * Also in this loop we work out the coordinates of all the
     * points for this redraw.
     */
    points_moved = false;
    for (i = 0; i < state.@params.n; i++) {
        UntanglePoint p = state.pts[i];
        long x, y;

        if (ui.dragpoint == i)
            p = ui.newpoint;

        if (oldstate != null)
            p = mix(oldstate.pts[i], p, animtime / ui.anim_length);

	x = p.x * ds.tilesize / p.d;
	y = p.y * ds.tilesize / p.d;

        if (ds.x[i] != x || ds.y[i] != y)
            points_moved = true;

        ds.x[i] = x;
        ds.y[i] = y;
    }

    if (ds.bg == bg && ds.dragpoint == ui.dragpoint && !points_moved)
        return;                        /* nothing to do */

    ds.dragpoint = ui.dragpoint;
    ds.bg = bg;

    ComputeSize(state.@params, ds.tilesize, out w, out h);

    dr.clip(0, 0, w, h);
    dr.draw_rect(0, 0, w, h, bg);
    dr.draw_rect_outline(0, 0, w, h, COL_LINE);
    /*
     * Draw the edges.
     */

    for (i = 0; (e = state.graph.edges.index234(i)) != null; i++) {
        dr.draw_line((int)ds.x[e.a], (int)ds.y[e.a], (int)ds.x[e.b], (int)ds.y[e.b],
		  COL_LINE);
    }

    /*
     * Draw the points.
     * 
     * When dragging, we should not only vary the colours, but
     * leave the UntanglePoint being dragged until last.
     */
    for (j = 0; j < 3; j++) {
	int thisc = (j == 0 ? COL_POINT :
		     j == 1 ? COL_NEIGHBOUR : COL_DRAGPOINT);
	for (i = 0; i < state.@params.n; i++) {
            int c;

	    if (ui.dragpoint == i) {
		c = COL_DRAGPOINT;
	    } else if (ui.dragpoint >= 0 &&
		       isedge(state.graph.edges, ui.dragpoint, i)) {
		c = COL_NEIGHBOUR;
	    } else {
		c = COL_POINT;
	    }

	    if (c == thisc) {
            dr.draw_circle((int)ds.x[i], (int)ds.y[i], CIRCLE_RADIUS,
                            c, COL_OUTLINE);
	    }
	}
    }

    dr.draw_update(0, 0, w, h);
    dr.unclip();
}

//static float game_anim_length(UntangleState oldstate,
//                              UntangleState newstate, int dir, UntangleUI ui)
//{
//    if (ui.just_moved)
//    return 0.0F;
//    if ((dir < 0 ? oldstate : newstate).just_solved)
//    ui.anim_length = SOLVEANIM_TIME;
//    else
//    ui.anim_length = ANIM_TIME;
//    return ui.anim_length;
//}

//static float game_flash_length(UntangleState oldstate,
//                               UntangleState newstate, int dir, UntangleUI ui)
//{
//    if (!oldstate.completed && newstate.completed &&
//    !oldstate.cheated && !newstate.cheated)
//        return FLASH_TIME;
//    return 0.0F;
//}

//static int game_status(UntangleState state)
//{
//    return state.completed ? +1 : 0;
//}

//static int game_timing_state(UntangleState state, UntangleUI ui)
//{
//    return true;
//}
    }
}
