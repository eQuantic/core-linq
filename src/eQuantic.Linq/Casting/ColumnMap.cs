using System.Linq.Expressions;

namespace eQuantic.Linq.Casting;

public abstract class ColumnMap<TEntity>
{
    public Expression<Func<TEntity, object>>? ColumnExpression { get; init; }
    public string ColumnName { get; init; } = string.Empty;
    
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