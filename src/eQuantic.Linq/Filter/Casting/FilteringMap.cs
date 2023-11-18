using System.Linq.Expressions;
using eQuantic.Linq.Casting;

namespace eQuantic.Linq.Filter.Casting;

public class FilteringMap<TEntity>: ColumnMap<TEntity>
{
    public SetNewStringValue SetValue { get; set; }
    public FilterOperator? Operator { get; set; }
    public SetNewFiltering<TEntity>? CustomFiltering { get; set; }
        
    public FilteringMap(Expression<Func<TEntity, object>> column, SetNewStringValue setValue = null, FilterOperator? @operator = null)
        : base(column)
    {
        SetValue = setValue;
        Operator = @operator;
    }

    public FilteringMap(string columnNameName, SetNewStringValue setValue = null, FilterOperator? @operator = null)
        : base(columnNameName)
    {
        SetValue = setValue;
        Operator = @operator;
    }
    
    public FilteringMap(SetNewFiltering<TEntity> customFiltering)
    {
        CustomFiltering = customFiltering;
    }

    public FilteringMap()
    {
            
    }
}