using System.Linq.Expressions;

namespace eQuantic.Linq;

public interface IColumn
{
    public string ColumnName { get; set; }
}

public interface IColumn<T> : IColumn
{
    void SetColumn(Expression<Func<T, object>> expression, bool useColumnFallback = false);
}