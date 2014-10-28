using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextToXml
{
    public class ScriptLine
    {
        public string[] parts = null;

        public string Arg(int i)
        {
            if (parts != null && parts.Length > i)
                return parts[i];
            return "";
        }

        public string this[int index]
        {
            get
            {
                return Arg(index);
            }
        }
        public int Length
        {
            get
            {
                return parts.Length;
            }
        }

        public List<ScriptLine> Sublines = new List<ScriptLine>();

        public void SetString(string s)
        {
            parts = s.Split(' ');
        }
    }
}
