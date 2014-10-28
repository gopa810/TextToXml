using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextToXml
{
    public class TokenParserMachineOld
    {
        public ParserNode Root = new ParserNode();

        public ParserNode ProcessTokens(List<ParserNode> array, string format)
        {
            if (format.ToLower() == "c#")
            {
                Root = ProcessTokensCSharp(array);
            }
            else if (format.ToLower() == "perl")
            {
                Root.Nodes = array;
            }
            else
            {
                Root.Nodes = array;
            }

            return Root;
        }

        /// <summary>
        /// Reorganizing nodes according C# format
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public ParserNode ProcessTokensCSharp(List<ParserNode> array)
        {
            List<ParserNode> root = array;

            ParserNode result = new ParserNode();
            int mode = 0;
            ParserNode lastNode = result;
            StringBuilder sb = new StringBuilder();
            List<string> strList = new List<string>();

            foreach (ParserNode pn in root)
            {
                if (mode == 0)
                {
                    if (pn.Name == "using")
                    {
                        result.AddNode(pn);
                        lastNode = pn;
                        mode = 1;
                        sb.Remove(0, sb.Length);
                    }
                    else if (pn.Name == "namespace")
                    {
                        result.AddNode(pn);
                        lastNode = pn;
                        mode = 2;
                        sb.Remove(0, sb.Length);
                    }
                    else
                    {
                        lastNode.AddNode(pn);
                    }
                }
                else if (mode == 1)
                {
                    if (pn.Name == ";")
                    {
                        lastNode.Name = sb.ToString();
                        lastNode.Type = "USING";
                        lastNode = result;
                        mode = 0;
                    }
                    else
                    {
                        sb.Append(pn.Name);
                    }
                }
                else if (mode == 2)
                {
                    if (pn.Name == "{")
                    {
                        lastNode.Name = sb.ToString();
                        lastNode.Type = "NAMESPACE";
                        strList.Clear();
                        mode = 3;
                    }
                    else if (pn.Name == "}")
                    {
                        lastNode = lastNode.Parent;
                        mode = 0;
                    }
                    else
                    {
                        sb.Append(pn.Name);
                    }
                }
                else if (mode == 3) // wait for { and take string before as definition what is in the block
                {
                    if (pn.Name == "{")
                    {
                        if (strList.Count > 0)
                        {
                            lastNode = lastNode.AddNode(strList[strList.Count - 1]);
                            if (strList.Count > 1)
                                lastNode.Type = strList[strList.Count - 2].ToUpper();
                            for (int i = 0; i < strList.Count - 2; i++)
                            {
                                lastNode.AddNode(strList[i], "DOMAINPROPERTY");
                            }
                        }
                        else
                        {
                            lastNode = lastNode.AddNode("{}");
                        }
                        if (lastNode.Type == "CLASS")
                            mode = 4;
                        else
                            mode = 3;
                    }
                    else if (pn.Name == "}")
                    {
                        lastNode = lastNode.Parent;
                        mode = 2;
                    }
                    else
                    {
                        strList.Add(pn.Name);
                    }
                }
                else if (mode == 4)
                {
                    lastNode.AddNode(pn);
                }
            }

            return result;
        }


    }
}
