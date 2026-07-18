using System.Linq.Expressions;
using eQuantic.Linq.Expressions.Metadata;

namespace eQuantic.Linq.Expressions.Nodes;

/// <summary>Serializable counterpart of <see cref="NewExpression"/>, including anonymous-type construction.</summary>
public sealed class NewNode : ExpressionNode
{
    /// <summary>Type being constructed.</summary>
    public TypeRef Type { get; set; } = null!;

    /// <summary>Constructor to invoke. Null for parameterless value-type construction.</summary>
    public ConstructorRef? Constructor { get; set; }

    /// <summary>Constructor arguments.</summary>
    public List<ExpressionNode>? Arguments { get; set; }

    /// <summary>Members mapped to each argument — present for anonymous-type projections.</summary>
    public List<MemberRef>? Members { get; set; }
}

/// <summary>Serializable counterpart of <see cref="NewArrayExpression"/> (<c>NewArrayInit</c> and <c>NewArrayBounds</c>).</summary>
public sealed class NewArrayNode : ExpressionNode
{
    /// <summary><c>NewArrayInit</c> (elements) or <c>NewArrayBounds</c> (dimension sizes).</summary>
    public ExpressionType NodeType { get; set; }

    /// <summary>Array element type.</summary>
    public TypeRef ElementType { get; set; } = null!;

    /// <summary>Element expressions or bound expressions, according to <see cref="NodeType"/>.</summary>
    public List<ExpressionNode>? Expressions { get; set; }
}

/// <summary>Serializable counterpart of <see cref="MemberInitExpression"/> (object initializers).</summary>
public sealed class MemberInitNode : ExpressionNode
{
    /// <summary>The <c>new</c> expression being initialized.</summary>
    public NewNode NewExpression { get; set; } = null!;

    /// <summary>Member bindings applied after construction.</summary>
    public List<MemberBindingNode> Bindings { get; set; } = [];
}

/// <summary>Serializable counterpart of <see cref="ListInitExpression"/> (collection initializers).</summary>
public sealed class ListInitNode : ExpressionNode
{
    /// <summary>The <c>new</c> expression being initialized.</summary>
    public NewNode NewExpression { get; set; } = null!;

    /// <summary>Element initializers.</summary>
    public List<ElementInitNode> Initializers { get; set; } = [];
}

/// <summary>Serializable counterpart of <see cref="ElementInit"/> — one <c>Add</c> call of a collection initializer.</summary>
public sealed class ElementInitNode
{
    /// <summary>The <c>Add</c> method invoked for this element.</summary>
    public MethodRef AddMethod { get; set; } = null!;

    /// <summary>Arguments passed to the <c>Add</c> method.</summary>
    public List<ExpressionNode> Arguments { get; set; } = [];
}
