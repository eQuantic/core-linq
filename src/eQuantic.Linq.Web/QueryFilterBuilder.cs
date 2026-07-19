using System.Linq.Expressions;
using System.Text;
using eQuantic.Linq.Expressions;
using eQuantic.Linq.Web.Syntax;

namespace eQuantic.Linq.Web;

/// <summary>Entry points for building typed query-string filter expressions in code.</summary>
public static class QueryFilterBuilder
{
    /// <summary>Starts an empty filter builder for <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">Root entity being filtered.</typeparam>
    public static QueryFilterBuilder<T> For<T>() => new();

    /// <summary>Parses a filter expression (<c>total:gt(100),status:eq(Paid)</c>) into a builder.</summary>
    /// <typeparam name="T">Root entity being filtered.</typeparam>
    /// <param name="filter">Filter expression.</param>
    /// <param name="options">Query-string options; defaults apply when omitted.</param>
    public static QueryFilterBuilder<T> Parse<T>(string filter, QueryStringOptions? options = null) =>
        QueryFilterBuilder<T>.Parse(filter, options);
}

/// <summary>
/// Builds query-string filter expressions from typed member selectors and round-trips them:
/// <c>ToString()</c> produces the filter value (<c>total:gt(100),status:eq(Paid)</c>) that the
/// parser accepts, and <see cref="Parse"/> reads one back into an inspectable, mutable builder.
/// Covers comparisons, null tests, <c>in</c>/<c>nin</c> membership and <c>and</c>/<c>or</c>/<c>not</c>
/// composition; the reverse rejects constructs it cannot model as typed (collection quantifiers,
/// aggregates, method segments) — use <see cref="QueryFilter"/> to execute those.
/// </summary>
/// <typeparam name="T">Root entity being filtered.</typeparam>
public sealed class QueryFilterBuilder<T>
{
    private readonly Group _root = new(or: false);

    /// <summary>Whether the builder holds no clauses (its <c>ToString()</c> is empty).</summary>
    public bool IsEmpty => _root.Children.Count == 0;

    /// <summary>Adds a comparison clause: <c>path:op(value)</c>, combined with AND.</summary>
    /// <typeparam name="TMember">Member type.</typeparam>
    /// <param name="selector">Member selector, e.g. <c>o =&gt; o.Total</c>.</param>
    /// <param name="op">Comparison operator.</param>
    /// <param name="value">Value to compare against (formatted invariantly, quoted when needed).</param>
    public QueryFilterBuilder<T> Where<TMember>(Expression<Func<T, TMember>> selector, FilterOperator op, TMember value)
    {
        _root.Children.Add(new Comparison(Path(selector), op, QueryLiteral.Value(value)));
        return this;
    }

    /// <summary>Adds a <c>path:eq(null)</c> clause.</summary>
    /// <typeparam name="TMember">Member type.</typeparam>
    /// <param name="selector">Member selector.</param>
    public QueryFilterBuilder<T> WhereNull<TMember>(Expression<Func<T, TMember>> selector)
    {
        _root.Children.Add(new Comparison(Path(selector), FilterOperator.Equal, "null"));
        return this;
    }

    /// <summary>Adds a <c>path:neq(null)</c> clause.</summary>
    /// <typeparam name="TMember">Member type.</typeparam>
    /// <param name="selector">Member selector.</param>
    public QueryFilterBuilder<T> WhereNotNull<TMember>(Expression<Func<T, TMember>> selector)
    {
        _root.Children.Add(new Comparison(Path(selector), FilterOperator.NotEqual, "null"));
        return this;
    }

    /// <summary>Adds a <c>path:in(v1|v2|…)</c> membership clause.</summary>
    /// <typeparam name="TMember">Member type.</typeparam>
    /// <param name="selector">Member selector.</param>
    /// <param name="values">Accepted values.</param>
    public QueryFilterBuilder<T> WhereIn<TMember>(Expression<Func<T, TMember>> selector, params TMember[] values)
    {
        _root.Children.Add(new Membership(Path(selector), negated: false, values.Select(v => QueryLiteral.Value(v)).ToList()));
        return this;
    }

    /// <summary>Adds a <c>path:nin(v1|v2|…)</c> negated-membership clause.</summary>
    /// <typeparam name="TMember">Member type.</typeparam>
    /// <param name="selector">Member selector.</param>
    /// <param name="values">Rejected values.</param>
    public QueryFilterBuilder<T> WhereNotIn<TMember>(Expression<Func<T, TMember>> selector, params TMember[] values)
    {
        _root.Children.Add(new Membership(Path(selector), negated: true, values.Select(v => QueryLiteral.Value(v)).ToList()));
        return this;
    }

    /// <summary>Adds an <c>or(…)</c> group; the clauses added inside are combined with OR.</summary>
    /// <param name="build">Builds the grouped clauses.</param>
    public QueryFilterBuilder<T> Or(Action<QueryFilterBuilder<T>> build) => Nest(or: true, build);

    /// <summary>Adds an explicit <c>and(…)</c> group; the clauses added inside are combined with AND.</summary>
    /// <param name="build">Builds the grouped clauses.</param>
    public QueryFilterBuilder<T> And(Action<QueryFilterBuilder<T>> build) => Nest(or: false, build);

    /// <summary>Adds a <c>not(…)</c> negation around the clauses added inside.</summary>
    /// <param name="build">Builds the negated clauses.</param>
    public QueryFilterBuilder<T> Not(Action<QueryFilterBuilder<T>> build)
    {
        if (build is null)
        {
            throw new ArgumentNullException(nameof(build));
        }

        var sub = new QueryFilterBuilder<T>();
        build(sub);
        Node inner = sub._root.Children.Count == 1 ? sub._root.Children[0] : new Group(or: false, sub._root.Children);
        _root.Children.Add(new Negation(inner));
        return this;
    }

    /// <summary>The filter as a serializable expression model (through the real parser).</summary>
    /// <param name="options">Query-string options; defaults apply when omitted.</param>
    public ExpressionModel<T> ToModel(QueryStringOptions? options = null) =>
        QueryFilter.ParseModel<T>(RequireNonEmpty(), options);

    /// <summary>The filter as a typed predicate (through the real parser).</summary>
    /// <param name="options">Query-string options; defaults apply when omitted.</param>
    public Expression<Func<T, bool>> ToPredicate(QueryStringOptions? options = null) =>
        QueryFilter.Parse<T>(RequireNonEmpty(), options);

    /// <summary>The filter query-string value (empty when no clause was added).</summary>
    public override string ToString()
    {
        var builder = new StringBuilder();
        _root.Write(builder, root: true);
        return builder.ToString();
    }

    /// <summary>Parses a filter expression into a builder.</summary>
    /// <param name="filter">Filter expression.</param>
    /// <param name="options">Query-string options; the root parameter name is honored.</param>
    public static QueryFilterBuilder<T> Parse(string filter, QueryStringOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            throw new ArgumentException("Filter expression is empty.", nameof(filter));
        }

        var reader = new SyntaxReader(filter);
        var builder = new QueryFilterBuilder<T>();
        builder._root.Children.AddRange(ParseList(reader));
        reader.SkipWhitespace();
        if (!reader.End)
        {
            throw reader.Error("unexpected trailing characters");
        }

        return builder;
    }

    private QueryFilterBuilder<T> Nest(bool or, Action<QueryFilterBuilder<T>> build)
    {
        if (build is null)
        {
            throw new ArgumentNullException(nameof(build));
        }

        var sub = new QueryFilterBuilder<T>();
        build(sub);
        _root.Children.Add(new Group(or, sub._root.Children));
        return this;
    }

    private string RequireNonEmpty() =>
        IsEmpty ? throw new InvalidOperationException("The filter builder has no clauses.") : ToString();

    private static string Path<TMember>(Expression<Func<T, TMember>> selector) =>
        QueryLiteral.Path(selector ?? throw new ArgumentNullException(nameof(selector)));

    // ---------------------------------------------------------------- reverse parsing

    private static List<Node> ParseList(SyntaxReader reader)
    {
        var nodes = new List<Node> { ParseNode(reader) };
        while (reader.TryConsume(','))
        {
            nodes.Add(ParseNode(reader));
        }

        return nodes;
    }

    private static Node ParseNode(SyntaxReader reader)
    {
        reader.SkipWhitespace();
        var identifier = reader.ReadIdentifier();
        var lower = identifier.ToLowerInvariant();

        if (lower is "and" or "or" or "not" && !reader.End && reader.Peek() == '(')
        {
            reader.Expect('(');
            var children = ParseList(reader);
            reader.Expect(')');

            if (lower == "not")
            {
                return new Negation(children.Count == 1 ? children[0] : new Group(or: false, children));
            }

            return new Group(or: lower == "or", children);
        }

        var path = ReadPath(reader, identifier);
        reader.Expect(':');
        return ReadComparison(reader, path);
    }

    private static string ReadPath(SyntaxReader reader, string first)
    {
        var path = first;
        while (reader.TryConsume('.'))
        {
            var segment = reader.ReadIdentifier();
            if (!reader.End && reader.Peek() == '(')
            {
                throw reader.Error("method and aggregate segments cannot be represented as a typed builder; use QueryFilter to execute them");
            }

            path += "." + segment;
        }

        return path;
    }

    private static Node ReadComparison(SyntaxReader reader, string path)
    {
        reader.SkipWhitespace();
        var mark = reader.Position;

        if (!reader.End && char.IsLetter(reader.Peek()))
        {
            var opToken = reader.ReadIdentifier();
            if (reader.TryConsume('('))
            {
                var lower = opToken.ToLowerInvariant();
                if (lower is "in" or "nin")
                {
                    var values = new List<string>();
                    do
                    {
                        values.Add(TokenOf(reader.ReadValueUntilPipeOrParenthesis()));
                    }
                    while (reader.TryConsume('|'));

                    reader.Expect(')');
                    return new Membership(path, negated: lower == "nin", values);
                }

                if (lower is "any" or "all")
                {
                    throw reader.Error("collection quantifiers (any/all) cannot be represented as a typed builder; use QueryFilter to execute them");
                }

                if (FilterOperatorTokens.TryFromToken(lower, out var op))
                {
                    var value = TokenOf(reader.ReadValue());
                    reader.Expect(')');
                    return new Comparison(path, op, value);
                }

                throw reader.Error($"unknown operator '{opToken}'");
            }

            reader.Seek(mark);
        }

        // Shorthand: path:value → equality.
        return new Comparison(path, FilterOperator.Equal, TokenOf(reader.ReadValue()));
    }

    private static string TokenOf(string? decoded) => decoded is null ? "null" : QueryLiteral.Quote(decoded);

    // ---------------------------------------------------------------- node model

    private abstract class Node
    {
        public abstract void Write(StringBuilder builder, bool root);
    }

    private sealed class Comparison(string path, FilterOperator op, string valueToken) : Node
    {
        public override void Write(StringBuilder builder, bool root) =>
            builder.Append(path).Append(':').Append(op.ToToken()).Append('(').Append(valueToken).Append(')');
    }

    private sealed class Membership(string path, bool negated, IReadOnlyList<string> valueTokens) : Node
    {
        public override void Write(StringBuilder builder, bool root)
        {
            builder.Append(path).Append(':').Append(negated ? "nin" : "in").Append('(');
            for (var i = 0; i < valueTokens.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append('|');
                }

                builder.Append(valueTokens[i]);
            }

            builder.Append(')');
        }
    }

    private sealed class Group(bool or, List<Node> children) : Node
    {
        public List<Node> Children { get; } = children;

        public Group(bool or) : this(or, [])
        {
        }

        public override void Write(StringBuilder builder, bool root)
        {
            var wrap = !root || or;
            if (wrap)
            {
                builder.Append(or ? "or" : "and").Append('(');
            }

            for (var i = 0; i < Children.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                Children[i].Write(builder, root: false);
            }

            if (wrap)
            {
                builder.Append(')');
            }
        }
    }

    private sealed class Negation(Node inner) : Node
    {
        public override void Write(StringBuilder builder, bool root)
        {
            builder.Append("not(");
            inner.Write(builder, root: false);
            builder.Append(')');
        }
    }
}
