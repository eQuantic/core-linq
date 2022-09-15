using System.Linq.Expressions;

namespace eQuantic.Linq.Filter;

public interface IFiltering : IColumn
{
    FilterOperator Operator { get; set; }
    string StringValue { get; set; }
}

public interface IFiltering<T> : IFiltering, IColumn<T>
{
    Expression<Func<T, object>> Expression { get; }
}