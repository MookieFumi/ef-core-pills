using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EfCoreInterceptors.Interceptors.Queries;

/// <summary>Adds Take(maxRows) to any query that returns a list and does not already limit it.</summary>
public class RowCapQueryInterceptor(int maxRows) : IQueryExpressionInterceptor
{
    public Expression QueryCompilationStarting(Expression queryExpression, QueryExpressionEventData eventData)
    {
        var type = queryExpression.Type;

        if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(IQueryable<>))
        {
            return queryExpression;
        }

        if (queryExpression is MethodCallExpression { Method.Name: nameof(Queryable.Take) or nameof(Queryable.Skip) })
        {
            return queryExpression;
        }

        var elementType = type.GetGenericArguments()[0];
        var take = typeof(Queryable)
            .GetMethods()
            .Single(m => m.Name == nameof(Queryable.Take) && m.GetParameters()[1].ParameterType == typeof(int))
            .MakeGenericMethod(elementType);

        return Expression.Call(take, queryExpression, Expression.Constant(maxRows));
    }
}
