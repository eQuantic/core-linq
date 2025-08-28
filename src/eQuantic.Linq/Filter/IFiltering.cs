using System.Linq.Expressions;

namespace eQuantic.Linq.Filter;

public interface IFiltering : IColumn
{
    FilterOperator Operator { get; set; }
    string StringValue { get; set; }

    public static IFiltering Parse(string query) => Filtering.Parse(query);

    public static bool TryParse(string query, out IFiltering filtering)
    {
        var result = Filtering.TryParse(query, out var f);
        filtering = f;
        return result;
    }
}

public interface IFiltering<T> : IFiltering, IColumn<T>
{
    Expression<Func<T, object>> Expression { get; }
}