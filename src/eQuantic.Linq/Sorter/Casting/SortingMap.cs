using System.Linq.Expressions;
using eQuantic.Linq.Casting;

namespace eQuantic.Linq.Sorter.Casting;

public class SortingMap<TEntity> : ColumnMap<TEntity>
{
    public SortDirection? Direction { get; set; }
    public SetNewSorting<TEntity> CustomSorting { get; set; }
        
    public SortingMap(Expression<Func<TEntity, object>> column, SortDirection? direction = null) : base(column)
    {
        Direction = direction;
    }

    public SortingMap(SetNewSorting<TEntity> customSorting)
    {
        CustomSorting = customSorting;
    }

    public SortingMap()
    {
            
    }
}