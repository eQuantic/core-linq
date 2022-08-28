using System.Linq.Expressions;

namespace eQuantic.Linq.Sorter;

public interface ISorting
{
    string ColumnName { get; }
    SortDirection SortDirection { get; }
}

public interface ISorting<T> : ISorting
{
    Expression<Func<T, object>> Expression { get; }
}