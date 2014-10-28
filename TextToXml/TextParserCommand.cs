using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextToXml
{
    public class TextParserCommand
    {
        public static void clr(DataContext ctx, string variable)
        {
            if (ctx.Scalars.ContainsKey(variable))
            {
                StringBuilder sb = ctx.Scalars[variable];
                sb.Remove(0, sb.Length);
            }
            else
            {
                ctx.Scalars[variable] = new StringBuilder();
            }
        }

        /// <summary>
        /// Appending scalar value at the end of scalar variable
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="buffer">Name of scalar variable</param>
        /// <param name="str">scalar value</param>
        public static void append(DataContext ctx, string buffer, string str)
        {
            StringBuilder sb = ctx.GetSafeScalar(buffer);
            sb.Append(ctx.GetStringValue(str));
        }

        public static void back(DataContext ctx, string val)
        {
            string val2 = ctx.GetStringValue(val);
            foreach (char pc in val2)
            {
                ctx.Input.PutCharLast(pc);
            }
        }

        /// <summary>
        /// Setting new value to scalar variable
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="buffer"></param>
        /// <param name="str"></param>
        public static void set(DataContext ctx, string buffer, string str)
        {
            StringBuilder sb = ctx.GetSafeScalar(buffer);
            sb.Remove(0,sb.Length);
            StringBuilder newl = ctx.GetScalar(str);
            sb.Append(ctx.GetStringValue(str));
        }

        public static void adddir(DataContext ctx, string dir)
        {
            adddir(ctx, dir, null);
        }

        public static void adddir(DataContext ctx, string dir, string type)
        {
            ParserNode pn = new ParserNode();
            pn.Name = ctx.GetStringValue(dir);
            if (type != null)
                pn.Type = ctx.GetStringValue(type);
            ctx.CurrentNode.AddNode(pn);
            ctx.LastNodeAdded = pn;
        }

        public static void nodestate(DataContext ctx, string state)
        {
            if (ctx.Parser != null && ctx.Parser.StateID.ContainsKey(state)
                && ctx.CurrentNode != null)
            {
                ctx.CurrentNode.NodeState = ctx.Parser.StateID[state];
            }
        }

        /// <summary>
        /// Changes current node (current directory) in result tree
        /// It may also change current state, if new node has assigned NodeState
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="dir"></param>
        public static void cd(DataContext ctx, string dir)
        {
            string[] path = ctx.GetStringValue(dir).Split('/');
            for (int i = 0; i < path.Length; i++)
            {
                if (path[i] == "~")
                {
                    if (ctx.LastNodeAdded != null)
                        ctx.CurrentNode = ctx.LastNodeAdded;
                }
                else if (path[i] == "")
                    ctx.CurrentNode = ctx.Root;
                else if (path[i] == "..")
                {
                    if (ctx.CurrentNode == null || ctx.CurrentNode.Parent == null)
                        throw new Exception("Nonexistent part of path .. for: " + dir);
                    ctx.CurrentNode = ctx.CurrentNode.Parent;
                }
                else
                {
                    ParserNode pn = ctx.CurrentNode.FindNode(path[i]);
                    if (pn == null)
                    {
                        throw new Exception("Nonexistent part of path " + path[i] + " for: " + dir);
                    }
                    ctx.CurrentNode = pn;
                }
            }

            if (ctx.CurrentNode != null && ctx.CurrentNode.NodeState >= 0)
                ctx.CurrentState = ctx.CurrentNode.NodeState;
        }

        /// <summary>
        /// Sets new state if first two arguments equals
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="var">first value</param>
        /// <param name="value">second value</param>
        /// <param name="next_state">new state to be set, if values are equal</param>
        public static void transition_if(DataContext ctx, string var, string value, string next_state)
        {
            string sb = ctx.GetStringValue(var);
            string val = ctx.GetStringValue(value);
            string ns = ctx.GetStringValue(next_state);

            if (sb == val)
            {
                ctx.CurrentState = ctx.Parser.GetStateID(ns);
                ctx.ShouldReturnTotal = true;
            }
        }

        public static void list_add(DataContext ctx, string listx, string valx)
        {
            List<string> list = ctx.GetSafeArray(listx);
            string val = ctx.GetStringValue(valx);

            list.Add(val);
        }

        public static void list_clear(DataContext ctx, string listx)
        {
            List<string> list = ctx.GetSafeArray(listx);
            list.Clear();
        }

    }
}
