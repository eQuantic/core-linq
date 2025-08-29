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

        public virtual LambdaExpression BuildLambda(PropertyInfo[] properties, CompositeOperator compositeOperator, IFiltering[] values)
        {
            // Handle Any/All operations for collection properties
            if (compositeOperator is CompositeOperator.Any or CompositeOperator.All)
            {
                return BuildCollectionLambda(properties, compositeOperator, values);
            }

            // For And/Or operations, this would be handled elsewhere
            throw new NotSupportedException($"Composite operator {compositeOperator} is not supported in this context");
        }

        protected virtual LambdaExpression BuildCollectionLambda(PropertyInfo[] collectionProperties, CompositeOperator compositeOperator, IFiltering[] values)
        {
            var param = Expression.Parameter(typeof(T), "entity");
            
            // Build collection access expression
            Expression collectionAccess = param;
            foreach (var prop in collectionProperties)
            {
                collectionAccess = Expression.Property(collectionAccess, prop);
            }

            var collectionType = collectionProperties.Last().PropertyType;
            var itemType = GetCollectionItemType(collectionType);
            
            if (itemType == null)
                throw new InvalidOperationException($"Property '{string.Join(".", collectionProperties.Select(p => p.Name))}' is not a collection type");

            // Build inner predicate for collection items using existing logic
            var innerPredicate = BuildInnerCollectionPredicate(itemType, values);
            
            // Build Any() or All() expression
            var methodName = compositeOperator == CompositeOperator.Any ? "Any" : "All";
            var enumerableType = typeof(Enumerable);
            var method = enumerableType.GetMethods()
                .Where(m => m.Name == methodName && m.GetParameters().Length == 2)
                .First()
                .MakeGenericMethod(itemType);

            var methodCall = Expression.Call(method, collectionAccess, innerPredicate);
            
            return Expression.Lambda<Func<T, bool>>(methodCall, param);
        }

        protected virtual LambdaExpression BuildInnerCollectionPredicate(Type itemType, IFiltering[] filters)
        {
            var itemParam = Expression.Parameter(itemType, "item");
            Expression? combinedExpression = null;

            foreach (var filter in filters)
            {
                var filterExpression = BuildSingleFilterExpression(itemParam, itemType, filter);
                
                combinedExpression = combinedExpression == null
                    ? filterExpression
                    : Expression.AndAlso(combinedExpression, filterExpression);
            }

            combinedExpression ??= Expression.Constant(true);
            
            var delegateType = typeof(Func<,>).MakeGenericType(itemType, typeof(bool));
            return Expression.Lambda(delegateType, combinedExpression, itemParam);
        }

        protected virtual Expression BuildSingleFilterExpression(ParameterExpression itemParam, Type itemType, IFiltering filter)
        {
            var properties = EntityBuilder.GetProperties(itemType, filter.ColumnName, false);
            var propertyType = properties.Last().PropertyType;
            
            // Build property access expression
            Expression propertyAccess = itemParam;
            foreach (var prop in properties)
            {
                propertyAccess = Expression.Property(propertyAccess, prop);
            }

            // Convert filter value to proper type
            var convertedValue = ConvertValueToType(filter.StringValue, propertyType, filter.Operator);
            var constantExpression = Expression.Constant(convertedValue, propertyType);

            // Build comparison expression based on operator
            return filter.Operator switch
            {
                FilterOperator.Equal => Expression.Equal(propertyAccess, constantExpression),
                FilterOperator.NotEqual => Expression.NotEqual(propertyAccess, constantExpression),
                FilterOperator.Contains when propertyType == typeof(string) => 
                    Expression.Call(propertyAccess, GetMethod("Contains", typeof(string)), constantExpression),
                FilterOperator.StartsWith when propertyType == typeof(string) => 
                    Expression.Call(propertyAccess, GetMethod("StartsWith", typeof(string)), constantExpression),
                FilterOperator.EndsWith when propertyType == typeof(string) => 
                    Expression.Call(propertyAccess, GetMethod("EndsWith", typeof(string)), constantExpression),
                FilterOperator.GreaterThan => Expression.GreaterThan(propertyAccess, constantExpression),
                FilterOperator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(propertyAccess, constantExpression),
                FilterOperator.LessThan => Expression.LessThan(propertyAccess, constantExpression),
                FilterOperator.LessThanOrEqual => Expression.LessThanOrEqual(propertyAccess, constantExpression),
                FilterOperator.NotContains when propertyType == typeof(string) => 
                    Expression.Not(Expression.Call(propertyAccess, GetMethod("Contains", typeof(string)), constantExpression)),
                _ => throw new NotSupportedException($"Filter operator {filter.Operator} is not supported for collection filtering")
            };
        }

        protected virtual Type? GetCollectionItemType(Type propertyType)
        {
            // Handle IEnumerable<T> through interfaces
            var enumerableInterface = propertyType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            
            return enumerableInterface?.GetGenericArguments().FirstOrDefault() ?? 
                   (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>) 
                       ? propertyType.GetGenericArguments().FirstOrDefault() 
                       : null);
        }

        protected virtual object? ConvertValueToType(string? value, Type targetType, FilterOperator @operator)
        {
            if (value == null) return null;
            
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            
            return underlyingType switch
            {
                Type t when t == typeof(string) => value,
                Type t when t == typeof(int) => int.Parse(value),
                Type t when t == typeof(long) => long.Parse(value),
                Type t when t == typeof(decimal) => decimal.Parse(value),
                Type t when t == typeof(double) => double.Parse(value),
                Type t when t == typeof(float) => float.Parse(value),
                Type t when t == typeof(bool) => bool.Parse(value),
                Type t when t == typeof(DateTime) => DateTime.Parse(value),
                Type t when t == typeof(DateTimeOffset) => DateTimeOffset.Parse(value),
                Type t when t == typeof(Guid) => Guid.Parse(value),
                Type t when t.IsEnum => Enum.Parse(t, value, true),
                _ => Convert.ChangeType(value, underlyingType, System.Globalization.CultureInfo.InvariantCulture)
            };
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