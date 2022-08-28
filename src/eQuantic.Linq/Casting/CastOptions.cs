using System.Linq.Expressions;

namespace eQuantic.Linq.Casting;

public interface ICastOptions<out TCastOptions, TEntity> where TCastOptions : ICastOptions<TCastOptions, TEntity>
{
    TCastOptions Map(string sourceColumnName, Expression<Func<TEntity, object>> destinationColumn);
    TCastOptions ExcludeUnmapped();
    TCastOptions ThrowUnmapped();
}
    
public class CastOptions<TCastOptions, TColumnMap, TEntity> : ICastOptions<TCastOptions, TEntity>
    where TCastOptions : CastOptions<TCastOptions, TColumnMap, TEntity>
    where TColumnMap : ColumnMap<TEntity>, new()
{
    protected readonly Dictionary<string, TColumnMap> Mapping = new Dictionary<string, TColumnMap>(StringComparer.InvariantCultureIgnoreCase);
    private bool excludeUnmapped;
    private bool throwUnmapped;
        
    public TCastOptions Map(string sourceColumnName, Expression<Func<TEntity, object>> destinationColumn)
    {
        Mapping.Add(sourceColumnName, new TColumnMap{ Column = destinationColumn });
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
        
    internal Dictionary<string, TColumnMap> GetMapping() => Mapping;
    internal bool GetExcludeUnmapped() => excludeUnmapped;
    internal bool GetThrowUnmapped() => throwUnmapped;
}