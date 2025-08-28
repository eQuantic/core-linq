using eQuantic.Linq.Filter;
using eQuantic.Linq.Filter.Extensions;

namespace eQuantic.Linq.Tests.Filter.Extensions;

[TestFixture]
public class ComparisonCastingTests
{
    [Test]
    public void Cast_Vs_CastWith_ShouldProduceSameResults()
    {
        // Arrange
        IFiltering[] filters =
        [
            new Filtering<ObjectA>(o => o.PropertyA, "test"),
            new Filtering<ObjectA>(o => o.CommonProperty, "test", FilterOperator.NotEqual)
        ];

        // Act - Original Cast method
        var originalResult = filters.Cast<ObjectB>(opt => opt
            .Map(nameof(ObjectA.PropertyA), o => o.PropertyB));

        // Act - New CastWith method
        var modernResult = filters.CastWith<ObjectB>(builder => builder
            .MapFilter(nameof(ObjectA.PropertyA), o => o.PropertyB));

        // Assert - Both should produce the same results
        Assert.Multiple(() =>
        {
            Assert.That(modernResult, Has.Length.EqualTo(originalResult.Length), "Length should be equal");
            
            for (int i = 0; i < originalResult.Length; i++)
            {
                Assert.That(modernResult[i].ColumnName, Is.EqualTo(originalResult[i].ColumnName), $"ColumnName at index {i}");
                Assert.That(modernResult[i].StringValue, Is.EqualTo(originalResult[i].StringValue), $"StringValue at index {i}");
                Assert.That(modernResult[i].Operator, Is.EqualTo(originalResult[i].Operator), $"Operator at index {i}");
            }
        });
    }
}