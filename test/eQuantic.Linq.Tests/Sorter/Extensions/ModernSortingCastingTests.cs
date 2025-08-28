using eQuantic.Linq.Sorter;
using eQuantic.Linq.Sorter.Extensions;

namespace eQuantic.Linq.Tests.Sorter.Extensions;

[TestFixture]
public class ModernSortingCastingTests
{
    [Test]
    public void CastWith_UsingFluentBuilder_ShouldWork()
    {
        // Arrange
        ISorting[] sortings =
        [
            new Sorting<ObjectA>(o => o.PropertyA, SortDirection.Ascending),
            new Sorting<ObjectA>(o => o.Date, SortDirection.Descending)
        ];

        // Act - Using modern CastWith method with FluentBuilder
        var sortingB = sortings.CastWith<ObjectB>(builder => builder
            .MapSorting(nameof(ObjectA.PropertyA), o => o.PropertyB));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(sortingB, Has.Length.EqualTo(2));
            Assert.That(sortingB[0].ColumnName, Is.EqualTo(nameof(ObjectB.PropertyB)));
            Assert.That(sortingB[0].SortDirection, Is.EqualTo(SortDirection.Ascending));
            Assert.That(sortingB[1].ColumnName, Is.EqualTo(nameof(ObjectB.Date)));
            Assert.That(sortingB[1].SortDirection, Is.EqualTo(SortDirection.Descending));
        });
    }

    [Test]
    public void CastWith_SingleSorting_ShouldWork()
    {
        // Arrange
        var sorting = new Sorting<ObjectA>(o => o.PropertyA, SortDirection.Descending);

        // Act - Using modern CastWith method with single sorting
        var sortingB = sorting.CastWith<ObjectB>(builder => builder
            .MapSorting(nameof(ObjectA.PropertyA), o => o.PropertyB));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(sortingB, Has.Length.EqualTo(1));
            Assert.That(sortingB[0].ColumnName, Is.EqualTo(nameof(ObjectB.PropertyB)));
            Assert.That(sortingB[0].SortDirection, Is.EqualTo(SortDirection.Descending));
        });
    }
}