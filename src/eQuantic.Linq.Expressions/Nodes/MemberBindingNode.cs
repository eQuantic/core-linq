using System.Linq.Expressions;
using System.Text.Json.Serialization;
using eQuantic.Linq.Expressions.Metadata;

namespace eQuantic.Linq.Expressions.Nodes;

/// <summary>Serializable counterpart of <see cref="MemberBinding"/> used inside object initializers.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(MemberAssignmentNode), "assign")]
[JsonDerivedType(typeof(MemberMemberBindingNode), "memberBinding")]
[JsonDerivedType(typeof(MemberListBindingNode), "listBinding")]
public abstract class MemberBindingNode
{
    /// <summary>Member being bound.</summary>
    public MemberRef Member { get; set; } = null!;
}

/// <summary>Binds a member to a value (<see cref="MemberAssignment"/>).</summary>
public sealed class MemberAssignmentNode : MemberBindingNode
{
    /// <summary>Value assigned to the member.</summary>
    public ExpressionNode Expression { get; set; } = null!;
}

/// <summary>Recursively initializes the members of a member (<see cref="MemberMemberBinding"/>).</summary>
public sealed class MemberMemberBindingNode : MemberBindingNode
{
    /// <summary>Nested bindings.</summary>
    public List<MemberBindingNode> Bindings { get; set; } = [];
}

/// <summary>Initializes a collection member with elements (<see cref="MemberListBinding"/>).</summary>
public sealed class MemberListBindingNode : MemberBindingNode
{
    /// <summary>Element initializers.</summary>
    public List<ElementInitNode> Initializers { get; set; } = [];
}
