using System.Linq.Expressions;
using System.Reflection;
using eQuantic.Linq.Filter;

namespace eQuantic.Linq.Expressions
{
    public class LambdaBuilder<T, TKey> : ILambdaBuilder
    {
        public virtual LambdaExpression BuildLambda(MethodInfo[] propertyAccessors)
        {
            if (propertyAccessors == null)
            {
                throw new ArgumentNullException(nameof(propertyAccessors));
            }
            var parameterExpression = GetParameterExpression();
            var propertyExpression = BuildPropertyExpression(propertyAccessors, parameterExpression);
            return Expression.Lambda<Func<T, TKey>>(propertyExpression, parameterExpression);
        }

        public virtual LambdaExpression BuildLambda(PropertyInfo[] properties, bool useNullCheckForNestedProperties = false)
        {
            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }
            var parameterExpression = GetParameterExpression();
            var propertyExpression = BuildPropertyExpression(properties, parameterExpression, useNullCheckForNestedProperties);
            return Expression.Lambda<Func<T, TKey>>(propertyExpression, parameterExpression);
        }

        public virtual LambdaExpression BuildLambda(PropertyInfo[] properties, object value, FilterOperator @operator = FilterOperator.Equal)
        {
            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }
            var parameterExpression = GetParameterExpression();
            var binaryExpression = BuildBinaryExpression(properties, parameterExpression, value, @operator);
            return Expression.Lambda<Func<T, bool>>(binaryExpression, parameterExpression);
        }

        protected static Expression BuildPropertyExpression(IEnumerable<MethodInfo> propertyAccessors, Expression parameterExpression)
        {
            Expression propertyExpression = null;

            foreach (var propertyAccessor in propertyAccessors)
            {
                var innerExpression = propertyExpression ?? parameterExpression;
                propertyExpression = Expression.Property(innerExpression, propertyAccessor);
            }

            return propertyExpression;
        }

        protected static Expression BuildPropertyExpression(IEnumerable<PropertyInfo> properties, ParameterExpression parameterExpression, bool useNullCheckForNestedProperties = false)
        {
            Expression propertyExpression = parameterExpression;
            var propertyList = properties.ToList();

            BinaryExpression nullCheck = null;
            for (var i = 0; i < propertyList.Count; i++)
            {
                var property = propertyList[i];

                var memberExpression = Expression.Property(propertyExpression, property);
                
                if (useNullCheckForNestedProperties
                    && i < propertyList.Count - 1
                    && IsNullable(property.PropertyType)
                    && property.PropertyType != typeof(string)
                    && (property.PropertyType.IsClass || property.PropertyType.IsValueType))
                {
                    if (nullCheck != null)
                    {
                        nullCheck = Expression.AndAlso(nullCheck, Expression.NotEqual(memberExpression, Expression.Constant(null)));
                    }
                    else
                    {
                        nullCheck = Expression.NotEqual(memberExpression, Expression.Constant(null));
                    }
                }
                propertyExpression = memberExpression;
            }

            if (nullCheck != null)
            {
                var defaultValue = Expression.Constant(IsNullable(propertyExpression.Type) ? null : Activator.CreateInstance(propertyExpression.Type), propertyExpression.Type);
                propertyExpression = Expression.Condition(nullCheck, propertyExpression, defaultValue);
            }

            return propertyExpression;
        }

        protected virtual Expression BuildBinaryExpression(PropertyInfo[] properties, ParameterExpression parameterExpression, object value, FilterOperator @operator)
        {
            switch (@operator)
            {
                case FilterOperator.GreaterThan:
                    return BuildGreaterThanExpression(properties, parameterExpression, value);

                case FilterOperator.GreaterThanOrEqual:
                    return BuildGreaterThanOrEqualExpression(properties, parameterExpression, value);

                case FilterOperator.LessThan:
                    return BuildLessThanExpression(properties, parameterExpression, value);

                case FilterOperator.LessThanOrEqual:
                    return BuildLessThanOrEqualExpression(properties, parameterExpression, value);

                case FilterOperator.Contains:
                    return BuildContainsExpression(properties, parameterExpression, value);

                case FilterOperator.StartsWith:
                    return BuildStartsWithExpression(properties, parameterExpression, value);

                case FilterOperator.EndsWith:
                    return BuildEndsWithExpression(properties, parameterExpression, value);

                case FilterOperator.NotEqual:
                    return BuildNotEqualExpression(properties, parameterExpression, value);

                case FilterOperator.NotContains:
                    return BuildNotContainsExpression(properties, parameterExpression, value);

                case FilterOperator.Equal:
                default:
                    return BuildEqualExpression(properties, parameterExpression, value);
            }
        }

        protected virtual Expression BuildNotContainsExpression(PropertyInfo[] properties, ParameterExpression parameterExpression, object value)
        {
            var propertyExpression = BuildPropertyExpression(properties, parameterExpression);
            var keyType = typeof(TKey);

            if (keyType != typeof(string))
            {
                var method = typeof(List<TKey>).GetMethod(nameof(Enumerable.Contains));
                return Expression.Not(Expression.Call(Expression.Constant(value), method, propertyExpression));
            }

            return Expression.Not(Expression.Call(propertyExpression, GetMethod(nameof(string.Contains), typeof(string)), Expression.Constant(value, keyType)));
        }

        protected virtual Expression BuildContainsExpression(PropertyInfo[] properties, ParameterExpression parameterExpression, object value)
        {
            var propertyExpression = BuildPropertyExpression(properties, parameterExpression);
            var keyType = typeof(TKey);

            if (keyType != typeof(string))
            {
                var method = typeof(List<TKey>).GetMethod(nameof(Enumerable.Contains));
                return Expression.Call(Expression.Constant(value), method, propertyExpression);
            }

            return Expression.Call(propertyExpression, GetMethod(nameof(string.Contains), typeof(string)), Expression.Constant(value, keyType));
        }

        protected virtual Expression BuildEndsWithExpression(PropertyInfo[] properties, ParameterExpression parameterExpression, object value)
        {
            var propertyExpression = BuildPropertyExpression(properties, parameterExpression);
            var constant = Expression.Constant(value, typeof(TKey));
            return Expression.Call(propertyExpression, GetMethod(nameof(string.EndsWith), typeof(string)), constant);
        }

        protected virtual Expression BuildEqualExpression(PropertyInfo[] properties, ParameterExpression parameterExpression, object value)
        {
            var propertyExpression = BuildPropertyExpression(properties, parameterExpression);
            var constant = Expression.Constant(value, typeof(TKey));
            return Expression.Equal(propertyExpression, constant);
        }

        protected virtual Expression BuildGreaterThanExpression(PropertyInfo[] properties, ParameterExpression parameterExpression, object value)
        {
            var propertyExpression = BuildPropertyExpression(properties, parameterExpression);
            var constant = Expression.Constant(value, typeof(TKey));
            return Expression.GreaterThan(propertyExpression, constant);
        }

        protected virtual Expression BuildGreaterThanOrEqualExpression(PropertyInfo[] properties, ParameterExpression parameterExpression, object value)
        {
            var propertyExpression = BuildPropertyExpression(properties, parameterExpression);
            var constant = Expression.Constant(value, typeof(TKey));
            return Expression.GreaterThanOrEqual(propertyExpression, constant);
        }

        protected virtual Expression BuildLessThanExpression(PropertyInfo[] properties, ParameterExpression parameterExpression, object value)
        {
            var propertyExpression = BuildPropertyExpression(properties, parameterExpression);
            var constant = Expression.Constant(value, typeof(TKey));
            return Expression.LessThan(propertyExpression, constant);
        }

        protected virtual Expression BuildLessThanOrEqualExpression(PropertyInfo[] properties, ParameterExpression parameterExpression, object value)
        {
            var propertyExpression = BuildPropertyExpression(properties, parameterExpression);
            var constant = Expression.Constant(value, typeof(TKey));
            return Expression.LessThanOrEqual(propertyExpression, constant);
        }

        protected virtual Expression BuildNotEqualExpression(PropertyInfo[] properties, ParameterExpression parameterExpression, object value)
        {
            var propertyExpression = BuildPropertyExpression(properties, parameterExpression);
            var constant = Expression.Constant(value, typeof(TKey));
            return Expression.NotEqual(propertyExpression, constant);
        }

        protected virtual Expression BuildStartsWithExpression(PropertyInfo[] properties, ParameterExpression parameterExpression, object value)
        {
            var propertyExpression = BuildPropertyExpression(properties, parameterExpression);
            var constant = Expression.Constant(value, typeof(TKey));
            return Expression.Call(propertyExpression, GetMethod(nameof(string.StartsWith), typeof(string)), constant);
        }

        protected ParameterExpression GetParameterExpression()
        {
            return Expression.Parameter(typeof(T), "entity");
        }

        protected static MethodInfo GetMethod(string name, Type type)
        {
            return type.GetMethod(name, new[] { type });
        }

        // Determining nullable types
        // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/nullable-value-types#how-to-identify-a-nullable-value-type
        // https://stackoverflow.com/a/374663/525162
        private static bool IsNullable(Type type)
        {
            if (!type.IsValueType) return true; // ref-type
            if (Nullable.GetUnderlyingType(type) != null) return true; // Nullable<T>
            return false; // value-type
        }
    }
}