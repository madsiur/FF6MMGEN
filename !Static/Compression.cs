// Author: Frederic Dupont <themadsiur@gmail.com>
// Copyright 2017 Frederic Dupont

// This file is part of FF6MMGEN.

// FF6MMGEN is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// FF6MMGEN is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with FF6MMGEN.  If not, see<http://www.gnu.org/licenses/>.

// This class is based on some code provided by Yousei/Flobbster
// FF3Edit
// Version: 12/6/2000
// Available here: http://www.ff6hacking.com/wiki/doku.php?id=ff3:ff3us:util:start#source_code

using System;

namespace FF6MMGEN
{
    public static class Compression
    {
        public static byte[] Compress(byte[] source, ulong fsize)
        {
            byte[] buf = new byte[0x11000];
            byte[] dest = new byte[fsize];
            byte[] b = new byte[16];
            byte p, n, run, bp;
            int maxx = 0, maxrun, x, w, start;
            ulong bpos, bpos2, size;
            source.CopyTo(buf, 0);
            int pTempPtr = 2;
            size = 0;
            bpos = 0; bp = 0;
            bpos2 = 2014;
            n = 0; p = 0;
            while (bpos < fsize)
            {
                maxrun = 0;
                if (bpos < 2048)
                    start = (int)bpos;
                else
                    start = 2048;
                for (x = 1; x <= start; x++)
                {
                    run = 0;
                    while ((run < 31 + 3) &&
                        (buf[bpos - (ulong)x + run] == buf[bpos + run]) &&
                        (bpos + run < fsize))
                    {
                        run++;
                    }
                    if (run > maxrun)
                    {
                        maxrun = run;
                        maxx = (int)((bpos2 - (ulong)x) & 2047);
                    }
                }
                if (maxrun >= 3)
                {
                    w = ((maxrun - 3) << 11) + maxx;
                    b[bp] = (byte)(w & 255);
                    b[bp + 1] = (byte)(w >> 8);
                    bp += 2;
                    bpos += (ulong)maxrun;
                    bpos2 = (bpos2 + (ulong)maxrun) & 2047;
                }
                else
                {
                    n = (byte)(n | (1 << p));
                    b[bp] = buf[bpos];
                    bp++; bpos++;
                    bpos2 = (bpos2 + 1) & 2047;
                }
                p = (byte)((p + 1) & 7);
                if (p == 0)
                {
                    dest[pTempPtr++] = n;
                    for (int tc = 0; tc < bp; tc++)
                        dest[pTempPtr++] = b[tc];
                    size += (ulong)(bp + 1);
                    n = 0; bp = 0;
                }
            }
            if (p != 0)
            {
                dest[pTempPtr++] = n;
                for (int tc = 0; tc < bp; tc++)
                    dest[pTempPtr++] = b[tc];
                size += (ulong)(bp + 1);
                n = 0; bp = 0;
            }
            size += 2;
            Bits.SetShort(dest, 0, (ushort)size);
            byte[] destFinal = new byte[size];
            Buffer.BlockCopy(dest, 0, destFinal, 0, (ushort)size);
            return destFinal;
        }

        public static byte[] Decompress(byte[] data, int offset)
        {
            byte[] temp = new byte[0x11000];
            int tempPtr = 0;
            byte[] buf2 = new byte[2048];
            byte n, x, b;
            uint size, w, num, i;
            ulong bpos, bpos2;
            int finalCount = 0;
            size = Bits.GetShort(data, offset); offset += 2;
            bpos = 0; bpos2 = 2014;
            do
            {
                n = data[bpos + (ulong)offset]; bpos++;
                for (x = 0; x < 8; x++)
                {
                    if (((n >> x) & 1) == 1)
                    {
                        b = data[bpos + (ulong)offset]; bpos++;
                        temp[tempPtr++] = b;
                        finalCount++;
                        buf2[bpos2 & 2047] = b; bpos2++;
                    }
                    else
                    {
                        w = (uint)(data[bpos + (ulong)offset] + (data[bpos + 1 + (ulong)offset] << 8));
                        bpos += 2;
                        num = (w >> 11) + 3;
                        w = w & 2047;
                        for (i = 0; i < num; i++)
                        {
                            b = buf2[(w + i) & 2047];
                            temp[tempPtr++] = b;
                            finalCount++;
                            buf2[bpos2 & 2047] = b; bpos2++;
                        }
                    }
                    if (bpos >= size)
                        x = 8;
                }
            }
            while (bpos < size);
            byte[] dest = new byte[finalCount];
            Buffer.BlockCopy(temp, 0, dest, 0, finalCount);
            return dest;
        }
    }
}
