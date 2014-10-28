using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace TextToXml
{
    public class Tokenizer
    {
        public virtual ParserNode Process(ParserNode root)
        {
            return root;
        }

        /// <summary>
        /// removes COMMENT, DOC and NEWLINE nodes from highest level
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        protected List<ParserNode> MoveStartingCommentsToNext(List<ParserNode> array)
        {
            bool isNL = false;
            ParserNode previous = null;
            List<ParserNode> temp = new List<ParserNode>();

            foreach (ParserNode item in array)
            {
                if (item.Type == "NEWLINE")
                {
                    isNL = true;
                    /*if (previous != null)
                    {
                        ParserNode after;
                        if (previous.Type == "NEWLINE")
                            after = previous.Parent;
                        else
                            after = previous.GetNode("AFTER", "AFTER");
                        after.AddNode(item);
                    }*/
                }
                else
                {
                    if (item.Type == "DOC" || item.Type == "COMMENT")
                    {
                        item.IsAttribute = true;
                        //item.Name = "attr-" + item.Name.ToLower();
                        if (isNL)
                        {
                            temp.Add(item);
                        }
                        else
                        {
                            if (previous != null)
                            {
                                ParserNode after;
                                after = previous.GetNode("AFTER", "AFTER");
                                after.AddNode(item);
                            }
                            else
                            {
                                temp.Add(item);
                                isNL = true;
                            }
                        }
                    }
                    else
                    {
                        if (temp.Count > 0)
                        {
                            ParserNode before = item.GetNode("BEFORE", "BEFORE");
                            foreach (ParserNode node in temp)
                            {
                                before.Nodes.Add(node);
                            }
                            temp.Clear();
                        }
                        isNL = false;
                    }
                }
                previous = item;
            }

            temp.Clear();
            foreach (ParserNode pn in array)
            {
                if (pn.Type != "DOC" && pn.Type != "COMMENT" && pn.Type != "NEWLINE")
                {
                    if (pn.Type == "")
                        pn.Type = "NODE";
                    temp.Add(pn);
                }
            }

            return temp;
        }

        public string JoinNamesFromArray(List<ParserNode> list, int start, int count)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                sb.Append(list[start + i].Name);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Finds next occurence of given string
        /// string must be on level = 0
        /// all brackets increases level (and closing bracktes decreases it)
        /// </summary>
        /// <param name="list"></param>
        /// <param name="startIndex"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public int FindNextIndexAtLevelZero(List<ParserNode> list, int startIndex, string str)
        {
            int level = 0;
            for (int i = startIndex; i < list.Count; i++)
            {
                if (list[i].Type == "STRING" || list[i].Type == "CONST_CHAR")
                    continue;
                if (list[i].Name == str && level == 0)
                {
                    return i;
                }
                else if (list[i].Name == "(" || list[i].Name == "[" || list[i].Name == "{")
                    level++;
                else if (list[i].Name == ")" || list[i].Name == "]" || list[i].Name == "}")
                    level--;
            }

            return -1;
        }

        public int RangeContainsName(List<ParserNode> list, int start, int end, string name)
        {
            for (int i = start; i <= end; i++)
            {
                if (list[i].Name == name)
                    return i;
            }
            return -1;
        }
    }

}
