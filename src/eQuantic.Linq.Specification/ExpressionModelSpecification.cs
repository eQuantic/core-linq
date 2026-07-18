using System.Linq.Expressions;
using eQuantic.Linq.Expressions;

namespace eQuantic.Linq.Specification;

/// <summary>
/// Specification satisfied by a serialized expression payload: a root-anchored
/// <see cref="ExpressionModel{TRoot}"/> — or its raw JSON — received from another process, a message
/// queue or an API client. The model materializes into a typed predicate through the expression engine
/// (type inference, strict resolution policies and DTO casting all apply via the configured serializer).
/// </summary>
/// <typeparam name="TEntity">Type of entity that checks this specification.</typeparam>
public class ExpressionModelSpecification<TEntity> : Specification<TEntity> where TEntity : class
{
    private readonly ExpressionModel<TEntity> model;
    private readonly ExpressionSerializer serializer;

    /// <summary>Creates the specification from a serializable expression model.</summary>
    /// <param name="model">Root-anchored model whose body is a boolean expression over <typeparamref name="TEntity"/>.</param>
    /// <param name="serializer">Serializer used to materialize the model; <see cref="ExpressionSerializer.Default"/> when omitted.</param>
    public ExpressionModelSpecification(ExpressionModel<TEntity> model, ExpressionSerializer? serializer = null)
    {
        this.model = model ?? throw new ArgumentNullException(nameof(model));
        this.serializer = serializer ?? ExpressionSerializer.Default;
    }

    /// <summary>Creates the specification directly from a JSON payload.</summary>
    /// <param name="json">JSON of a root-anchored expression model.</param>
    /// <param name="serializer">Serializer used to deserialize and materialize the model; <see cref="ExpressionSerializer.Default"/> when omitted.</param>
    public ExpressionModelSpecification(string json, ExpressionSerializer? serializer = null)
    {
        if (json is null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        this.serializer = serializer ?? ExpressionSerializer.Default;
        model = this.serializer.ModelFromJson<TEntity>(json);
    }

    /// <summary>The underlying serializable model (e.g. to forward it to another service).</summary>
    public ExpressionModel<TEntity> Model => model;

    /// <inheritdoc />
    public override Expression<Func<TEntity, bool>> SatisfiedBy()
    {
        return serializer.ToPredicate(model);
    }
}
