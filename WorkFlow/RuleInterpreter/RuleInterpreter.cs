using Microsoft.EntityFrameworkCore;
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
        private readonly Dictionary<string, object> _variables = new();

        public RuleInterpreter(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
            _variables["Status"] = false;
            _variables["Reason"] = string.Empty;
        }

        #region Public API

        public void SetVariable(string name, object value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                Logger.Log($"Variable name cannot be null or whitespace.", LogSource.Rule, LogLevel.Error);
                throw new ArgumentException("Variable name cannot be null or whitespace.", nameof(name));
            }
                

            _variables[name] = value;
        }

        public bool TryGetValue(string name, out object value)
        {
            return _variables.TryGetValue(name, out value);
        }

        public async Task<RuleExecutionResult> ExecuteAsync(dynamic rule)
        {
            Logger.Log($"Rule Info: '{rule.name}'", LogSource.Rule, LogLevel.Info);

            foreach (var step in rule.steps)
            {
                await ExecuteStepAsync(step);
            }

            return new RuleExecutionResult
            {
                Status = Convert.ToBoolean(_variables["Status"]),
                Reason = _variables["Reason"]?.ToString()
            };
        }

        #endregion

        #region Core Step Execution

        private async Task ExecuteStepAsync(dynamic step)
        {
            string action = step.action.ToString();

            try
            {
                switch (action)
                {
                    case "fetch":
                        await new FetchSingleEntityStep(_dbContext, _variables).ExecuteAsync(step);
                        break;

                    case "fetchList":
                        await new FetchEntityListStep(_dbContext, _variables).ExecuteAsync(step);
                        break;

                    case "foreach":
                        await ExecuteForEachAsync(step);
                        break;

                    case "if":
                        await ExecuteIfAsync(step);
                        break;

                    case "assign":
                        await new AssignStep(_variables).ExecuteAsync(step);
                        break;

                    case "calculateDuration":
                        await new CalculatedurationStep(_variables).ExecuteAsync(step);
                        break;

                    case "break":
                        _variables["__BreakSignal"] = true;
                        break;

                    case "continue":
                        _variables["__ContinueSignal"] = true;
                        break;

                    case "stop":
                        var status = step.status ?? false;
                        var reason = step.reason?.ToString() ?? "Stopped by rule.";
                        _variables["Status"] = status;
                        _variables["Reason"] = reason;
                        break;

                    case "log":
                        await new LogStep(_variables).ExecuteAsync(step);
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
        }

        #endregion

        #region foreach Handler

        private async Task ExecuteForEachAsync(dynamic step)
        {
            string source = step.source;
            string varName = step.@var;
            var body = step.body;

            if (!_variables.TryGetValue(source, out var listObj) || listObj is not IEnumerable<object> list)
                return;

            foreach (var item in list)
            {
                _variables[varName] = item;

                foreach (var innerStep in body)
                {
                    await ExecuteStepAsync(innerStep);
                }

            }
        }

        #endregion

        #region if Handler

        private async Task ExecuteIfAsync(dynamic step)
        {
            var ifStep = new IfStep(_dbContext, _variables);
            bool conditionResult = await ifStep.ExecuteAsync(step);

            var nextSteps = conditionResult ? step.then : step.@else ?? step.then;
            if (nextSteps == null) return;

            foreach (var nestedStep in nextSteps)
            {
                await ExecuteStepAsync(nestedStep);
            }
        }

        #endregion
    }
}
