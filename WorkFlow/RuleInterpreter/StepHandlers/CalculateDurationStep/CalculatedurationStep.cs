using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkFlow.Persistence;
using WorkFlow.RuleInterpreter.Helpers;

namespace WorkFlow.RuleInterpreter.StepHandlers.CalculateDurationStep
{
    public class CalculatedurationStep
    {
        private readonly Dictionary<string, object> _variables;

        public CalculatedurationStep(Dictionary<string, object> variables)
        {
            _variables = variables;
        }

        public Task ExecuteAsync(dynamic step)
        {
            string startDatePath = step.startDate;
            string endDatePath = step.endDate;
            string storeAs = step.storeAs;
            string unit = step.unit?.ToString()?.ToLower() ?? "days";

            var startDateObj = VariableResolver.ResolvePath(_variables, startDatePath);
            var endDateObj = VariableResolver.ResolvePath(_variables, endDatePath);

            if (startDateObj is not DateTime startDate || endDateObj is not DateTime endDate)
                throw new Exception("StartDate or EndDate is not a valid DateTime");

            int duration = unit switch
            {
                "days" => (endDate - startDate).Days,
                "months" => ((endDate.Year - startDate.Year) * 12 + endDate.Month - startDate.Month) - (endDate.Day < startDate.Day ? 1 : 0),
                "years" => endDate.Year - startDate.Year - (endDate.Month < startDate.Month || (endDate.Month == startDate.Month && endDate.Day < startDate.Day) ? 1 : 0),
                _ => throw new Exception($"Unsupported duration unit: {unit}")
            };

            _variables[storeAs] = duration;

            return Task.CompletedTask;
        }
    }
}
