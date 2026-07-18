using System.Linq.Expressions;
using System.Text;
using eQuantic.Linq.Expressions;
using eQuantic.Linq.Expressions.Metadata;
using eQuantic.Linq.Expressions.Nodes;

namespace eQuantic.Linq.Web.Syntax;

/// <summary>
/// Builds anonymous-type projections from <c>select</c> expressions, e.g.
/// <c>select=id,customerName=customer.name,items.count()</c>. Property types are discovered by a
/// first decode pass; the projection then materializes through the engine's emitted anonymous types.
/// </summary>
internal static class SelectSyntaxParser
{
    public static LambdaExpression Build(Type rootType, string select, QueryStringOptions options)
    {
        if (string.IsNullOrWhiteSpace(select))
        {
            throw new ArgumentException("Select expression is empty.", nameof(select));
        }

        var reader = new SyntaxReader(select);
        var entries = new List<(string? Alias, ExpressionNode Node)>();

        while (true)
        {
            reader.SkipWhitespace();
            var mark = reader.Position;

            // Optional alias: `alias=path`.
            string? alias = null;
            if (!reader.End && (char.IsLetter(reader.Peek()) || reader.Peek() == '_'))
            {
                var identifier = reader.ReadIdentifier();
                if (reader.TryConsume('='))
                {
                    alias = identifier;
                }
                else
                {
                    reader.Seek(mark);
                }
            }

            entries.Add((alias, FilterSyntaxParser.ParsePath(reader, options.RootParameterName, depth: 0)));

            if (!reader.TryConsume(','))
            {
                break;
            }
        }

        if (!reader.End)
        {
            throw reader.Error("unexpected trailing content");
        }

        var serializer = options.Serializer;
        var resolver = serializer.Options.TypeResolver;

        var properties = new List<AnonymousTypeProperty>(entries.Count);
        var arguments = new List<ExpressionNode>(entries.Count);
        var members = new List<MemberRef>(entries.Count);
        var usedNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (alias, node) in entries)
        {
            // First pass: decode the path alone to discover its result type.
            var probe = new ExpressionModel
            {
                Parameters = [new ParameterNode { Name = options.RootParameterName }],
                Body = node,
            };
            var lambda = serializer.ToLambda(probe, rootType);

            var name = Unique(alias ?? DefaultName(node), usedNames);

            properties.Add(new AnonymousTypeProperty(name, resolver.GetTypeRef(lambda.ReturnType)));
            arguments.Add(node);
            members.Add(new MemberRef { Name = name });
        }

        var projection = new ExpressionModel
        {
            Parameters = [new ParameterNode { Name = options.RootParameterName }],
            Body = new NewNode
            {
                Type = new TypeRef { IsAnonymous = true, Properties = properties },
                Arguments = arguments,
                Members = members,
            },
        };

        return serializer.ToLambda(projection, rootType);
    }

    private static string Unique(string name, HashSet<string> used)
    {
        if (used.Add(name))
        {
            return name;
        }

        var index = 2;
        while (!used.Add(name + index))
        {
            index++;
        }

        return name + index;
    }

    /// <summary>Default property name: Pascal-cased path segments concatenated (<c>customer.name</c> → <c>CustomerName</c>).</summary>
    private static string DefaultName(ExpressionNode node)
    {
        var builder = new StringBuilder();
        Append(node, builder);
        return builder.Length == 0 ? "Value" : builder.ToString();

        static void Append(ExpressionNode current, StringBuilder builder)
        {
            switch (current)
            {
                case MemberNode member:
                    if (member.Expression is not null)
                    {
                        Append(member.Expression, builder);
                    }

                    AppendPascal(builder, member.Member.Name);
                    break;

                case MethodCallNode call:
                    if (call.Object is not null)
                    {
                        Append(call.Object, builder);
                    }

                    AppendPascal(builder, call.Method.Name);
                    break;
            }
        }

        static void AppendPascal(StringBuilder builder, string name)
        {
            if (name.Length == 0)
            {
                return;
            }

            builder.Append(char.ToUpperInvariant(name[0]));
            if (name.Length > 1)
            {
                builder.Append(name, 1, name.Length - 1);
            }
        }
    }
}
