namespace eQuantic.Linq.Sorter;

public interface IEntitySorter<TEntity>
{
    IOrderedQueryable<TEntity> Sort(IQueryable<TEntity> collection);
}