using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace TextToXml
{
    public class TextParserMachine: ParserMachine
    {

        public bool ParseFileChar(DataContext ctx, char rc)
        {
            // get transition for char rc
            Transition trans = GetTransition(ctx.CurrentState, rc);
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
                    transCandidate = Delegate.DelegateProvideNewTransition(this, ctx, rc);
                if (transCandidate != null)
                    trans = transCandidate;
            }
            // if no trans (interrrupt translating process)
            // else execute actions
            if (trans != null)
            {
                bool breakWhile = false;
                ctx.CurrentState = trans.StateB;
                ExecuteScript(trans.actions, ctx, string.Format("{0}", rc));
                while (ctx.CurrentState == -1)
                {
                    string newActions = null;
                    if (Delegate != null)
                        newActions = Delegate.DelegateProvideUpdatedActions(this, ctx, trans);
                    if (newActions != null)
                    {
                        trans.actions = newActions;
                        ExecuteScript(trans.actions, ctx, string.Format("{0}", rc));
                    }
                    else
                    {
                        breakWhile = true;
                        break;
                    }
                }
                ctx.ShouldReturnTotal = false;
                if (breakWhile)
                    return true;
            }
            else
                return true;

            return false;
        }

        public void ParseFile(DataContext ctx, string fileName, string fileContent)
        {
            ctx.Input.Data = fileContent;
            ctx.Parser = this;
            TextParserCommand.clr(ctx, "$FILE");
            TextParserCommand.append(ctx, "$FILE", fileName);
            char rc = ' ';
            while (ctx.Input.GetChar(ref rc))
            {
                if (ParseFileChar(ctx, rc))
                    break;
            }

            ParseFileChar(ctx, ' ');
            ParseFileChar(ctx, '\n');
        }
    }
}
