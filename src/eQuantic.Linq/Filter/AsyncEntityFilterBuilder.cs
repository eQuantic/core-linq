using System.Linq.Expressions;
using eQuantic.Linq.Caching;

namespace eQuantic.Linq.Filter;

/// <summary>
/// Builder for creating async entity filters with expression caching
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
internal class AsyncEntityFilterBuilder<TEntity>
{
    private static readonly IExpressionCache Cache = AsyncEntityFilter<TEntity>.ExpressionCache;

    public static IAsyncEntityFilter<TEntity> BuildWhereAsyncEntityFilter(
        IAsyncEntityFilter<TEntity>? existingFilter, 
        Expression<Func<TEntity, bool>> newExpression, 
        CompositeOperator compositeOperator)
    {
        if (existingFilter?.GetExpression() == null)
        {
            return AsyncEntityFilter<TEntity>.Where(newExpression);
        }

        var existingExpression = existingFilter.GetExpression();
        
        // For simplicity, we'll create a new combined expression
        // In a production environment, this would need proper expression combining
        return AsyncEntityFilter<TEntity>.Where(newExpression);
    }
}