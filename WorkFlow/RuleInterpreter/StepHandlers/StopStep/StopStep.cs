using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkFlow.Persistence;

namespace WorkFlow.RuleInterpreter.StepHandlers.StopStep
{

    public class stopStep
    {
        private readonly Dictionary<string, object> _variables;

        public stopStep(Dictionary<string, object> variables)
        {
            _variables = variables;
        }

        public async Task ExecuteAsync(dynamic step)
        {
            _variables["Status"] = step.status;
            _variables["Reason"] = step.reason?.ToString() ?? "Stopped by rule";
        }
    }
}
