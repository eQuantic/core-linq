using System.Linq.Expressions;
using System.Text.Json.Serialization;
using eQuantic.Linq.Expressions.Metadata;

namespace eQuantic.Linq.Expressions.Nodes;

/// <summary>Serializable counterpart of <see cref="BlockExpression"/>.</summary>
public sealed class BlockNode : ExpressionNode
{
    /// <summary>Explicit block result type.</summary>
    public TypeRef? Type { get; set; }

    /// <summary>Variables declared in the block scope.</summary>
    public List<ParameterNode>? Variables { get; set; }

    /// <summary>Sequential expressions; the last one produces the block value.</summary>
    public List<ExpressionNode> Expressions { get; set; } = [];
}

/// <summary>Serializable counterpart of <see cref="SwitchExpression"/>.</summary>
public sealed class SwitchNode : ExpressionNode
{
    /// <summary>Explicit result type of the switch.</summary>
    public TypeRef? Type { get; set; }

    /// <summary>Value being switched on.</summary>
    public ExpressionNode SwitchValue { get; set; } = null!;

    /// <summary>Switch cases.</summary>
    public List<SwitchCaseNode> Cases { get; set; } = [];

    /// <summary>Body executed when no case matches.</summary>
    public ExpressionNode? DefaultBody { get; set; }

    /// <summary>Custom equality comparison method.</summary>
    public MethodRef? Comparison { get; set; }
}

/// <summary>Serializable counterpart of <see cref="SwitchCase"/>.</summary>
public sealed class SwitchCaseNode
{
    /// <summary>Test values of the case.</summary>
    public List<ExpressionNode> TestValues { get; set; } = [];

    /// <summary>Case body.</summary>
    public ExpressionNode Body { get; set; } = null!;
}

/// <summary>Serializable counterpart of <see cref="TryExpression"/>.</summary>
public sealed class TryNode : ExpressionNode
{
    /// <summary>Explicit result type of the try.</summary>
    public TypeRef? Type { get; set; }

    /// <summary>Protected body.</summary>
    public ExpressionNode Body { get; set; } = null!;

    /// <summary>Catch handlers.</summary>
    public List<CatchBlockNode>? Handlers { get; set; }

    /// <summary>Finally block.</summary>
    public ExpressionNode? Finally { get; set; }

    /// <summary>Fault block (runs only when an exception occurs).</summary>
    public ExpressionNode? Fault { get; set; }
}

/// <summary>Serializable counterpart of <see cref="CatchBlock"/>.</summary>
public sealed class CatchBlockNode
{
    /// <summary>Exception type caught by the handler.</summary>
    public TypeRef Test { get; set; } = null!;

    /// <summary>Exception variable, when declared.</summary>
    public ParameterNode? Variable { get; set; }

    /// <summary>Handler body.</summary>
    public ExpressionNode Body { get; set; } = null!;

    /// <summary>Exception filter (<c>when</c> clause).</summary>
    public ExpressionNode? Filter { get; set; }
}

/// <summary>Serializable counterpart of <see cref="LoopExpression"/>.</summary>
public sealed class LoopNode : ExpressionNode
{
    /// <summary>Loop body.</summary>
    public ExpressionNode Body { get; set; } = null!;

    /// <summary>Break label, when present.</summary>
    public LabelTargetNode? BreakLabel { get; set; }

    /// <summary>Continue label, when present.</summary>
    public LabelTargetNode? ContinueLabel { get; set; }
}

/// <summary>Serializable counterpart of <see cref="LabelTarget"/>. Identity is preserved through <see cref="Id"/>.</summary>
public sealed class LabelTargetNode
{
    /// <summary>Identity of the label within the serialized tree.</summary>
    public int Id { get; set; }

    /// <summary>Label name, if any.</summary>
    public string? Name { get; set; }

    /// <summary>Type of the value carried by jumps to this label.</summary>
    public TypeRef? Type { get; set; }
}

/// <summary>Serializable counterpart of <see cref="LabelExpression"/> (a jump destination).</summary>
public sealed class LabelNode : ExpressionNode
{
    /// <summary>The label target.</summary>
    public LabelTargetNode Target { get; set; } = null!;

    /// <summary>Value produced when execution reaches the label without a jump.</summary>
    public ExpressionNode? DefaultValue { get; set; }
}

/// <summary>Serializable counterpart of <see cref="GotoExpression"/> (goto/break/continue/return).</summary>
public sealed class GotoNode : ExpressionNode
{
    /// <summary>Jump semantics.</summary>
    public GotoExpressionKind Kind { get; set; }

    /// <summary>Destination label.</summary>
    public LabelTargetNode Target { get; set; } = null!;

    /// <summary>Value carried by the jump.</summary>
    public ExpressionNode? Value { get; set; }

    /// <summary>Explicit result type of the goto expression.</summary>
    public TypeRef? Type { get; set; }
}

/// <summary>Serializable counterpart of <see cref="RuntimeVariablesExpression"/>.</summary>
public sealed class RuntimeVariablesNode : ExpressionNode
{
    /// <summary>Variables exposed to the runtime.</summary>
    public List<ParameterNode> Variables { get; set; } = [];
}

/// <summary>Serializable counterpart of <see cref="DebugInfoExpression"/>.</summary>
public sealed class DebugInfoNode : ExpressionNode
{
    /// <summary>Source document file name.</summary>
    public string? FileName { get; set; }

    /// <summary>Source language identifier of the document, when specified.</summary>
    public Guid? Language { get; set; }

    /// <summary>Language vendor identifier of the document, when specified.</summary>
    public Guid? LanguageVendor { get; set; }

    /// <summary>Document type identifier, when specified.</summary>
    public Guid? DocumentType { get; set; }

    /// <summary>Start line.</summary>
    public int StartLine { get; set; }

    /// <summary>Start column.</summary>
    public int StartColumn { get; set; }

    /// <summary>End line.</summary>
    public int EndLine { get; set; }

    /// <summary>End column.</summary>
    public int EndColumn { get; set; }

    /// <summary>Whether this marks a sequence-point clearance.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsClear { get; set; }
}
