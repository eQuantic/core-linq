using System.Linq.Expressions;
using eQuantic.Linq.Casting;

namespace eQuantic.Linq.Sorter.Casting;

public class SortingCastOptions<TEntity> : CastOptions<SortingCastOptions<TEntity>, SortingMap<TEntity>, TEntity>
{
    public SortingCastOptions<TEntity> Map(string sourceColumnName, Expression<Func<TEntity, object>> destinationColumn, SortDirection? direction)
    {
        Mapping.Add(sourceColumnName, new SortingMap<TEntity>(destinationColumn, direction));
        return this;
    }

    public SortingCastOptions<TEntity> CustomMap(string sourceColumnName, SetNewSorting<TEntity> customFiltering)
    {
        Mapping.Add(sourceColumnName, new SortingMap<TEntity>(customFiltering));
        return this;
    }
}