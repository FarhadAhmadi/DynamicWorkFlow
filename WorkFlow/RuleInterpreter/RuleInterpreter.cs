using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using WorkFlow.DTO;
using WorkFlow.Exceptions;
using WorkFlow.Persistence;
using WorkFlow.RuleInterpreter.StepHandlers.AssignStep;
using WorkFlow.RuleInterpreter.StepHandlers.CalculateDurationStep;
using WorkFlow.RuleInterpreter.StepHandlers.FetchEntityListStep;
using WorkFlow.RuleInterpreter.StepHandlers.FetchSingleEntityStep;
using WorkFlow.RuleInterpreter.StepHandlers.IfStep;

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
                throw new ArgumentException("Variable name cannot be null or whitespace.", nameof(name));

            _variables[name] = value;
        }

        public bool TryGetValue(string name, out object value)
        {
            return _variables.TryGetValue(name, out value);
        }

        public async Task<RuleExecutionResult> ExecuteAsync(dynamic rule)
        {
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

            // Log step entry
            AddLog($"[ExecuteStep] Starting action: '{action}'");

            try
            {
                switch (action)
                {
                    case "fetch":
                        AddLog("[fetch] Executing fetch single entity.");
                        await new FetchSingleEntityStep(_dbContext, _variables).ExecuteAsync(step);
                        break;

                    case "fetchList":
                        AddLog("[fetchList] Executing fetch list of entities.");
                        await new FetchEntityListStep(_dbContext, _variables).ExecuteAsync(step);
                        break;

                    case "foreach":
                        AddLog("[foreach] Starting foreach loop.");
                        await ExecuteForEachAsync(step);
                        break;

                    case "if":
                        AddLog("[if] Evaluating condition.");
                        await ExecuteIfAsync(step);
                        break;

                    case "assign":
                        AddLog($"[assign] Assigning variable: {step.variable}");
                        await new AssignStep(_variables).ExecuteAsync(step);
                        break;

                    case "calculateDuration":
                        AddLog("[calculateDuration] Calculating duration.");
                        await new CalculatedurationStep(_variables).ExecuteAsync(step);
                        break;

                    case "break":
                        AddLog("[break] Breaking loop.");
                        _variables["__BreakSignal"] = true;
                        break;

                    case "continue":
                        AddLog("[continue] Continuing loop.");
                        _variables["__ContinueSignal"] = true;
                        break;

                    case "stop":
                        var status = step.status ?? false;
                        var reason = step.reason?.ToString() ?? "Stopped by rule.";
                        _variables["Status"] = status;
                        _variables["Reason"] = reason;
                        AddLog($"[stop] Execution stopped. Status: {status}, Reason: {reason}");
                        break;

                    case "log":
                        string message = step.message?.ToString() ?? "(empty log)";
                        AddLog($"[log] {message}");
                        break;

                    default:
                        throw new InvalidOperationException($"Unknown action: {action}");
                }
            }
            catch (Exception ex)
            {
                AddLog($"[error] Exception in action '{action}': {ex.Message}");
                throw;
            }

            AddLog($"[ExecuteStep] Completed action: '{action}'");
        }

        private void AddLog(string message)
        {
            if (!_variables.ContainsKey("Logs"))
            {
                _variables["Logs"] = new List<string>();
            }

            var logs = _variables["Logs"] as List<string>;
            logs.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
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

                try
                {
                    foreach (var innerStep in body)
                    {
                        await ExecuteStepAsync(innerStep);
                    }
                }
                catch (ContinueException)
                {
                    continue;
                }
                catch (BreakException)
                {
                    break;
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
