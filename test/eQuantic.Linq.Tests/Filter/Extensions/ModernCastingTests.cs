using eQuantic.Linq.Filter;
using eQuantic.Linq.Filter.Extensions;

namespace eQuantic.Linq.Tests.Filter.Extensions;

[TestFixture]
public class ModernCastingTests
{
    [Test]
    public void CastWith_UsingFluentBuilder_ShouldWork()
    {
        // Arrange
        IFiltering[] filters =
        [
            new Filtering<ObjectA>(o => o.PropertyA, "test"),
            new Filtering<ObjectA>(o => o.CommonProperty, "test", FilterOperator.NotEqual)
        ];

        // Act - Using modern CastWith method with FluentBuilder
        var filterB = filters.CastWith<ObjectB>(builder => builder
            .MapFilter(nameof(ObjectA.PropertyA), o => o.PropertyB));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(filterB, Has.Length.EqualTo(2));
            Assert.That(filterB[0].ColumnName, Is.EqualTo(nameof(ObjectB.PropertyB)));
            Assert.That(filterB[0].StringValue, Is.EqualTo("test"));
            Assert.That(filterB[1].ColumnName, Is.EqualTo(nameof(ObjectB.CommonProperty)));
            Assert.That(filterB[1].Operator, Is.EqualTo(FilterOperator.NotEqual));
        });
    }

    [Test]
    public void CastWith_SingleFilter_ShouldWork()
    {
        // Arrange
        var filter = new Filtering<ObjectA>(o => o.PropertyA, "modern");

        // Act - Using modern CastWith method with single filter
        var filterB = filter.CastWith<ObjectB>(builder => builder
            .MapFilter(nameof(ObjectA.PropertyA), o => o.PropertyB));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(filterB, Has.Length.EqualTo(1));
            Assert.That(filterB[0].ColumnName, Is.EqualTo(nameof(ObjectB.PropertyB)));
            Assert.That(filterB[0].StringValue, Is.EqualTo("modern"));
        });
    }

    [Test]
    public void CastWith_WithValueTransform_ShouldWork()
    {
        // Arrange
        var filter = new Filtering<ObjectA>(o => o.PropertyA, "lowercase");

        // Act - Using modern CastWith with value transformation
        var filterB = filter.CastWith<ObjectB>(builder => builder
            .MapFilter(nameof(ObjectA.PropertyA), o => o.PropertyB, value => value.ToUpperInvariant()));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(filterB, Has.Length.EqualTo(1));
            Assert.That(filterB[0].ColumnName, Is.EqualTo(nameof(ObjectB.PropertyB)));
            Assert.That(filterB[0].StringValue, Is.EqualTo("LOWERCASE"));
        });
    }
}