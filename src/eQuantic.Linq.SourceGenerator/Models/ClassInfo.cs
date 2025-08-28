using System.Collections.Generic;

namespace eQuantic.Linq.SourceGenerator.Models;

internal sealed class ClassInfo
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<PropertyInfo> Properties { get; set; } = new();
    public GenerateSpecificationsAttribute Configuration { get; set; } = new();
}

internal sealed class PropertyInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsValueType { get; set; }
    public SpecPropertyAttribute? Configuration { get; set; }
}