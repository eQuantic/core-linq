using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using eQuantic.Linq.Expressions;
using eQuantic.Linq.Extensions;

namespace eQuantic.Linq.Filter
{
    internal class EntityFilterBuilder<T>
    {
        private readonly LambdaExpression keySelector;

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityFilterBuilder{T}"/> class.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="value">The value.</param>
        /// <param name="operator">The operator.</param>
        /// <param name="useColumnFallback">if set to <c>true</c> fallback to search for Column attributes if the property name isn't found in TEntity</param>
        /// <param name="lambdaBuilderFactory">The lambda builder factory</param>
        public EntityFilterBuilder(string propertyName, object value, FilterOperator @operator, bool useColumnFallback = false, ILambdaBuilderFactory lambdaBuilderFactory = null)
        {
            var properties = EntityBuilder.GetProperties<T>(propertyName, useColumnFallback);
            var keyType = properties.Last().PropertyType;
            var builder = GetLambdaBuilderFactory(lambdaBuilderFactory).Create(typeof(T), keyType);
            var convertedValue = ConvertValueAux(value, keyType, @operator);

            keySelector = builder.BuildLambda(properties.ToArray(), convertedValue, @operator);
        }

        public IEntityFilter<T> BuildWhereEntityFilter()
        {
            var typeArgs = new[] { typeof(T) };

            var filterType = typeof(WhereEntityFilter<>).MakeGenericType(typeArgs);

            return (IEntityFilter<T>)Activator.CreateInstance(filterType, keySelector);
        }

        public IEntityFilter<T> BuildWhereEntityFilter(IEntityFilter<T> filter, CompositeOperator compositeOperator = CompositeOperator.And)
        {
            var typeArgs = new[] { typeof(T) };

            var filterType = typeof(WhereEntityFilter<>).MakeGenericType(typeArgs);

            return (IEntityFilter<T>)Activator.CreateInstance(filterType, filter, keySelector, compositeOperator);
        }

        protected virtual object ConvertValue<TValue>(TValue value, Type keyType, FilterOperator? @operator = null)
        {
            if (typeof(TValue) == keyType) return value;

            if (string.IsNullOrEmpty(value?.ToString()))
            {
                return Nullable.GetUnderlyingType(keyType) != null ? null : GetDefaultValue(keyType);
            }

            if (keyType == typeof(Guid))
            {
                return Guid.Parse(value.ToString());
            }
            if (keyType.IsEnum && !int.TryParse(value.ToString(), out _))
            {
                return Enum.Parse(keyType, value.ToString(), true);
            }

			if ((keyType == typeof(DateTimeOffset) || keyType == typeof(DateTimeOffset?)) && DateTimeOffset.TryParse(value.ToString(), out var dateTimeOffsetValue))
			{
				return dateTimeOffsetValue;
			}


			if ((@operator is FilterOperator.Contains || @operator is FilterOperator.NotContains) && keyType != typeof(string) && value is string)
            {
                var values = value.ToString().Split(',');
                return values.Select(v => ConvertValue<string>(v, keyType)).ToListOfType(keyType);
            }
            var underlyingType = Nullable.GetUnderlyingType(keyType) ?? keyType;

            return Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
        }

        private static object GetDefaultValue(Type type)
        {
            // Validate parameters.
            if (type == null) throw new ArgumentNullException(nameof(type));

            // We want an Func<object> which returns the default.
            // Create that expression here.
            var e = Expression.Lambda<Func<object>>(
                // Have to convert to object.
                Expression.Convert(
                    // The default value, always get what the *code* tells us.
                    Expression.Default(type), typeof(object)
                )
            );

            // Compile and return the value.
            return e.Compile()();
        }

        private object ConvertValueAux<TValue>(TValue value, Type keyType, FilterOperator? @operator = null)
        {
            return ConvertValue(value, keyType, @operator);
        }

        private static ILambdaBuilderFactory GetLambdaBuilderFactory(ILambdaBuilderFactory lambdaBuilderFactory)
        {
            return lambdaBuilderFactory ?? LambdaBuilderFactory.Current;
        }
    }
}