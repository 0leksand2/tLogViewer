using System;
using System.Collections.Generic;
using System.Text;

namespace tLogViewer.Reader.Services
{
    public class LogReader
    {
        public void ReadLog(string path)
        {
            using var fs = File.OpenRead(path);
            using var reader = new BinaryReader(fs);
            long timestamp = reader.ReadInt64();

            byte stx;

            do
            {
                stx = reader.ReadByte();
            }
            while (stx != 0xFE && stx != 0xFD);

            var len = reader.ReadByte();

            if(stx == 0xFE)
            {
                ReadMavlink1();
            }
            else if (stx == 0xFD)
            {
                ReadMavlink2();
            }
        }

        public void ReadMavlink1() { }

        public void ReadMavlink2() { }

    }
}
