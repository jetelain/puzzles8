using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games
{
    public class OriginalRandom : Random
    {
        private readonly byte[] seedbuf = new byte[40];
        private readonly byte[] databuf = new byte[20];
        private int pos;

        public OriginalRandom(byte[] seed, int len)
        {
            byte[] temp = new byte[20];
            SHA.Simple(seed, len, seedbuf);
            SHA.Simple(seedbuf, 20, temp);
            Array.Copy(temp, 0, seedbuf, 20, 20);
            SHA.Simple(seedbuf, 40, databuf);
            pos = 0;
        }

        public OriginalRandom(string encodedState)
        {
            int pos, digits;
            byte @byte;

            this.pos = 0;

            @byte = 0;
            digits = 0;
            pos = 0;
            foreach (char c in encodedState)
            {
                int v;

                if (c >= '0' && c <= '9')
                    v = c - '0';
                else if (c >= 'A' && c <= 'F')
                    v = c - 'A' + 10;
                else if (c >= 'a' && c <= 'f')
                    v = c - 'a' + 10;
                else
                    v = 0;

                @byte = (byte)((@byte << 4) | v);
                digits++;

                if (digits == 2)
                {
                    /*
                     * We have a byte. Put it somewhere.
                     */
                    if (pos < this.seedbuf.Length)
                        this.seedbuf[pos++] = @byte;
                    else if (pos < this.seedbuf.Length + this.databuf.Length)
                        this.databuf[pos++ - this.seedbuf.Length] = @byte;
                    else if (pos == this.seedbuf.Length + this.databuf.Length &&
                         @byte <= this.databuf.Length)
                        this.pos = @byte;
                    @byte = 0;
                    digits = 0;
                }
            }
        }

        public static OriginalRandom FromTextSeed(string seed)
        {
            var bytes = Encoding.GetEncoding("ASCII").GetBytes(seed);
            return new OriginalRandom(bytes, bytes.Length);
        }


        ulong random_bits(int bits)
        {
            ulong ret = 0;
            int n;

            for (n = 0; n < bits; n += 8)
            {
                if (this.pos >= 20)
                {
                    int i;

                    for (i = 0; i < 20; i++)
                    {
                        if (this.seedbuf[i] != 0xFF)
                        {
                            this.seedbuf[i]++;
                            break;
                        }
                        else
                            this.seedbuf[i] = 0;
                    }
                    SHA.Simple(this.seedbuf, 40, this.databuf);
                    this.pos = 0;
                }
                ret = (ret << 8) | this.databuf[this.pos++];
            }

            /*
             * `(1 << bits) - 1' is not good enough, since if bits==32 on a
             * 32-bit machine, behaviour is undefined and Intel has a nasty
             * habit of shifting left by zero instead. We'll shift by
             * bits-1 and then separately shift by one.
             */
            ret &= (1UL << (bits - 1)) * 2UL - 1UL;
            return ret;
        }

        ulong random_upto(ulong limit)
        {
            int bits = 0;
            ulong max, divisor, data;

            while ((limit >> bits) != 0)
                bits++;

            bits += 3;
            Debug.Assert(bits < 32);

            max = 1UL << bits;
            divisor = max / limit;
            max = limit * divisor;

            do
            {
                data = random_bits(bits);
            } while (data >= max);

            return data / divisor;
        }

        public override int Next(int maxValue)
        {
            return (int)random_upto((ulong)maxValue);
        }

        public override int Next(int minValue, int maxValue)
        {
            return minValue + (int)random_upto((ulong)maxValue);
        }

        public override double NextDouble()
        {
            return random_upto(100000000UL) / 100000000.0F;
        }
    }
}
