using System.Linq.Expressions;
using eQuantic.Linq.Expressions;
using eQuantic.Linq.Specification;

namespace eQuantic.Linq.Web.Specification;

/// <summary>
/// Specification satisfied by a REST filter query string
/// (e.g. <c>total:gt(100),items:any(price:gt(50))</c>): the exact syntax an API endpoint receives
/// becomes a composable specification. Parsing is eager (invalid syntax fails at construction);
/// the underlying <see cref="ExpressionModel{TRoot}"/> stays available for transport.
/// </summary>
/// <typeparam name="TEntity">Type of entity that checks this specification.</typeparam>
public class QueryStringSpecification<TEntity> : eQuantic.Linq.Specification.Specification<TEntity> where TEntity : class
{
    private readonly ExpressionModel<TEntity> model;
    private readonly QueryStringOptions options;

    /// <summary>Creates the specification from a filter query string.</summary>
    /// <param name="filter">Filter expression, e.g. <c>status:in(Paid|Shipped),total:gt(100)</c>.</param>
    /// <param name="options">Syntax options; defaults apply when omitted.</param>
    public QueryStringSpecification(string filter, QueryStringOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            throw new ArgumentException("Filter expression is empty.", nameof(filter));
        }

        this.options = options ?? new QueryStringOptions();
        model = QueryFilter.ParseModel<TEntity>(filter, this.options);
    }

    /// <summary>The parsed filter as a serializable model (e.g. to forward it to another service).</summary>
    public ExpressionModel<TEntity> Model => model;

    /// <inheritdoc />
    public override Expression<Func<TEntity, bool>> SatisfiedBy()
    {
        return options.Serializer.ToPredicate(model);
    }
}
