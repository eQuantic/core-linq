using System;

namespace eQuantic.Linq.SourceGenerator;

/// <summary>
/// Configures how a property should be handled by the source generator.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class SpecPropertyAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the filter operators to generate for this property.
    /// </summary>
    public FilterOperator Operators { get; set; } = FilterOperator.Equal;

    /// <summary>
    /// Gets or sets whether to exclude this property from generation.
    /// </summary>
    public bool Exclude { get; set; } = false;

    /// <summary>
    /// Gets or sets a custom name for generated methods. If null, uses the property name.
    /// </summary>
    public string? MethodName { get; set; }

    /// <summary>
    /// Gets or sets whether to include this property in sorting generation.
    /// </summary>
    public bool IncludeInSorting { get; set; } = true;
}