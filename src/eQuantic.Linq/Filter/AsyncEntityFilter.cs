using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using eQuantic.Linq.Caching;

namespace eQuantic.Linq.Filter;

/// <summary>
/// Enables asynchronous filtering of entities with expression caching for improved performance.
/// </summary>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
public static class AsyncEntityFilter<TEntity>
{
    private static readonly IExpressionCache Cache = new ExpressionCache();

    /// <summary>
    /// Returns an <see cref="IAsyncEntityFilter{TEntity}"/> instance that allows construction of
    /// <see cref="IAsyncEntityFilter{TEntity}"/> objects through the use of LINQ syntax.
    /// </summary>
    /// <returns>An <see cref="IAsyncEntityFilter{TEntity}"/> instance.</returns>
    public static IAsyncEntityFilter<TEntity> AsQueryable()
    {
        return new EmptyAsyncEntityFilter();
    }

    /// <summary>
    /// Returns an <see cref="IAsyncEntityFilter{TEntity}"/> that filters a sequence based on a predicate.
    /// </summary>
    /// <param name="predicate">The predicate.</param>
    /// <returns>A new <see cref="IAsyncEntityFilter{TEntity}"/>.</returns>
    public static IAsyncEntityFilter<TEntity> Where(Expression<Func<TEntity, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return new WhereAsyncEntityFilter(predicate);
    }

    /// <summary>
    /// Returns an <see cref="IAsyncEntityFilter{TEntity}"/> that filters a sequence based on filterings.
    /// </summary>
    /// <param name="filters">The filters.</param>
    /// <returns>A new async entity filter.</returns>
    public static IAsyncEntityFilter<TEntity> Where(params IFiltering[] filters)
    {
        return filters is [CompositeFiltering<TEntity> compositeFiltering]
            ? Where(compositeFiltering.Values, compositeFiltering.CompositeOperator)
            : Where(filters, CompositeOperator.And);
    }

    private static IAsyncEntityFilter<TEntity> Where(IFiltering[] filters, CompositeOperator compositeOperator)
    {
        // For now, we'll use the synchronous version and wrap it
        var syncFilter = EntityFilter<TEntity>.Where(filters);
        var expression = syncFilter.GetExpression();
        
        return expression == null 
            ? new EmptyAsyncEntityFilter() 
            : new WhereAsyncEntityFilter(expression);
    }

    /// <summary>
    /// Gets the shared expression cache instance.
    /// </summary>
    public static IExpressionCache ExpressionCache => Cache;

    private sealed class EmptyAsyncEntityFilter : IAsyncEntityFilter<TEntity>
    {
        public IQueryable<TEntity> Filter(IQueryable<TEntity> collection) => collection;
        
        public Expression<Func<TEntity, bool>> GetExpression() => null!;

        public async Task<TEntity[]> FilterAsync(IQueryable<TEntity> collection, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => collection.ToArray(), cancellationToken).ConfigureAwait(false);
        }

        public async IAsyncEnumerable<TEntity> FilterAsyncEnumerable(IQueryable<TEntity> collection, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            foreach (var entity in collection)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return entity;
            }
        }

        public async Task<TEntity?> FirstOrDefaultAsync(IQueryable<TEntity> collection, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => collection.FirstOrDefault(), cancellationToken).ConfigureAwait(false);
        }

        public async Task<int> CountAsync(IQueryable<TEntity> collection, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => collection.Count(), cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> AnyAsync(IQueryable<TEntity> collection, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => collection.Any(), cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class WhereAsyncEntityFilter : IAsyncEntityFilter<TEntity>
    {
        private readonly Expression<Func<TEntity, bool>> _predicate;
        private readonly string _cacheKey;
        private readonly Func<TEntity, bool> _compiledPredicate;

        public WhereAsyncEntityFilter(Expression<Func<TEntity, bool>> predicate)
        {
            _predicate = predicate;
            _cacheKey = GenerateStableCacheKey(predicate);
            
            // Cache compiled predicate for LINQ to Objects scenarios (eager compilation)
            _compiledPredicate = Cache.GetOrCreate(_cacheKey, () => predicate);
        }

        /// <summary>
        /// Generates a stable cache key based on expression structure, not string representation
        /// </summary>
        private static string GenerateStableCacheKey(Expression expression)
        {
            var visitor = new ExpressionHashVisitor();
            visitor.Visit(expression);
            return $"Filter_{typeof(TEntity).Name}_{visitor.GetHash()}";
        }

        public IQueryable<TEntity> Filter(IQueryable<TEntity> collection) => collection.Where(_predicate);
        
        public Expression<Func<TEntity, bool>> GetExpression() => _predicate;

        public async Task<TEntity[]> FilterAsync(IQueryable<TEntity> collection, CancellationToken cancellationToken = default)
        {
            // Try to detect and use EF Core async methods if available
            if (IsEntityFrameworkQueryable(collection))
            {
                return await TryUseEfCoreToArrayAsync(collection.Where(_predicate), cancellationToken)
                    ?? await FallbackToSyncWithTask(collection, cancellationToken);
            }
            else
            {
                // LINQ to Objects - use compiled predicate (no unnecessary Task.Run)
                cancellationToken.ThrowIfCancellationRequested();
                return collection.Where(_compiledPredicate).ToArray();
            }
        }

        public async IAsyncEnumerable<TEntity> FilterAsyncEnumerable(IQueryable<TEntity> collection, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (IsEntityFrameworkQueryable(collection) && collection is IAsyncEnumerable<TEntity> asyncCollection)
            {
                // EF Core - use async enumerable with filtering
                await foreach (var entity in asyncCollection.WithCancellation(cancellationToken))
                {
                    if (_compiledPredicate(entity))
                        yield return entity;
                }
            }
            else
            {
                // LINQ to Objects - enumerate with cancellation checks
                foreach (var entity in collection.Where(_compiledPredicate))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return entity;
                }
            }
        }

        public async Task<TEntity?> FirstOrDefaultAsync(IQueryable<TEntity> collection, CancellationToken cancellationToken = default)
        {
            if (IsEntityFrameworkQueryable(collection))
            {
                return await TryUseEfCoreFirstOrDefaultAsync(collection.Where(_predicate), cancellationToken)
                    ?? await Task.FromResult(collection.Where(_compiledPredicate).FirstOrDefault());
            }
            else
            {
                cancellationToken.ThrowIfCancellationRequested();
                return collection.Where(_compiledPredicate).FirstOrDefault();
            }
        }

        public async Task<int> CountAsync(IQueryable<TEntity> collection, CancellationToken cancellationToken = default)
        {
            if (IsEntityFrameworkQueryable(collection))
            {
                return await TryUseEfCoreCountAsync(collection, _predicate, cancellationToken)
                    ?? await Task.FromResult(collection.Count(_compiledPredicate));
            }
            else
            {
                cancellationToken.ThrowIfCancellationRequested();
                return collection.Count(_compiledPredicate);
            }
        }

        public async Task<bool> AnyAsync(IQueryable<TEntity> collection, CancellationToken cancellationToken = default)
        {
            if (IsEntityFrameworkQueryable(collection))
            {
                return await TryUseEfCoreAnyAsync(collection, _predicate, cancellationToken)
                    ?? await Task.FromResult(collection.Any(_compiledPredicate));
            }
            else
            {
                cancellationToken.ThrowIfCancellationRequested();
                return collection.Any(_compiledPredicate);
            }
        }

        /// <summary>
        /// Detects if the queryable is from Entity Framework by checking the provider type
        /// </summary>
        private static bool IsEntityFrameworkQueryable(IQueryable collection)
        {
            var providerType = collection.Provider.GetType();
            return providerType.FullName?.Contains("EntityFramework") == true ||
                   providerType.FullName?.Contains("Microsoft.EntityFrameworkCore") == true;
        }

        /// <summary>
        /// Attempts to use EF Core's ToArrayAsync using reflection (if available)
        /// </summary>
        private static async Task<TEntity[]?> TryUseEfCoreToArrayAsync(IQueryable<TEntity> query, CancellationToken cancellationToken)
        {
            try
            {
                // Look for EntityFrameworkQueryableExtensions.ToArrayAsync
                var efExtensionsType = Type.GetType("Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions, Microsoft.EntityFrameworkCore");
                if (efExtensionsType != null)
                {
                    var toArrayAsyncMethod = efExtensionsType.GetMethod("ToArrayAsync", new[] { typeof(IQueryable<TEntity>), typeof(CancellationToken) });
                    if (toArrayAsyncMethod != null)
                    {
                        var task = (Task<TEntity[]>?)toArrayAsyncMethod.Invoke(null, [query, cancellationToken]);
                        if (task != null)
                        {
                            return await task.ConfigureAwait(false);
                        }
                    }
                }
            }
            catch
            {
                // Fallback to sync if EF Core methods fail
            }
            return null;
        }

        /// <summary>
        /// Fallback method using Task.Run for when EF Core async isn't available
        /// </summary>
        private async Task<TEntity[]> FallbackToSyncWithTask(IQueryable<TEntity> collection, CancellationToken cancellationToken)
        {
            return await Task.Run(() => collection.Where(_compiledPredicate).ToArray(), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Attempts to use EF Core's FirstOrDefaultAsync using reflection
        /// </summary>
        private static async Task<TEntity?> TryUseEfCoreFirstOrDefaultAsync(IQueryable<TEntity> query, CancellationToken cancellationToken)
        {
            try
            {
                var efExtensionsType = Type.GetType("Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions, Microsoft.EntityFrameworkCore");
                if (efExtensionsType != null)
                {
                    var firstOrDefaultAsyncMethod = efExtensionsType.GetMethod("FirstOrDefaultAsync", new[] { typeof(IQueryable<TEntity>), typeof(CancellationToken) });
                    if (firstOrDefaultAsyncMethod != null)
                    {
                        var task = (Task<TEntity?>?)firstOrDefaultAsyncMethod.Invoke(null, [query, cancellationToken]);
                        if (task != null)
                        {
                            return await task.ConfigureAwait(false);
                        }
                    }
                }
            }
            catch
            {
                // Fallback to sync
            }
            return default;
        }

        /// <summary>
        /// Attempts to use EF Core's CountAsync using reflection
        /// </summary>
        private static async Task<int?> TryUseEfCoreCountAsync(IQueryable<TEntity> query, Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken)
        {
            try
            {
                var efExtensionsType = Type.GetType("Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions, Microsoft.EntityFrameworkCore");
                if (efExtensionsType != null)
                {
                    var countAsyncMethod = efExtensionsType.GetMethod("CountAsync", new[] { typeof(IQueryable<TEntity>), typeof(Expression<Func<TEntity, bool>>), typeof(CancellationToken) });
                    if (countAsyncMethod != null)
                    {
                        var task = (Task<int>?)countAsyncMethod.Invoke(null, [query, predicate, cancellationToken]);
                        if (task != null)
                        {
                            return await task.ConfigureAwait(false);
                        }
                    }
                }
            }
            catch
            {
                // Fallback to sync
            }
            return null;
        }

        /// <summary>
        /// Attempts to use EF Core's AnyAsync using reflection
        /// </summary>
        private static async Task<bool?> TryUseEfCoreAnyAsync(IQueryable<TEntity> query, Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken)
        {
            try
            {
                var efExtensionsType = Type.GetType("Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions, Microsoft.EntityFrameworkCore");
                if (efExtensionsType != null)
                {
                    var anyAsyncMethod = efExtensionsType.GetMethod("AnyAsync", new[] { typeof(IQueryable<TEntity>), typeof(Expression<Func<TEntity, bool>>), typeof(CancellationToken) });
                    if (anyAsyncMethod != null)
                    {
                        var task = (Task<bool>?)anyAsyncMethod.Invoke(null, [query, predicate, cancellationToken]);
                        if (task != null)
                        {
                            return await task.ConfigureAwait(false);
                        }
                    }
                }
            }
            catch
            {
                // Fallback to sync
            }
            return null;
        }
    }

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