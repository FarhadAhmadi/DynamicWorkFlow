using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using WorkFlow.Persistence;
using WorkFlow.RuleInterpreter.Helpers;

namespace WorkFlow.RuleInterpreter.StepHandlers.FetchEntityListStep
{
    public class FetchEntityListStep
    {
        private readonly DatabaseContext _dbContext;
        private readonly Dictionary<string, object> _variables;

        public FetchEntityListStep(DatabaseContext dbContext, Dictionary<string, object> variables)
        {
            _dbContext = dbContext;
            _variables = variables;
        }

        public async Task ExecuteAsync(dynamic step)
        {
            string entityName = step.entity;
            var filter = step.filter; // فرض بر این است که به شکل داینامیک و ساده است: { "PersonId": 12 }
            string storeAs = step.storeAs;

            // 1. گرفتن DbSet با استفاده از نام entity
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

            // 2. ساخت queryable به صورت داینامیک
            IQueryable query = (IQueryable)dbSet;

            // 3. اعمال فیلترهای ساده به صورت داینامیک
            IDictionary<string, object> filterDict = null;

            // If it's a JObject (Newtonsoft), convert it
            if (filter is JObject jObject)
            {
                filterDict = jObject.ToObject<Dictionary<string, object>>();
            }
            // If it's already a Dictionary (rare, but possible)
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

                    object value = VariableResolver.EvaluateValue(_variables, rawValue);

                    var parameter = Expression.Parameter(entityType, "x");
                    var property = Expression.PropertyOrField(parameter, propertyName);

                    var targetType = Nullable.GetUnderlyingType(property.Type) ?? property.Type;
                    var constant = Expression.Constant(Convert.ChangeType(value, targetType), property.Type);

                    var equality = Expression.Equal(property, constant);
                    var lambda = Expression.Lambda(equality, parameter);

                    var whereMethod = typeof(Queryable)
                        .GetMethods()
                        .First(m => m.Name == "Where" &&
                                    m.GetParameters().Length == 2)
                        .MakeGenericMethod(entityType);

                    query = (IQueryable)whereMethod.Invoke(null, new object[] { query, lambda });
                }
            }

            // 4. ToListAsync به صورت داینامیک
            var toListAsyncMethod = typeof(EntityFrameworkQueryableExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                    m.Name == "ToListAsync" &&
                    m.IsGenericMethodDefinition &&  // توجه کن اینجا باید IsGenericMethodDefinition باشه
                    m.GetParameters().Length == 2 &&
                    m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IQueryable<>) &&
                    m.GetParameters()[1].ParameterType == typeof(CancellationToken)
                );

            if (toListAsyncMethod == null)
                throw new Exception("ToListAsync method not found");

            // تبدیل به متد generic با نوع درست:
            var genericToListAsyncMethod = toListAsyncMethod.MakeGenericMethod(entityType);

            var task = (Task)genericToListAsyncMethod.Invoke(null, new object[] { query, CancellationToken.None });

            await task.ConfigureAwait(false);

            var resultProperty = task.GetType().GetProperty("Result");
            var list = resultProperty.GetValue(task);

            // ذخیره نتیجه در متغیرها
            _variables[storeAs] = list;
        }
    }
}
