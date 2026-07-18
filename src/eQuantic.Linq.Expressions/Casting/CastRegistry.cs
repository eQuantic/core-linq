using System.Linq.Expressions;

namespace eQuantic.Linq.Expressions.Casting;

/// <summary>Immutable-after-build registry of type-pair mappings used by the cast rewriter.</summary>
internal sealed class CastRegistry
{
    private readonly Dictionary<(Type Source, Type Target), Pair> _pairs = [];

    internal sealed class Pair
    {
        private readonly Dictionary<string, LambdaExpression> _maps = new(StringComparer.OrdinalIgnoreCase);

        public bool AutoMapByName { get; set; } = true;

        public bool ColumnFallback { get; set; } = true;

        public void AddMap(string sourceMemberName, LambdaExpression target) => _maps[sourceMemberName] = target;

        public bool TryGetMap(string sourceMemberName, out LambdaExpression target) =>
            _maps.TryGetValue(sourceMemberName, out target!);
    }

    public Pair GetOrAddPair(Type source, Type target)
    {
        if (!_pairs.TryGetValue((source, target), out var pair))
        {
            pair = new Pair();
            _pairs.Add((source, target), pair);
        }

        return pair;
    }

    public Pair? Find(Type source, Type target) =>
        _pairs.TryGetValue((source, target), out var pair) ? pair : null;

    public bool IsSourceType(Type type)
    {
        foreach (var key in _pairs.Keys)
        {
            if (key.Source == type)
            {
                return true;
            }
        }

        return false;
    }
}
