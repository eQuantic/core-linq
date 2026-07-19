using System.Globalization;
using System.Linq.Expressions;
using eQuantic.Linq.Expressions;

namespace eQuantic.Linq.Web;

/// <summary>
/// Formats values and member paths into query-string grammar tokens, and back. Shared by the
/// typed query builders so their <c>ToString()</c> output is exactly what the parser accepts.
/// </summary>
internal static class QueryLiteral
{
    // Characters that carry grammar meaning at the top level, forcing a value to be quoted.
    private static readonly char[] Reserved = [',', '(', ')', '|', '\''];

    /// <summary>The camelCase member path of a selector (e.g. <c>o => o.Customer.Name</c> → <c>customer.name</c>).</summary>
    public static string Path(LambdaExpression selector)
    {
        var path = selector.GetMemberPath();
        return string.Join(".", path.Split('.').Select(CamelCase));
    }

    /// <summary>Validates and trims a caller-supplied path, used verbatim (may carry method/aggregate segments).</summary>
    public static string RawPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is empty.", nameof(path));
        }

        return path.Trim();
    }

    /// <summary>Formats a value as a grammar literal: <c>null</c> keyword, invariant text, quoted when needed.</summary>
    public static string Value(object? value) => value switch
    {
        null => "null",
        bool b => b ? "true" : "false",
        string s => Quote(s),
        Enum e => Quote(e.ToString()),
        IFormattable f => Quote(f.ToString(null, CultureInfo.InvariantCulture)),
        _ => Quote(value.ToString() ?? string.Empty),
    };

    /// <summary>Wraps a value in single quotes (escaping <c>'</c> as <c>''</c>) only when the grammar requires it.</summary>
    public static string Quote(string value)
    {
        var needsQuoting = value.Length == 0
            || value != value.Trim()
            || value.Equals("null", StringComparison.OrdinalIgnoreCase)
            || value.IndexOfAny(Reserved) >= 0;

        if (!needsQuoting)
        {
            return value;
        }

        return "'" + value.Replace("'", "''") + "'";
    }

    private static string CamelCase(string name) =>
        name.Length > 0 && char.IsUpper(name[0])
            ? char.ToLowerInvariant(name[0]) + name.Substring(1)
            : name;
}
