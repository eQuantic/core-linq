using System.Linq.Expressions;

namespace eQuantic.Linq.Casting;

public abstract class ColumnMap<TEntity>
{
    public Expression<Func<TEntity, object>> Column { get; set; }

    protected ColumnMap(Expression<Func<TEntity, object>> column)
    {
        Column = column;
    }

    protected ColumnMap() { }
}