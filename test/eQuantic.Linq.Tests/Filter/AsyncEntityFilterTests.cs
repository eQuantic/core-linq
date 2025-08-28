using System.Linq.Expressions;
using eQuantic.Linq.Filter;

namespace eQuantic.Linq.Tests.Filter;

[TestFixture]
public class AsyncEntityFilterTests
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
    public async Task FilterAsync_WithPredicateFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var filter = AsyncEntityFilter<ObjectA>.Where(o => o.Age > 30);

        // Act
        var result = await filter.FilterAsync(queryable);

        // Assert
        Assert.That(result, Has.Length.EqualTo(2));
        Assert.That(result.All(o => o.Age > 30), Is.True);
    }

    [Test]
    public async Task FilterAsync_WithCancellationToken_ShouldWork()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var filter = AsyncEntityFilter<ObjectA>.Where(o => o.IsActive);

        // Act
        var result = await filter.FilterAsync(queryable, CancellationToken.None);

        // Assert - Should complete without cancellation
        Assert.That(result, Has.Length.EqualTo(3));
        Assert.That(result.All(o => o.IsActive), Is.True);
    }

    [Test]
    public async Task FilterAsyncEnumerable_ShouldYieldFilteredResults()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var filter = AsyncEntityFilter<ObjectA>.Where(o => o.Name.StartsWith("A"));

        // Act
        var results = new List<ObjectA>();
        await foreach (var item in filter.FilterAsyncEnumerable(queryable))
        {
            results.Add(item);
        }

        // Assert
        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results.First().Name, Is.EqualTo("Alice"));
    }

    [Test]
    public async Task FirstOrDefaultAsync_WithMatchingFilter_ShouldReturnFirstMatch()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var filter = AsyncEntityFilter<ObjectA>.Where(o => o.Age > 30);

        // Act
        var result = await filter.FirstOrDefaultAsync(queryable);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Age, Is.GreaterThan(30));
    }

    [Test]
    public async Task FirstOrDefaultAsync_WithNoMatches_ShouldReturnNull()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var filter = AsyncEntityFilter<ObjectA>.Where(o => o.Age > 100);

        // Act
        var result = await filter.FirstOrDefaultAsync(queryable);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task CountAsync_WithFilter_ShouldReturnCorrectCount()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var filter = AsyncEntityFilter<ObjectA>.Where(o => o.IsActive);

        // Act
        var count = await filter.CountAsync(queryable);

        // Assert
        Assert.That(count, Is.EqualTo(3));
    }

    [Test]
    public async Task AnyAsync_WithMatchingFilter_ShouldReturnTrue()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var filter = AsyncEntityFilter<ObjectA>.Where(o => o.Name == "Alice");

        // Act
        var hasAny = await filter.AnyAsync(queryable);

        // Assert
        Assert.That(hasAny, Is.True);
    }

    [Test]
    public async Task AnyAsync_WithNoMatches_ShouldReturnFalse()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var filter = AsyncEntityFilter<ObjectA>.Where(o => o.Name == "NonExistent");

        // Act
        var hasAny = await filter.AnyAsync(queryable);

        // Assert
        Assert.That(hasAny, Is.False);
    }

    [Test]
    public async Task Where_WithCompositeFiltering_ShouldCombineFilters()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var filters = new[]
        {
            new Filtering("Age", "30", FilterOperator.GreaterThan),
            new Filtering("IsActive", "true", FilterOperator.Equal)
        };

        // Act
        var filter = AsyncEntityFilter<ObjectA>.Where(filters);
        var result = await filter.FilterAsync(queryable);

        // Assert
        Assert.That(result, Has.Length.EqualTo(2));
        Assert.That(result.All(o => o.Age > 30 && o.IsActive), Is.True);
    }

    [Test]
    public void AsQueryable_ShouldReturnEmptyFilter()
    {
        // Act
        var filter = AsyncEntityFilter<ObjectA>.AsQueryable();

        // Assert
        Assert.That(filter, Is.Not.Null);
        // Can't check exact type due to access level, but we can verify behavior
        var result = filter.Filter(_testData.AsQueryable());
        Assert.That(result.Count(), Is.EqualTo(_testData.Count));
    }

    [Test]
    public async Task EmptyFilter_ShouldNotFilterResults()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var filter = AsyncEntityFilter<ObjectA>.AsQueryable();

        // Act
        var result = await filter.FilterAsync(queryable);

        // Assert
        Assert.That(result, Has.Length.EqualTo(_testData.Count));
    }

    [Test]
    public void Where_WithNullPredicate_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            AsyncEntityFilter<ObjectA>.Where((Expression<Func<ObjectA, bool>>)null!));
    }

    [Test]
    public async Task FilterAsync_WithExpressionCaching_ShouldUseCache()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        Expression<Func<ObjectA, bool>> sharedExpression = o => o.Age > 25;
        
        // Clear cache to start fresh
        AsyncEntityFilter<ObjectA>.ExpressionCache.Clear();
        
        // Act - Multiple calls with same shared expression
        var filter1 = AsyncEntityFilter<ObjectA>.Where(sharedExpression);
        var filter2 = AsyncEntityFilter<ObjectA>.Where(sharedExpression);
        
        var result1 = await filter1.FilterAsync(queryable);
        var result2 = await filter2.FilterAsync(queryable);

        // Assert
        Assert.That(result1, Has.Length.EqualTo(result2.Length));
        
        // Verify cache statistics - should have at least one entry
        var cacheStats = AsyncEntityFilter<ObjectA>.ExpressionCache.GetStatistics();
        Assert.That(cacheStats.TotalEntries, Is.GreaterThan(0));
    }
}