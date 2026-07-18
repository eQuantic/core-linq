using System.Text.Json;
using eQuantic.Linq.Expressions.Resolution;

namespace eQuantic.Linq.Expressions;

/// <summary>Behavioral options of <see cref="ExpressionSerializer"/>.</summary>
public sealed class ExpressionSerializerOptions
{
    /// <summary>Type naming/resolution strategy. Defaults to <see cref="DefaultTypeResolver"/> with a permissive policy.</summary>
    public ITypeResolver TypeResolver { get; set; } = new DefaultTypeResolver();

    /// <summary>
    /// Evaluates every sub-tree that does not depend on lambda parameters before serializing,
    /// folding compiler-generated closures and captured variables into portable constants. Enabled by default;
    /// disable only when serializing hand-built trees guaranteed to contain no captured state.
    /// </summary>
    public bool EnablePartialEvaluation { get; set; } = true;

    /// <summary>
    /// Detects <see cref="IQueryable"/> constants (query roots) and serializes them as re-bindable
    /// <see cref="Nodes.QueryRootNode"/> placeholders. Enabled by default.
    /// </summary>
    public bool DetectQueryableRoots { get; set; } = true;

    /// <summary>Supplies the local <see cref="IQueryable"/> for each query-root element type when rebuilding serialized queries.</summary>
    public Func<Type, IQueryable?>? QueryRootProvider { get; set; }

    /// <summary>
    /// Static classes probed (in order) when an inferred payload calls an extension method in instance
    /// style (e.g. <c>items.Any(...)</c>). Defaults to <see cref="Queryable"/> then <see cref="Enumerable"/>.
    /// </summary>
    public IList<Type> ExtensionMethodTypes { get; } = new List<Type> { typeof(Queryable), typeof(Enumerable) };

    /// <summary>Writes indented JSON. Defaults to compact output.</summary>
    public bool WriteIndented { get; set; }

    /// <summary>Last-chance hook to customize the underlying <see cref="JsonSerializerOptions"/>.</summary>
    public Action<JsonSerializerOptions>? ConfigureJson { get; set; }

    /// <summary>Maximum node depth accepted when rebuilding expressions (hostile-payload guard). Defaults to 1024.</summary>
    public int MaxDepth { get; set; } = 1024;

    /// <summary>Maximum total node count accepted when rebuilding expressions (hostile-payload guard). Defaults to 100 000.</summary>
    public int MaxNodes { get; set; } = 100_000;

    /// <summary>
    /// Optional gate applied to every method a payload resolves (calls, element initializers,
    /// operators, switch comparisons). Return <see langword="false"/> to reject the method.
    /// </summary>
    public Func<System.Reflection.MethodInfo, bool>? MethodFilter { get; set; }

    /// <summary>Format provider used to coerce string constants into member types (dates, decimals…). Defaults to invariant.</summary>
    public IFormatProvider FormatProvider { get; set; } = System.Globalization.CultureInfo.InvariantCulture;
}
