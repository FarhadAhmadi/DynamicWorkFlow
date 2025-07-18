using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using WorkFlow.DTO;
using WorkFlow.Persistence;
using WorkFlow.RuleInterpreter.StepHandlers.AssignStep;
using WorkFlow.RuleInterpreter.StepHandlers.CalculateDurationStep;
using WorkFlow.RuleInterpreter.StepHandlers.FetchEntityListStep;
using WorkFlow.RuleInterpreter.StepHandlers.FetchSingleEntityStep;
using WorkFlow.RuleInterpreter.StepHandlers.IfStep;
using WorkFlow.RuleInterpreter.StepHandlers.LogStep;

namespace WorkFlow.RuleInterpreter
{
    public class RuleInterpreter
    {
        private readonly DatabaseContext _dbContext;
        private readonly RuleExecutionContext _ruleExecutionContext;
        private Dictionary<string, dynamic> _stepMap = new();

        public RuleInterpreter(DatabaseContext dbContext, RuleExecutionContext ruleExecutionContext)
        {
            _dbContext = dbContext;
            _ruleExecutionContext = ruleExecutionContext;
            _ruleExecutionContext.Set("Status", false);
            _ruleExecutionContext.Set("Reason", string.Empty);
            _ruleExecutionContext.Set("__BreakSignal", false);
            _ruleExecutionContext.Set("__ContinueSignal", false);
        }

        #region Public API

        public void SetVariable(string name, object value)
        {
            _ruleExecutionContext.Set(name, value);
        }

        public async Task<RuleExecutionResult> ExecuteAsync(dynamic rule)
        {
            Logger.Log($"Rule Info: '{rule.name}'", LogSource.Rule, LogLevel.Info);

            var steps = rule.steps as JArray;
            if (steps == null || !steps.Any())
                throw new Exception("Invalid rule format: steps missing.");

            // Index all steps by ID
            _stepMap = ((IEnumerable<dynamic>)rule.steps)
                .Where(s => s.id != null)
                .ToDictionary(s => (string)s.id, s => s);

            // Start execution at the first step
            if (_stepMap.TryGetValue("start", out var startStep))
            {
                await ExecuteStepByIdAsync("start");
            }
            else
            {
                throw new Exception("No start step defined in rule.");
            }

            return new RuleExecutionResult
            {
                Status = _ruleExecutionContext.Get<bool>("Status"),
                Reason = _ruleExecutionContext.Get<string>("Reason")
            };
        }

        #endregion

        #region Step Dispatcher

        private async Task ExecuteStepByIdAsync(string stepId)
        {
            while (stepId != null)
            {
                if (!_stepMap.TryGetValue(stepId, out var step))
                    throw new Exception($"Step with ID '{stepId}' not found.");

                string nextStepId = step.next != null ? (string)step.next : null;
                string action = step.action.ToString();

                try
                {
                    switch (action)
                    {
                        case "fetch":
                            await new FetchSingleEntityStep(_dbContext, _ruleExecutionContext).ExecuteAsync(step);
                            break;

                        case "fetchList":
                            await new FetchEntityListStep(_dbContext, _ruleExecutionContext).ExecuteAsync(step);
                            break;

                        case "foreach":
                            await ExecuteForEachAsync(step);
                            break;

                        case "if":
                            nextStepId = await ExecuteIfAsync(step);
                            break;

                        case "assign":
                            await new AssignStep(_ruleExecutionContext).ExecuteAsync(step);
                            break;

                        case "calculateDuration":
                            await new CalculatedurationStep(_ruleExecutionContext).ExecuteAsync(step);
                            break;

                        case "break":
                            _ruleExecutionContext.Set("__BreakSignal", true);
                            return;

                        case "continue":
                            _ruleExecutionContext.Set("__ContinueSignal", true);
                            return;

                        case "stop":
                            var status = step.status ?? false;
                            var reason = step.reason?.ToString() ?? "Stopped by rule.";
                            _ruleExecutionContext.Set("Status", status);
                            _ruleExecutionContext.Set("Reason", reason);
                            return;

                        case "log":
                            await new LogStep(_ruleExecutionContext).ExecuteAsync(step);
                            break;

                        default:
                            Logger.Log($"Unknown action: {action}", LogSource.Rule, LogLevel.Error);
                            throw new InvalidOperationException($"Unknown action: {action}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Exception in action '{action}': {ex.Message}", LogSource.Rule, LogLevel.Error);
                    throw;
                }

                stepId = nextStepId;
            }
        }

        #endregion

        #region foreach Handler

        private async Task ExecuteForEachAsync(dynamic step)
        {
            string source = step.source;
            string varName = step.@var;
            var body = step.body;

            if (!_ruleExecutionContext.Variables.TryGetValue(source, out var listObj) || listObj is not IEnumerable<object> list)
                return;

            foreach (var item in list)
            {
                _ruleExecutionContext.Set(varName, item);

                foreach (var stepId in body)
                {
                    await ExecuteStepByIdAsync((string)stepId);
                    if (_ruleExecutionContext.Get<bool>("__BreakSignal"))
                    {
                        _ruleExecutionContext.Set("__BreakSignal", false);
                        return;
                    }
                    if (_ruleExecutionContext.Get<bool>("__ContinueSignal"))
                    {
                        _ruleExecutionContext.Set("__ContinueSignal", false);
                        break;
                    }
                }
            }
        }

        #endregion

        #region if Handler

        private async Task<string?> ExecuteIfAsync(dynamic step)
        {
            var ifStep = new IfStep(_dbContext, _ruleExecutionContext);
            bool conditionResult = await ifStep.ExecuteAsync(step);

            var nextSteps = conditionResult ? step.then : step.@else;
            if (nextSteps == null) return null;

            foreach (var stepId in nextSteps)
            {
                await ExecuteStepByIdAsync((string)stepId);
            }

            return step.next != null ? (string)step.next : null;
        }

        #endregion
    }
}
