using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextToXml
{
    public class ScriptLineStack
    {
        List<ScriptLine> stack = new List<ScriptLine>();

        public ScriptLineStack()
        {
        }

        public void Push(ScriptLine current)
        {
            stack.Add(current);
        }

        public void Pop()
        {
            if (stack.Count > 1)
                stack.RemoveAt(stack.Count - 1);
        }

        public ScriptLine Current
        {
            get
            {
                if (stack.Count > 0)
                    return stack[stack.Count - 1];
                return null;
            }
        }
    }
}
