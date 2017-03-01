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

using FF6MMGEN.Classes;
using System;

namespace FF6MMGEN
{
    class Program
    {
        struct Trigger
        {
            public int cx;
            public int cy;
        }

        private static readonly int WOB_TILE_PROPERTIES = 0xEE9B14;
        private static readonly int WOR_TILE_PROPERTIES = 0xEE9D14;
        private static readonly int WOB_MAP_PTR = 0xEEB20F;
        private static readonly int WOR_MAP_PTR = 0xEEB224;
        private static readonly int WOB_MINI_MAP_PTR = 0xEEB24B;
        private static readonly int WOR_MINI_MAP_PTR = 0xEEB24E;
        private static readonly int FALCON_GFX_PTR = 0xEEB251;
        private static readonly int ENDING_PAL_PTR = 0xEEB254;
        private static readonly int MAP_TRIGGERS = 0xDFBB00;
        private static readonly int EVENT_TRIGGERS = 0xC40000;
        private static readonly int ASM_CODE_A = 0xEE9B0E;
        private static readonly int ASM_CODE_B = 0xEEB1F2;

        private static readonly string GAMECODE = "C3F6";

        private static readonly int XLEFT = 41;
        private static readonly int XRIGHT = 49;
        private static readonly int YTOP = 42;
        private static readonly int YBOTTOM = 56;
        
        private static readonly byte[] asmA = { 0xEA, 0x20, 0xF2, 0xB1, 0x28, 0x60 };
        private static readonly byte[] asmB =
        {
            0x8F, 0xB0, 0xE1, 0x7E,
            0x8F, 0xB2, 0xE1, 0x7E,
            0x60
        };

        private static byte[] rom;
        private static byte[] wobMap;
        private static byte[] worMap;
        private static byte[] wobMiniMap;
        private static byte[] worMiniMap;
        private static ushort[] wobTilesProp;
        private static ushort[] worTilesProp;
        private static Trigger[] wobTriggers;
        private static Trigger[] worTriggers;
        private static byte[] falconGfx;
        private static byte[] endingPal;

        private static string path;
        private static int xLeft;
        private static int xRight;
        private static int yTop;
        private static int yBottom;

        private static OptionSet options;
        private static bool help;

        static void Main(string[] args)
        {
            path = string.Empty;
            help = false;
            xLeft = xRight = yTop = yBottom = -1;

            options = new OptionSet() {
                { "?|help=|h=", "Prints out the options.", h => help = h != null },
                { "r=|rom=", "The ROM path.", r => path = r },
                { "x1=", "1st X coord. from left, 0 to 63. Default: " + XLEFT + ".", x1 => xLeft = validatePosition("x1", x1, 0, 63)},
                { "x2=", "2nd X coord. from left, 1 to 64. Default: " + XRIGHT + ".", x2 => xRight = validatePosition("x2", x2, 1,  64)},
                { "y1=", "1st Y coord. from top,  0 to 63. Default: " + YTOP + ".", y1 => yTop = validatePosition("y1", y1, 0,  63)},
                { "y2=", "2nd Y coord. from top,  1 to 64. Default: " + YBOTTOM + ".", y2 => yBottom = validatePosition("y2", y2, 1, 64)},
            };

            try
            {
                options.Parse(args);
            }
            catch (OptionException)
            {
                ShowHelp("Error - usage is: ");
            }

            if(help)
            {
                ShowHelp("ff6mmgen.exe <[-r|-rom] romname.[smc|sfc]> [-x1 value] [-x2 value] [-y1 value] [-y2 value]. Example: 'ff6mmgen.exe -r ff6.smc -x1 41 -x2 49 -y1 42 -y2 56'.");
            }

            if(path == null || path == string.Empty)
            {
                ShowHelp("Rom path (-r|-rom romanme.[smc|sfc]) is an mandatory argument.");
            }

            if (xLeft == -1 || xRight == -1 || yTop == -1 || yBottom == -1)
            {
                xLeft = XLEFT;
                xRight = XRIGHT;
                yTop = YTOP;
                yBottom = YBOTTOM;
            }
            else if (xLeft >= xRight)
            {
                ShowHelp("x1 must be smaller than x2.");
            }
            else if (yTop >= yBottom)
            {
                ShowHelp("y1 must be smaller than y2.");
            }
            
            Rom rm = new Rom(path);

            if (rm.ReadRom())
            {
                string gameCode = rm.GetGameCode();

                if (gameCode != GAMECODE)
                {
                    Console.WriteLine("Invalid Game code at $C0FFB0: " + gameCode + ". Game code value must be " + GAMECODE);
                }
                else
                {
                    rom = rm.Content;

                    try
                    {
                        // read the data
                        wobMap = Decompress(WOB_MAP_PTR);
                        worMap = Decompress(WOR_MAP_PTR);
                        wobMiniMap = Decompress(WOB_MINI_MAP_PTR);
                        worMiniMap = Decompress(WOR_MINI_MAP_PTR);
                        wobTilesProp = ReadTileProperties(WOB_TILE_PROPERTIES);
                        worTilesProp = ReadTileProperties(WOR_TILE_PROPERTIES);
                        wobTriggers = ReadMapTriggers(0);
                        worTriggers = ReadMapTriggers(1);

                        // Get data that goes after WOB & WOR Mini-Maps
                        endingPal = ReadBytesFromPointer(ENDING_PAL_PTR, 256);
                        falconGfx = ReadBytesFromPointer(FALCON_GFX_PTR, Bits.GetShort(rom, GetOffsetFromPointer(FALCON_GFX_PTR)));

                        // Generate Mini-Maps
                        byte[] compWob = WriteMiniMap(true);
                        byte[] compWor = WriteMiniMap(false);

                        int wobOff = GetOffsetFromPointer(WOB_MINI_MAP_PTR);
                        int palOff = GetOffsetFromPointer(ENDING_PAL_PTR);

                        int totalSize = compWob.Length + compWor.Length + falconGfx.Length + endingPal.Length;
                        int offset = wobOff;

                        // Write Data
                        Bits.SetBytes(rm.Content, offset, compWob);
                        offset += compWob.Length;

                        Bits.SetBytes(rm.Content, offset, compWor);
                        int worPtr = AbsToSmc(offset);
                        offset += compWor.Length;

                        Bits.SetBytes(rm.Content, offset, falconGfx);
                        int falconPtr = AbsToSmc(offset);
                        offset += falconGfx.Length;

                        Bits.SetBytes(rm.Content, offset, endingPal);
                        int palPtr = AbsToSmc(offset);

                        // Write Pointers
                        Bits.SetInt24(rm.Content, SmcToAbs(WOR_MINI_MAP_PTR), worPtr);
                        Bits.SetInt24(rm.Content, SmcToAbs(FALCON_GFX_PTR), falconPtr);
                        Bits.SetInt24(rm.Content, SmcToAbs(ENDING_PAL_PTR), palPtr);

                        // Modify palette ASM
                        Bits.SetBytes(rom, SmcToAbs(ASM_CODE_A), asmA);
                        Bits.SetBytes(rom, SmcToAbs(ASM_CODE_B), asmB);

                        // Modify palette (location color)
                        Bits.SetShort(rom, 0x12EEB2, 0x7FFF);


                        if (rm.WriteRom())
                        {
                            Console.WriteLine("Operation Completed!");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine("Error! " + e.Message);
                    }
                }
            }
        }

        private static int validatePosition(string name, string position, int min, int max)
        {
            int pos;

            if (!int.TryParse(position, out pos))
            {
                ShowHelp(name + " must be a number.");
            }
            else
            {
                pos = pos < min || pos > max ? -1 : pos;
            }

            if(pos == -1)
            {
                ShowHelp(name + " must be between " + min + " and " + max + ".");
            }

            return pos;
        }
        private static void ShowHelp(string message)
        {
            Console.Error.WriteLine(message);
            options.WriteOptionDescriptions(Console.Error);
            Environment.Exit(-1);
        }

        #region FF3Edit code

        public static byte[] WriteMiniMap(bool isWob)
        {
            byte[] aMap = new byte[4096];
            int[] bPix = new int[16];
            byte[] map;
            byte[] miniMap;
            ushort[] tileProperties;
            int bAverage;
            Trigger pTrigger;
            bool tFound;

            int y, x, ry, rx;

            if(isWob)
            {
                map = wobMap;
                miniMap = wobMiniMap;
                tileProperties = wobTilesProp;
            }
            else
            {
                map = worMap;
                miniMap = worMiniMap;
                tileProperties = worTilesProp;
            }

            for (y = 0; y < 256; y += 4)
            {
                for (x = 0; x < 256; x += 4)
                {
                    for (int y1 = 0; y1 < 4; y1++)
                    {
                        for (int x1 = 0; x1 < 4; x1++)
                        {
                            bPix[y1 * 4 + x1] = (tileProperties[map[(y + y1) * 256 + (x + x1)]] & 0x0C) >> 2;
                        }
                    }

                    bAverage = 0;
                    for (int i = 0; i < 16; i++)
                    {
                        bPix[i] *= 16;
                        bAverage += bPix[i];
                    }
                    bAverage /= 16;

                    if (bAverage == 0) bAverage = 1;
                    else if (bAverage > 0 && bAverage < 6) bAverage = 2;
                    else if (bAverage >= 6 && bAverage < 24) bAverage = 3;
                    else if (bAverage >= 24 && bAverage < 32) bAverage = 4;
                    else if (bAverage >= 32) bAverage = 10;

                    tFound = false;

                    // Set locations on map
                    for (int y1 = 0; y1 < 4 && !tFound; y1++)
                    {
                        for (int x1 = 0; x1 < 4 && !tFound; x1++)
                        {
                            pTrigger = GetTrigger(x + x1, y + y1, isWob);

                            if (pTrigger.cx != -1)
                            {
                                bAverage = 5;
                                tFound = true;
                            }
                        }
                    }

                    aMap[(y / 4) * 64 + (x / 4)] = (byte)bAverage;
                }
            }

            string s = "22";
            s.ToString();

            // Sealed Gate continent
            if (isWob)
            {
                for (int y2 = 0; y2 < 64; y2++)
                {
                    for (int x2 = 0; x2 < 64; x2++)
                    {
                        if (x2 >= xLeft && x2 < xRight && y2 >= yTop && y2 < yBottom)
                        {
                            int offset = y2 * 64 + x2;
                            byte bAvg = aMap[offset];

                            if (bAvg == 2) bAvg = 6;
                            else if (bAvg == 3) bAvg = 7;
                            else if (bAvg == 4) bAvg = 8;
                            else if (bAvg == 10) bAvg = 8;
                            else if (bAvg == 5) bAvg = 9;

                            aMap[offset] = bAvg;
                        }
                    }
                }
            }

            ry = 0;
            for (y = 0; y < 8; y += 8, ry += 8)
            {
                rx = 0;
                for (x = 0; x < 64; x += 8, rx += 8)
                {
                    int position = (ry * 32) + (rx * 4);
                    byte[] bPlanes = new byte[4];

                    for (int row = 0; row < 8; row++)
                    {
                        bPlanes[0] = bPlanes[1] = bPlanes[2] = bPlanes[3] = 0;

                        for (int col = 0; col < 8; col++)
                        {
                            byte bColor = aMap[(y + row) * 64 + (x + col)];
                            if ((bColor & 1) == 1) bPlanes[0] |= (byte)(1 << (7 - col));
                            if ((bColor & 2) == 2) bPlanes[1] |= (byte)(1 << (7 - col));
                            if ((bColor & 4) == 4) bPlanes[2] |= (byte)(1 << (7 - col));
                            if ((bColor & 8) == 8) bPlanes[3] |= (byte)(1 << (7 - col));
                        }

                        miniMap[position + row * 2] = bPlanes[0];
                        miniMap[position + row * 2 + 1] = bPlanes[1];
                        miniMap[position + row * 2 + 16] = bPlanes[2];
                        miniMap[position + row * 2 + 17] = bPlanes[3];
                    }
                }
            }

            for (y = 16; y < 24; y += 8, ry += 8)
            {
                rx = 0;
                for (x = 0; x < 64; x += 8, rx += 8)
                {
                    int position = (ry * 32) + (rx * 4);
                    byte[] bPlanes = new byte[4];

                    for (int row = 0; row < 8; row++)
                    {
                        bPlanes[0] = bPlanes[1] = bPlanes[2] = bPlanes[3] = 0;

                        for (int col = 0; col < 8; col++)
                        {
                            byte bColor = aMap[(y + row) * 64 + (x + col)];
                            if ((bColor & 1) == 1) bPlanes[0] |= (byte)(1 << (7 - col));
                            if ((bColor & 2) == 2) bPlanes[1] |= (byte)(1 << (7 - col));
                            if ((bColor & 4) == 4) bPlanes[2] |= (byte)(1 << (7 - col));
                            if ((bColor & 8) == 8) bPlanes[3] |= (byte)(1 << (7 - col));
                        }

                        miniMap[position + row * 2] = bPlanes[0];
                        miniMap[position + row * 2 + 1] = bPlanes[1];
                        miniMap[position + row * 2 + 16] = bPlanes[2];
                        miniMap[position + row * 2 + 17] = bPlanes[3];
                    }
                }
            }

            for (y = 8; y < 16; y += 8, ry += 8)
            {
                rx = 0;
                for (x = 0; x < 64; x += 8, rx += 8)
                {
                    int position = (ry * 32) + (rx * 4);
                    byte[] bPlanes = new byte[4];

                    for (int row = 0; row < 8; row++)
                    {
                        bPlanes[0] = bPlanes[1] = bPlanes[2] = bPlanes[3] = 0;

                        for (int col = 0; col < 8; col++)
                        {
                            byte bColor = aMap[(y + row) * 64 + (x + col)];
                            if ((bColor & 1) == 1) bPlanes[0] |= (byte)(1 << (7 - col));
                            if ((bColor & 2) == 2) bPlanes[1] |= (byte)(1 << (7 - col));
                            if ((bColor & 4) == 4) bPlanes[2] |= (byte)(1 << (7 - col));
                            if ((bColor & 8) == 8) bPlanes[3] |= (byte)(1 << (7 - col));
                        }

                        miniMap[position + row * 2] = bPlanes[0];
                        miniMap[position + row * 2 + 1] = bPlanes[1];
                        miniMap[position + row * 2 + 16] = bPlanes[2];
                        miniMap[position + row * 2 + 17] = bPlanes[3];
                    }
                }
            }

            for (y = 24; y < 40; y += 8, ry += 8)
            {
                rx = 0;
                for (x = 0; x < 64; x += 8, rx += 8)
                {
                    int position = (ry * 32) + (rx * 4);
                    byte[] bPlanes = new byte[4];

                    for (int row = 0; row < 8; row++)
                    {
                        bPlanes[0] = bPlanes[1] = bPlanes[2] = bPlanes[3] = 0;

                        for (int col = 0; col < 8; col++)
                        {
                            byte bColor = aMap[(y + row) * 64 + (x + col)];
                            if ((bColor & 1) == 1) bPlanes[0] |= (byte)(1 << (7 - col));
                            if ((bColor & 2) == 2) bPlanes[1] |= (byte)(1 << (7 - col));
                            if ((bColor & 4) == 4) bPlanes[2] |= (byte)(1 << (7 - col));
                            if ((bColor & 8) == 8) bPlanes[3] |= (byte)(1 << (7 - col));
                        }

                        miniMap[position + row * 2] = bPlanes[0];
                        miniMap[position + row * 2 + 1] = bPlanes[1];
                        miniMap[position + row * 2 + 16] = bPlanes[2];
                        miniMap[position + row * 2 + 17] = bPlanes[3];
                    }
                }
            }

            for (y = 48; y < 56; y += 8, ry += 8)
            {
                rx = 0;
                for (x = 0; x < 64; x += 8, rx += 8)
                {
                    int position = (ry * 32) + (rx * 4);
                    byte[] bPlanes = new byte[4];

                    for (int row = 0; row < 8; row++)
                    {
                        bPlanes[0] = bPlanes[1] = bPlanes[2] = bPlanes[3] = 0;

                        for (int col = 0; col < 8; col++)
                        {
                            byte bColor = aMap[(y + row) * 64 + (x + col)];
                            if ((bColor & 1) == 1) bPlanes[0] |= (byte)(1 << (7 - col));
                            if ((bColor & 2) == 2) bPlanes[1] |= (byte)(1 << (7 - col));
                            if ((bColor & 4) == 4) bPlanes[2] |= (byte)(1 << (7 - col));
                            if ((bColor & 8) == 8) bPlanes[3] |= (byte)(1 << (7 - col));
                        }

                        miniMap[position + row * 2] = bPlanes[0];
                        miniMap[position + row * 2 + 1] = bPlanes[1];
                        miniMap[position + row * 2 + 16] = bPlanes[2];
                        miniMap[position + row * 2 + 17] = bPlanes[3];
                    }
                }
            }

            for (y = 40; y < 48; y += 8, ry += 8)
            {
                rx = 0;
                for (x = 0; x < 64; x += 8, rx += 8)
                {
                    int position = (ry * 32) + (rx * 4);
                    byte[] bPlanes = new byte[4];

                    for (int row = 0; row < 8; row++)
                    {
                        bPlanes[0] = bPlanes[1] = bPlanes[2] = bPlanes[3] = 0;

                        for (int col = 0; col < 8; col++)
                        {
                            byte bColor = aMap[(y + row) * 64 + (x + col)];
                            if ((bColor & 1) == 1) bPlanes[0] |= (byte)(1 << (7 - col));
                            if ((bColor & 2) == 2) bPlanes[1] |= (byte)(1 << (7 - col));
                            if ((bColor & 4) == 4) bPlanes[2] |= (byte)(1 << (7 - col));
                            if ((bColor & 8) == 8) bPlanes[3] |= (byte)(1 << (7 - col));
                        }

                        miniMap[position + row * 2] = bPlanes[0];
                        miniMap[position + row * 2 + 1] = bPlanes[1];
                        miniMap[position + row * 2 + 16] = bPlanes[2];
                        miniMap[position + row * 2 + 17] = bPlanes[3];
                    }
                }
            }

            for (y = 56; y < 64; y += 8, ry += 8)
            {
                rx = 0;
                for (x = 0; x < 64; x += 8, rx += 8)
                {
                    int position = (ry * 32) + (rx * 4);
                    byte[] bPlanes = new byte[4];

                    for (int row = 0; row < 8; row++)
                    {
                        bPlanes[0] = bPlanes[1] = bPlanes[2] = bPlanes[3] = 0;

                        for (int col = 0; col < 8; col++)
                        {
                            byte bColor = aMap[(y + row) * 64 + (x + col)];
                            if ((bColor & 1) == 1) bPlanes[0] |= (byte)(1 << (7 - col));
                            if ((bColor & 2) == 2) bPlanes[1] |= (byte)(1 << (7 - col));
                            if ((bColor & 4) == 4) bPlanes[2] |= (byte)(1 << (7 - col));
                            if ((bColor & 8) == 8) bPlanes[3] |= (byte)(1 << (7 - col));
                        }

                        miniMap[position + row * 2] = bPlanes[0];
                        miniMap[position + row * 2 + 1] = bPlanes[1];
                        miniMap[position + row * 2 + 16] = bPlanes[2];
                        miniMap[position + row * 2 + 17] = bPlanes[3];
                    }
                }
            }

            return Compression.Compress(miniMap, 0x800);
        }

        private static Trigger[] ReadMapTriggers(int mapIndex)
        {
            int dwEntranceOffset = SmcToAbs(MAP_TRIGGERS) + mapIndex * 2;
            int dwEventOffset = SmcToAbs(EVENT_TRIGGERS) + mapIndex * 2;
            int wThisOffset = Bits.GetShort(rom, dwEntranceOffset);
            int wNextOffset = Bits.GetShort(rom, dwEntranceOffset + 2);
            int wThisEventOffset = Bits.GetShort(rom, dwEventOffset);
            int wNextEventOffset = Bits.GetShort(rom, dwEventOffset + 2);
            int numTriggers = (wNextOffset - wThisOffset) / 6;
            int numEvents = (wNextEventOffset - wThisEventOffset) / 5;
            Trigger[] triggers = new Trigger[numTriggers + numEvents];

            int offset = SmcToAbs(MAP_TRIGGERS) + wThisOffset;

            for (int i = 0; i < numTriggers; i++)
            {
                int x = rom[offset];
                int y = rom[offset + 1];
                triggers[i] = new Trigger { cx = x, cy = y };
                offset += 6;
            }

            offset = SmcToAbs(EVENT_TRIGGERS) + wThisEventOffset;
            int eventEnd = numTriggers + numEvents;

            for (int i = numTriggers; i < eventEnd; i++)
            {
                int x = rom[offset];
                int y = rom[offset + 1];
                triggers[i] = new Trigger { cx = x, cy = y };
                offset += 5;
            }

            return triggers;
        }

        #endregion

        private static Trigger GetTrigger(int x, int y, bool isWob)
        {
            Trigger trigger;
            int index;

            if (isWob)
            {
                index = Array.FindIndex(wobTriggers, t => t.cx == x && t.cy == y);
                trigger = index == -1 ? new Trigger { cx = -1, cy = -1 } : wobTriggers[index];
            }
            else
            {
                index = Array.FindIndex(worTriggers, t => t.cx == x && t.cy == y);
                trigger = index == -1 ? new Trigger { cx = -1, cy = -1 } : worTriggers[index];
            }

            return trigger;                  
        }

        private static byte[] ReadBytesFromPointer(int pointer, ushort size)
        {
            int absOffset = GetOffsetFromPointer(pointer);
            return Bits.GetBytes(rom, absOffset, size);
        }

        private static ushort[] ReadTileProperties(int offset)
        {
            return Bits.GetShorts(rom, SmcToAbs(offset), 256);
        }

        private static byte[] Decompress(int pointer)
        {
            int absOffset = GetOffsetFromPointer(pointer);
            return Compression.Decompress(rom, absOffset);
        }

        private static int GetOffsetFromPointer(int pointer)
        {
            return SmcToAbs(Bits.GetInt24(rom, SmcToAbs(pointer)));
        }

        private static int SmcToAbs(int offset)
        {
            return offset - 0xC00000;
        }

        private static int AbsToSmc(int offset)
        {
            return offset + 0xC00000;
        }
    }
}
