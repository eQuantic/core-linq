using System;

namespace eQuantic.Linq.SourceGenerator;

/// <summary>
/// Marks a class for automatic generation of specifications, filters, and sorting methods.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class GenerateSpecificationsAttribute : Attribute
{
    /// <summary>
    /// Gets or sets whether to generate filter methods.
    /// </summary>
    public bool IncludeFilters { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to generate sorting methods.
    /// </summary>
    public bool IncludeSorting { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to generate async methods.
    /// </summary>
    public bool GenerateAsyncMethods { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to include validation.
    /// </summary>
    public bool IncludeValidation { get; set; } = false;

    /// <summary>
    /// Gets or sets the namespace for generated classes. If null, uses the same namespace as the target class.
    /// </summary>
    public string? Namespace { get; set; }
}