using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextToXml
{
    public class DataInputStream
    {
        public string Data = "";
        public List<char> PreBuffer = new List<char>();
        protected int _index = 0;

        public int Position
        {
            get { return _index; }
        }

        public bool GetChar(ref char rc)
        {
            if (PreBuffer.Count > 0)
            {
                rc = PreBuffer[0];
                PreBuffer.RemoveAt(0);
                return true;
            }
            else if (_index < Data.Length)
            {
                rc = Data[_index];
                _index++;
                return true;
            }

            return false;
        }

        public void PutCharFirst(char pc)
        {
            PreBuffer.Insert(0, pc);
        }

        public void PutCharLast(char pc)
        {
            PreBuffer.Add(pc);
        }
    }
}
