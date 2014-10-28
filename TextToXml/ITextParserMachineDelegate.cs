using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextToXml
{
    public interface ITextParserMachineDelegate
    {
        /// <summary>
        /// Transition for current state and current character does not exist.
        /// So this is callback from TextParserMachine to external object,
        /// so new transition can be provided by external object.
        /// </summary>
        /// <param name="parser"></param>
        /// <param name="ctx"></param>
        /// <param name="rc"></param>
        /// <returns></returns>
        Transition DelegateProvideNewTransition(ParserMachine parser, DataContext ctx, char rc);
        Transition DelegateProvideNewTokenTransition(ParserMachine parser, DataContext ctx, string rc);

        /// <summary>
        /// This method tries to get update for actions, because transition exists
        /// but destination state is not set
        /// and current actions does not change status
        /// so we need update for actions
        /// </summary>
        /// <param name="parser"></param>
        /// <param name="ctx"></param>
        /// <param name="trans"></param>
        /// <returns></returns>
        string DelegateProvideUpdatedActions(ParserMachine parser, DataContext ctx, Transition trans);
        string DelegateProvideUpdatedTokenActions(ParserMachine parser, DataContext ctx, Transition trans);
    }
}
