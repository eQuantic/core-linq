using System.Linq.Expressions;
using eQuantic.Linq.Expressions.Metadata;

namespace eQuantic.Linq.Expressions.Nodes;

/// <summary>Serializable counterpart of <see cref="InvocationExpression"/> (invoking a delegate or lambda).</summary>
public sealed class InvocationNode : ExpressionNode
{
    /// <summary>Delegate- or lambda-valued expression being invoked.</summary>
    public ExpressionNode Expression { get; set; } = null!;

    /// <summary>Invocation arguments.</summary>
    public List<ExpressionNode>? Arguments { get; set; }
}

/// <summary>Serializable counterpart of <see cref="TypeBinaryExpression"/> (<c>TypeIs</c> / <c>TypeEqual</c>).</summary>
public sealed class TypeBinaryNode : ExpressionNode
{
    /// <summary><c>TypeIs</c> or <c>TypeEqual</c>.</summary>
    public ExpressionType NodeType { get; set; }

    /// <summary>Operand whose type is tested.</summary>
    public ExpressionNode Expression { get; set; } = null!;

    /// <summary>Type operand of the test.</summary>
    public TypeRef TypeOperand { get; set; } = null!;
}

/// <summary>Serializable counterpart of <see cref="IndexExpression"/> (indexer or array element access).</summary>
public sealed class IndexNode : ExpressionNode
{
    /// <summary>Indexed object.</summary>
    public ExpressionNode Object { get; set; } = null!;

    /// <summary>Indexer property; null for direct array access.</summary>
    public MemberRef? Indexer { get; set; }

    /// <summary>Index arguments.</summary>
    public List<ExpressionNode> Arguments { get; set; } = [];
}

/// <summary>Serializable counterpart of <see cref="DefaultExpression"/> (<c>default(T)</c> and the empty expression).</summary>
public sealed class DefaultNode : ExpressionNode
{
    /// <summary>Type whose default value is produced; <c>void</c> yields the empty expression.</summary>
    public TypeRef Type { get; set; } = null!;
}

/// <summary>
/// Placeholder for an <see cref="IQueryable"/> root captured inside an expression (e.g. the source of a serialized query).
/// On reconstruction it is re-bound through <see cref="ExpressionSerializerOptions.QueryRootProvider"/>.
/// </summary>
public sealed class QueryRootNode : ExpressionNode
{
    /// <summary>Element type of the queryable root.</summary>
    public TypeRef ElementType { get; set; } = null!;

    /// <summary>Static type the root constant had in the original tree, for diagnostics.</summary>
    public TypeRef? QueryableType { get; set; }
}
