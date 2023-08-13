using eQuantic.Linq.Filter;
using eQuantic.Linq.Filter.Extensions;
using eQuantic.Linq.Sorter;
using eQuantic.Linq.Sorter.Extensions;

namespace eQuantic.Linq.Tests.Sorter.Extensions;

[TestFixture]
public class SortingExtensionsTests
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
    public void SortingExtensions_Cast()
    {
        // Arrange
        ISorting[] sorters = { 
            new Sorting<ObjectA>(o => o.PropertyA),
            new Sorting<ObjectA>(o => o.CommonProperty, SortDirection.Descending)
        };

        // Act
        var sorterB = sorters.Cast<ObjectB>(opt => opt.Map(nameof(ObjectA.PropertyA), o => o.PropertyB));
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(sorterB[0].ColumnName, Is.EqualTo(nameof(ObjectB.PropertyB)));
            Assert.That(sorterB[0].SortDirection, Is.EqualTo(SortDirection.Ascending));
            
            Assert.That(sorterB[1].ColumnName, Is.EqualTo(nameof(ObjectB.CommonProperty)));
            Assert.That(sorterB[1].SortDirection, Is.EqualTo(SortDirection.Descending));
        });
    }
}