using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using WorkFlow.Persistence;
using WorkFlow.RuleInterpreter.Helpers;

namespace WorkFlow.RuleInterpreter.StepHandlers.IfStep
{
    public class IfStep
    {
        private readonly DatabaseContext _dbContext;
        private readonly RuleExecutionContext _ruleExecutionContext;
        public IfStep(DatabaseContext dbContext, RuleExecutionContext ruleExecutionContext)
        {
            _dbContext = dbContext;
            _ruleExecutionContext = ruleExecutionContext;
        }

        public async Task<bool> ExecuteAsync(dynamic step)
        {
            var condition = step.condition;
            string fieldPath = condition.field;
            string op = condition.@operator;
            object expectedValue = condition.value;

            object actualValue = VariableResolver.ResolvePath(_ruleExecutionContext, fieldPath);

            if (actualValue == null)
                return false;

            // Resolve the dynamic part here
            expectedValue = VariableResolver.EvaluateValue(_ruleExecutionContext, expectedValue);

            // unwrap JValue if needed
            if (expectedValue is JValue jval)
            {
                expectedValue = jval.Value;
            }

            int Compare(object a, object b)
            {
                try
                {
                    if (a == null || b == null)
                        throw new InvalidOperationException("Values cannot be null");

                    // If both are numeric types, convert both to double and compare
                    if (IsNumericType(a) && IsNumericType(b))
                    {
                        double da = Convert.ToDouble(a);
                        double db = Convert.ToDouble(b);
                        return da.CompareTo(db);
                    }

                    // If types differ but are IComparable, try converting b to a's type (optional)
                    if (a.GetType() != b.GetType())
                    {
                        try
                        {
                            b = Convert.ChangeType(b, a.GetType());
                        }
                        catch
                        {
                            // ignore conversion failure, fallback to direct comparison
                        }
                    }

                    if (a is IComparable ac)
                        return ac.CompareTo(b);

                    throw new InvalidOperationException("Values are not comparable");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FATAL] Unexpected error in workflow engine: {ex}");
                    throw;
                }
            }

            bool IsNumericType(object o)
            {
                return o is byte || o is sbyte ||
                       o is short || o is ushort ||
                       o is int || o is uint ||
                       o is long || o is ulong ||
                       o is float || o is double ||
                       o is decimal;
            }


            switch (op)
            {
                case "==":
                    return object.Equals(actualValue.ToString().Trim(), expectedValue.ToString().Trim());
                case "!=":
                    return !object.Equals(actualValue.ToString().Trim(), expectedValue.ToString().Trim());
                case ">=":
                    return Compare(actualValue, expectedValue) >= 0;
                case "<=":
                    return Compare(actualValue, expectedValue) <= 0;
                case ">":
                    return Compare(actualValue, expectedValue) > 0;
                case "<":
                    return Compare(actualValue, expectedValue) < 0;
                default:
                    throw new Exception($"Unsupported operator: {op}");
            }
        }
    }
}