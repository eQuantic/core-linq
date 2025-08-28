using eQuantic.Linq.Filter;
using eQuantic.Linq.SourceGenerator;

namespace SourceGeneratorExample.Models;

[GenerateSpecifications]
public partial class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string? Description { get; set; }

    public bool IsAvailable { get; set; }

    public DateTime CreatedAt { get; set; }

    public int CategoryId { get; set; }
}