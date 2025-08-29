using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace eQuantic.Linq.Expressions;

/// <summary>
/// Provides utilities for resolving property information from entity types, supporting nested property paths 
/// and column attribute fallbacks for dynamic property resolution in LINQ expressions.
/// </summary>
internal static class EntityBuilder
{
    /// <summary>
    /// Resolves a property path for a specific type into a list of PropertyInfo objects.
    /// </summary>
    /// <typeparam name="T">The root entity type to resolve properties from.</typeparam>
    /// <param name="propertyName">The property path, supporting dot notation for nested properties.</param>
    /// <param name="useColumnFallback">Whether to fall back to column attributes if property name lookup fails.</param>
    /// <returns>A list of PropertyInfo objects representing the complete property path.</returns>
    /// <exception cref="ArgumentException">Thrown when any property in the path cannot be found.</exception>
    public static List<PropertyInfo> GetProperties<T>(string propertyName, bool useColumnFallback = false)
    {
        var declaringType = typeof(T);
        return GetProperties(declaringType, propertyName, useColumnFallback);
    }

    /// <summary>
    /// Resolves a property path into a list of PropertyInfo objects, supporting nested property navigation.
    /// </summary>
    /// <param name="declaringType">The root type to start property resolution from.</param>
    /// <param name="propertyName">The property path, supporting dot notation for nested properties (e.g., "User.Profile.Name").</param>
    /// <param name="useColumnFallback">Whether to fall back to column attributes if property name lookup fails.</param>
    /// <returns>A list of PropertyInfo objects representing the complete property path.</returns>
    /// <exception cref="ArgumentException">Thrown when any property in the path cannot be found in its declaring type.</exception>
    public static List<PropertyInfo> GetProperties(Type declaringType, string propertyName, bool useColumnFallback = false)
    {
        var properties = new List<PropertyInfo>();
        
        foreach (var name in propertyName.Split('.'))
        {
            var property = GetPropertyByName(propertyName, declaringType, name, useColumnFallback);

            // Ensure property was found before attempting to use it
            if (property == null)
            {
                throw new ArgumentException($"Property '{name}' not found in type '{declaringType.Name}' for property path '{propertyName}'.", nameof(propertyName));
            }

            properties.Add(property);

            declaringType = property.PropertyType;
        }

        return properties;
    }

    /// <summary>
    /// Retrieves a single property by name from the specified type, with optional column attribute fallback.
    /// </summary>
    /// <param name="propertyName">The full property path for error reporting purposes.</param>
    /// <param name="declaringType">The type to search for the property.</param>
    /// <param name="subPropertyName">The specific property name to find.</param>
    /// <param name="useColumnFallback">Whether to search column attributes if property name lookup fails.</param>
    /// <returns>The PropertyInfo object for the found property.</returns>
    /// <exception cref="ArgumentException">Thrown when the property cannot be found by name or column attribute.</exception>
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