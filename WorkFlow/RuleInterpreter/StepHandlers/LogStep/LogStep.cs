using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WorkFlow.RuleInterpreter.Helpers;

namespace WorkFlow.RuleInterpreter.StepHandlers.LogStep
{
    public class LogStep
    {
        private readonly Dictionary<string, object> _variables;

        public LogStep(Dictionary<string, object> variables)
        {
            _variables = variables;
        }

        public Task ExecuteAsync(dynamic step)
        {
            if (step.message == null)
                throw new ArgumentException("Log step requires a 'message' property.");

            string rawMessage = step.message.ToString();
            string resolvedMessage = ResolveMessage(rawMessage);

            // Optional: allow log level from DSL like { "level": "debug" }
            LogLevel level = LogLevel.Info;
            if (step.level != null)
            {
                Enum.TryParse(step.level.ToString(), true, out level);
            }

            Logger.Log(resolvedMessage, LogSource.Rule, level);

            return Task.CompletedTask;
        }

        private string ResolveMessage(string message)
        {
            var regex = new Regex(@"@([a-zA-Z0-9_\.]+)");
            return regex.Replace(message, match =>
            {
                string path = match.Groups[1].Value;
                var value = VariableResolver.ResolvePath(_variables, path);
                return value?.ToString() ?? "null";
            });
        }
    }
}
