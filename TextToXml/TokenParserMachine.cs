using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace TextToXml
{
    public class TokenParserMachine: ParserMachine
    {
        public void ParseFile(DataContext ctx)
        {
            ctx.Parser = this;
            ctx.InputNodes.Clear();
            ctx.InputNodes.AddRange(ctx.Root.Nodes);
            ctx.Root.Nodes.Clear();
            ctx.CurrentNode = ctx.Root;
            for (ctx.CurrentNodeIndex = 0; ctx.CurrentNodeIndex < ctx.InputNodes.Count; ctx.CurrentNodeIndex++)
            {
                ParserNode currentNode = ctx.InputNodes[ctx.CurrentNodeIndex];
                ctx.SetScalarValue("$CURRTYPE", currentNode.Type);
                ctx.SetScalarValue("$CURRNAME", currentNode.Name);
                Debugger.Log(0, "", string.Format("NEW_QUEUE_ITEM {0} ({1})\n", currentNode.Name, currentNode.Type));
                Debugger.Log(0, "", "    CURENT_STATE = " + StateName[ctx.CurrentState] + "\n");
                ctx.CurrentInputNode = currentNode;
                // get transition for char rc
                Transition trans = GetTransition(ctx.CurrentState, currentNode);
                // if no trans, then find transition (others)
                if (trans == null)
                {
                    trans = GetAnyTransition(ctx.CurrentState);
                }
                // if no trans (ask user to create new transition
                if (trans == null)
                {
                    if (!StateName.ContainsKey(ctx.CurrentState))
                    {
                        string newName = GetUniqueStateName("new");
                        StateID[newName] = ctx.CurrentState;
                        StateName[ctx.CurrentState] = newName;
                    }
                    Transition transCandidate = null;
                    if (Delegate != null)
                        transCandidate = Delegate.DelegateProvideNewTokenTransition(this, ctx, currentNode.Name);
                    if (transCandidate != null)
                        trans = transCandidate;
                }
                // if no trans (interrrupt translating process)
                // else execute actions
                if (trans != null)
                {
                    bool breakWhile = false;
                    ctx.CurrentState = trans.StateB;
                    ExecuteScript(trans.actions, ctx, currentNode.Name);
                    while (ctx.CurrentState == -1)
                    {
                        string newActions = null;
                        if (Delegate != null)
                            newActions = Delegate.DelegateProvideUpdatedTokenActions(this, ctx, trans);
                        if (newActions != null)
                        {
                            trans.actions = newActions;
                            ExecuteScript(trans.actions, ctx, currentNode.Name);
                        }
                        else
                        {
                            breakWhile = true;
                            break;
                        }
                    }
                    ctx.ShouldReturnTotal = false;
                    if (breakWhile)
                        break;
                }
                else
                    break;
                Debugger.Log(0, "", "    NEW_STATE = " + StateName[ctx.CurrentState] + "\n");
            }
        }

    }
}
