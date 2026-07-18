using System.Linq.Expressions;
using eQuantic.Linq.Expressions.Nodes;

namespace eQuantic.Linq.Expressions.Conversion;

/// <summary>
/// Shared expectation rules between the encoder (which omits inferable type info in
/// <see cref="TypeInfoMode.Minimal"/>) and the decoder (which re-infers it). Both sides MUST stay in
/// lockstep: the encoder only omits what these rules let the decoder recover.
/// </summary>
internal static class InferenceRules
{
    /// <summary>Expected type of one binary operand given its sibling's type.</summary>
    public static Type? OperandExpectation(ExpressionType nodeType, Type siblingType, bool isRight)
    {
        switch (nodeType)
        {
            case ExpressionType.LeftShift:
            case ExpressionType.RightShift:
            case ExpressionType.LeftShiftAssign:
            case ExpressionType.RightShiftAssign:
                return isRight ? typeof(int) : siblingType;
            case ExpressionType.Coalesce:
                return isRight ? Nullable.GetUnderlyingType(siblingType) ?? siblingType : siblingType;
            case ExpressionType.ArrayIndex:
                return isRight ? typeof(int) : siblingType;
            default:
                return siblingType;
        }
    }

    /// <summary>A node that cannot be decoded without a contextual expected type.</summary>
    public static bool NeedsTypeContext(ExpressionNode node) =>
        node is ConstantNode { Type: null, ValueType: null, Expression: null };

    /// <summary>Expected types of <c>object</c>/<c>void</c> carry no useful constraint for constants.</summary>
    public static Type? Meaningful(Type? expected) =>
        expected is null || expected == typeof(object) || expected == typeof(void) ? null : expected;

    /// <summary>Unwraps <c>Expression&lt;TDelegate&gt;</c> parameters to the delegate type used as decode expectation.</summary>
    public static Type UnwrapDelegateExpectation(Type parameterType) =>
        parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(Expression<>)
            ? parameterType.GetGenericArguments()[0]
            : parameterType;
}
