using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextToXml
{
    public class ParserNode
    {
        public string Type = string.Empty;
        public string Name = string.Empty;
        public string BigComment = null;
        public string SmallComment = null;
        public bool IsAttribute = false;

        public ParserNode()
        {
        }

        public ParserNode(string n)
        {
            Name = n;
        }

        public ParserNode(string n, string t)
        {
            Name = n;
            Type = t;
        }

        public int PositionInFile = 0;
        /// <summary>
        /// Parent node (directory)
        /// </summary>
        public ParserNode Parent = null;
        /// <summary>
        /// If this parameter is >= 0, then it serves for
        /// changing directories. When directory is changed, we
        /// will check in the new directory, if this NodeState is
        /// set. If yes, we will change CurrentState to the value set here.
        /// </summary>
        public int NodeState = -1;
        /// <summary>
        /// Child nodes.
        /// </summary>
        public List<ParserNode> Nodes = new List<ParserNode>();

        public ParserNode AddNode(string name)
        {
            return AddNode(name, "");
        }

        public ParserNode AddNode(string name, string type)
        {
            ParserNode pn = new ParserNode();
            pn.Name = name;
            pn.Type = type;
            pn.Parent = this;
            Nodes.Add(pn);
            return pn;
        }

        public ParserNode AddNode(ParserNode node)
        {
            if (node != null)
            {
                node.Parent = this;
                Nodes.Add(node);
            }
            return node;
        }

        public ParserNode FindNode(string name)
        {
            foreach (ParserNode pn in Nodes)
            {
                if (pn.Name == name)
                    return pn;
            }
            return null;
        }

        public ParserNode GetNode(string name, string type)
        {
            ParserNode pn = FindNode(name);
            if (pn == null)
            {
                pn = new ParserNode();
                pn.Name = name;
                pn.Type = type;
                AddNode(pn);
            }

            return pn;
        }

        public override string ToString()
        {
            return string.Format("{0} ({1})", Name, Type);
        }
    }
}
