using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextToXml
{
    public class DataContext
    {
        public DataInputStream Input = new DataInputStream();

        public List<ParserNode> InputNodes = new List<ParserNode>();
        public int CurrentNodeIndex = 0;
        public ParserNode CurrentInputNode = null;

        public ParserMachine Parser = null;
        public int CurrentState = 0;
        public Dictionary<string, StringBuilder> Scalars = new Dictionary<string, StringBuilder>();
        public Dictionary<string, List<string>> Arrays = new Dictionary<string, List<string>>();
        public List<string[]> ArgumentStack = new List<string[]>();
        public ParserNode Root = new ParserNode();
        public ParserNode CurrentNode = null;
        public ParserNode LastNodeAdded = null;
        public bool ShouldReturn = false;
        public bool ShouldReturnTotal = false;


        public DataContext()
        {
            CurrentNode = Root;
        }

        public StringBuilder GetSafeScalar(string name)
        {
            if (Scalars.ContainsKey(name))
                return Scalars[name];
            StringBuilder sb = new StringBuilder();
            Scalars[name] = sb;
            return sb;
        }

        public StringBuilder GetScalar(string name)
        {
            if (Scalars.ContainsKey(name))
                return Scalars[name];
            return null;
        }

        public void SetScalarValue(string name, string value)
        {
            StringBuilder sb = GetSafeScalar(name);
            sb.Remove(0, sb.Length);
            sb.Append(value);
        }

        public List<string> GetSafeArray(string name)
        {
            if (Arrays.ContainsKey(name))
                return Arrays[name];
            List<string> ar = new List<string>();
            Arrays[name] = ar;
            return ar;
        }

        public static string RawStringToRegular(string s)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in s)
            {
                if (c == '\\')
                    sb.Append("\\\\");
                else if (c == ' ')
                    sb.Append("\\s");
                else if (c == '\n')
                    sb.Append("\\n");
                else if (c == '\t')
                    sb.Append("\\t");
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }

        public string GetStringValue(string str)
        {
            if (str.StartsWith("$"))
            {
                int i = 0;
                if (int.TryParse(str.Substring(1), out i))
                {
                    if (ArgumentStack.Count > 0 && ArgumentStack[0].Length >= i && i > 0)
                    {
                        return ArgumentStack[0][i - 1];
                    }
                    return string.Empty;
                }
                else if (Scalars.ContainsKey(str))
                {
                    return Scalars[str].ToString();
                }
                else
                {
                    return string.Empty;
                }
            }
            else if (str.IndexOf('\\') >= 0)
            {
                StringBuilder sb = new StringBuilder();
                bool spec = false;
                foreach (char c in str)
                {
                    if (spec)
                    {
                        if (c == 'n') sb.Append('\n');
                        else if (c == 't') sb.Append('\t');
                        else if (c == 's') sb.Append(' ');
                        else sb.Append(c);
                        spec = false;
                    }
                    else if (c == '\\')
                    {
                        spec = true;
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                return sb.ToString();
            }
            else
            {
                return str;
            }
        }
    }
}
