using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextToXml
{
    public class Transition
    {
        public enum Operator
        {
            Eq,
            NotEq
        }

        public Transition(ParserMachine pm)
        {
            Parser = pm;
        }

        public ParserMachine Parser = null;
        public int StateA;
        public int StateB;
        public string actions;
        public bool charactersExcluded
        {
            get { return CompareOperator == Operator.NotEq; }
            set { CompareOperator = (value ? Operator.NotEq : Operator.Eq); }
        }
        protected bool p_anyChar = false;
        public string AttributeName = "NAME";
        public Operator CompareOperator = Operator.Eq;

        public int CompareOperatorIndex
        {
            get
            {
                switch (CompareOperator)
                {
                    case Operator.NotEq:
                        return 1;
                    default:
                        return 0;
                }
            }
            set
            {
                if (value == 1)
                    CompareOperator = Operator.NotEq;
                else
                    CompareOperator = Operator.Eq;
            }
        }

        public bool AnyChar
        {
            get { return p_anyChar; }
            set { p_anyChar = value; }
        }
        public string FromState
        {
            get
            {
                if (StateA == -1)
                    return string.Empty;
                return Parser.StateName[StateA];
            }
            set
            {
                if (Parser.StateID.ContainsKey(value))
                    StateA = Parser.StateID[value];
                else
                {
                    StateA = Parser.AddState(value);
                }
            }
        }

        public string ToState
        {
            get
            {
                if (StateB == -1)
                    return string.Empty;
                return Parser.StateName[StateB];
            }
            set
            {
                if (Parser.StateID.ContainsKey(value))
                    StateB = Parser.StateID[value];
                else
                {
                    StateB = Parser.AddState(value);
                }
            }
        }

        protected string p_strs;

        public virtual string characters
        {
            get { return p_strs; }
            set { p_strs = value; p_chars = null; }
        }

        private HashSet<TransitionCharacter> p_chars = null;

        public void GetCharSeparate(string value)
        {
            p_chars = new HashSet<TransitionCharacter>();
            bool isSpecial = false;
            p_chars.Clear();
            string value2 = "";
            if (value.StartsWith("NOT ") || value.StartsWith("not "))
            {
                value2 = value.Substring(4);
                charactersExcluded = true;
            }
            else
            {
                value2 = value;
                charactersExcluded = false;
            }
            foreach (char c in value2)
            {
                if (isSpecial)
                {
                    if (c == '*')
                        AnyChar = true;
                    p_chars.Add(new TransitionCharacter(isSpecial, c));
                    isSpecial = false;
                }
                else if (c == '\\')
                {
                    isSpecial = true;
                }
                else
                {
                    p_chars.Add(new TransitionCharacter(isSpecial, c));
                }

            }
        }



        public bool RespondToCharOld(char rc)
        {
            if (p_anyChar)
                return true;
            if (p_chars == null)
                GetCharSeparate(characters);
            if (charactersExcluded)
            {
                foreach (TransitionCharacter tc in p_chars)
                {
                    if (tc.RespondsToChar(rc))
                        return false;
                }
                return true;
            }
            else
            {
                foreach (TransitionCharacter tc in p_chars)
                {
                    if (tc.RespondsToChar(rc))
                        return true;
                }
                return false;
            }
        }

        public virtual bool RespondToNode(ParserNode pn)
        {
            string s = string.Empty;
            if (AttributeName == "NAME")
            {
                s = pn.Name;
            }
            else if (AttributeName == "TYPE")
            {
                s = pn.Type;
            }

            if (CompareOperator == Operator.Eq)
            {
                return s == p_strs;
            }
            else if (CompareOperator == Operator.NotEq)
            {
                return s != p_strs;
            }

            return true;
        }

        public bool RespondToObject(object orc)
        {
            if (orc is char)
            {
                return RespondToCharOld((char)orc);
            }
            else if (orc is ParserNode)
            {
                return RespondToNode((ParserNode)orc);
            }

            return false;
        }

    }
}
