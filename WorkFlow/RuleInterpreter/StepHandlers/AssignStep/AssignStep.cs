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
        private readonly RuleExecutionContext _ruleExecutionContext;

        public AssignStep(RuleExecutionContext ruleExecutionContext)
        {
            _ruleExecutionContext = ruleExecutionContext;
        }

        public Task ExecuteAsync(dynamic step)
        {
            string variableName = step.variable;
            object rawValue = step.value;

            object value;
            if (rawValue is string strValue && strValue.Contains("."))
            {
                value = VariableResolver.ResolvePath(_ruleExecutionContext, strValue);
            }
            else
            {
                value = rawValue;
            }

            _ruleExecutionContext.Set(variableName, value);
            return Task.CompletedTask;
        }
    }
}
