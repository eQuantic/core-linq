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
/// Clauses fold left to right — <c>Where(a).And(b).Or(c)</c> is <c>(a AND b) OR c</c> — and
/// consecutive same-operator clauses flatten (a pure AND chain emits a comma list); use the
/// <c>And</c>/<c>Or</c>/<c>Not</c> group overloads for explicit nesting. Covers comparisons, null
/// tests, <c>in</c>/<c>nin</c> membership and <c>and</c>/<c>or</c>/<c>not</c>; the reverse rejects
/// constructs it cannot model as typed (collection quantifiers, aggregates, method segments) —
/// use <see cref="QueryFilter"/> to execute those.
/// </summary>
/// <typeparam name="T">Root entity being filtered.</typeparam>
public sealed class QueryFilterBuilder<T>
{
    private Node? _root;

    /// <summary>Whether the builder holds no clauses (its <c>ToString()</c> is empty).</summary>
    public bool IsEmpty => _root is null;

    // ---------------------------------------------------------------- AND clauses

    /// <summary>Adds a comparison clause combined with AND: <c>path:op(value)</c>.</summary>
    /// <typeparam name="TMember">Member type.</typeparam>
    /// <param name="selector">Member selector, e.g. <c>o =&gt; o.Total</c>.</param>
    /// <param name="op">Comparison operator.</param>
    /// <param name="value">Value to compare against (formatted invariantly, quoted when needed).</param>
    public QueryFilterBuilder<T> Where<TMember>(Expression<Func<T, TMember>> selector, FilterOperator op, TMember value) =>
        Fold(new Comparison(Path(selector), op, QueryLiteral.Value(value)), or: false);

    /// <summary>Adds a comparison clause (AND) by path string; the path is used verbatim.</summary>
    /// <param name="path">Member path (may carry method/aggregate segments the lambda form cannot express).</param>
    /// <param name="op">Comparison operator.</param>
    /// <param name="value">Value to compare against.</param>
    public QueryFilterBuilder<T> Where(string path, FilterOperator op, object? value) =>
        Fold(new Comparison(QueryLiteral.RawPath(path), op, QueryLiteral.Value(value)), or: false);

    /// <summary>Adds a comparison clause combined with AND (reads naturally after <see cref="Where{TMember}"/>).</summary>
    /// <typeparam name="TMember">Member type.</typeparam>
    /// <param name="selector">Member selector.</param>
    /// <param name="op">Comparison operator.</param>
    /// <param name="value">Value to compare against.</param>
    public QueryFilterBuilder<T> And<TMember>(Expression<Func<T, TMember>> selector, FilterOperator op, TMember value) =>
        Where(selector, op, value);

    /// <summary>Adds a comparison clause combined with AND by path string.</summary>
    /// <param name="path">Member path (used verbatim).</param>
    /// <param name="op">Comparison operator.</param>
    /// <param name="value">Value to compare against.</param>
    public QueryFilterBuilder<T> And(string path, FilterOperator op, object? value) =>
        Where(path, op, value);

    // ---------------------------------------------------------------- OR clauses

    /// <summary>Adds a comparison clause combined with OR: everything built so far <c>OR</c> this clause.</summary>
    /// <typeparam name="TMember">Member type.</typeparam>
    /// <param name="selector">Member selector.</param>
    /// <param name="op">Comparison operator.</param>
    /// <param name="value">Value to compare against.</param>
    public QueryFilterBuilder<T> Or<TMember>(Expression<Func<T, TMember>> selector, FilterOperator op, TMember value) =>
        Fold(new Comparison(Path(selector), op, QueryLiteral.Value(value)), or: true);

    /// <summary>Adds a comparison clause combined with OR by path string.</summary>
    /// <param name="path">Member path (used verbatim).</param>
    /// <param name="op">Comparison operator.</param>
    /// <param name="value">Value to compare against.</param>
    public QueryFilterBuilder<T> Or(string path, FilterOperator op, object? value) =>
        Fold(new Comparison(QueryLiteral.RawPath(path), op, QueryLiteral.Value(value)), or: true);

    // ---------------------------------------------------------------- null / membership (AND)

    /// <summary>Adds a <c>path:eq(null)</c> clause combined with AND.</summary>
    /// <typeparam name="TMember">Member type.</typeparam>
    /// <param name="selector">Member selector.</param>
    public QueryFilterBuilder<T> WhereNull<TMember>(Expression<Func<T, TMember>> selector) =>
        Fold(new Comparison(Path(selector), FilterOperator.Equal, "null"), or: false);

    /// <summary>Adds a <c>path:eq(null)</c> clause combined with AND by path string.</summary>
    /// <param name="path">Member path (used verbatim).</param>
    public QueryFilterBuilder<T> WhereNull(string path) =>
        Fold(new Comparison(QueryLiteral.RawPath(path), FilterOperator.Equal, "null"), or: false);

    /// <summary>Adds a <c>path:neq(null)</c> clause combined with AND.</summary>
    /// <typeparam name="TMember">Member type.</typeparam>
    /// <param name="selector">Member selector.</param>
    public QueryFilterBuilder<T> WhereNotNull<TMember>(Expression<Func<T, TMember>> selector) =>
        Fold(new Comparison(Path(selector), FilterOperator.NotEqual, "null"), or: false);

    /// <summary>Adds a <c>path:neq(null)</c> clause combined with AND by path string.</summary>
    /// <param name="path">Member path (used verbatim).</param>
    public QueryFilterBuilder<T> WhereNotNull(string path) =>
        Fold(new Comparison(QueryLiteral.RawPath(path), FilterOperator.NotEqual, "null"), or: false);

    /// <summary>Adds a <c>path:in(v1|v2|…)</c> membership clause combined with AND.</summary>
    /// <typeparam name="TMember">Member type.</typeparam>
    /// <param name="selector">Member selector.</param>
    /// <param name="values">Accepted values.</param>
    public QueryFilterBuilder<T> WhereIn<TMember>(Expression<Func<T, TMember>> selector, params TMember[] values) =>
        Fold(new Membership(Path(selector), negated: false, values.Select(v => QueryLiteral.Value(v)).ToList()), or: false);

    /// <summary>Adds a <c>path:in(v1|v2|…)</c> membership clause combined with AND by path string.</summary>
    /// <param name="path">Member path (used verbatim).</param>
    /// <param name="values">Accepted values.</param>
    public QueryFilterBuilder<T> WhereIn(string path, params object?[] values) =>
        Fold(new Membership(QueryLiteral.RawPath(path), negated: false, values.Select(QueryLiteral.Value).ToList()), or: false);

    /// <summary>Adds a <c>path:nin(v1|v2|…)</c> negated-membership clause combined with AND.</summary>
    /// <typeparam name="TMember">Member type.</typeparam>
    /// <param name="selector">Member selector.</param>
    /// <param name="values">Rejected values.</param>
    public QueryFilterBuilder<T> WhereNotIn<TMember>(Expression<Func<T, TMember>> selector, params TMember[] values) =>
        Fold(new Membership(Path(selector), negated: true, values.Select(v => QueryLiteral.Value(v)).ToList()), or: false);

    /// <summary>Adds a <c>path:nin(v1|v2|…)</c> negated-membership clause combined with AND by path string.</summary>
    /// <param name="path">Member path (used verbatim).</param>
    /// <param name="values">Rejected values.</param>
    public QueryFilterBuilder<T> WhereNotIn(string path, params object?[] values) =>
        Fold(new Membership(QueryLiteral.RawPath(path), negated: true, values.Select(QueryLiteral.Value).ToList()), or: false);

    // ---------------------------------------------------------------- groups

    /// <summary>Combines everything built so far with OR against a nested group of clauses.</summary>
    /// <param name="build">Builds the grouped clauses.</param>
    public QueryFilterBuilder<T> Or(Action<QueryFilterBuilder<T>> build) => FoldGroup(build, or: true);

    /// <summary>Combines everything built so far with AND against a nested group of clauses.</summary>
    /// <param name="build">Builds the grouped clauses.</param>
    public QueryFilterBuilder<T> And(Action<QueryFilterBuilder<T>> build) => FoldGroup(build, or: false);

    /// <summary>Adds (with AND) a <c>not(…)</c> negation around the clauses added inside.</summary>
    /// <param name="build">Builds the negated clauses.</param>
    public QueryFilterBuilder<T> Not(Action<QueryFilterBuilder<T>> build)
    {
        if (build is null)
        {
            throw new ArgumentNullException(nameof(build));
        }

        var sub = new QueryFilterBuilder<T>();
        build(sub);
        return sub._root is null ? this : Fold(new Negation(sub._root), or: false);
    }

    // ---------------------------------------------------------------- output

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
        if (_root is null)
        {
            return string.Empty;
        }

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
        var nodes = ParseList(reader);
        reader.SkipWhitespace();
        if (!reader.End)
        {
            throw reader.Error("unexpected trailing characters");
        }

        return new QueryFilterBuilder<T> { _root = nodes.Count == 1 ? nodes[0] : new Group(or: false, nodes) };
    }

    // ---------------------------------------------------------------- folding

    private QueryFilterBuilder<T> Fold(Node node, bool or)
    {
        if (_root is null)
        {
            _root = node;
        }
        else if (_root is Group group && group.Or == or)
        {
            group.Children.Add(node);
        }
        else
        {
            _root = new Group(or, [_root, node]);
        }

        return this;
    }

    private QueryFilterBuilder<T> FoldGroup(Action<QueryFilterBuilder<T>> build, bool or)
    {
        if (build is null)
        {
            throw new ArgumentNullException(nameof(build));
        }

        var sub = new QueryFilterBuilder<T>();
        build(sub);
        return sub._root is null ? this : Fold(sub._root, or);
    }

    private string RequireNonEmpty() =>
        _root is null ? throw new InvalidOperationException("The filter builder has no clauses.") : ToString();

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
        public bool Or { get; } = or;

        public List<Node> Children { get; } = children;

        public override void Write(StringBuilder builder, bool root)
        {
            var wrap = !root || Or;
            if (wrap)
            {
                builder.Append(Or ? "or" : "and").Append('(');
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
