using eQuantic.Linq.Filter;
using eQuantic.Linq.SourceGenerator;

namespace SourceGeneratorExample.Models;

[GenerateSpecifications(
    IncludeFilters = true,
    IncludeSorting = true
)]
public partial class User
{
    public int Id { get; set; }

    [SpecProperty(
        Operators = eQuantic.Linq.SourceGenerator.FilterOperator.Equal | eQuantic.Linq.SourceGenerator.FilterOperator.Contains | eQuantic.Linq.SourceGenerator.FilterOperator.StartsWith,
        MethodName = "Name"
    )]
    public string Name { get; set; } = string.Empty;

    public string? Email { get; set; }

    [SpecProperty(
        Operators = eQuantic.Linq.SourceGenerator.FilterOperator.Equal | eQuantic.Linq.SourceGenerator.FilterOperator.GreaterThan | eQuantic.Linq.SourceGenerator.FilterOperator.LessThan
    )]
    public int Age { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    [SpecProperty(Exclude = true)]
    public string Password { get; set; } = string.Empty;
}