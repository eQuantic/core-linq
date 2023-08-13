using eQuantic.Linq.Filter;
using eQuantic.Linq.Filter.Extensions;

namespace eQuantic.Linq.Tests.Filter.Extensions;

[TestFixture]
public class FilteringExtensionsTests
{
    public class ObjectA
    {
        public string PropertyA { get; set; } = string.Empty;
        public string CommonProperty { get; set; } = string.Empty;
    }

    public class ObjectB
    {
        public string PropertyB { get; set; } = string.Empty;
        public string CommonProperty { get; set; } = string.Empty;
    }

    [Test]
    public void FilteringExtensions_Cast()
    {
        // Arrange
        IFiltering[] filters =
        {
            new Filtering<ObjectA>(o => o.PropertyA, "test"),
            new Filtering<ObjectA>(o => o.CommonProperty, "test", FilterOperator.NotEqual)
        };

        // Act
        var filterB = filters.Cast<ObjectB>(opt => opt.Map(nameof(ObjectA.PropertyA), o => o.PropertyB));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(filterB[0].ColumnName, Is.EqualTo(nameof(ObjectB.PropertyB)));
            Assert.That(filterB[0].StringValue, Is.EqualTo("test"));
            
            Assert.That(filterB[1].ColumnName, Is.EqualTo(nameof(ObjectB.CommonProperty)));
            Assert.That(filterB[1].StringValue, Is.EqualTo("test"));
            Assert.That(filterB[1].Operator, Is.EqualTo(FilterOperator.NotEqual));
        });
    }
}