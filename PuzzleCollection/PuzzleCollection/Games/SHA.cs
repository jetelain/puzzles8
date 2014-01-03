using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games
{
    public class SHA
    {
        private readonly uint[] h = new uint[5];
        private readonly byte[] block = new byte[64];
        private int blkused;
        private uint lenhi, lenlo;

        /* ----------------------------------------------------------------------
         * Core SHA algorithm: processes 16-word blocks into a message digest.
         */

        static uint rol(uint x, int y) { return (uint)(((x) << (y)) | (((uint)x) >> (32 - y))); }

        static void SHA_Core_Init(uint[] h)
        {
            h[0] = 0x67452301;
            h[1] = 0xefcdab89;
            h[2] = 0x98badcfe;
            h[3] = 0x10325476;
            h[4] = 0xc3d2e1f0;
        }

        static void SHATransform(uint[] digest, uint[] block)
        {
            uint[] w = new uint[80];
            uint a, b, c, d, e;
            int t;

            for (t = 0; t < 16; t++)
                w[t] = block[t];

            for (t = 16; t < 80; t++)
            {
                uint tmp = w[t - 3] ^ w[t - 8] ^ w[t - 14] ^ w[t - 16];
                w[t] = rol(tmp, 1);
            }

            a = digest[0];
            b = digest[1];
            c = digest[2];
            d = digest[3];
            e = digest[4];

            for (t = 0; t < 20; t++)
            {
                uint tmp =
                    rol(a, 5) + ((b & c) | (d & ~b)) + e + w[t] + 0x5a827999;
                e = d;
                d = c;
                c = rol(b, 30);
                b = a;
                a = tmp;
            }
            for (t = 20; t < 40; t++)
            {
                uint tmp = rol(a, 5) + (b ^ c ^ d) + e + w[t] + 0x6ed9eba1;
                e = d;
                d = c;
                c = rol(b, 30);
                b = a;
                a = tmp;
            }
            for (t = 40; t < 60; t++)
            {
                uint tmp = rol(a,
                         5) + ((b & c) | (b & d) | (c & d)) + e + w[t] +
                    0x8f1bbcdc;
                e = d;
                d = c;
                c = rol(b, 30);
                b = a;
                a = tmp;
            }
            for (t = 60; t < 80; t++)
            {
                uint tmp = rol(a, 5) + (b ^ c ^ d) + e + w[t] + 0xca62c1d6;
                e = d;
                d = c;
                c = rol(b, 30);
                b = a;
                a = tmp;
            }

            digest[0] += a;
            digest[1] += b;
            digest[2] += c;
            digest[3] += d;
            digest[4] += e;
        }

        /* ----------------------------------------------------------------------
         * Outer SHA algorithm: take an arbitrary length byte string,
         * convert it into 16-word blocks with the prescribed padding at
         * the end, and pass those blocks to the core SHA algorithm.
         */
        public SHA()
        {
            SHA_Core_Init(this.h);
            this.blkused = 0;
            this.lenhi = this.lenlo = 0;
        }

        public void Bytes(byte[] q, int len)
        {
            uint[] wordblock = new uint[16];
            uint lenw = (uint)len;
            int i;
            int qOffset = 0;

            /*
             * Update the length field.
             */
            this.lenlo += lenw;
            this.lenhi += (this.lenlo < lenw) ? 1u : 0u;

            if (this.blkused != 0 && this.blkused + len < 64)
            {
                /*
                 * Trivial case: just add to the block.
                 */
                Array.Copy(q, qOffset, this.block, this.blkused, len);
                this.blkused += len;
            }
            else
            {
                /*
                 * We must complete and process at least one block.
                 */
                while (this.blkused + len >= 64)
                {
                    Array.Copy(q, qOffset, this.block, this.blkused, 64 - this.blkused);
                    qOffset += 64 - this.blkused;
                    len -= 64 - this.blkused;
                    /* Now process the block. Gather bytes big-endian into words */
                    for (i = 0; i < 16; i++)
                    {
                        wordblock[i] =
                            (((uint)this.block[i * 4 + 0]) << 24) |
                            (((uint)this.block[i * 4 + 1]) << 16) |
                            (((uint)this.block[i * 4 + 2]) << 8) |
                            (((uint)this.block[i * 4 + 3]) << 0);
                    }
                    SHATransform(this.h, wordblock);
                    this.blkused = 0;
                }
                Array.Copy(q, qOffset, this.block, 0, len);
                this.blkused = len;
            }
        }

        public void Final(byte[] output)
        {

            int i;
            int pad;
            byte[] c = new byte[64];
            uint lenhi, lenlo;

            if (this.blkused >= 56)
                pad = 56 + 64 - this.blkused;
            else
                pad = 56 - this.blkused;

            lenhi = (this.lenhi << 3) | (this.lenlo >> (32 - 3));
            lenlo = (this.lenlo << 3);

            Array.Clear(c, 0, pad);
            c[0] = 0x80;
            Bytes(c, pad);

            c[0] = (byte)((lenhi >> 24) & 0xFF);
            c[1] = (byte)((lenhi >> 16) & 0xFF);
            c[2] = (byte)((lenhi >> 8) & 0xFF);
            c[3] = (byte)((lenhi >> 0) & 0xFF);
            c[4] = (byte)((lenlo >> 24) & 0xFF);
            c[5] = (byte)((lenlo >> 16) & 0xFF);
            c[6] = (byte)((lenlo >> 8) & 0xFF);
            c[7] = (byte)((lenlo >> 0) & 0xFF);

            Bytes(c, 8);

            for (i = 0; i < 5; i++)
            {
                output[i * 4] = (byte)((this.h[i] >> 24) & 0xFF);
                output[i * 4 + 1] = (byte)((this.h[i] >> 16) & 0xFF);
                output[i * 4 + 2] = (byte)((this.h[i] >> 8) & 0xFF);
                output[i * 4 + 3] = (byte)((this.h[i]) & 0xFF);
            }
        }

        public static void Simple(byte[] p, int len, byte[] output)
        {
            SHA s = new SHA();
            s.Bytes(p, len);
            s.Final(output);
        }
    }
}
