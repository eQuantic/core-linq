using System.Linq.Expressions;

namespace eQuantic.Linq.Casting;

public abstract class ColumnMap<TEntity>
{
    public Expression<Func<TEntity, object>>? ColumnExpression { get; set; }
    public string ColumnName { get; set; }
    
    protected ColumnMap(Expression<Func<TEntity, object>> column)
    {
        ColumnExpression = column;
    }

    protected ColumnMap(string columnName)
    {
        ColumnName = columnName;
    }
    protected ColumnMap() { }
}