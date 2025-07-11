using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace WorkFlow.RuleInterpreter.Helpers
{
    public static class ExpressionBuilder
    {
        public static Expression<Func<T, bool>> BuildEqualityExpression<T>(string propertyName, object value)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.PropertyOrField(parameter, propertyName);
            var targetType = Nullable.GetUnderlyingType(property.Type) ?? property.Type;

            var convertedValue = Convert.ChangeType(value, targetType);
            var constant = Expression.Constant(convertedValue, property.Type);
            var equality = Expression.Equal(property, constant);

            return Expression.Lambda<Func<T, bool>>(equality, parameter);
        }

        public static Expression<Func<T, bool>> BuildComparisonExpression<T>(string propertyName, string op, object value)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.PropertyOrField(parameter, propertyName);
            var targetType = Nullable.GetUnderlyingType(property.Type) ?? property.Type;

            var convertedValue = Convert.ChangeType(value, targetType);
            var constant = Expression.Constant(convertedValue, property.Type);

            Expression comparison = op switch
            {
                "==" => Expression.Equal(property, constant),
                "!=" => Expression.NotEqual(property, constant),
                ">" => Expression.GreaterThan(property, constant),
                ">=" => Expression.GreaterThanOrEqual(property, constant),
                "<" => Expression.LessThan(property, constant),
                "<=" => Expression.LessThanOrEqual(property, constant),
                _ => throw new NotSupportedException($"Unsupported operator: {op}")
            };

            return Expression.Lambda<Func<T, bool>>(comparison, parameter);
        }
    }
}
