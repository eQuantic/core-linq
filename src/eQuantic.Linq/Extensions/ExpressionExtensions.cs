using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using eQuantic.Linq.Expressions;

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
    /// <param name="useColumnFallback">Use column fallback</param>
    /// <returns></returns>
    public static string GetColumnName(this Expression expression, bool useColumnFallback = false)
    {
        if (expression == null)
        {
            throw new ArgumentNullException(nameof(expression));
        }

        var sb = new StringBuilder();

        while (true)
        {
            var piece = expression.GetExpressionMemberName(ref expression, useColumnFallback);
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

    public static LambdaExpression GetColumnExpression<TEntity>(this string columnName, bool useColumnFallback = false)
    {
        var properties = EntityBuilder.GetProperties<TEntity>(columnName, useColumnFallback);
        var keyType = properties.Last().PropertyType;
        var builder = LambdaBuilderFactory.Current.Create(typeof(TEntity), keyType);
        return builder.BuildLambda(properties.ToArray());
    }

    /// <summary>
    /// Gets the name of the expression member.
    /// </summary>
    /// <param name="expr">The expr.</param>
    /// <param name="nextExpr">The next expr.</param>
    /// <param name="useColumnFallback">Use column fallback</param>
    /// <returns></returns>
    /// <exception cref="InvalidDataException">Cannot parse your column expression</exception>
    private static string GetExpressionMemberName(this Expression expr, ref Expression nextExpr, bool useColumnFallback = false)
    {
        if (expr is MemberExpression memberExpr)
        {
            nextExpr = memberExpr.Expression;
            return memberExpr.GetPropertyNameFromMemberExpression(useColumnFallback);
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
            
        if (expr is LambdaExpression lambdaExpr)
        {
            var memberExpression = lambdaExpr.Body as MemberExpression ?? (MemberExpression)((UnaryExpression)lambdaExpr.Body).Operand;
            nextExpr = memberExpression.Expression;
            return memberExpression.GetPropertyNameFromMemberExpression(useColumnFallback);
        }
        return string.Empty;
    }

    private static string GetPropertyNameFromMemberExpression(this MemberExpression memberExpr, bool useColumnFallback = false)
    {
        if (!useColumnFallback) return memberExpr.Member.Name;
        var columnAttribute = memberExpr.Member.GetCustomAttribute<ColumnAttribute>();
        return string.IsNullOrEmpty(columnAttribute?.Name) ? memberExpr.Member.Name : columnAttribute.Name;

    }
}