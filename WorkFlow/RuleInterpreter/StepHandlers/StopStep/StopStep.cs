using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkFlow.Persistence;

namespace WorkFlow.RuleInterpreter.StepHandlers.StopStep
{
    public class StopStepHandler
    {
        public async Task ExecuteAsync(dynamic step, RuleExecutionContext context)
        {
            context.Set("Status", step.status);
            context.Set("Reason", step.reason?.ToString() ?? "Stopped by rule");
        }
    }
}
