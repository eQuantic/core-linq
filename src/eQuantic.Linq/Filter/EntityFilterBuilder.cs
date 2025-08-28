using System.Globalization;
using System.Linq.Expressions;
using eQuantic.Linq.Expressions;
using eQuantic.Linq.Extensions;

namespace eQuantic.Linq.Filter;

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

    public static IEntityFilter<T> BuildWhereEntityFilter(IEntityFilter<T> filter, Expression<Func<T, bool>> predicate, CompositeOperator compositeOperator = CompositeOperator.And)
    {
        var typeArgs = new[] { typeof(T) };
        var filterType = typeof(WhereEntityFilter<>).MakeGenericType(typeArgs);

        return (IEntityFilter<T>)Activator.CreateInstance(filterType, filter, predicate, compositeOperator);
    }


    /// <summary>
    /// Converts value to the target type using modern pattern matching and switch expressions.
    /// </summary>
    /// <typeparam name="TValue">The type of the input value.</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <param name="keyType">The target type to convert to.</param>
    /// <param name="operator">The filter operator being used.</param>
    /// <returns>The converted value.</returns>
    protected virtual object? ConvertValue<TValue>(TValue value, Type keyType, FilterOperator? @operator = null)
    {
        // Fast path: if types match, return as-is
        if (typeof(TValue) == keyType) return value;

        // Handle null or empty values using pattern matching
        var stringValue = value?.ToString();
        if (string.IsNullOrEmpty(stringValue))
        {
            return Nullable.GetUnderlyingType(keyType) != null ? null : GetDefaultValue(keyType);
        }

        // Handle special collections for Contains/NotContains operators
        if ((@operator is FilterOperator.Contains or FilterOperator.NotContains) &&
            keyType != typeof(string) &&
            value is string strValue)
        {
            var values = strValue.Split(',');
            return values.Select(v => ConvertValue<string>(v, keyType)).ToListOfType(keyType);
        }

        // Use pattern matching for type-specific conversions
        return keyType switch
        {
            // GUID conversion
#if NET8_0_OR_GREATER
            Type t when t == typeof(Guid) => Guid.TryParse(stringValue, out var guidValue) ? guidValue : throw new FormatException($"Invalid GUID format: {stringValue}"),
#else
            Type t when t == typeof(Guid) => Guid.Parse(stringValue),
#endif

            // Enum conversion (non-numeric string values)
            Type t when t.IsEnum && !int.TryParse(stringValue, out _) => Enum.Parse(t, stringValue, true),

            // DateTimeOffset conversion
            Type t when (t == typeof(DateTimeOffset) || t == typeof(DateTimeOffset?)) &&
                       DateTimeOffset.TryParse(stringValue, out var dateTimeOffsetValue) => dateTimeOffsetValue,

            // DateTime conversion
            Type t when (t == typeof(DateTime) || t == typeof(DateTime?)) &&
                       DateTime.TryParse(stringValue, out var dateTimeValue) => dateTimeValue,

            // Boolean conversion
            Type t when (t == typeof(bool) || t == typeof(bool?)) &&
                       bool.TryParse(stringValue, out var boolValue) => boolValue,

            // Numeric types with TryParse patterns
            Type t when (t == typeof(int) || t == typeof(int?)) &&
                       int.TryParse(stringValue, out var intValue) => intValue,

            Type t when (t == typeof(long) || t == typeof(long?)) &&
                       long.TryParse(stringValue, out var longValue) => longValue,

            Type t when (t == typeof(decimal) || t == typeof(decimal?)) &&
                       decimal.TryParse(stringValue, out var decimalValue) => decimalValue,

            Type t when (t == typeof(double) || t == typeof(double?)) &&
                       double.TryParse(stringValue, out var doubleValue) => doubleValue,

            Type t when (t == typeof(float) || t == typeof(float?)) &&
                       float.TryParse(stringValue, out var floatValue) => floatValue,

            // Default case: use Convert.ChangeType
            _ => ConvertToUnderlyingType(value, keyType)
        };
    }

    /// <summary>
    /// Converts to underlying type handling nullable types using pattern matching.
    /// </summary>
    private static object ConvertToUnderlyingType(object? value, Type keyType)
    {
        var underlyingType = Nullable.GetUnderlyingType(keyType) ?? keyType;
        return Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
    }

    private static object GetDefaultValue(Type type)
    {
        // Validate parameters.
        ArgumentNullException.ThrowIfNull(type);

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

    private object? ConvertValueAux<TValue>(TValue value, Type keyType, FilterOperator? @operator = null)
    {
        return ConvertValue(value, keyType, @operator);
    }

    private static ILambdaBuilderFactory GetLambdaBuilderFactory(ILambdaBuilderFactory lambdaBuilderFactory)
    {
        return lambdaBuilderFactory ?? LambdaBuilderFactory.Current;
    }
}
