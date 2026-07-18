using System.Linq.Expressions;
using eQuantic.Linq.Expressions.Metadata;
using eQuantic.Linq.Expressions.Nodes;

namespace eQuantic.Linq.Web.Syntax;

/// <summary>
/// Recursive-descent parser for the filter syntax. Produces lean <see cref="ExpressionNode"/> trees —
/// member names and raw string constants only — leaving every typing decision to the expression
/// engine's root-anchored inference.
///
/// Grammar (informal):
/// <code>
///   filters     := filter (',' filter)*                            // top-level comma = AND
///   filter      := 'and(' filters ')' | 'or(' filters ')' | 'not(' filters ')' | comparison
///   comparison  := path ':' ( op '(' value ')' | 'any(' filters? ')' | 'all(' filters ')'
///                 | 'in(' value ('|' value)* ')' | 'nin(' … ')' | shorthandValue )
///   path        := segment ('.' segment)*
///   segment     := identifier | identifier '(' … ')'               // method / aggregate segments
///   op          := eq | neq | ct | nct | sw | ew | gt | gte | lt | lte
/// </code>
/// </summary>
internal static class FilterSyntaxParser
{
    /// <summary>Parses a full filter expression into a boolean body node over the root parameter.</summary>
    public static ExpressionNode ParseBody(string text, string rootParameterName)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Filter expression is empty.", nameof(text));
        }

        var reader = new SyntaxReader(text);
        var items = ParseFilterItems(reader, rootParameterName, depth: 0);

        if (!reader.End)
        {
            throw reader.Error("unexpected trailing content");
        }

        return Combine(items, ExpressionType.AndAlso);
    }

    // ---------------------------------------------------------------- filters

    private static List<ExpressionNode> ParseFilterItems(SyntaxReader reader, string parameterName, int depth)
    {
        var items = new List<ExpressionNode> { ParseFilter(reader, parameterName, depth) };
        while (reader.TryConsume(','))
        {
            items.Add(ParseFilter(reader, parameterName, depth));
        }

        return items;
    }

    private static ExpressionNode ParseFilter(SyntaxReader reader, string parameterName, int depth)
    {
        reader.SkipWhitespace();
        var mark = reader.Position;

        var identifier = reader.ReadIdentifier();
        var lower = identifier.ToLowerInvariant();

        if (lower is "and" or "or" or "not" && reader.TryConsume('('))
        {
            var children = ParseFilterItems(reader, parameterName, depth);
            reader.Expect(')');

            return lower switch
            {
                "and" => Combine(children, ExpressionType.AndAlso),
                "or" => Combine(children, ExpressionType.OrElse),
                _ => Not(Combine(children, ExpressionType.AndAlso)),
            };
        }

        reader.Seek(mark);
        return ParseComparison(reader, parameterName, depth);
    }

    private static ExpressionNode ParseComparison(SyntaxReader reader, string parameterName, int depth)
    {
        var path = ParsePath(reader, parameterName, depth);
        reader.Expect(':');

        var mark = reader.Position;
        string? op = null;
        var hasParenthesis = false;

        reader.SkipWhitespace();
        if (!reader.End && (char.IsLetter(reader.Peek()) || reader.Peek() == '_'))
        {
            op = reader.ReadIdentifier();
            hasParenthesis = reader.TryConsume('(');
        }

        if (!hasParenthesis)
        {
            // Shorthand: path:value → equality (classic simplified format).
            reader.Seek(mark);
            var raw = reader.ReadValue();
            return Comparison(path, "eq", raw, reader);
        }

        var lowerOp = op!.ToLowerInvariant();

        switch (lowerOp)
        {
            case "any" or "all":
            {
                if (lowerOp == "any" && reader.TryConsume(')'))
                {
                    return Call(path, "Any");
                }

                var elementParameter = ParameterNameFor(depth + 1);
                var inner = ParseFilterItems(reader, elementParameter, depth + 1);
                reader.Expect(')');

                return Call(path, lowerOp == "any" ? "Any" : "All", Lambda(elementParameter, Combine(inner, ExpressionType.AndAlso)));
            }

            case "in" or "nin":
            {
                var values = ReadPipeSeparatedValues(reader);
                reader.Expect(')');

                if (values.Count == 0)
                {
                    throw reader.Error($"'{lowerOp}' requires at least one value");
                }

                var equalities = values
                    .Select(value => (ExpressionNode)Binary(ExpressionType.Equal, path, Constant(value)))
                    .ToList();

                var membership = Combine(equalities, ExpressionType.OrElse);
                return lowerOp == "in" ? membership : Not(membership);
            }

            default:
            {
                var value = reader.ReadValue();
                reader.Expect(')');
                return Comparison(path, lowerOp, value, reader);
            }
        }
    }

    private static List<string?> ReadPipeSeparatedValues(SyntaxReader reader)
    {
        var values = new List<string?>();
        reader.SkipWhitespace();

        if (!reader.End && reader.Peek() == ')')
        {
            return values;
        }

        while (true)
        {
            var raw = reader.ReadValueUntilPipeOrParenthesis();
            values.Add(raw);

            if (!reader.TryConsume('|'))
            {
                return values;
            }
        }
    }

    private static ExpressionNode Comparison(ExpressionNode path, string op, string? value, SyntaxReader reader) => op switch
    {
        "eq" => Binary(ExpressionType.Equal, path, Constant(value)),
        "neq" => Binary(ExpressionType.NotEqual, path, Constant(value)),
        "gt" => Binary(ExpressionType.GreaterThan, path, Constant(value)),
        "gte" => Binary(ExpressionType.GreaterThanOrEqual, path, Constant(value)),
        "lt" => Binary(ExpressionType.LessThan, path, Constant(value)),
        "lte" => Binary(ExpressionType.LessThanOrEqual, path, Constant(value)),
        "ct" => Call(path, "Contains", Constant(value)),
        "nct" => Not(Call(path, "Contains", Constant(value))),
        "sw" => Call(path, "StartsWith", Constant(value)),
        "ew" => Call(path, "EndsWith", Constant(value)),
        _ => throw reader.Error(
            $"unknown operator '{op}' (expected: eq, neq, ct, nct, sw, ew, gt, gte, lt, lte, in, nin, any, all)"),
    };

    // ---------------------------------------------------------------- paths

    /// <summary>Parses a navigation path (with optional method/aggregate segments) over the given parameter.</summary>
    public static ExpressionNode ParsePath(SyntaxReader reader, string parameterName, int depth)
    {
        ExpressionNode current = new ParameterNode { Name = parameterName };

        while (true)
        {
            var name = reader.ReadIdentifier();

            current = reader.TryConsume('(')
                ? ParseMethodSegment(reader, current, name, depth)
                : new MemberNode
                {
                    Member = new MemberRef { Name = name },
                    Expression = current,
                };

            if (!reader.TryConsume('.'))
            {
                return current;
            }
        }
    }

    private static ExpressionNode ParseMethodSegment(SyntaxReader reader, ExpressionNode target, string name, int depth)
    {
        var lower = name.ToLowerInvariant();

        switch (lower)
        {
            // Aggregates over collections: selector is a path relative to the element.
            case "sum" or "min" or "max" or "average" or "avg":
            {
                var methodName = lower == "avg" ? "Average" : Capitalize(lower);

                if (reader.TryConsume(')'))
                {
                    return Call(target, methodName);
                }

                var elementParameter = ParameterNameFor(depth + 1);
                var body = ParsePath(reader, elementParameter, depth + 1);
                reader.Expect(')');

                return Call(target, methodName, Lambda(elementParameter, body));
            }

            // count() / count(filters) / any(filters) / all(filters) as path segments.
            case "count" or "any" or "all":
            {
                var methodName = Capitalize(lower);

                if (reader.TryConsume(')'))
                {
                    if (lower == "all")
                    {
                        throw reader.Error("'all' requires a predicate");
                    }

                    return Call(target, methodName);
                }

                var elementParameter = ParameterNameFor(depth + 1);
                var inner = ParseFilterItems(reader, elementParameter, depth + 1);
                reader.Expect(')');

                return Call(target, methodName, Lambda(elementParameter, Combine(inner, ExpressionType.AndAlso)));
            }

            // Any other segment: instance method with literal arguments (toLower(), substring(0,3), …).
            default:
            {
                var arguments = new List<ExpressionNode>();

                if (!reader.TryConsume(')'))
                {
                    do
                    {
                        arguments.Add(Constant(reader.ReadValue()));
                    }
                    while (reader.TryConsume(','));

                    reader.Expect(')');
                }

                return new MethodCallNode
                {
                    Method = new MethodRef { Name = name },
                    Object = target,
                    Arguments = arguments.Count == 0 ? null : arguments,
                };
            }
        }
    }

    // ---------------------------------------------------------------- node helpers

    internal static ExpressionNode Combine(List<ExpressionNode> nodes, ExpressionType op)
    {
        var result = nodes[0];
        for (var i = 1; i < nodes.Count; i++)
        {
            result = Binary(op, result, nodes[i]);
        }

        return result;
    }

    private static BinaryNode Binary(ExpressionType op, ExpressionNode left, ExpressionNode right) => new()
    {
        NodeType = op,
        Left = left,
        Right = right,
    };

    private static UnaryNode Not(ExpressionNode operand) => new()
    {
        NodeType = ExpressionType.Not,
        Operand = operand,
    };

    private static ConstantNode Constant(string? value) => new() { Value = value };

    private static MethodCallNode Call(ExpressionNode target, string methodName, params ExpressionNode[] arguments) => new()
    {
        Method = new MethodRef { Name = methodName },
        Object = target,
        Arguments = arguments.Length == 0 ? null : arguments.ToList(),
    };

    private static LambdaNode Lambda(string parameterName, ExpressionNode body) => new()
    {
        Parameters = [new ParameterNode { Name = parameterName }],
        Body = body,
    };

    private static string ParameterNameFor(int depth) => "x" + depth;

    private static string Capitalize(string value) =>
        char.ToUpperInvariant(value[0]) + value.Substring(1);
}
