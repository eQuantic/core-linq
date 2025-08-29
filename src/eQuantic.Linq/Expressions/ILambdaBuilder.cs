using System.Linq.Expressions;
using System.Reflection;
using eQuantic.Linq.Filter;

namespace eQuantic.Linq.Expressions;

public interface ILambdaBuilder
{
    LambdaExpression BuildLambda(MethodInfo[] propertyAccessors);
    LambdaExpression BuildLambda(PropertyInfo[] properties, bool useNullCheckForNestedProperties = false);
    LambdaExpression BuildLambda(PropertyInfo[] properties, object value, FilterOperator @operator = FilterOperator.Equal);
    LambdaExpression BuildLambda(PropertyInfo[] properties, CompositeOperator compositeOperator, IFiltering[] values);
}