using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkFlow.RuleInterpreter
{
    public class RuleExecutionContext
    {
        public Dictionary<string, object> Variables { get; } = new();

        public void Set(string name, object value)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Variable name cannot be null or whitespace.", nameof(name));

            Variables[name] = value;
        }

        public T Get<T>(string name)
        {
            if (!Variables.ContainsKey(name))
                throw new KeyNotFoundException($"Variable '{name}' not found in context.");

            var value = Variables[name];

            // Handle JValue wrapping (from Newtonsoft.Json)
            if (value is Newtonsoft.Json.Linq.JValue jValue)
            {
                return jValue.ToObject<T>();
            }

            // Normal casting
            return (T)Convert.ChangeType(value, typeof(T));
        }

        public bool TryGet<T>(string name, out T value)
        {
            if (Variables.TryGetValue(name, out var rawValue) && rawValue is T castValue)
            {
                value = castValue;
                return true;
            }

            value = default!;
            return false;
        }
    }

}
