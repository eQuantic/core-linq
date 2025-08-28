using eQuantic.Linq.Casting;
using eQuantic.Linq.Filter;
using eQuantic.Linq.Filter.Extensions;

namespace eQuantic.Linq.Tests.Filter.Extensions;

[TestFixture]
public class CastWithDebugTests
{
    [Test]
    public void CastWith_ExcludeUnmapped_DetailedDebug()
    {
        // Arrange
        IFiltering[] filters =
        [
            new Filtering<ObjectA>(o => o.PropertyA, "test1"),     // Will be mapped
            new Filtering<ObjectA>(o => o.CommonProperty, "test2", FilterOperator.NotEqual) // Won't be mapped
        ];

        Console.WriteLine("=== ORIGINAL CAST METHOD ===");
        var originalResult = filters.Cast<ObjectB>(opt => opt
            .Map(nameof(ObjectA.PropertyA), o => o.PropertyB)
            .ExcludeUnmapped());
        
        Console.WriteLine($"Original Cast result: {originalResult.Length} items");
        foreach (var item in originalResult)
        {
            Console.WriteLine($"  {item.ColumnName} = '{item.StringValue}' ({item.Operator})");
        }

        Console.WriteLine("\n=== CAST WITH FLUENT BUILDER ===");
        var fluentResult = filters.CastWith<ObjectB>(builder => builder
            .MapFilter(nameof(ObjectA.PropertyA), o => o.PropertyB)
            .ExcludeUnmapped());
        
        Console.WriteLine($"CastWith result: {fluentResult.Length} items");
        foreach (var item in fluentResult)
        {
            Console.WriteLine($"  {item.ColumnName} = '{item.StringValue}' ({item.Operator})");
        }

        Console.WriteLine("\n=== WITHOUT EXCLUDE UNMAPPED ===");
        var withoutExcludeResult = filters.Cast<ObjectB>(opt => opt
            .Map(nameof(ObjectA.PropertyA), o => o.PropertyB));
        
        Console.WriteLine($"Without ExcludeUnmapped result: {withoutExcludeResult.Length} items");
        foreach (var item in withoutExcludeResult)
        {
            Console.WriteLine($"  {item.ColumnName} = '{item.StringValue}' ({item.Operator})");
        }

        // Assert
        Assert.That(originalResult.Length, Is.EqualTo(fluentResult.Length), 
            "Original Cast and CastWith should produce same results");
    }

    [Test]
    public void DebugFluentCastBuilderDirectly()
    {
        // Test FluentCastBuilder directly
        var builder = FluentCastBuilder<ObjectB>.Create();
        builder.MapFilter(nameof(ObjectA.PropertyA), o => o.PropertyB)
               .ExcludeUnmapped();
        
        var options = builder.BuildFilteringOptions();
        
        Console.WriteLine("=== FLUENT BUILDER OPTIONS DEBUG ===");
        Console.WriteLine($"ExcludeUnmapped: {options.GetExcludeUnmapped()}");
        Console.WriteLine($"Mappings count: {options.GetMapping().Count}");
        foreach (var mapping in options.GetMapping())
        {
            Console.WriteLine($"  Mapping: {mapping.Key} -> {mapping.Value.ColumnExpression?.ToString() ?? mapping.Value.ColumnName}");
        }
        
        // Assert
        Assert.That(options.GetExcludeUnmapped(), Is.True, "FluentBuilder should set ExcludeUnmapped correctly");
    }
}