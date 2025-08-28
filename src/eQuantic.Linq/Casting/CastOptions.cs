using System.Linq.Expressions;

namespace eQuantic.Linq.Casting;

public interface ICastOptions<out TCastOptions, TEntity> where TCastOptions : ICastOptions<TCastOptions, TEntity>
{
    TCastOptions Map(string sourceColumnName, Expression<Func<TEntity, object>> destinationColumn);
    TCastOptions Map(string sourceColumnName, string destinationColumnName);
    TCastOptions Exclude(string sourceColumnName);
    TCastOptions ExcludeUnmapped();
    TCastOptions ThrowUnmapped();
}
    
public class CastOptions<TCastOptions, TColumnMap, TEntity> : ICastOptions<TCastOptions, TEntity>
    where TCastOptions : CastOptions<TCastOptions, TColumnMap, TEntity>
    where TColumnMap : ColumnMap<TEntity>, new()
{
    protected readonly Dictionary<string, TColumnMap> Mapping = new(StringComparer.InvariantCultureIgnoreCase);
    private readonly HashSet<string> excluded = new(StringComparer.InvariantCultureIgnoreCase);
    private bool excludeUnmapped;
    private bool throwUnmapped;
    private bool useColumnFallback;
    private ColumnFallbackApplicability columnFallbackApplicability;
    
    public TCastOptions Map(string sourceColumnName, Expression<Func<TEntity, object>> destinationColumn)
    {
        Mapping.Add(sourceColumnName, new TColumnMap{ ColumnExpression = destinationColumn });
        return (TCastOptions)this;
    }
    
    public TCastOptions Map(string sourceColumnName, string destinationColumnName)
    {
        Mapping.Add(sourceColumnName, new TColumnMap{ ColumnName = destinationColumnName });
        return (TCastOptions)this;
    }
    public TCastOptions Exclude(string sourceColumnName)
    {
        excluded.Add(sourceColumnName);
        return (TCastOptions)this;
    }
    public TCastOptions ExcludeUnmapped()
    {
        excludeUnmapped = true;
        return (TCastOptions)this;
    }
        
    public TCastOptions ThrowUnmapped()
    {
        throwUnmapped = true;
        return (TCastOptions)this;
    }
    
    public TCastOptions UseColumnFallback(ColumnFallbackApplicability applicability = ColumnFallbackApplicability.ToDestination)
    {
        useColumnFallback = true;
        columnFallbackApplicability = applicability;
        return (TCastOptions)this;
    }
    
    // Modern property-style internal accessors
    internal Dictionary<string, TColumnMap> GetMapping() => Mapping;
    internal HashSet<string> GetExcluded() => excluded;
    internal bool GetExcludeUnmapped() => excludeUnmapped;
    internal bool GetThrowUnmapped() => throwUnmapped;
    internal bool GetUseColumnFallback() => useColumnFallback;
    internal ColumnFallbackApplicability GetColumnFallbackApplicability() => columnFallbackApplicability;
}