using eQuantic.Linq;
using eQuantic.Linq.Filter;
using eQuantic.Linq.Sorter;

namespace eQuantic.Linq.Tests;

[TestFixture]
public class FluentQueryTests
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
    public void FluentQuery_BasicFiltering_ShouldWork()
    {
        // Arrange
        var queryable = _testData.AsQueryable();

        // Act - Using new fluent API
        var result = Query.For<ObjectA>()
            .Where(o => o.Age > 30)
            .ApplyTo(queryable)
            .ToArray();

        // Assert
        Assert.That(result, Has.Length.EqualTo(2));
        Assert.That(result.All(o => o.Age > 30), Is.True);
    }

    [Test]
    public void FluentQuery_ChainingAndOr_ShouldWork()
    {
        // Arrange
        var queryable = _testData.AsQueryable();

        // Act - Chain And/Or conditions
        var result = Query.For<ObjectA>()
            .Where(o => o.Age > 25)
            .And(o => o.IsActive)
            .ApplyTo(queryable)
            .ToArray();

        // Assert
        Assert.That(result, Has.Length.EqualTo(2)); // Charlie(35) and Eva(32)
        Assert.That(result.All(o => o.Age > 25 && o.IsActive), Is.True);
    }

    [Test]
    public void FluentQuery_WithSorting_ShouldWork()
    {
        // Arrange
        var queryable = _testData.AsQueryable();

        // Act - Apply filtering and sorting
        var result = Query.For<ObjectA>()
            .Where(o => o.IsActive)
            .OrderBy(o => o.Age)
            .ApplyTo(queryable)
            .ToArray();

        // Assert
        Assert.That(result, Has.Length.EqualTo(3));
        Assert.That(result.Select(o => o.Age), Is.Ordered);
        Assert.That(result.First().Name, Is.EqualTo("Alice"));
    }

    [Test]
    public void FluentQuery_ComparisonMethods_ShouldWork()
    {
        // Arrange
        var queryable = _testData.AsQueryable();

        // Act - Use typed comparison methods
        var result = Query.For<ObjectA>()
            .WhereGreaterThan(o => o.Age, 28)
            .WhereLessThanOrEqual(o => o.Age, 35)
            .ApplyTo(queryable)
            .ToArray();

        // Assert
        Assert.That(result, Has.Length.EqualTo(3)); // Bob(30), Charlie(35), Eva(32)
        Assert.That(result.All(o => o.Age > 28 && o.Age <= 35), Is.True);
    }

    [Test]
    public void FluentQuery_WhereInCondition_ShouldWork()
    {
        // Arrange
        var queryable = _testData.AsQueryable();
        var targetAges = new[] { 25, 30, 35 };

        // Act - Use WhereIn method
        var result = Query.For<ObjectA>()
            .WhereIn(o => o.Age, targetAges)
            .OrderBy(o => o.Age)
            .ApplyTo(queryable)
            .ToArray();

        // Assert
        Assert.That(result, Has.Length.EqualTo(3)); // Alice(25), Bob(30), Charlie(35)
        Assert.That(result.Select(o => o.Age), Is.EqualTo(new[] { 25, 30, 35 }));
    }

    [Test]
    public void FluentQuery_BetweenCondition_ShouldWork()
    {
        // Arrange
        var queryable = _testData.AsQueryable();

        // Act - Use WhereBetween method
        var result = Query.For<ObjectA>()
            .WhereBetween(o => o.Age, 28, 32)
            .OrderBy(o => o.Age)
            .ApplyTo(queryable)
            .ToArray();

        // Assert
        Assert.That(result, Has.Length.EqualTo(3)); // David(28), Bob(30), Eva(32)
        Assert.That(result.All(o => o.Age >= 28 && o.Age <= 32), Is.True);
    }

    [Test]
    public async Task FluentQuery_AsyncOperations_ShouldWork()
    {
        // Arrange
        var queryable = _testData.AsQueryable();

        // Act - Use async operations
        var result = await Query.For<ObjectA>()
            .Where(o => o.IsActive)
            .OrderByDescending(o => o.Age)
            .ApplyToAsync(queryable);

        // Assert
        Assert.That(result, Has.Length.EqualTo(3));
        Assert.That(result.Select(o => o.Age), Is.Ordered.Descending);
        Assert.That(result.First().Name, Is.EqualTo("Charlie")); // Oldest active user
    }

    [Test]
    public void FluentQuery_ComplexChaining_ShouldWork()
    {
        // Arrange
        var queryable = _testData.AsQueryable();

        // Act - Complex chaining: current verbose vs new fluent API
        
        // Old way (commented):
        // var oldFilter = EntityFilter<ObjectA>.Where(u => u.Age > 18).Where(u => u.IsActive);
        // var oldSorter = EntitySorter<ObjectA>.OrderBy(u => u.Name).ThenByDescending(u => u.Age);
        
        // New fluent way:
        var result = Query.For<ObjectA>()
            .Where(u => u.Age > 18)
            .And(u => u.IsActive)
            .OrderBy(u => u.Name)
            .ThenByDescending(u => u.Age)
            .ApplyTo(queryable)
            .ToArray();

        // Assert
        Assert.That(result, Has.Length.EqualTo(3)); // All active users over 18
        Assert.That(result.Select(o => o.Name), Is.Ordered);
        Assert.That(result.All(o => o.Age > 18 && o.IsActive), Is.True);
    }

    [Test]
    public void ValueQuery_WithValueTypes_ShouldWork()
    {
        // Arrange - Test with value types (int)
        var numbers = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        // Act - Use ValueQuery for value types
        var result = ValueQuery.For<int>()
            .Where(x => x > 3)
            .WhereLessThanOrEqual(x => x, 8)
            .OrderByDescending(x => x)
            .ApplyTo(numbers)
            .ToArray();

        // Assert
        Assert.That(result, Is.EqualTo(new[] { 8, 7, 6, 5, 4 }));
    }

    [Test]
    public void ValueQuery_WithRecordTypes_ShouldWork()
    {
        // Arrange - Test with record types
        var records = new[]
        {
            new PersonRecord("Alice", 25),
            new PersonRecord("Bob", 30),
            new PersonRecord("Charlie", 35)
        };

        // Act - Use ValueQuery for record types
        var result = ValueQuery.For<PersonRecord>()
            .WhereGreaterThan(p => p.Age, 27)
            .OrderBy(p => p.Name)
            .ApplyTo(records)
            .ToArray();

        // Assert
        Assert.That(result, Has.Length.EqualTo(2)); // Bob and Charlie
        Assert.That(result.Select(p => p.Name), Is.EqualTo(new[] { "Bob", "Charlie" }));
    }
}

public record PersonRecord(string Name, int Age);