using System.Diagnostics;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using eQuantic.Linq.Caching;
using eQuantic.Linq.Specification;

namespace eQuantic.Linq.Filter;

/// <summary>
/// Filters the collection using a predicate.
/// </summary>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
[DebuggerDisplay("EntityFilter ( where {ToString()} )")]
internal sealed class WhereEntityFilter<TEntity> : IEntityFilter<TEntity>
{
    private static readonly IExpressionCache Cache = new ExpressionCache();
    
    private readonly IEntityFilter<TEntity>? baseFilter;
    private readonly CompositeOperator compositeOperator = CompositeOperator.And;
    private readonly Expression<Func<TEntity, bool>> predicate;
    private readonly Func<TEntity, bool>? compiledPredicate;
    private readonly string? cacheKey;

    /// <summary>Initializes a new instance of the <see cref="WhereEntityFilter{TEntity}"/> class.</summary>
    /// <param name="predicate">The predicate.</param>
    public WhereEntityFilter(Expression<Func<TEntity, bool>> predicate)
    {
        this.predicate = predicate;
        this.cacheKey = GenerateStableCacheKey(predicate);
        
        // Cache compiled predicate for performance (eager compilation)
        this.compiledPredicate = Cache.GetOrCreate(cacheKey, () => predicate);
    }

    /// <summary>Initializes a new instance of the <see cref="WhereEntityFilter{TEntity}"/> class.</summary>
    /// <param name="baseFilter">The base filter.</param>
    /// <param name="predicate">The predicate.</param>
    /// <param name="compositeOperator">The composite operator</param>
    public WhereEntityFilter(IEntityFilter<TEntity> baseFilter, Expression<Func<TEntity, bool>> predicate,
        CompositeOperator compositeOperator = CompositeOperator.And)
    {
        this.baseFilter = baseFilter;
        this.predicate = predicate;
        this.compositeOperator = compositeOperator;
        
        // For composite filters, we still cache the predicate part
        this.cacheKey = GenerateStableCacheKey(predicate);
        this.compiledPredicate = Cache.GetOrCreate(cacheKey, () => predicate);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not WhereEntityFilter<TEntity> filter)
        {
            return false;
        }

        return baseFilter == filter.baseFilter && predicate.ToString() == filter.predicate.ToString();
    }

    /// <summary>Filters the specified collection.</summary>
    /// <param name="collection">The collection.</param>
    /// <returns>A filtered collection.</returns>
    public IQueryable<TEntity> Filter(IQueryable<TEntity> collection)
    {
        // For now, always use the expression since we're working with IQueryable
        // The compiled predicate optimization is primarily for the async scenarios
        // where we convert to arrays/enumerables
        return baseFilter == null 
            ? collection.Where(predicate) 
            : baseFilter.Filter(collection).Where(predicate);
    }

    /// <summary>
    /// Gets the expression.
    /// </summary>
    /// <returns></returns>
    public Expression<Func<TEntity, bool>> GetExpression()
    {
        if (baseFilter == null)
        {
            return predicate;
        }

        return compositeOperator == CompositeOperator.And
            ? baseFilter.GetExpression().AndAlso(predicate)
            : baseFilter.GetExpression().OrElse(predicate);
    }

    public override int GetHashCode()
    {
        return (baseFilter, predicate).GetHashCode();
    }

    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        var baseFilterPresentation =
            baseFilter != null ? baseFilter.ToString() : string.Empty;

        // The returned string is used in de DebuggerDisplay.
        if (!string.IsNullOrEmpty(baseFilterPresentation))
        {
            return baseFilterPresentation + ", " + predicate.ToString();
        }

        return predicate.ToString();
    }

    /// <summary>
    /// Generates a stable cache key based on expression structure, not string representation
    /// </summary>
    private static string GenerateStableCacheKey(Expression expression)
    {
        var visitor = new ExpressionHashVisitor();
        visitor.Visit(expression);
        return $"SyncFilter_{typeof(TEntity).Name}_{visitor.GetHash()}";
    }

    /// <summary>
    /// Gets the shared expression cache instance for testing/monitoring
    /// </summary>
    internal static IExpressionCache ExpressionCache => Cache;

    /// <summary>
    /// Visitor that generates stable hash based on Expression structure, not string representation
    /// </summary>
    private sealed class ExpressionHashVisitor : ExpressionVisitor
    {
        private readonly StringBuilder _hashBuilder = new();

        public string GetHash()
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(_hashBuilder.ToString());
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes)[..16]; // First 16 chars
        }

        public override Expression? Visit(Expression? node)
        {
            if (node != null)
            {
                _hashBuilder.Append(node.NodeType.ToString());
                _hashBuilder.Append(node.Type.Name);
            }
            return base.Visit(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            _hashBuilder.Append(node.Member.Name);
            return base.VisitMember(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            _hashBuilder.Append(node.Value?.ToString() ?? "null");
            return base.VisitConstant(node);
        }
    }
}