using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextToXml
{
    public class TransitionCharacter
    {
        public bool IsSpecial = false;
        public char C = ' ';

        public TransitionCharacter(bool isSpecial, char rc)
        {
            IsSpecial = isSpecial;
            C = rc;
        }

        public bool RespondsToChar(char rc)
        {
            if (IsSpecial)
            {
                switch (C)
                {
                    case 'n':
                        return rc == '\n';
                    case 't':
                        return rc == '\t';
                    case 's':
                        return rc == ' ';
                    case 'S':
                        return Char.IsWhiteSpace(rc);
                    case 'N':
                        return rc != '\n';
                    case 'P':
                        return rc != '"' && rc != '\\';
                    case 'A':
                        return rc != '\'' && rc != '\\';
                    case 'w':
                        return Char.IsLetter(rc);
                    case 'd':
                        return Char.IsDigit(rc);
                    case '*':
                        return true;
                    default:
                        return rc == C;
                }
            }
            else
            {
                return rc == C;
            }
        }

        public override string ToString()
        {
            if (IsSpecial)
                return string.Format("\\{0}", C);
            else
                return string.Format("{0}", C);
        }
    }
}
