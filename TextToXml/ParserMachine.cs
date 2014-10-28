using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace TextToXml
{
    public class ParserMachine
    {
        public ITextParserMachineDelegate Delegate = null;


        public Dictionary<int, string> StateName = new Dictionary<int, string>();
        public Dictionary<string, int> StateID = new Dictionary<string, int>();
        public List<Transition> Transitions = new List<Transition>();
        public Dictionary<string, string> Actions = new Dictionary<string, string>();
        public string[] FileExtensions = { };
        public string ParserName = string.Empty;
        public string[] currentLine = null;

        public string Arg(int i)
        {
            if (currentLine != null && currentLine.Length > i)
                return currentLine[i];
            return "";
        }

        public int AddState(string name)
        {
            if (name.Length == 0)
                return -1;
            int id = StateID.Count;
            StateName[id] = name;
            StateID[name] = id;
            return id;
        }

        public int GetStateID(string name)
        {
            if (StateID.ContainsKey(name))
                return StateID[name];
            return AddState(name);
        }

        public string GetUniqueStateName(string prefix)
        {
            if (StateID.ContainsKey(prefix) == false)
                return prefix;
            for (int i = 0; i < 1000; i++)
            {
                if (StateID.ContainsKey(string.Format("{0}_{1}", prefix, i)) == false)
                    return string.Format("{0}_{1}", prefix, i);
            }
            return prefix;
        }
        
        public Transition GetTransition(int currentState, object rc)
        {
            foreach (Transition t in Transitions)
            {
                if (!t.AnyChar && t.RespondToObject(rc) && t.StateA == currentState)
                    return t;
            }
            return null;
        }

        public Transition GetAnyTransition(int currentState)
        {
            foreach (Transition t in Transitions)
            {
                if (t.AnyChar && t.StateA == currentState)
                    return t;
            }
            return null;
        }

        /// <summary>
        /// Executing script
        /// </summary>
        /// <param name="script"></param>
        /// <param name="ctx"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public string ExecuteScript(string script, DataContext ctx, params string[] args)
        {
            string[] lines = script.Split('\n');
            string ret = string.Empty;
            ctx.ArgumentStack.Insert(0, args);
            ScriptLineStack stack = new ScriptLineStack();
            ScriptLine scriptX = new ScriptLine();
            stack.Push(scriptX);
            foreach (string line in lines)
            {
                ScriptLine lineX = new ScriptLine();
                lineX.SetString(line.TrimStart());
                if (lineX[0] == "foreach" || lineX[0] == "for" || lineX[0] == "if")
                {
                    stack.Current.Sublines.Add(lineX);
                    stack.Push(lineX);
                }
                else if (lineX[0] == "end")
                {
                    stack.Pop();
                }
                else
                {
                    stack.Current.Sublines.Add(lineX);
                }
            }
            ret = ExecuteScriptLines(scriptX.Sublines, ctx);

            ctx.ArgumentStack.RemoveAt(0);
            return ret;
        }

        protected string ExecuteScriptLines(List<ScriptLine> lines, DataContext ctx)
        {
            string ret = string.Empty;
            foreach (ScriptLine line in lines)
            {
                ret = ExecuteCommand(line, ctx);
                if (ctx.ShouldReturn)
                {
                    ctx.ShouldReturn = false;
                    break;
                }
                if (ctx.ShouldReturnTotal)
                    break;
            }
            return ret;
        }
        /// <summary>
        /// Execute command
        /// </summary>
        /// <param name="parts">Parts of command (instruction)</param>
        /// <param name="ctx">Context of data space</param>
        /// <param name="args">arguments list, referenced by $1, $2, $3, ...</param>
        /// <returns></returns>
        public string ExecuteCommand(ScriptLine parts, DataContext ctx)
        {
            string ret = string.Empty;
            if (parts.Length == 0)
                return ret;

            switch (parts[0])
            {
                case "list.add":
                    TextParserCommand.list_add(ctx, parts[1], parts[2]);
                    break;
                case "list.clear":
                    {
                        List<string> list = ctx.GetSafeArray(parts[1]);
                        list.Clear();
                    }
                    break;
                case "ask":
                    ctx.CurrentState = -1;
                    break;
                case "append":
                    if (parts.Length == 3)
                    {
                        TextParserCommand.append(ctx, parts[1], parts[2]);
                    }
                    break;
                case "back":
                    if (parts.Length == 2)
                        TextParserCommand.back(ctx, parts[1]);
                    break;
                case "cd":
                    if (parts.Length == 2)
                        TextParserCommand.cd(ctx, parts[1]);
                    break;
                case "clr":
                    for (int i = 1; i < parts.Length; i++)
                    {
                        TextParserCommand.clr(ctx, parts[i]);
                    }
                    break;
                case "foreach":
                    {
                        List<string> list = ctx.GetSafeArray(parts[2]);
                        StringBuilder sb = ctx.GetSafeScalar(parts[1]);
                        if (list.Count > 0)
                        {
                            foreach (string val in list)
                            {
                                sb.Remove(0,sb.Length);
                                sb.Append(val);
                                ExecuteScriptLines(parts.Sublines, ctx);
                            }
                        }
                    }
                    break;
                case "if":
                    {
                        string val1 = ctx.GetStringValue(parts[1]);
                        string val2 = ctx.GetStringValue(parts[3]);
                        string oper = ctx.GetStringValue(parts[2]);
                        if (oper == "==" || oper == "eq")
                        {
                            if (val1 == val2)
                                ExecuteScriptLines(parts.Sublines, ctx);
                        }
                        else if (oper == "!=" || oper == "ne")
                        {
                            if (val1 != val2)
                                ExecuteScriptLines(parts.Sublines, ctx);
                        }
                        else if (oper == "in")
                        {
                            for (int y = 3; y < parts.Length; y++)
                            {
                                if (val1 == ctx.GetStringValue(parts[y]))
                                {
                                    ExecuteScriptLines(parts.Sublines, ctx);
                                    break;
                                }
                            }
                        }
                    }
                    break;
                case "mkdir":
                case "adddir":
                    if (parts.Length == 2)
                        TextParserCommand.adddir(ctx, parts[1]);
                    else if (parts.Length == 3)
                        TextParserCommand.adddir(ctx, parts[1], parts[2]);
                    break;
                case "nodestate":
                    if (parts.Length == 2)
                        TextParserCommand.nodestate(ctx, parts[1]);
                    break;
                case "set":
                    if (parts.Length == 3)
                    {
                        TextParserCommand.set(ctx, parts[1], parts[2]);
                    }
                    break;
                case "transition_if":
                    if (parts.Length == 4)
                    {
                        TextParserCommand.transition_if(ctx, parts[1], parts[2], parts[3]);
                    }
                    break;
                case "parser.state.set":
                    {
                        string state = ctx.GetStringValue(parts[1]);
                        ctx.CurrentState = ctx.Parser.GetStateID(state);
                    }
                    break;
                case "return":
                    ctx.ShouldReturn = true;
                    break;
                case "exit":
                    ctx.ShouldReturnTotal = true;
                    break;
            }

            return ret;
        }

        public void Save(XmlDocument doc)
        {
            XmlElement root = doc.CreateElement("txml");
            doc.AppendChild(root);

            root.AppendChild(SaveStates(doc));
            root.AppendChild(SaveTransitions(doc));
            root.AppendChild(SaveActions(doc));
            root.AppendChild(SaveFileExtensions(doc));

            XmlElement name = doc.CreateElement("parsername");
            name.InnerText = ParserName;
            root.AppendChild(name);
        }

        public void Load(XmlDocument doc)
        {
            Clear();
            if (doc.ChildNodes.Count > 0)
            {
                XmlElement root = doc.ChildNodes[0] as XmlElement;
                foreach (XmlNode item in root.ChildNodes)
                {
                    if (item is XmlElement)
                    {
                        XmlElement elem = item as XmlElement;
                        if (elem.Name == "states")
                            LoadStates(elem);
                        else if (elem.Name == "transitions")
                            LoadTransitions(elem);
                        else if (elem.Name == "actions")
                            LoadActions(elem);
                        else if (elem.Name == "parsername")
                            ParserName = elem.InnerText;
                        else if (elem.Name == "fileextensions")
                            LoadFileExtensions(elem);
                    }
                }
            }
        }

        public void Clear()
        {
            StateID.Clear();
            StateName.Clear();
            Transitions.Clear();
            Actions.Clear();
        }

        protected XmlElement SaveStates(XmlDocument doc)
        {
            XmlElement elem1 = doc.CreateElement("states");
            foreach (KeyValuePair<int, string> pair in StateName)
            {
                XmlElement elem2 = doc.CreateElement("state");
                elem2.SetAttribute("sid", pair.Key.ToString());
                elem2.SetAttribute("name", pair.Value);
                elem1.AppendChild(elem2);
            }
            return elem1;
        }

        protected void LoadStates(XmlElement elem)
        {
            foreach (XmlElement item in elem.ChildNodes)
            {
                int stateID;
                string stateName;
                if (int.TryParse(item.GetAttribute("sid"), out stateID))
                {
                    stateName = item.GetAttribute("name");
                    StateID[stateName] = stateID;
                    StateName[stateID] = stateName;
                }
            }
        }

        protected virtual XmlElement SaveTransitions(XmlDocument doc)
        {
            XmlElement elem1 = doc.CreateElement("transitions");
            foreach (Transition trans in Transitions)
            {
                XmlElement elem2 = doc.CreateElement("transition");
                elem2.SetAttribute("statea", trans.StateA.ToString());
                elem2.SetAttribute("stateb", trans.StateB.ToString());
                elem2.SetAttribute("chars", trans.characters);
                elem2.SetAttribute("attrib", trans.AttributeName);
                elem2.SetAttribute("oper", trans.CompareOperatorIndex.ToString());
                elem2.InnerText = trans.actions;
                elem1.AppendChild(elem2);
            }
            return elem1;
        }

        protected virtual void LoadTransitions(XmlElement elem)
        {
            foreach (XmlElement item in elem.ChildNodes)
            {
                Transition tr = new Transition(this);
                if (int.TryParse(item.GetAttribute("statea"), out tr.StateA))
                {
                    if (int.TryParse(item.GetAttribute("stateb"), out tr.StateB))
                    {
                        if (item.HasAttribute("oper"))
                            tr.CompareOperatorIndex = int.Parse(item.GetAttribute("oper"));
                        if (item.HasAttribute("attrib"))
                            tr.AttributeName = item.GetAttribute("attrib");
                        tr.characters = item.GetAttribute("chars");
                        tr.actions = item.InnerText;
                        Transitions.Add(tr);
                    }
                }
            }
        }

        protected XmlElement SaveActions(XmlDocument doc)
        {
            XmlElement elem1 = doc.CreateElement("actions");
            foreach (KeyValuePair<string, string> pair in Actions)
            {
                XmlElement elem2 = doc.CreateElement("action");
                elem2.SetAttribute("name", pair.Key);
                elem2.InnerText = pair.Value;
                elem1.AppendChild(elem2);
            }
            return elem1;
        }

        protected void LoadActions(XmlElement elem)
        {
            foreach (XmlElement item in elem.ChildNodes)
            {
                Actions[item.GetAttribute("name")] = item.InnerText;
            }
        }

        protected XmlElement SaveFileExtensions(XmlDocument doc)
        {
            XmlElement elem2 = doc.CreateElement("fileextensions");
            foreach (string s in FileExtensions)
            {
                XmlElement elem3 = doc.CreateElement("ext");
                elem2.AppendChild(elem3);
                elem3.InnerText = s;
            }
            return elem2;
        }

        protected void LoadFileExtensions(XmlElement elem)
        {
            FileExtensions = new string[] { };
            if (elem.ChildNodes.Count > 0)
            {
                List<string> strs = new List<string>();
                foreach (XmlElement item in elem.ChildNodes)
                {
                    strs.Add(item.InnerText);
                }
                FileExtensions = strs.ToArray();
            }
        }

    }
}
