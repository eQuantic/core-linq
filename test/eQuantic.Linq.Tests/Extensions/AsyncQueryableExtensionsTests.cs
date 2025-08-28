using eQuantic.Linq.Extensions;
using eQuantic.Linq.Filter;
using eQuantic.Linq.Sorter;

namespace eQuantic.Linq.Tests.Extensions;

[TestFixture]
public class AsyncQueryableExtensionsTests
{
    private List<ObjectA> _testData = null!;

    [SetUp]
    public void SetUp()
    {
        _testData = new List<ObjectA>
        {
            new() { Id = 1, Name = "Alice", Age = 25, IsActive = true },
            new() { Id = 2, Name = "Bob", Age = 30, IsActive = false },
            new() { Id = 3, Name = "Charlie", Age = 35, IsActive = true },
            new() { Id = 4, Name = "David", Age = 28, IsActive = false },
            new() { Id = 5, Name = "Eva", Age = 32, IsActive = true }
        };
    }

    [Test]
    public async Task WhereAsync_WithFilterings_ShouldReturnFilteredResults()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var filterings = new IFiltering[]
        {
            new Filtering("Age", "30", FilterOperator.GreaterThan)
        };

        // Act
        var result = await queryable.WhereAsync(CancellationToken.None, filterings);

        // Assert
        Assert.That(result, Has.Length.EqualTo(2));
        Assert.That(result.All(o => o.Age > 30), Is.True);
    }

    [Test]
    public async Task WhereAsyncEnumerable_ShouldYieldFilteredResults()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var filterings = new IFiltering[]
        {
            new Filtering("IsActive", "true", FilterOperator.Equal)
        };

        // Act
        var results = new List<ObjectA>();
        await foreach (var item in queryable.WhereAsyncEnumerable(filterings))
        {
            results.Add(item);
        }

        // Assert
        Assert.That(results, Has.Count.EqualTo(3));
        Assert.That(results.All(o => o.IsActive), Is.True);
    }

    [Test]
    public async Task OrderByAsync_WithSortings_ShouldReturnSortedResults()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var sortings = new ISorting[]
        {
            new Sorting("Age", SortDirection.Ascending)
        };

        // Act
        var result = await queryable.OrderByAsync(CancellationToken.None, sortings);

        // Assert
        Assert.That(result, Has.Length.EqualTo(_testData.Count));
        Assert.That(result.Select(o => o.Age), Is.Ordered);
        Assert.That(result.First().Name, Is.EqualTo("Alice"));
    }

    [Test]
    public async Task OrderByWithNullCheckAsync_ShouldHandleNullsCorrectly()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var sortings = new ISorting[]
        {
            new Sorting("Name", SortDirection.Descending)
        };

        // Act
        var result = await queryable.OrderByWithNullCheckAsync(CancellationToken.None, sortings);

        // Assert
        Assert.That(result, Has.Length.EqualTo(_testData.Count));
        Assert.That(result.Select(o => o.Name), Is.Ordered.Descending);
        Assert.That(result.First().Name, Is.EqualTo("Eva"));
    }

    [Test]
    public async Task OrderByAsyncEnumerable_ShouldYieldSortedResults()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var sortings = new ISorting[]
        {
            new Sorting("Id", SortDirection.Ascending)
        };

        // Act
        var results = new List<ObjectA>();
        await foreach (var item in queryable.OrderByAsyncEnumerable(sortings))
        {
            results.Add(item);
        }

        // Assert
        Assert.That(results, Has.Count.EqualTo(_testData.Count));
        Assert.That(results.Select(o => o.Id), Is.Ordered);
    }

    [Test]
    public async Task FilterSortPageAsync_WithFilteringAndSorting_ShouldReturnPagedResults()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var filterings = new IFiltering[]
        {
            new Filtering("IsActive", "true", FilterOperator.Equal)
        };
        var sortings = new ISorting[]
        {
            new Sorting("Age", SortDirection.Ascending)
        };
        const int skip = 1;
        const int take = 2;

        // Act
        var result = await queryable.FilterSortPageAsync(skip, take, CancellationToken.None, filterings, sortings);

        // Assert
        Assert.That(result, Has.Length.EqualTo(take));
        Assert.That(result.All(o => o.IsActive), Is.True);
        Assert.That(result.Select(o => o.Age), Is.Ordered);
    }

    [Test]
    public async Task FilterSortPageAsync_WithOnlyFiltering_ShouldReturnFilteredPagedResults()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var filterings = new IFiltering[]
        {
            new Filtering("Age", "28", FilterOperator.GreaterThanOrEqual)
        };
        const int skip = 0;
        const int take = 2;

        // Act
        var result = await queryable.FilterSortPageAsync(skip, take, CancellationToken.None, filterings);

        // Assert
        Assert.That(result, Has.Length.EqualTo(take));
        Assert.That(result.All(o => o.Age >= 28), Is.True);
    }

    [Test]
    public async Task FilterSortPageAsync_WithOnlySorting_ShouldReturnSortedPagedResults()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var sortings = new ISorting[]
        {
            new Sorting("Name", SortDirection.Ascending)
        };
        const int skip = 1;
        const int take = 2;

        // Act
        var result = await queryable.FilterSortPageAsync(skip, take, CancellationToken.None, null, sortings);

        // Assert
        Assert.That(result, Has.Length.EqualTo(take));
        Assert.That(result.Select(o => o.Name), Is.Ordered);
    }

    [Test]
    public async Task FilterSortPageAsync_WithNoFilteringOrSorting_ShouldReturnPagedResults()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        const int skip = 2;
        const int take = 2;

        // Act
        var result = await queryable.FilterSortPageAsync(skip, take);

        // Assert
        Assert.That(result, Has.Length.EqualTo(take));
    }

    [Test]
    public async Task FirstOrDefaultAsync_WithMatchingFilter_ShouldReturnFirst()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var filterings = new IFiltering[]
        {
            new Filtering("Age", "30", FilterOperator.GreaterThan)
        };

        // Act
        var result = await queryable.FirstOrDefaultAsync(CancellationToken.None, filterings);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Age, Is.GreaterThan(30));
    }

    [Test]
    public async Task FirstOrDefaultAsync_WithNoMatches_ShouldReturnNull()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var filterings = new IFiltering[]
        {
            new Filtering("Age", "100", FilterOperator.GreaterThan)
        };

        // Act
        var result = await queryable.FirstOrDefaultAsync(CancellationToken.None, filterings);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task CountAsync_WithFiltering_ShouldReturnCorrectCount()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var filterings = new IFiltering[]
        {
            new Filtering("IsActive", "true", FilterOperator.Equal)
        };

        // Act
        var count = await queryable.CountAsync(CancellationToken.None, filterings);

        // Assert
        Assert.That(count, Is.EqualTo(3));
    }

    [Test]
    public async Task AnyAsync_WithMatchingFilter_ShouldReturnTrue()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var filterings = new IFiltering[]
        {
            new Filtering("Name", "Alice", FilterOperator.Equal)
        };

        // Act
        var hasAny = await queryable.AnyAsync(CancellationToken.None, filterings);

        // Assert
        Assert.That(hasAny, Is.True);
    }

    [Test]
    public async Task AnyAsync_WithNoMatches_ShouldReturnFalse()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var filterings = new IFiltering[]
        {
            new Filtering("Name", "NonExistent", FilterOperator.Equal)
        };

        // Act
        var hasAny = await queryable.AnyAsync(CancellationToken.None, filterings);

        // Assert
        Assert.That(hasAny, Is.False);
    }

    [Test]
    public async Task WhereAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var filterings = new IFiltering[]
        {
            new Filtering("IsActive", "true", FilterOperator.Equal)
        };
        using var cts = new CancellationTokenSource();

        // Act
        cts.Cancel();

        // Assert
        Assert.ThrowsAsync<OperationCanceledException>(async () => 
            await queryable.WhereAsync(cts.Token, filterings));
    }

    [Test]
    public async Task OrderByAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var sortings = new ISorting[]
        {
            new Sorting("Name", SortDirection.Ascending)
        };
        using var cts = new CancellationTokenSource();

        // Act
        cts.Cancel();

        // Assert
        Assert.ThrowsAsync<TaskCanceledException>(async () => 
            await queryable.OrderByAsync(cts.Token, sortings));
    }

    [Test]
    public async Task ComplexScenario_FilterSortCount_ShouldWorkTogether()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var filterings = new IFiltering[]
        {
            new Filtering("Age", "27", FilterOperator.GreaterThan)
        };
        var sortings = new ISorting[]
        {
            new Sorting("Age", SortDirection.Descending),
            new Sorting("Name", SortDirection.Ascending)
        };

        // Act
        var filteredCount = await queryable.CountAsync(CancellationToken.None, filterings);
        var sortedResults = await queryable.OrderByAsync(CancellationToken.None, sortings);
        var filteredAndSorted = await queryable.FilterSortPageAsync(0, 10, CancellationToken.None, filterings, sortings);

        // Assert
        Assert.That(filteredCount, Is.EqualTo(4)); // Age > 27: Bob(30), Charlie(35), David(28), Eva(32)
        Assert.That(sortedResults, Has.Length.EqualTo(_testData.Count));
        Assert.That(filteredAndSorted, Has.Length.EqualTo(4));
        Assert.That(filteredAndSorted.All(o => o.Age > 27), Is.True);
        
        // Verify sorting: Descending by Age, then Ascending by Name
        Assert.That(filteredAndSorted.Select(o => o.Age), Is.Ordered.Descending);
    }
}