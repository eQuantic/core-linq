using System.Linq.Expressions;

namespace eQuantic.Linq.Sorter;

public interface ISorting
{
    string ColumnName { get; }
    SortDirection SortDirection { get; }
    
    public static ISorting Parse(string query) => Sorting.Parse(query);

    public static bool TryParse(string query, out ISorting sorting)
    {
        var result = Sorting.TryParse(query, out var s);
        sorting = s;
        return result;
    }
}

public interface ISorting<T> : ISorting
{
    Expression<Func<T, object>> Expression { get; }
}