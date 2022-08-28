using System.Linq.Expressions;
using System.Text;

namespace eQuantic.Linq.Extensions;

/// <summary>
/// Expression Extensions
/// </summary>
public static class ExpressionExtensions
{
    private const string PropertiesQueryStringDelimiter = ".";

    /// <summary>
    /// Gets the name of the column.
    /// </summary>
    /// <param name="expression">The expression.</param>
    /// <returns></returns>
    public static string GetColumnName(this Expression expression)
    {
        if (expression == null)
        {
            throw new ArgumentNullException(nameof(expression));
        }

        var sb = new StringBuilder();

        while (true)
        {
            var piece = expression.GetExpressionMemberName(ref expression);
            if (string.IsNullOrEmpty(piece))
            {
                break;
            }
            if (sb.Length > 0)
            {
                sb.Insert(0, PropertiesQueryStringDelimiter);
            }
            sb.Insert(0, piece);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Gets the name of the expression member.
    /// </summary>
    /// <param name="expr">The expr.</param>
    /// <param name="nextExpr">The next expr.</param>
    /// <returns></returns>
    /// <exception cref="InvalidDataException">Cannot parse your column expression</exception>
    private static string GetExpressionMemberName(this Expression expr, ref Expression nextExpr)
    {
        if (expr is MemberExpression memberExpr)
        {
            nextExpr = memberExpr.Expression;
            return memberExpr.Member.Name;
        }

        if (expr is BinaryExpression binaryExpr && expr.NodeType == ExpressionType.ArrayIndex)
        {
            var memberName = GetExpressionMemberName(binaryExpr.Left, ref nextExpr);
            if (string.IsNullOrEmpty(memberName))
            {
                throw new InvalidDataException("Cannot parse your column expression");
            }
            return $"{memberName}[{binaryExpr.Right}]";
        }
        return string.Empty;
    }
}