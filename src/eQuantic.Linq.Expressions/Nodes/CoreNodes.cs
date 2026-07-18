using System.Linq.Expressions;
using System.Text.Json.Serialization;
using eQuantic.Linq.Expressions.Metadata;
using eQuantic.Linq.Expressions.Serialization;

namespace eQuantic.Linq.Expressions.Nodes;

/// <summary>Serializable counterpart of <see cref="ConstantExpression"/>.</summary>
public sealed class ConstantNode : ExpressionNode
{
    /// <summary>Static type of the constant (the <c>Expression.Type</c>).</summary>
    public TypeRef? Type { get; set; }

    /// <summary>Runtime type of the value when it differs from <see cref="Type"/> (e.g. a boxed value typed as <c>object</c>).</summary>
    public TypeRef? ValueType { get; set; }

    /// <summary>
    /// The constant value. When produced by JSON deserialization this holds a <see cref="System.Text.Json.JsonElement"/>
    /// that is materialized against <see cref="ValueType"/>/<see cref="Type"/> during expression reconstruction.
    /// </summary>
    [JsonConverter(typeof(RawValueJsonConverter))]
    public object? Value { get; set; }

    /// <summary>Nested node used when the constant itself holds an <see cref="Expression"/> value.</summary>
    public ExpressionNode? Expression { get; set; }
}

/// <summary>Serializable counterpart of <see cref="ParameterExpression"/>. Identity is preserved through <see cref="Id"/> so every occurrence rebuilds to the same instance.</summary>
public sealed class ParameterNode : ExpressionNode
{
    /// <summary>Identity of the parameter within the serialized tree.</summary>
    public int Id { get; set; }

    /// <summary>Parameter name, if any.</summary>
    public string? Name { get; set; }

    /// <summary>Parameter type.</summary>
    public TypeRef? Type { get; set; }

    /// <summary>Whether the parameter is passed by reference.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsByRef { get; set; }
}

/// <summary>Serializable counterpart of <see cref="LambdaExpression"/>.</summary>
public sealed class LambdaNode : ExpressionNode
{
    /// <summary>Exact delegate type of the lambda (e.g. <c>Func&lt;Order, bool&gt;</c>).</summary>
    public TypeRef? DelegateType { get; set; }

    /// <summary>Declared parameters.</summary>
    public List<ParameterNode> Parameters { get; set; } = [];

    /// <summary>Lambda body.</summary>
    public ExpressionNode Body { get; set; } = null!;

    /// <summary>Optional lambda name.</summary>
    public string? Name { get; set; }

    /// <summary>Whether the lambda was created with tail-call optimization enabled.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool TailCall { get; set; }
}

/// <summary>Serializable counterpart of <see cref="BinaryExpression"/> — covers arithmetic, comparison, logical, coalesce, array index and all assignment variants.</summary>
public sealed class BinaryNode : ExpressionNode
{
    /// <summary>The binary operation (e.g. <c>Add</c>, <c>Equal</c>, <c>Coalesce</c>, <c>AddAssign</c>).</summary>
    public ExpressionType NodeType { get; set; }

    /// <summary>Left operand.</summary>
    public ExpressionNode Left { get; set; } = null!;

    /// <summary>Right operand.</summary>
    public ExpressionNode Right { get; set; } = null!;

    /// <summary>Whether a lifted operation produces a nullable result (<c>LiftToNull</c>).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool LiftToNull { get; set; }

    /// <summary>User-defined operator method, when the operation is not intrinsic.</summary>
    public MethodRef? Method { get; set; }

    /// <summary>Conversion lambda used by <c>Coalesce</c> and compound assignments.</summary>
    public LambdaNode? Conversion { get; set; }
}

/// <summary>Serializable counterpart of <see cref="UnaryExpression"/> — covers negation, logical/bitwise not, conversions, quote, throw, array length and more.</summary>
public sealed class UnaryNode : ExpressionNode
{
    /// <summary>The unary operation (e.g. <c>Negate</c>, <c>Convert</c>, <c>Quote</c>, <c>Throw</c>).</summary>
    public ExpressionType NodeType { get; set; }

    /// <summary>Operand. Null only for a rethrow (<c>Throw</c> without operand).</summary>
    public ExpressionNode? Operand { get; set; }

    /// <summary>Result type for operations that carry one (conversions, <c>TypeAs</c>, <c>Throw</c>, <c>Unbox</c>).</summary>
    public TypeRef? Type { get; set; }

    /// <summary>User-defined operator method, when the operation is not intrinsic.</summary>
    public MethodRef? Method { get; set; }
}

/// <summary>Serializable counterpart of <see cref="MethodCallExpression"/>.</summary>
public sealed class MethodCallNode : ExpressionNode
{
    /// <summary>The invoked method.</summary>
    public MethodRef Method { get; set; } = null!;

    /// <summary>Instance receiving the call; null for static methods.</summary>
    public ExpressionNode? Object { get; set; }

    /// <summary>Call arguments.</summary>
    public List<ExpressionNode>? Arguments { get; set; }
}

/// <summary>Serializable counterpart of <see cref="MemberExpression"/> (field or property access).</summary>
public sealed class MemberNode : ExpressionNode
{
    /// <summary>The accessed member.</summary>
    public MemberRef Member { get; set; } = null!;

    /// <summary>Instance whose member is accessed; null for static members.</summary>
    public ExpressionNode? Expression { get; set; }
}

/// <summary>Serializable counterpart of <see cref="ConditionalExpression"/> (<c>test ? ifTrue : ifFalse</c> and <c>if</c>/<c>else</c> statements).</summary>
public sealed class ConditionalNode : ExpressionNode
{
    /// <summary>Condition.</summary>
    public ExpressionNode Test { get; set; } = null!;

    /// <summary>Expression evaluated when the condition is true.</summary>
    public ExpressionNode IfTrue { get; set; } = null!;

    /// <summary>Expression evaluated when the condition is false.</summary>
    public ExpressionNode IfFalse { get; set; } = null!;

    /// <summary>Explicit result type (e.g. <c>void</c> for statement-style conditionals).</summary>
    public TypeRef? Type { get; set; }
}
