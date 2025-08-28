using eQuantic.Linq.Sorter;
using eQuantic.Linq.Sorter.Extensions;

namespace eQuantic.Linq.Tests.Sorter.Extensions;

[TestFixture]
public class SortingExtensionsTests
{
    [Test]
    public void SortingExtensions_Cast()
    {
        // Arrange
        ISorting[] sorters = { 
            new Sorting<ObjectA>(o => o.PropertyA),
            new Sorting<ObjectA>(o => o.CommonProperty, SortDirection.Descending),
            new Sorting<ObjectA>(o => o.Date),
            new Sorting<ObjectA>(o => o.SubObject.PropertyA)
        };

        // Act
        var sorterB = sorters.Cast<ObjectB>(opt => 
            opt
                .Map(nameof(ObjectA.PropertyA), o => o.PropertyB)
                .Map($"{nameof(ObjectA.SubObject)}.{nameof(SubObjectA.PropertyA)}", o => o.SubObject.PropertyB)
        );
        
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