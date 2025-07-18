using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkFlow.RuleInterpreter.Helpers
{
    public static class VariableResolver
    {
        public static object ResolvePath(RuleExecutionContext ruleExecutionContext, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            var parts = path.Split('.');
            if (parts.Length == 0 || !ruleExecutionContext.TryGet<object>(parts[0], out var current))
                return null;

            for (int i = 1; i < parts.Length; i++)
            {
                if (current == null) return null;

                var propInfo = current.GetType().GetProperty(parts[i]);
                if (propInfo == null)
                    return null;

                current = propInfo.GetValue(current);
            }

            return current;
        }


        public static object EvaluateValue(RuleExecutionContext ruleExecutionContext, object input)
        {
            if (input == null)
                return null;

            string str = input.ToString();

            if (str.StartsWith("@"))
            {
                string path = str.Substring(1); // Remove '@'
                return ResolvePath(ruleExecutionContext, path);
            }

            return input;
        }
    }
}
