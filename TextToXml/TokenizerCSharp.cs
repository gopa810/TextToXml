using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextToXml
{
    public class TokenizerCSharp: Tokenizer
    {
        /// <summary>
        /// Main processing
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        public override ParserNode Process(ParserNode root)
        {
            List<ParserNode> nodes = MoveStartingCommentsToNext(root.Nodes);
            return ProcessClassBody(nodes);
        }

        public virtual ParserNode ProcessNodeFrom(List<ParserNode> nodes, int fromIndex, out int lastIndex)
        {
            int start;
            int end;
            string nodeName = nodes[fromIndex].Name;
            ParserNode newNode;
            if (nodes.Count <= fromIndex)
            {
                lastIndex = -1;
                return null;
            }

            if (nodeName == "using")
            {
                if ((fromIndex + 1) < nodes.Count && nodes[fromIndex + 1].Name == "(")
                {
                    newNode = ProcessWhileBlock(nodes, fromIndex, out lastIndex);
                }
                else
                {
                    newNode = ProcessReturnBlock(nodes, fromIndex, out lastIndex);
                }
            }
            else if (nodeName == "namespace")
            {
                start = FindNextIndexAtLevelZero(nodes, fromIndex, "{");
                end = FindNextIndexAtLevelZero(nodes, start + 1, "}");
                List<ParserNode> subrange = nodes.GetRange(start + 1, end - start - 1);
                //newNode = nodes[fromIndex];
                //newNode.AddNode(JoinNamesFromArray(nodes, fromIndex + 1, start - fromIndex - 1), "ns-name");
                newNode = ProcessClassBody(subrange);
                newNode.Type = "namespace";
                newNode.Name = JoinNamesFromArray(nodes, fromIndex + 1, start - fromIndex - 1);
                //newNode.AddNode(node1);
                lastIndex = end;
            }
            else if (nodeName == "while" || nodeName == "switch")
            {
                newNode = ProcessWhileBlock(nodes, fromIndex, out lastIndex);
            }
            else if (nodeName == "foreach")
            {
                newNode = ProcessForeach(nodes, fromIndex, out lastIndex);
            }
            else if (nodeName == "do")
            {
                newNode = ProcessDoWhile(nodes, fromIndex, out lastIndex);
            }
            else if (nodeName == "for")
            {
                newNode = ProcessFor(nodes, fromIndex, out lastIndex);
            }
            else if (nodeName == "case"
                && fromIndex + 2 < nodes.Count && nodes[fromIndex + 2].Name == ":")
            {
                newNode = ProcessGotoCase(nodes, fromIndex, out lastIndex);
            }
            else if (nodes[fromIndex].Name == "goto"
                && fromIndex + 2 < nodes.Count && nodes[fromIndex + 2].Name == ";")
            {
                newNode = ProcessGotoCase(nodes, fromIndex, out lastIndex);
            }
            else if (nodes[fromIndex].Name == "break" || nodes[fromIndex].Name == "continue")
            {
                lastIndex = FindNextIndexAtLevelZero(nodes, fromIndex, ";");
                newNode = nodes[fromIndex];
                newNode.Type = newNode.Name;
            }
            else if (nodeName == "try")
            {
                newNode = ProcessTryCatchFinally(nodes, fromIndex, out lastIndex);
            }
            else if (nodeName == "return" || nodeName == "throw")
            {
                newNode = ProcessReturnBlock(nodes, fromIndex, out lastIndex);
            }
            else if (fromIndex + 1 < nodes.Count && nodes[fromIndex + 1].Name == ":")
            {
                lastIndex = fromIndex + 1;
                newNode = nodes[fromIndex];
                newNode.Type = (newNode.Name == "default" ? "default" : "label");
            }
            else if (nodes[fromIndex].Name == "if")
            {
                newNode = ProcessIfStatement(nodes, fromIndex, out lastIndex);
            }
            else
            {
                newNode = ProcessUnknownStatement(nodes, fromIndex, out lastIndex);
            }
            return newNode;
        }

        /// <summary>
        /// processing DO - WHILE statement
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="fromIndex"></param>
        /// <param name="lastIndex"></param>
        /// <returns></returns>
        public virtual ParserNode ProcessDoWhile(List<ParserNode> nodes, int fromIndex, out int lastIndex)
        {
            ParserNode newNode, node1;
            ParserNode catch_node = null;
            ParserNode catch_cond = null;
            int last = 0;
            ParserNode try_body = ProcessNodeFrom(nodes, fromIndex + 1, out last);
            if (last > 0 && (last + 1) < nodes.Count
                && nodes[last + 1].Name == "while")
            {
                catch_node = nodes[last + 1];
                if (nodes[last + 2].Name == "(")
                {
                    catch_cond = ProcessExpression(nodes, last + 3, ")", out last);
                    last++;
                }
                else
                {
                    throw new Exception("expecting 'while' after 'do' block");
                }
            }
            newNode = nodes[fromIndex];
            newNode.Type = "do-while";

            if (try_body != null)
            {
                node1 = newNode.AddNode("body", "body");
                node1.AddNode(try_body);
            }

            if (catch_cond != null)
            {
                catch_cond.Type = "do-cond";
                newNode.AddNode(catch_cond);
            }

            lastIndex = last;
            return newNode;
        }


        /// <summary>
        /// processing:
        /// try - catch
        /// try - finally
        /// try - catch - finally
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="fromIndex"></param>
        /// <param name="lastIndex"></param>
        /// <returns></returns>
        public virtual ParserNode ProcessTryCatchFinally(List<ParserNode> nodes, int fromIndex, out int lastIndex)
        {
            ParserNode newNode, node1;
            List<ParserNode> catch_nodes = new List<ParserNode>();
            List<ParserNode> catch_blocks = new List<ParserNode>();
            List<ParserNode> catch_conds = new List<ParserNode>();
            ParserNode finally_node = null;
            ParserNode finally_block = null;
            int last = 0;
            ParserNode try_body = ProcessNodeFrom(nodes, fromIndex + 1, out last);
            while (last > 0 && (last + 1) < nodes.Count 
                && nodes[last + 1].Name == "catch")
            {
                catch_nodes.Add(nodes[last + 1]);
                ParserNode cond_node = null;
                if (nodes[last + 2].Name == "(")
                {
                    cond_node = ProcessExpression(nodes, last + 3, ")", out last);
                    last--;
                }
                catch_conds.Add(cond_node);
                catch_blocks.Add(ProcessNodeFrom(nodes, last + 2, out last));
            }
            if (last > 0 && (last + 1) < nodes.Count 
                && nodes[last + 1].Name == "finally")
            {
                finally_node = nodes[last + 1];
                finally_block = ProcessNodeFrom(nodes, last + 2, out last);
            }
            newNode = nodes[fromIndex];

            if (try_body != null)
            {
                node1 = newNode.AddNode("body", "body");
                node1.AddNode(try_body);
            }
            while (catch_nodes.Count > 0)
            {
                ParserNode catch_node = catch_nodes[0];
                ParserNode catch_block = catch_blocks[0];
                ParserNode catch_cond = catch_conds[0];

                catch_node.Type = "catch";
                catch_node.AddNode("body", "body").AddNode(catch_block);
                if (catch_cond != null)
                {
                    catch_node.AddNode("var", "var").AddNode(catch_cond);
                }
                newNode.AddNode(catch_node);

                catch_nodes.RemoveAt(0);
                catch_blocks.RemoveAt(0);
                catch_conds.RemoveAt(0);
            }
            if (finally_node != null)
            {
                finally_node.Type = "finally";
                newNode.AddNode(finally_node);
                finally_node.AddNode("body", "body").AddNode(finally_block);
            }
            lastIndex = last;
            return newNode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="fromIndex"></param>
        /// <param name="lastIndex"></param>
        /// <returns></returns>
        public virtual ParserNode ProcessForeach(List<ParserNode> nodes, int fromIndex, out int lastIndex)
        {
            if ((fromIndex + 1) >= nodes.Count || nodes[fromIndex + 1].Name != "(")
                throw new Exception("expected ( after foreach");
            int end_brack = 0;
            ParserNode body_cond = ProcessExpression(nodes, fromIndex + 2, "in", out end_brack);
            if (end_brack < 0)
                throw new Exception("not found 'in' for foreach");
            ParserNode enum_cond = ProcessExpression(nodes, end_brack + 1, ")", out end_brack);
            if (end_brack < 0)
                throw new Exception("not found ')' for foreach");
            ParserNode body_node = ProcessNodeFrom(nodes, end_brack + 1, out lastIndex);
            ParserNode newNode = nodes[fromIndex];
            newNode.AddNode(body_cond);
            body_cond.Type = "scalar-expr";
            newNode.AddNode(enum_cond);
            enum_cond.Type = "list-expr";
            ParserNode node1 = newNode.AddNode("body", "body");
            node1.AddNode(body_node);
            newNode.Type = newNode.Name;
            return newNode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="fromIndex"></param>
        /// <param name="lastIndex"></param>
        /// <returns></returns>
        public virtual ParserNode ProcessFor(List<ParserNode> nodes, int fromIndex, out int lastIndex)
        {
            if ((fromIndex + 1) >= nodes.Count || nodes[fromIndex + 1].Name != "(")
                throw new Exception("expected ( after 'for'");
            int end_brack = 0;
            ParserNode init_cond = ProcessExpression(nodes, fromIndex + 2, ";", out end_brack);
            if (end_brack < 0)
                throw new Exception("not found first ';' for 'for'");
            ParserNode cont_cond = ProcessExpression(nodes, end_brack + 1, ";", out end_brack);
            if (end_brack < 0)
                throw new Exception("not found second ';' for 'for'");
            ParserNode iter_cond = ProcessExpression(nodes, end_brack + 1, ")", out end_brack);
            if (end_brack < 0)
                throw new Exception("not found ')' for 'for'");
            ParserNode body_node = ProcessNodeFrom(nodes, end_brack + 1, out lastIndex);
            ParserNode newNode = nodes[fromIndex];
            newNode.AddNode(init_cond);
            init_cond.Type = "init-expr";
            newNode.AddNode(cont_cond);
            cont_cond.Type = "cont-expr";
            newNode.AddNode(iter_cond);
            iter_cond.Type = "iter-expr";
            ParserNode node1 = newNode.AddNode("body", "body");
            node1.AddNode(body_node);
            newNode.Type = newNode.Name;
            return newNode;
        }

        /// <summary>
        /// processes statements like USING or RETURN
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="fromIndex"></param>
        /// <param name="lastIndex"></param>
        /// <returns></returns>
        public virtual ParserNode ProcessReturnBlock(List<ParserNode> nodes, int fromIndex, out int lastIndex)
        {
            ParserNode node1 = ProcessExpression(nodes, fromIndex + 1, ";", out lastIndex);
            ParserNode newNode = nodes[fromIndex];
            newNode.AddNode(node1);
            newNode.Type = newNode.Name;
            return newNode;
        }

        /// <summary>
        /// processing statement like:
        /// while ( ... ) { ... }
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="fromIndex"></param>
        /// <param name="lastIndex"></param>
        /// <returns></returns>
        public virtual ParserNode ProcessWhileBlock(List<ParserNode> nodes, int fromIndex, out int lastIndex)
        {
            if ((fromIndex + 1) >= nodes.Count || nodes[fromIndex + 1].Name != "(")
                throw new Exception("expected ( after while or switch");
            int end_brack = 0;
            ParserNode body_cond = ProcessExpression(nodes, fromIndex + 2, ")", out end_brack);
            if (end_brack < 0)
                throw new Exception("not found ) for " + nodes[fromIndex].Name);
            ParserNode body_node = ProcessNodeFrom(nodes, end_brack + 1, out lastIndex);
            ParserNode newNode = nodes[fromIndex];
            newNode.AddNode(body_cond);
            ParserNode node1 = newNode.AddNode("body", "body");
            node1.AddNode(body_node);
            newNode.Type = newNode.Name;
            return newNode;
        }

        /// <summary>
        /// processing GOTO or CASE statement
        /// for case we do not change type of subnode, because
        /// it will be already set from parser as string, char or number
        /// for goto we change type of subnode to 'label'
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="fromIndex"></param>
        /// <param name="lastIndex"></param>
        /// <returns></returns>
        public virtual ParserNode ProcessGotoCase(List<ParserNode> nodes, int fromIndex, out int lastIndex)
        {
            ParserNode newNode;

            newNode = nodes[fromIndex];
            newNode.Type = newNode.Name;
            newNode.AddNode(nodes[fromIndex + 1]);
            lastIndex = fromIndex + 2;
            if (newNode.Type == "goto")
                nodes[fromIndex + 1].Type = "label";
            return newNode;
        }

        /// <summary>
        /// Processing IF - THEN - ELSE statement
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="fromIndex"></param>
        /// <param name="lastIndex"></param>
        /// <returns></returns>
        public virtual ParserNode ProcessIfStatement(List<ParserNode> nodes, int fromIndex, out int lastIndex)
        {
            ParserNode newNode, node1;
            int last = 0;
            if (nodes[fromIndex + 1].Name != "(")
                throw new Exception("not found ( after if");
            int cond_end = FindNextIndexAtLevelZero(nodes, fromIndex + 2, ")");
            ParserNode if_cond = ProcessExpression(nodes, fromIndex + 2, ")", out cond_end);
            ParserNode if_body_node = ProcessNodeFrom(nodes, cond_end + 1, out last);
            ParserNode if_else_node = null;
            if (last > 0 && (last + 1) < nodes.Count)
            {
                if (nodes[last + 1].Name == "else")
                {
                    if_else_node = ProcessNodeFrom(nodes, last + 2, out last);
                }
            }
            newNode = nodes[fromIndex];
            if (if_cond != null)
            {
                newNode.AddNode(if_cond);
                if_cond.Type = "if-cond";
            }
            if (if_body_node != null)
            {
                node1 = newNode.AddNode("", "if-true");
                node1.AddNode(if_body_node);
            }
            if (if_else_node != null)
            {
                node1 = newNode.AddNode("", "if-false");
                node1.AddNode(if_else_node);
            }
            lastIndex = last;
            return newNode;
        }

        /// <summary>
        /// processing statement begining with unrecognized word
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="fromIndex"></param>
        /// <param name="lastIndex"></param>
        /// <returns></returns>
        public virtual ParserNode ProcessUnknownStatement(List<ParserNode> nodes, int fromIndex, out int lastIndex)
        {
            ParserNode newNode;
            int start_semicolon = FindNextIndexAtLevelZero(nodes, fromIndex, ";");
            int start_curly = FindNextIndexAtLevelZero(nodes, fromIndex, "{");
            int start_eq = FindNextIndexAtLevelZero(nodes, fromIndex, "=");
            if (start_eq < 0)
                start_eq = Math.Max(start_semicolon, start_curly);

            if ((start_curly < start_semicolon || start_semicolon < 0) && start_curly >= 0 && start_eq >= start_curly)
            {
                newNode = ProcessStatementFollowedWithBlock(nodes, fromIndex, start_curly, out lastIndex);
            }
            else if (start_semicolon >= 0)
            {
                newNode = ProcessStatementFollowedWithSemicolon(nodes, fromIndex, start_semicolon, out lastIndex);
            }
            else
            {
                newNode = nodes[0];
                lastIndex = fromIndex;
            }

            return newNode;
        }

        /// <summary>
        /// After statement is block, so it can be class, 
        /// interface, enum, .... or property or method
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="fromIndex"></param>
        /// <param name="start_curly"></param>
        /// <param name="lastIndex"></param>
        /// <returns></returns>
        public virtual ParserNode ProcessStatementFollowedWithBlock(List<ParserNode> nodes, int fromIndex, int start_curly, out int lastIndex)
        {
            ParserNode node1, newNode;
            int start = start_curly;
            int end = FindNextIndexAtLevelZero(nodes, start + 1, "}");
            int start_brack = FindNextIndexAtLevelZero(nodes, fromIndex, "(");
            int start_double = FindNextIndexAtLevelZero(nodes, fromIndex, ":");
            List<ParserNode> subrange = nodes.GetRange(start + 1, end - start - 1);
            if (start_curly > fromIndex + 1)
            {
                int nameIndex = start_curly - 1;
                if (start_brack > fromIndex && start_brack < start_curly)
                    nameIndex = start_brack - 1;
                else if (start_double > fromIndex && start_double < start_curly)
                    nameIndex = start_double - 1;
                // name
                newNode = nodes[nameIndex];
                // type
                node1 = nodes[nameIndex - 1];
                newNode.Type = node1.Name;
                for (int k = fromIndex; k < nameIndex - 1; k++)
                {
                    newNode.AddNode(nodes[k]);
                    nodes[k].Type = "attr";
                    nodes[k].IsAttribute = true;
                }
                if (newNode.Type == "class")
                {
                    for(int i = start_double; i >= 0 && i < start_curly; i++)
                    {
                        if (nodes[i].Name != ",")
                        {
                            newNode.AddNode(nodes[i]);
                            nodes[i].IsAttribute = true;
                            nodes[i].Type = "base-class";
                        }
                    }
                    node1 = ProcessClassBody(subrange);
                    newNode.Nodes.AddRange(node1.Nodes);
                }
                else if (newNode.Type == "interface")
                {
                    node1 = ProcessClassBody(subrange);
                    newNode.Nodes.AddRange(node1.Nodes);
                }
                else if (newNode.Type == "enum")
                {
                    if (start_double > fromIndex && start_double < start_curly)
                        newNode.AddNode(nodes[start_double + 1]).Type = "base-type";
                    node1 = ProcessEnumBody(subrange);
                    newNode.Nodes.AddRange(node1.Nodes);
                }
                else if (newNode.Type == "struct")
                {
                    node1 = ProcessClassBody(subrange);
                    newNode.Nodes.AddRange(node1.Nodes);
                }
                else if (newNode.Type == "namespace")
                {
                    node1 = ProcessClassBody(subrange);
                    newNode.Nodes.AddRange(node1.Nodes);
                }
                else
                {
                    if (start_brack > fromIndex && start_brack < start_curly)
                    {
                        node1 = nodes[start_brack - 2];
                        node1.Type = "attr-return-type";
                        node1.IsAttribute = true;
                        newNode.AddNode(node1);
                        newNode.Type = "method";
                        int end_brack = FindNextIndexAtLevelZero(nodes, start_brack + 1, ")");
                        if (end_brack != start_curly - 1)
                            throw new Exception("Problem with parsing: ) is not before {");
                        node1 = ProcessFuncArguments(nodes.GetRange(start_brack + 1, end_brack - start_brack - 1));
                        if (node1 != null)
                        {
                            node1.Name = "arguments";
                            node1.Type = "attr-args";
                            node1.IsAttribute = true;
                            newNode.AddNode(node1);
                        }
                    }
                    else
                    {
                        ParserNode pn = newNode.AddNode(newNode.Name, newNode.Type);
                        pn.Nodes.AddRange(newNode.Nodes);
                        newNode.Nodes.Clear();
                        newNode.Name = "property";
                    }
                    node1 = ProcessClassBody(subrange);
                    //node1.Type = "body";
                    newNode.Nodes.AddRange(node1.Nodes);
                }
            }
            else if (start_curly == fromIndex + 1)
            {
                node1 = ProcessClassBody(subrange);
                node1.Type = "named-block";
                node1.Name = nodes[fromIndex].Name;
                newNode = node1;
            }
            else
            {
                newNode = ProcessClassBody(subrange);
                newNode.Type = "block";
            }
            lastIndex = end;

            return newNode;
        }

        public virtual ParserNode ProcessStatementFollowedWithSemicolon(List<ParserNode> nodes, int fromIndex, int start_semicolon, out int lastIndex)
        {
            int start_brack = FindNextIndexAtLevelZero(nodes, fromIndex, "(");
            ParserNode newNode = new ParserNode();
            if (start_brack < start_semicolon && RangeContainsName(nodes, fromIndex, start_brack, "delegate") >= 0)
            {
                newNode.Name = nodes[start_brack - 1].Name;
                newNode.Type = "delegate";
                newNode.AddNode(nodes[start_brack - 2].Name, "return-type").IsAttribute = true;
                for (int i = fromIndex; i < (start_brack - 2); i++)
                {
                    if (nodes[i].Name != "delegate")
                        newNode.AddNode(nodes[i].Name, "attr").IsAttribute = true;
                }
                ParserNode node1 = ProcessFuncArguments(nodes.GetRange(start_brack + 1, start_semicolon - start_brack - 2));
                if (node1 != null)
                {
                    node1.Name = "arguments";
                    node1.Type = "attr-args";
                    node1.IsAttribute = true;
                    newNode.AddNode(node1);
                }
                lastIndex = start_semicolon;
            }
            else
            {
                newNode = ProcessExpression(nodes, fromIndex, ";", out lastIndex);
                //newNode.Name = "expr";
                //newNode.Type = "expr";
                //newNode.Nodes = nodes.GetRange(fromIndex, start_semicolon - fromIndex);
                //lastIndex = start_semicolon;
            }
            return newNode;
        }

        /// <summary>
        /// this is main processing function
        /// It processes first node in the queue
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        public virtual ParserNode ProcessNodeHead(List<ParserNode> nodes)
        {
            int end;
            ParserNode newNode = ProcessNodeFrom(nodes, 0, out end);
            nodes.RemoveRange(0, end + 1);
            return newNode;
        }

        /// <summary>
        /// Processing arguments for function
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        public virtual ParserNode ProcessFuncArguments(List<ParserNode> nodes)
        {
            ParserNode pn = new ParserNode();
            ParserNode node = new ParserNode();
            pn.AddNode(node);
            int level = 0;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].Name == "<")
                {
                    level++;
                    node.AddNode(nodes[i]);
                }
                else if (nodes[i].Name == ">")
                {
                    level--;
                    node.AddNode(nodes[i]);
                }
                else if (nodes[i].Name == "," && level == 0)
                {
                    node = new ParserNode();
                    pn.AddNode(node);
                }
                else if (nodes.Count == i + 1 || (nodes[i + 1].Name == "," && level == 0))
                {
                    node.Name = nodes[i].Name;
                }
                else
                {
                    node.AddNode(nodes[i]);
                }
            }

            return pn;
        }

        /// <summary>
        /// Processing of general expression
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="startIndex"></param>
        /// <param name="endToken"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public virtual ParserNode ProcessExpression(List<ParserNode> nodes, int startIndex, string endToken, out int end)
        {
            end = FindNextIndexAtLevelZero(nodes, startIndex, endToken);
            ParserNode pn = new ParserNode();
            pn.Name = "expr";
            pn.Type = "expr";
            List<ParserNode> subrange = nodes.GetRange(startIndex, end - startIndex);
            subrange = ProcessExpressionRange(subrange);
            pn.Nodes.AddRange(subrange);
            return pn;
        }

        /// <summary>
        /// processing body of definitions
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        public virtual ParserNode ProcessClassBody(List<ParserNode> nodes)
        {
            ParserNode returnNode = new ParserNode();
            ParserNode value = null;
            while (nodes.Count > 0)
            {
                value = ProcessNodeHead(nodes);
                if (value != null)
                    returnNode.AddNode(value);
            }

            return returnNode;
        }

        /// <summary>
        /// processing of enum body
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        public virtual ParserNode ProcessEnumBody(List<ParserNode> nodes)
        {
            ParserNode pn = new ParserNode();
            ParserNode node = new ParserNode();
            pn.AddNode(node);
            int mode = 0;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (mode == 0)
                {
                    // waiting name
                    if (nodes[i].Name == ",")
                    {
                        node = new ParserNode();
                        pn.AddNode(node);
                    }
                    else if (nodes[i].Name == "=")
                    {
                        mode = 1;
                    }
                    else
                    {
                        node.Name = nodes[i].Name;
                        node.Type = "enum-item";
                    }
                }
                else if (mode == 1)
                {
                    node.AddNode(nodes[i].Name, "enum-value");
                    mode = 0;
                }
            }

            return pn;
        }

        public virtual List<ParserNode> ProcessExpressionRange(List<ParserNode> subrange)
        {
            List<ParserNode> nodes = new List<ParserNode>();
            ProcessExpressionReplaceSubexpressions(subrange, nodes);

            // dealing with brackets
            for (int i = 1; i < nodes.Count - 1; i++)
            {
                if (nodes[i].Name == ".")
                {
                    ParserNode pn = nodes[i];
                    pn.AddNode(nodes[i - 1]);
                    pn.AddNode(nodes[i + 1]);
                    nodes.RemoveAt(i + 1);
                    nodes.RemoveAt(i - 1);
                    pn.Type = "NODE.DOT";
                    pn.Name = "";
                }
            }

            for (int i = 1; i < nodes.Count; i++)
            {
                if ((nodes[i].Name == "++" || nodes[i].Name == "--") && nodes[i - 1].Type.StartsWith("NODE"))
                {
                    nodes[i].Type = "NODE." + nodes[i].Name + "OPER";
                    nodes[i].AddNode(nodes[i - 1]);
                    i--;
                    nodes.RemoveAt(i);
                }
            }


            SubmergeToPrevious(nodes, "NODE.SUBEXPR", "func-args");
            SubmergeToNext(nodes, "NODE.SUBEXPR", "retype");
            SubmergeToPrevious(nodes, "NODE.INDEX", null);

            return nodes;
        }

        public virtual void ProcessExpressionReplaceSubexpressions(List<ParserNode> input, List<ParserNode> output)
        {
            // replacing bracket sequence with node
            for (int i = 0; i < input.Count; i++)
            {
                if (input[i].Name == "(")
                {
                    int j = 0;
                    j = FindNextIndexAtLevelZero(input, i + 1, ")");
                    if (j < 0)
                        throw new Exception("Expected ) in expression");
                    input[i].Nodes.AddRange(ProcessExpressionRange(input.GetRange(i + 1, j - i - 1)));
                    input[i].Type = "NODE.SUBEXPR";
                    output.Add(input[i]);
                    i = j;
                }
                else if (input[i].Name == "[")
                {
                    int j = 0;
                    j = FindNextIndexAtLevelZero(input, i + 1, "]");
                    if (j < 0)
                        throw new Exception("Expected ] in expression");
                    input[i].Nodes.AddRange(ProcessExpressionRange(input.GetRange(i + 1, j - i - 1)));
                    input[i].Type = "NODE.INDEX";
                    output.Add(input[i]);
                    i = j;
                }
                else
                {
                    output.Add(input[i]);
                }
            }
        }

        public virtual void SubmergeToPrevious(List<ParserNode> nodes, string strType, string strNewType)
        {
            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                if (nodes[i].Type == strType)
                {
                    if (i > 0 
                        && nodes[i - 1].Type.StartsWith("NODE"))
                    {
                        if (strNewType != null)
                            nodes[i].Type = strNewType;
                        nodes[i - 1].AddNode(nodes[i]);
                        nodes.RemoveAt(i);
                    }
                }
            }
        }

        public virtual void SubmergeToNext(List<ParserNode> nodes, string strType, string strNewType)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].Type == strType)
                {
                    if ((i < nodes.Count - 1) 
                        && nodes[i + 1].Type.StartsWith("NODE"))
                    {
                        if (strNewType != null)
                            nodes[i].Type = strNewType;
                        nodes[i].AddNode(nodes[i + 1]);
                        nodes.RemoveAt(i + 1);
                    }
                }
            }
        }
    }
}
