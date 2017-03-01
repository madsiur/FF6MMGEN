// Author: Frederic Dupont <themadsiur@gmail.com>
// Copyright 2017 Frederic Dupont

// This file is part of FF6MMGEN.

// FF6MMGENis free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// FF6MMGEN is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with FF6MMGEN.  If not, see<http://www.gnu.org/licenses/>.

// This class is based on some code by giangurgolo
// ZONE DOCTOR - Final Fantasy 6 Editor
// Version: 3.18.4
// Date: August 26, 2013
// Available here: http://www.ff6hacking.com/wiki/doku.php?id=ff3:ff3us:util:start#source_code

using System;
using System.IO;
using System.Text;

namespace FF6MMGEN.Classes
{  
    public class Rom
    {
        public string Filename { get; private set; }
        public long RomSize { get; private set; }
        public string GameCode { get; private set; }
        public byte[] Content { get; set; }
        public byte[] Header { get; private set; }
        public ushort CheckSum { get; private set; }

        public Rom(string fileName)
        {
            this.Filename = fileName;
        }

        public string GetGameCode()
        {
            return Encoding.UTF8.GetString(Bits.GetBytes(Content, 0xFFB0, 4));
        }      

        public void SetRomChecksum()
        {
            int chunk0 = 0;
            int chunk1 = 0;

            for (int i = 0; i < Content.Length; i++)
            {
                if (i < 0x200000)
                    chunk0 += Content[i];
                else
                    chunk1 += Content[i];
            }

            CheckSum = (ushort)((chunk0 + chunk1) & 0xFFFF);

            Bits.SetShort(Content, 0xFFDE, (ushort)(CheckSum & 0xFFFF));
            Bits.SetShort(Content, 0xFFDC, (ushort)(CheckSum ^ 0xFFFF));
        }

        public bool WriteRom()
        {
            try
            {
                SetRomChecksum();
                AddHeader();
                BinaryWriter binWriter = new BinaryWriter(File.Open(Filename, FileMode.Create));
                binWriter.Write(Content);
                binWriter.Close();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to write to file " + Filename, "Error: " + ex.Message);
                return false;
            }
        }

        public bool ReadRom()
        {
            bool valid = false;

            try
            {
                FileInfo fInfo = new FileInfo(Filename);
                RomSize = fInfo.Length;

                if (!(fInfo.Extension.ToLower() == ".smc") && !(fInfo.Extension.ToLower() == ".sfc"))
                {
                    Console.Error.WriteLine("Invalid ROM extension: " + fInfo.Extension);
                }
                else if (RomSize < 0x300000 || RomSize > 0x600000)
                {
                    Console.Error.WriteLine("Invalid ROM size: $" + RomSize.ToString("X8"));
                }
                else
                {
                    FileStream fStream = new FileStream(Filename, FileMode.Open, FileAccess.Read);
                    BinaryReader br = new BinaryReader(fStream);
                    Content = br.ReadBytes((int)fInfo.Length);
                    br.Close();
                    fStream.Close();
                    RemoveHeader();
                    valid = true;
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Unable to read ROM " + Filename, "Error: " + e.Message);
            }

            return valid;
        }

        public void RemoveHeader()
        {
            Header = null;

            if ((RomSize & 0x200) == 0x200)
            {
                RomSize -= 0x200;
                Header = Bits.GetBytes(Content, 0, 0x200);
                Content = Bits.GetBytes(Content, 0x200, (int)RomSize);
            }
        }

        public void AddHeader()
        {
            if (Header != null)
            {
                RomSize += 0x200;
                byte[] temp = new byte[RomSize];
                Header.CopyTo(temp, 0);
                Content.CopyTo(temp, 0x200);
                Content = temp;
            }
        }
    }
}
