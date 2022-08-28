using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace eQuantic.Linq.Expressions;

internal static class EntityBuilder
{
    public static List<PropertyInfo> GetProperties<T>(string propertyName, bool useColumnFallback = false)
    {
        var properties = new List<PropertyInfo>();

        var declaringType = typeof(T);

        foreach (var name in propertyName.Split('.'))
        {
            var property = GetPropertyByName(propertyName, declaringType, name, useColumnFallback);

            properties.Add(property);

            declaringType = property.PropertyType;
        }

        return properties;
    }

    private static PropertyInfo GetPropertyByName(string propertyName, IReflect declaringType, string subPropertyName, bool useColumnFallback = false)
    {
        const BindingFlags flags = BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public;
        var prop = declaringType.GetProperty(subPropertyName, flags);

        if (prop == null)
        {
            if (useColumnFallback)
            {
                var propertiesWithColumns = declaringType.GetProperties(flags)
                    .Where(p => p.IsDefined(typeof(ColumnAttribute), false));
                foreach (var propertyInfo in propertiesWithColumns)
                {
                    if (propertyInfo.GetCustomAttribute<ColumnAttribute>()?.Name == subPropertyName)
                    {
                        return propertyInfo;
                    }
                }
            }

            throw new ArgumentException($"{propertyName} could not be parsed. {declaringType} does not contain a property named '{subPropertyName}'.", nameof(propertyName));
        }

        return prop;
    }
}