using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using WorkFlow.Persistence;
using WorkFlow.RuleInterpreter.Helpers;

namespace WorkFlow.RuleInterpreter.StepHandlers.FetchSingleEntityStep
{
    public class FetchSingleEntityStep
    {
        private readonly DatabaseContext _dbContext;
        private readonly RuleExecutionContext _ruleExecutionContext;

        public FetchSingleEntityStep(DatabaseContext dbContext, RuleExecutionContext ruleExecutionContext)
        {
            _dbContext = dbContext;
            _ruleExecutionContext = ruleExecutionContext;
        }
        public async Task ExecuteAsync(dynamic step)
        {
            string entityName = step.entity;
            var filter = step.filter;
            string storeAs = step.storeAs;

            // 1. Get DbSet by entity name
            var dbSetProperty = _dbContext.GetType()
                .GetProperties()
                .FirstOrDefault(p =>
                    p.PropertyType.IsGenericType &&
                    p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>) &&
                    string.Equals(p.Name, entityName, StringComparison.OrdinalIgnoreCase));

            if (dbSetProperty == null)
                throw new Exception($"Entity '{entityName}' not found in DbContext");

            var dbSet = dbSetProperty.GetValue(_dbContext);
            var entityType = dbSetProperty.PropertyType.GetGenericArguments()[0];

            // 2. Build IQueryable dynamically
            IQueryable query = (IQueryable)dbSet;

            // 3. Apply filter if exists
            IDictionary<string, object> filterDict = null;

            if (filter is JObject jObject)
            {
                filterDict = jObject.ToObject<Dictionary<string, object>>();
            }
            else if (filter is IDictionary<string, object> directDict)
            {
                filterDict = directDict;
            }

            if (filterDict != null && filterDict.Count > 0)
            {
                foreach (var filterProp in filterDict)
                {
                    string propertyName = filterProp.Key;
                    object rawValue = filterProp.Value;

                    object value;
                    if (rawValue is string strValue && strValue.StartsWith("@"))
                    {
                        string path = strValue.Substring(1);
                        value = VariableResolver.ResolvePath(_ruleExecutionContext, path);
                    }
                    else
                    {
                        value = rawValue;
                    }

                    if (value == null)
                        throw new Exception($"Filter value for property '{propertyName}' is null");

                    var parameter = Expression.Parameter(entityType, "x");
                    var property = Expression.PropertyOrField(parameter, propertyName);
                    var targetType = Nullable.GetUnderlyingType(property.Type) ?? property.Type;
                    var constant = Expression.Constant(Convert.ChangeType(value, targetType), property.Type);
                    var equality = Expression.Equal(property, constant);
                    var lambda = Expression.Lambda(equality, parameter);

                    var whereMethod = typeof(Queryable)
                        .GetMethods()
                        .First(m => m.Name == "Where" && m.GetParameters().Length == 2)
                        .MakeGenericMethod(entityType);

                    query = (IQueryable)whereMethod.Invoke(null, new object[] { query, lambda });
                }
            }

            // 4. Execute SingleOrDefaultAsync
            var singleOrDefaultAsyncMethod = typeof(EntityFrameworkQueryableExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                    m.Name == "SingleOrDefaultAsync" &&
                    m.IsGenericMethodDefinition &&
                    m.GetParameters().Length == 2 &&
                    m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IQueryable<>) &&
                    m.GetParameters()[1].ParameterType == typeof(CancellationToken));

            if (singleOrDefaultAsyncMethod == null)
                throw new Exception("SingleOrDefaultAsync method not found");

            var genericSingleMethod = singleOrDefaultAsyncMethod.MakeGenericMethod(entityType);

            var task = (Task)genericSingleMethod.Invoke(null, new object[] { query, CancellationToken.None });
            await task.ConfigureAwait(false);

            var resultProperty = task.GetType().GetProperty("Result");
            var result = resultProperty.GetValue(task);

            // 5. Store result in context
            _ruleExecutionContext.Set(storeAs, result);
        }
    }
}
