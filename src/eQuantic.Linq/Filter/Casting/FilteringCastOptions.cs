using System.Linq.Expressions;
using eQuantic.Linq.Casting;

namespace eQuantic.Linq.Filter.Casting;

public class FilteringCastOptions<TEntity> : CastOptions<FilteringCastOptions<TEntity>, FilteringMap<TEntity>, TEntity>
{
    public FilteringCastOptions<TEntity> Map(string sourceColumnName,
        Expression<Func<TEntity, object>> destinationColumn, SetNewStringValue setValue, FilterOperator? @operator = null)
    {
        Mapping.Add(sourceColumnName, new FilteringMap<TEntity>(destinationColumn, setValue, @operator));
        return this;
    }

    public FilteringCastOptions<TEntity> CustomMap(string sourceColumnName, SetNewFiltering<TEntity> customFiltering)
    {
        Mapping.Add(sourceColumnName, new FilteringMap<TEntity>(customFiltering));
        return this;
    }
}