using System.Collections;
using System.Reflection;
using System.Text;

namespace eQuantic.Linq.Web.Documentation;

/// <summary>Kind of query-string parameter described by <see cref="EntityQueryDocumentation"/>.</summary>
public enum EntityQueryParameterKind
{
    /// <summary>Filter expressions (<c>filter</c>).</summary>
    Filter,

    /// <summary>Ordering expressions (<c>orderBy</c>).</summary>
    OrderBy,

    /// <summary>Elements to skip (<c>skip</c>).</summary>
    Skip,

    /// <summary>Elements to take (<c>take</c>).</summary>
    Take,

    /// <summary>Projection paths (<c>select</c>).</summary>
    Select,
}

/// <summary>Documentation of one query-string parameter of the entity-query surface.</summary>
public sealed class EntityQueryParameterDocumentation
{
    internal EntityQueryParameterDocumentation(
        string name, EntityQueryParameterKind kind, string description, string? example, bool isInteger, bool repeatable)
    {
        Name = name;
        Kind = kind;
        Description = description;
        Example = example;
        IsInteger = isInteger;
        Repeatable = repeatable;
    }

    /// <summary>Query-string key (honors <see cref="QueryStringOptions"/> customization).</summary>
    public string Name { get; }

    /// <summary>Which part of the query surface this parameter drives.</summary>
    public EntityQueryParameterKind Kind { get; }

    /// <summary>Human-oriented description (markdown) including syntax and entity-specific paths.</summary>
    public string Description { get; }

    /// <summary>Example value built from the entity's actual members.</summary>
    public string? Example { get; }

    /// <summary>Whether the parameter is a non-negative integer (paging) rather than an expression string.</summary>
    public bool IsInteger { get; }

    /// <summary>Whether the parameter may appear multiple times (values combine with AND).</summary>
    public bool Repeatable { get; }
}

/// <summary>
/// Builds human-oriented documentation of the query-string surface for a root entity: the five
/// parameters (with configured key names), the filterable/sortable member paths discovered by
/// reflection (camelCase, one navigation level deep, collections, enum values, <c>[Column]</c>
/// aliases) and syntax examples derived from the entity's actual members. Consumed by the
/// OpenAPI integration packages; usable directly for any other documentation surface.
/// </summary>
public static class EntityQueryDocumentation
{
    private const int MaxPathLines = 40;

    /// <summary>Builds the documentation model for <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">Root entity type.</typeparam>
    /// <param name="options">Query-string options; key names are honored. Defaults apply when omitted.</param>
    public static EntityQueryDocumentationModel For<T>(QueryStringOptions? options = null) => For(typeof(T), options);

    /// <summary>Builds the documentation model for <paramref name="entityType"/>.</summary>
    /// <param name="entityType">Root entity type.</param>
    /// <param name="options">Query-string options; key names are honored. Defaults apply when omitted.</param>
    public static EntityQueryDocumentationModel For(Type entityType, QueryStringOptions? options = null)
    {
        if (entityType is null)
        {
            throw new ArgumentNullException(nameof(entityType));
        }

        options ??= new QueryStringOptions();

        var catalog = BuildCatalog(entityType);
        var pathBlock = RenderPathBlock(entityType, catalog);
        var filterExample = BuildFilterExample(catalog);
        var orderByExample = BuildOrderByExample(catalog);
        var selectExample = BuildSelectExample(catalog);

        var filterDescription =
            "Filter expression. May be repeated; all values combine with AND.\n\n" +
            "Syntax: `path:op(value)` with `eq`, `neq`, `gt`, `gte`, `lt`, `lte`, `ct`, `nct`, `sw`, `ew` — " +
            "shorthand `path:value` — groups `and(…)`, `or(…)`, `not(…)` — membership `path:in(a|b)`, `nin(…)` — " +
            "null tests `path:eq(null)` — collections `items:any(…)`, `items:all(…)` — " +
            "aggregates `items.count():gt(1)`, `items.sum(path):gt(200)` (`min`/`max`/`average`) — " +
            "method segments `path.toLower():ct(x)`, `path.substring(0,3):eq(abc)` — " +
            "quoted values `'it''s'`. Names match case-insensitively.\n\n" +
            pathBlock;

        var orderByDescription =
            "Ordering: comma-separated `path` or `path:desc` (direction defaults to `asc`); " +
            "method segments allowed (`name.toLower():desc`).\n\n" +
            pathBlock;

        var selectDescription =
            "Projection: comma-separated paths, optionally aliased (`alias=path`); " +
            "aggregate segments allowed (`items.count()`). The response is shaped accordingly.\n\n" +
            pathBlock;

        var parameters = new List<EntityQueryParameterDocumentation>
        {
            new(options.FilterKey, EntityQueryParameterKind.Filter, filterDescription, filterExample, isInteger: false, repeatable: true),
            new(options.OrderByKey, EntityQueryParameterKind.OrderBy, orderByDescription, orderByExample, isInteger: false, repeatable: false),
            new(options.SkipKey, EntityQueryParameterKind.Skip, "Number of elements to skip (non-negative). Applied after filtering and ordering.", "0", isInteger: true, repeatable: false),
            new(options.TakeKey, EntityQueryParameterKind.Take, "Maximum number of elements to return (non-negative).", "20", isInteger: true, repeatable: false),
            new(options.SelectKey, EntityQueryParameterKind.Select, selectDescription, selectExample, isInteger: false, repeatable: false),
        };

        return new EntityQueryDocumentationModel(entityType.Name, catalog.Select(p => p.Display).ToList(), parameters);
    }

    private sealed class CatalogEntry
    {
        public CatalogEntry(string path, string display, Type type, bool isCollection)
        {
            Path = path;
            Display = display;
            Type = type;
            IsCollection = isCollection;
        }

        public string Path { get; }
        public string Display { get; }
        public Type Type { get; }
        public bool IsCollection { get; }
    }

    private static List<CatalogEntry> BuildCatalog(Type entityType)
    {
        var entries = new List<CatalogEntry>();
        AppendMembers(entityType, prefix: string.Empty, entries, depth: 0);
        return entries;
    }

    private static void AppendMembers(Type type, string prefix, List<CatalogEntry> entries, int depth)
    {
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            var path = prefix + CamelCase(property.Name);
            var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            var alias = GetColumnAlias(property);

            if (IsSimple(propertyType))
            {
                entries.Add(new CatalogEntry(path, Describe(path, propertyType, alias), propertyType, isCollection: false));
            }
            else if (TryGetElementType(propertyType, out var elementType))
            {
                var elementNames = SimpleMemberNames(elementType);
                var display = $"`{path}` — collection of {elementType.Name}" +
                              (elementNames.Count > 0 ? $" ({string.Join(", ", elementNames)})" : string.Empty) +
                              "; use with `any`/`all`/aggregates";
                entries.Add(new CatalogEntry(path, display, elementType, isCollection: true));
            }
            else if (depth == 0 && propertyType.IsClass)
            {
                AppendMembers(propertyType, path + ".", entries, depth + 1);
            }
        }
    }

    private static string Describe(string path, Type type, string? alias)
    {
        var builder = new StringBuilder("`").Append(path).Append('`');
        if (type.IsEnum)
        {
            var names = Enum.GetNames(type);
            var shown = names.Length > 12 ? names.Take(12).Append("…") : names;
            builder.Append(" (").Append(string.Join("|", shown)).Append(')');
        }

        if (alias is not null)
        {
            builder.Append(" — alias `").Append(alias).Append('`');
        }

        return builder.ToString();
    }

    private static string RenderPathBlock(Type entityType, List<CatalogEntry> catalog)
    {
        if (catalog.Count == 0)
        {
            return string.Empty;
        }

        var shown = catalog.Take(MaxPathLines).Select(e => e.Display);
        var suffix = catalog.Count > MaxPathLines ? $", … ({catalog.Count - MaxPathLines} more)" : string.Empty;
        return $"Paths on {entityType.Name}: " + string.Join(", ", shown) + suffix;
    }

    private static string? BuildFilterExample(List<CatalogEntry> catalog)
    {
        var clauses = new List<string>();

        var numeric = catalog.FirstOrDefault(e => !e.IsCollection && IsNumeric(e.Type));
        if (numeric is not null)
        {
            clauses.Add($"{numeric.Path}:gt(0)");
        }

        var text = catalog.FirstOrDefault(e => !e.IsCollection && e.Type == typeof(string));
        if (text is not null && clauses.Count < 2)
        {
            clauses.Add($"{text.Path}:ct(a)");
        }

        var @enum = catalog.FirstOrDefault(e => !e.IsCollection && e.Type.IsEnum);
        if (@enum is not null && clauses.Count < 2)
        {
            clauses.Add($"{@enum.Path}:eq({Enum.GetNames(@enum.Type).FirstOrDefault()})");
        }

        if (clauses.Count == 0 && catalog.Count > 0 && !catalog[0].IsCollection)
        {
            clauses.Add($"{catalog[0].Path}:eq(value)");
        }

        return clauses.Count > 0 ? string.Join(",", clauses) : null;
    }

    private static string? BuildOrderByExample(List<CatalogEntry> catalog)
    {
        var sortable = catalog.Where(e => !e.IsCollection).Take(2).ToList();
        return sortable.Count switch
        {
            0 => null,
            1 => sortable[0].Path,
            _ => $"{sortable[0].Path}:desc,{sortable[1].Path}",
        };
    }

    private static string? BuildSelectExample(List<CatalogEntry> catalog)
    {
        var selectable = catalog.Where(e => !e.IsCollection).Take(2).Select(e => e.Path).ToList();
        return selectable.Count > 0 ? string.Join(",", selectable) : null;
    }

    private static IReadOnlyList<string> SimpleMemberNames(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0 &&
                        IsSimple(Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType))
            .Select(p => CamelCase(p.Name))
            .ToList();

    private static bool IsSimple(Type type) =>
        type.IsPrimitive || type.IsEnum ||
        type == typeof(string) || type == typeof(decimal) ||
        type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan) ||
        type == typeof(Guid)
#if !NETSTANDARD2_0
        || type == typeof(DateOnly) || type == typeof(TimeOnly)
#endif
        ;

    private static bool IsNumeric(Type type) =>
        type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte) ||
        type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort) || type == typeof(sbyte) ||
        type == typeof(decimal) || type == typeof(double) || type == typeof(float);

    private static bool TryGetElementType(Type type, out Type elementType)
    {
        elementType = typeof(object);
        if (type == typeof(string) || !typeof(IEnumerable).IsAssignableFrom(type))
        {
            return false;
        }

        var enumerable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            ? type
            : type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumerable is null)
        {
            return false;
        }

        elementType = enumerable.GetGenericArguments()[0];
        return !IsSimple(elementType);
    }

    private static string? GetColumnAlias(PropertyInfo property)
    {
        foreach (var attribute in property.GetCustomAttributes(inherit: true))
        {
            if (attribute.GetType().FullName == "System.ComponentModel.DataAnnotations.Schema.ColumnAttribute")
            {
                var name = attribute.GetType().GetProperty("Name")?.GetValue(attribute) as string;
                return string.IsNullOrEmpty(name) ? null : name;
            }
        }

        return null;
    }

    private static string CamelCase(string name) =>
        name.Length > 0 && char.IsUpper(name[0])
            ? char.ToLowerInvariant(name[0]) + name.Substring(1)
            : name;
}

/// <summary>Documentation of the whole entity-query surface for one root entity.</summary>
public sealed class EntityQueryDocumentationModel
{
    internal EntityQueryDocumentationModel(
        string entityName, IReadOnlyList<string> propertyPaths, IReadOnlyList<EntityQueryParameterDocumentation> parameters)
    {
        EntityName = entityName;
        PropertyPaths = propertyPaths;
        Parameters = parameters;
    }

    /// <summary>Root entity name.</summary>
    public string EntityName { get; }

    /// <summary>Rendered member-path lines (camelCase, enum values, aliases, collection hints).</summary>
    public IReadOnlyList<string> PropertyPaths { get; }

    /// <summary>The five query-string parameters with configured names, descriptions and examples.</summary>
    public IReadOnlyList<EntityQueryParameterDocumentation> Parameters { get; }
}
