using eQuantic.Linq.Filter;
using eQuantic.Linq.Filter.Extensions;

namespace eQuantic.Linq.Tests.Filter.Extensions;

[TestFixture]
public class ExcludeUnmappedDebugTests
{
    [Test]
    public void ExcludeUnmapped_ShouldExcludeUnmappedFields()
    {
        // Arrange
        IFiltering[] filters =
        [
            new Filtering<ObjectA>(o => o.PropertyA, "test1"),     // This will be mapped
            new Filtering<ObjectA>(o => o.CommonProperty, "test2") // This won't be mapped
        ];

        // Act - Map only PropertyA, exclude unmapped
        var result = filters.Cast<ObjectB>(opt => opt
            .Map(nameof(ObjectA.PropertyA), o => o.PropertyB)
            .ExcludeUnmapped());

        // Debug output
        Console.WriteLine($"Total filters: {result.Length}");
        foreach (var filter in result)
        {
            Console.WriteLine($"Filter: {filter.ColumnName} = {filter.StringValue}");
        }

        // Assert - Should only have the mapped PropertyA -> PropertyB
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Length.EqualTo(1), "Should only have mapped properties");
            Assert.That(result[0].ColumnName, Is.EqualTo(nameof(ObjectB.PropertyB)));
            Assert.That(result[0].StringValue, Is.EqualTo("test1"));
        });
    }

    [Test]
    public void WithoutExcludeUnmapped_ShouldIncludeUnmappedFields()
    {
        // Arrange
        IFiltering[] filters =
        [
            new Filtering<ObjectA>(o => o.PropertyA, "test1"),     // This will be mapped
            new Filtering<ObjectA>(o => o.CommonProperty, "test2") // This won't be mapped but should be auto-mapped
        ];

        // Act - Map only PropertyA, don't exclude unmapped
        var result = filters.Cast<ObjectB>(opt => opt
            .Map(nameof(ObjectA.PropertyA), o => o.PropertyB));

        // Debug output
        Console.WriteLine($"Total filters without exclude: {result.Length}");
        foreach (var filter in result)
        {
            Console.WriteLine($"Filter: {filter.ColumnName} = {filter.StringValue}");
        }

        // Assert - Should have both: mapped and auto-mapped
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Length.EqualTo(2), "Should have both mapped and auto-mapped properties");
            
            // Check mapped property
            var mappedFilter = result.First(f => f.ColumnName == nameof(ObjectB.PropertyB));
            Assert.That(mappedFilter.StringValue, Is.EqualTo("test1"));
            
            // Check auto-mapped property (CommonProperty exists in both ObjectA and ObjectB)
            var autoMappedFilter = result.First(f => f.ColumnName == nameof(ObjectB.CommonProperty));
            Assert.That(autoMappedFilter.StringValue, Is.EqualTo("test2"));
        });
    }
}