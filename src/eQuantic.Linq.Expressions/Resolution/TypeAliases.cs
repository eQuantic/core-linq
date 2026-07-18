namespace eQuantic.Linq.Expressions.Resolution;

/// <summary>Bidirectional table of well-known type aliases used to keep the JSON short and runtime-agnostic.</summary>
internal static class TypeAliases
{
    private static readonly Dictionary<Type, string> AliasByType = new()
    {
        [typeof(object)] = "object",
        [typeof(string)] = "string",
        [typeof(bool)] = "bool",
        [typeof(byte)] = "byte",
        [typeof(sbyte)] = "sbyte",
        [typeof(char)] = "char",
        [typeof(decimal)] = "decimal",
        [typeof(double)] = "double",
        [typeof(float)] = "float",
        [typeof(int)] = "int",
        [typeof(uint)] = "uint",
        [typeof(long)] = "long",
        [typeof(ulong)] = "ulong",
        [typeof(short)] = "short",
        [typeof(ushort)] = "ushort",
        [typeof(void)] = "void",
        [typeof(Guid)] = "guid",
        [typeof(DateTime)] = "datetime",
        [typeof(DateTimeOffset)] = "datetimeoffset",
        [typeof(TimeSpan)] = "timespan",
#if NET6_0_OR_GREATER
        [typeof(DateOnly)] = "dateonly",
        [typeof(TimeOnly)] = "timeonly",
#endif
    };

    private static readonly Dictionary<string, Type> TypeByAlias = BuildReverse();

    private static Dictionary<string, Type> BuildReverse()
    {
        var reverse = new Dictionary<string, Type>(StringComparer.Ordinal);
        foreach (var pair in AliasByType)
        {
            reverse[pair.Value] = pair.Key;
        }

        return reverse;
    }

    public static bool TryGetAlias(Type type, out string alias)
    {
        if (AliasByType.TryGetValue(type, out var found))
        {
            alias = found;
            return true;
        }

        alias = string.Empty;
        return false;
    }

    public static bool TryGetType(string alias, out Type type)
    {
        if (TypeByAlias.TryGetValue(alias, out var found))
        {
            type = found;
            return true;
        }

        type = null!;
        return false;
    }
}
