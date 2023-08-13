using eQuantic.Linq.Filter;
using eQuantic.Linq.Filter.Extensions;

namespace eQuantic.Linq.Tests.Filter.Extensions;

[TestFixture]
public class FilteringExtensionsTests
{
    [Test]
    public void FilteringExtensions_Cast()
    {
        // Arrange
        IFiltering[] filters =
        {
            new Filtering<ObjectA>(o => o.PropertyA, "test"),
            new Filtering<ObjectA>(o => o.CommonProperty, "test", FilterOperator.NotEqual),
            new Filtering<ObjectA>(o => o.SubObject.PropertyA, "test", FilterOperator.StartsWith)
        };

        // Act
        var filterB = filters.Cast<ObjectB>(opt => opt
            .Map(nameof(ObjectA.PropertyA), o => o.PropertyB)
            .Map($"{nameof(ObjectA.SubObject)}.{nameof(SubObjectA.PropertyA)}", o => o.SubObject.PropertyB)
        );

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(filterB[0].ColumnName, Is.EqualTo(nameof(ObjectB.PropertyB)));
            Assert.That(filterB[0].StringValue, Is.EqualTo("test"));
            
            Assert.That(filterB[1].ColumnName, Is.EqualTo(nameof(ObjectB.CommonProperty)));
            Assert.That(filterB[1].StringValue, Is.EqualTo("test"));
            Assert.That(filterB[1].Operator, Is.EqualTo(FilterOperator.NotEqual));
            
            Assert.That(filterB[2].ColumnName, Is.EqualTo($"{nameof(ObjectB.SubObject)}.{nameof(SubObjectB.PropertyB)}"));
            Assert.That(filterB[2].StringValue, Is.EqualTo("test"));
            Assert.That(filterB[2].Operator, Is.EqualTo(FilterOperator.StartsWith));
        });
    }
}