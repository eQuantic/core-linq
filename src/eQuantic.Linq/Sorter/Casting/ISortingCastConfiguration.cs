using eQuantic.Linq.Casting;

namespace eQuantic.Linq.Sorter.Casting;

public interface ISortingCastConfiguration<TEntity> : ICastConfiguration<SortingCastOptions<TEntity>, TEntity>
{
}