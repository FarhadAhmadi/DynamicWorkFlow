using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkFlow.Persistence;
using WorkFlow.RuleInterpreter.Helpers;

namespace WorkFlow.RuleInterpreter.StepHandlers.AssignStep
{
    public class AssignStep
    {
        private readonly Dictionary<string, object> _variables;

        public AssignStep(Dictionary<string, object> variables)
        {
            _variables = variables;
        }

        public Task ExecuteAsync(dynamic step)
        {
            string variableName = step.variable;
            object rawValue = step.value;

            object value;
            if (rawValue is string strValue && strValue.Contains("."))
            {
                value = VariableResolver.ResolvePath(_variables, strValue);
            }
            else
            {
                value = rawValue;
            }

            _variables[variableName] = value;
            return Task.CompletedTask;
        }
    }
}
