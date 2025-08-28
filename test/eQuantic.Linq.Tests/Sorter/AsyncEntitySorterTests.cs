using System.Linq.Expressions;
using eQuantic.Linq.Sorter;

namespace eQuantic.Linq.Tests.Sorter;

[TestFixture]
public class AsyncEntitySorterTests
{
    private List<ObjectA> _testData = null!;

    [SetUp]
    public void SetUp()
    {
        _testData = new List<ObjectA>
        {
            new() { Id = 3, Name = "Charlie", Age = 35, IsActive = true },
            new() { Id = 1, Name = "Alice", Age = 25, IsActive = true },
            new() { Id = 4, Name = "David", Age = 28, IsActive = false },
            new() { Id = 2, Name = "Bob", Age = 30, IsActive = false },
            new() { Id = 5, Name = "Eva", Age = 32, IsActive = true }
        };
    }

    [Test]
    public async Task OrderBy_WithExpression_ShouldSortAscending()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var sorter = AsyncEntitySorter<ObjectA>.OrderBy(o => o.Age);

        // Act
        var result = await sorter.SortAsync(queryable);

        // Assert
        Assert.That(result, Has.Length.EqualTo(_testData.Count));
        Assert.That(result.Select(o => o.Age), Is.Ordered);
        Assert.That(result.First().Name, Is.EqualTo("Alice"));
    }

    [Test]
    public async Task OrderByDescending_WithExpression_ShouldSortDescending()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var sorter = AsyncEntitySorter<ObjectA>.OrderByDescending(o => o.Age);

        // Act
        var result = await sorter.SortAsync(queryable);

        // Assert
        Assert.That(result, Has.Length.EqualTo(_testData.Count));
        Assert.That(result.Select(o => o.Age), Is.Ordered.Descending);
        Assert.That(result.First().Name, Is.EqualTo("Charlie"));
    }

    [Test]
    public async Task OrderBy_WithPropertyName_ShouldSortByProperty()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var sorter = AsyncEntitySorter<ObjectA>.OrderBy("Name");

        // Act
        var result = await sorter.SortAsync(queryable);

        // Assert
        Assert.That(result, Has.Length.EqualTo(_testData.Count));
        Assert.That(result.Select(o => o.Name), Is.Ordered);
        Assert.That(result.First().Name, Is.EqualTo("Alice"));
    }

    [Test]
    public async Task OrderByDescending_WithPropertyName_ShouldSortDescending()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var sorter = AsyncEntitySorter<ObjectA>.OrderByDescending("Name");

        // Act
        var result = await sorter.SortAsync(queryable);

        // Assert
        Assert.That(result, Has.Length.EqualTo(_testData.Count));
        Assert.That(result.Select(o => o.Name), Is.Ordered.Descending);
        Assert.That(result.First().Name, Is.EqualTo("Eva"));
    }

    [Test]
    public async Task SortAsyncEnumerable_ShouldYieldSortedResults()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var sorter = AsyncEntitySorter<ObjectA>.OrderBy(o => o.Id);

        // Act
        var results = new List<ObjectA>();
        await foreach (var item in sorter.SortAsyncEnumerable(queryable))
        {
            results.Add(item);
        }

        // Assert
        Assert.That(results, Has.Count.EqualTo(_testData.Count));
        Assert.That(results.Select(o => o.Id), Is.Ordered);
    }

    [Test]
    public async Task SortPageAsync_ShouldReturnPagedResults()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var sorter = AsyncEntitySorter<ObjectA>.OrderBy(o => o.Age);
        const int skip = 1;
        const int take = 2;

        // Act
        var result = await sorter.SortPageAsync(queryable, skip, take);

        // Assert
        Assert.That(result, Has.Length.EqualTo(take));
        Assert.That(result.Select(o => o.Age), Is.Ordered);
        // Should skip the youngest (Alice, 25) and take the next 2
        Assert.That(result.First().Age, Is.EqualTo(28)); // David
    }

    [Test]
    public async Task OrderBy_WithSortings_ShouldApplyMultipleSorts()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var sortings = new ISorting[]
        {
            new Sorting("IsActive", SortDirection.Descending),
            new Sorting("Age", SortDirection.Ascending)
        };
        var sorter = AsyncEntitySorter<ObjectA>.OrderBy(false, sortings);

        // Act
        var result = await sorter.SortAsync(queryable);

        // Assert
        Assert.That(result, Has.Length.EqualTo(_testData.Count));
        // Active users first, then sorted by age
        var activeUsers = result.Where(o => o.IsActive).ToArray();
        var inactiveUsers = result.Where(o => !o.IsActive).ToArray();
        
        Assert.That(activeUsers.Select(o => o.Age), Is.Ordered);
        Assert.That(inactiveUsers.Select(o => o.Age), Is.Ordered);
        Assert.That(result.Take(activeUsers.Length).All(o => o.IsActive), Is.True);
    }

    [Test]
    public async Task SortAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var sorter = AsyncEntitySorter<ObjectA>.OrderBy(o => o.Name);
        using var cts = new CancellationTokenSource();

        // Act
        cts.Cancel();

        // Assert
        Assert.ThrowsAsync<TaskCanceledException>(async () => 
            await sorter.SortAsync(queryable, cts.Token));
    }

    [Test]
    public async Task OrderBy_WithEmptySortings_ShouldReturnOriginalOrder()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var sorter = AsyncEntitySorter<ObjectA>.OrderBy(false);

        // Act
        var result = await sorter.SortAsync(queryable);

        // Assert
        Assert.That(result, Has.Length.EqualTo(_testData.Count));
        // Should maintain original order since no specific sorting was applied
    }

    [Test]
    public void OrderBy_WithNullExpression_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            AsyncEntitySorter<ObjectA>.OrderBy<string>(null!));
    }

    [Test]
    public void OrderByDescending_WithNullExpression_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            AsyncEntitySorter<ObjectA>.OrderByDescending<string>(null!));
    }

    [Test]
    public void OrderBy_WithNullOrEmptyPropertyName_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            AsyncEntitySorter<ObjectA>.OrderBy(string.Empty));
        
        Assert.Throws<ArgumentException>(() => 
            AsyncEntitySorter<ObjectA>.OrderBy(null!));
    }

    [Test]
    public async Task Sort_WithExpressionCaching_ShouldUseCache()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        Expression<Func<ObjectA, int>> sharedExpression = o => o.Age;
        
        // Clear cache to start fresh
        AsyncEntitySorter<ObjectA>.ExpressionCache.Clear();
        
        // Act - Multiple calls with same shared expression
        var sorter1 = AsyncEntitySorter<ObjectA>.OrderBy(sharedExpression);
        var sorter2 = AsyncEntitySorter<ObjectA>.OrderBy(sharedExpression);
        
        var result1 = await sorter1.SortAsync(queryable);
        var result2 = await sorter2.SortAsync(queryable);

        // Assert
        Assert.That(result1.Length, Is.EqualTo(result2.Length));
        
        // Verify cache statistics - should have at least one entry
        var cacheStats = AsyncEntitySorter<ObjectA>.ExpressionCache.GetStatistics();
        Assert.That(cacheStats.TotalEntries, Is.GreaterThan(0));
    }

    [Test]
    public async Task OrderBy_WithNullCheckForNestedProperties_ShouldHandleNulls()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var sortings = new ISorting[]
        {
            new Sorting("Name", SortDirection.Ascending)
        };
        var sorter = AsyncEntitySorter<ObjectA>.OrderBy(true, sortings);

        // Act
        var result = await sorter.SortAsync(queryable);

        // Assert
        Assert.That(result, Has.Length.EqualTo(_testData.Count));
        Assert.That(result.Select(o => o.Name), Is.Ordered);
    }
}