using eQuantic.Linq.Casting;

namespace eQuantic.Linq.Filter.Casting;

public interface IFilteringCastConfiguration<TEntity> : ICastConfiguration<FilteringCastOptions<TEntity>, TEntity>
{
}