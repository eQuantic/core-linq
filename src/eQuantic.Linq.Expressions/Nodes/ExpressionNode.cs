using System.Text.Json.Serialization;

namespace eQuantic.Linq.Expressions.Nodes;

/// <summary>
/// Base class of the serializable expression model. Every node mirrors a structural family of
/// <see cref="System.Linq.Expressions.Expression"/> and is JSON-polymorphic through the <c>$type</c> discriminator.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ConstantNode), "constant")]
[JsonDerivedType(typeof(ParameterNode), "parameter")]
[JsonDerivedType(typeof(LambdaNode), "lambda")]
[JsonDerivedType(typeof(BinaryNode), "binary")]
[JsonDerivedType(typeof(UnaryNode), "unary")]
[JsonDerivedType(typeof(MethodCallNode), "call")]
[JsonDerivedType(typeof(MemberNode), "member")]
[JsonDerivedType(typeof(ConditionalNode), "conditional")]
[JsonDerivedType(typeof(NewNode), "new")]
[JsonDerivedType(typeof(NewArrayNode), "newArray")]
[JsonDerivedType(typeof(MemberInitNode), "memberInit")]
[JsonDerivedType(typeof(ListInitNode), "listInit")]
[JsonDerivedType(typeof(InvocationNode), "invoke")]
[JsonDerivedType(typeof(TypeBinaryNode), "typeBinary")]
[JsonDerivedType(typeof(IndexNode), "index")]
[JsonDerivedType(typeof(DefaultNode), "default")]
[JsonDerivedType(typeof(BlockNode), "block")]
[JsonDerivedType(typeof(SwitchNode), "switch")]
[JsonDerivedType(typeof(TryNode), "try")]
[JsonDerivedType(typeof(LoopNode), "loop")]
[JsonDerivedType(typeof(LabelNode), "label")]
[JsonDerivedType(typeof(GotoNode), "goto")]
[JsonDerivedType(typeof(RuntimeVariablesNode), "runtimeVariables")]
[JsonDerivedType(typeof(DebugInfoNode), "debugInfo")]
[JsonDerivedType(typeof(QueryRootNode), "queryRoot")]
public abstract class ExpressionNode
{
}
