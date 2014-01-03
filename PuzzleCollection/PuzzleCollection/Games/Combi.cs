using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games
{
    class Combi
    {
        internal int r, n, nleft, total;
        internal int[] a;

        private static long factx(long x, long y)
        {
            long acc = 1, i;

            for (i = y; i <= x; i++)
                acc *= i;
            return acc;
        }

        internal void Reset()
        {
            nleft = total;
            for (int i = 0; i < r; i++)
                a[i] = i;
        }

        public Combi(int r, int n)
        {
            long nfr, nrf;

            Debug.Assert(r <= n);
            Debug.Assert(n >= 1);

            this.r = r;
            this.n = n;

            this.a = new int[r];

            nfr = factx(n, r+1);
            nrf = factx(n-r, 1);
            this.total = (int)(nfr / nrf);

            Reset();
        }

        /* returns NULL when we're done otherwise returns input. */
        internal bool Next()
        {
            int i = this.r - 1, j;

            if (this.nleft == this.total)
                goto done;
            else if (this.nleft <= 0)
                return false;

            while (this.a[i] == this.n - this.r + i)
                i--;
            this.a[i] += 1;
            for (j = i + 1; j < this.r; j++)
                this.a[j] = this.a[i] + j - i;

        done:
            this.nleft--;
            return true;
        }
    }
}
