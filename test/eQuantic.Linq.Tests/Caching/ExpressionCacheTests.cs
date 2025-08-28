using System.Linq.Expressions;
using eQuantic.Linq.Caching;

namespace eQuantic.Linq.Tests.Caching;

[TestFixture]
public class ExpressionCacheTests
{
    private ExpressionCache _cache = null!;

    [SetUp]
    public void SetUp()
    {
        _cache = new ExpressionCache();
    }

    [TearDown]
    public void TearDown()
    {
        _cache.Clear();
    }

    [Test]
    public void GetOrCreate_WithNewKey_ShouldCreateAndCacheExpression()
    {
        // Arrange
        const string key = "test_key";
        Expression<Func<string, bool>> CreateExpression() => s => s.Contains("test");

        // Act
        var result1 = _cache.GetOrCreate(key, CreateExpression);
        var result2 = _cache.GetOrCreate(key, CreateExpression);

        // Assert
        Assert.That(result1, Is.Not.Null);
        Assert.That(result2, Is.Not.Null);
        Assert.That(ReferenceEquals(result1, result2), Is.True, "Should return the same cached instance");
    }

    [Test]
    public void GetOrCreate_WithExistingKey_ShouldReturnCachedInstance()
    {
        // Arrange
        const string key = "existing_key";
        var factoryCallCount = 0;
        Expression<Func<int, bool>> CreateExpression()
        {
            factoryCallCount++;
            return x => x > 0;
        }

        // Act
        var result1 = _cache.GetOrCreate(key, CreateExpression);
        var result2 = _cache.GetOrCreate(key, CreateExpression);

        // Assert
        Assert.That(factoryCallCount, Is.EqualTo(1), "Factory should be called only once");
        Assert.That(ReferenceEquals(result1, result2), Is.True);
    }

    [Test]
    public void GetStatistics_AfterHitsAndMisses_ShouldReturnCorrectValues()
    {
        // Arrange
        const string key1 = "key1";
        const string key2 = "key2";
        Expression<Func<string, bool>> CreateExpression1() => s => s.Length > 0;
        Expression<Func<string, bool>> CreateExpression2() => s => s.StartsWith("a");

        // Act
        _cache.GetOrCreate(key1, CreateExpression1); // Miss
        _cache.GetOrCreate(key1, CreateExpression1); // Hit
        _cache.GetOrCreate(key2, CreateExpression2); // Miss
        _cache.GetOrCreate(key1, CreateExpression1); // Hit

        var stats = _cache.GetStatistics();

        // Assert
        Assert.That(stats.HitCount, Is.EqualTo(2));
        Assert.That(stats.MissCount, Is.EqualTo(2));
        Assert.That(stats.TotalEntries, Is.EqualTo(2));
        Assert.That(stats.HitRatio, Is.EqualTo(0.5));
    }

    [Test]
    public void Clear_ShouldResetCacheAndStatistics()
    {
        // Arrange
        const string key = "test_key";
        Expression<Func<int, bool>> CreateExpression() => x => x > 0;

        _cache.GetOrCreate(key, CreateExpression);
        _cache.GetOrCreate(key, CreateExpression);

        // Act
        _cache.Clear();
        var stats = _cache.GetStatistics();

        // Assert
        Assert.That(stats.HitCount, Is.EqualTo(0));
        Assert.That(stats.MissCount, Is.EqualTo(0));
        Assert.That(stats.TotalEntries, Is.EqualTo(0));
        Assert.That(stats.HitRatio, Is.EqualTo(0.0));
    }

    [Test]
    public void CreateKey_WithMultipleComponents_ShouldJoinWithPipe()
    {
        // Act
        var key = ExpressionCache.CreateKey("component1", "component2", "component3");

        // Assert
        Assert.That(key, Is.EqualTo("component1|component2|component3"));
    }

    [Test]
    public void CreateFilterKey_WithTypeAndParameters_ShouldReturnFormattedKey()
    {
        // Act
        var key = ExpressionCache.CreateFilterKey<string>("Name", "John", "Equal");

        // Assert
        Assert.That(key, Is.EqualTo("filter|System.String|Name|Equal|John"));
    }

    [Test]
    public void CreateSortKey_WithTypeAndParameters_ShouldReturnFormattedKey()
    {
        // Act
        var key = ExpressionCache.CreateSortKey<ObjectA>("Name", "Ascending");

        // Assert
        Assert.That(key, Does.Contain("sort"));
        Assert.That(key, Does.Contain("ObjectA"));
        Assert.That(key, Does.Contain("Name"));
        Assert.That(key, Does.Contain("Ascending"));
    }

    [Test]
    public void GetOrCreate_WithNullKey_ShouldThrowArgumentException()
    {
        // Arrange
        Expression<Func<string, bool>> CreateExpression() => s => s.Length > 0;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _cache.GetOrCreate(string.Empty, CreateExpression));
        Assert.Throws<ArgumentException>(() => _cache.GetOrCreate(null!, CreateExpression));
    }

    [Test]
    public void GetOrCreate_WithNullFactory_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _cache.GetOrCreate<Func<string, bool>>("key", null!));
    }

    [Test]
    public void GetOrCreate_WithCompiledExpression_ShouldWorkCorrectly()
    {
        // Arrange
        const string key = "compiled_test";
        Expression<Func<ObjectA, bool>> CreateExpression() => obj => obj.Name == "Test";

        // Act
        var compiledDelegate = _cache.GetOrCreate(key, CreateExpression);
        var testObject = new ObjectA { Name = "Test", Age = 25, Id = 1, IsActive = true };
        var result = compiledDelegate(testObject);

        // Assert
        Assert.That(result, Is.True);
    }
}
