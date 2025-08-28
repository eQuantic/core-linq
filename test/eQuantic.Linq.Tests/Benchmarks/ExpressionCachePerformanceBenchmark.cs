using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using eQuantic.Linq.Caching;
using eQuantic.Linq.Filter;
using System.Linq.Expressions;

namespace eQuantic.Linq.Tests.Benchmarks;

/// <summary>
/// Benchmarks to demonstrate performance improvements with expression caching
/// </summary>
[SimpleJob(RuntimeMoniker.Net60)]
[MemoryDiagnoser]
[Config(typeof(Config))]
public class ExpressionCachePerformanceBenchmark
{
    private class Config : ManualConfig
    {
        public Config()
        {
            AddJob(Job.MediumRun.WithToolchain(InProcessEmitToolchain.Instance));
        }
    }

    private List<ObjectA> _testData = null!;
    private IExpressionCache _cache = null!;

    [GlobalSetup]
    public void Setup()
    {
        _testData = new List<ObjectA>();
        for (int i = 0; i < 10000; i++)
        {
            _testData.Add(new ObjectA
            {
                Id = i,
                Name = $"Name{i % 100}",
                Age = 20 + (i % 50),
                IsActive = i % 2 == 0
            });
        }

        _cache = new ExpressionCache();
    }

    [Benchmark(Baseline = true)]
    public int WithoutCache_CreateAndExecuteFilter()
    {
        var queryable = _testData.AsQueryable();
        var count = 0;

        // Simulate repeated filter creation (common in web applications)
        for (int i = 0; i < 100; i++)
        {
            Expression<Func<ObjectA, bool>> filter = o => o.Age > 25 && o.IsActive;
            var compiledFilter = filter.Compile();
            count += queryable.Where(compiledFilter).Count();
        }

        return count;
    }

    [Benchmark]
    public int WithCache_CreateAndExecuteFilter()
    {
        var queryable = _testData.AsQueryable();
        var count = 0;

        // Same scenario but with caching
        for (int i = 0; i < 100; i++)
        {
            var cacheKey = ExpressionCache.CreateFilterKey<ObjectA>("Age_IsActive", "25_true", "GreaterThan_Equal");
            var compiledFilter = _cache.GetOrCreate(cacheKey, () => 
            {
                Expression<Func<ObjectA, bool>> filter = o => o.Age > 25 && o.IsActive;
                return filter;
            });
            
            count += queryable.Where(compiledFilter).Count();
        }

        return count;
    }

    [Benchmark]
    public int WithoutCache_ComplexFilter()
    {
        var queryable = _testData.AsQueryable();
        var count = 0;

        for (int i = 0; i < 50; i++)
        {
            Expression<Func<ObjectA, bool>> complexFilter = o => 
                (o.Age > 30 && o.Name.StartsWith("Name1")) || 
                (o.Age < 25 && o.IsActive) ||
                (o.Id % 10 == 0);
                
            var compiledFilter = complexFilter.Compile();
            count += queryable.Where(compiledFilter).Count();
        }

        return count;
    }

    [Benchmark]
    public int WithCache_ComplexFilter()
    {
        var queryable = _testData.AsQueryable();
        var count = 0;

        for (int i = 0; i < 50; i++)
        {
            var cacheKey = ExpressionCache.CreateFilterKey<ObjectA>("ComplexFilter", "30_Name1_25_10", "Complex");
            var compiledFilter = _cache.GetOrCreate(cacheKey, () => 
            {
                Expression<Func<ObjectA, bool>> complexFilter = o => 
                    (o.Age > 30 && o.Name.StartsWith("Name1")) || 
                    (o.Age < 25 && o.IsActive) ||
                    (o.Id % 10 == 0);
                return complexFilter;
            });
            
            count += queryable.Where(compiledFilter).Count();
        }

        return count;
    }

    [Benchmark]
    public void AsyncEntityFilter_WithCache()
    {
        var queryable = _testData.AsQueryable();

        for (int i = 0; i < 20; i++)
        {
            var filter = AsyncEntityFilter<ObjectA>.Where(o => o.Age > 30);
            _ = filter.Filter(queryable).ToArray();
        }
    }

    [Benchmark]
    public void TraditionalEntityFilter_WithoutCache()
    {
        var queryable = _testData.AsQueryable();

        for (int i = 0; i < 20; i++)
        {
            var filter = EntityFilter<ObjectA>.Where(o => o.Age > 30);
            _ = filter.Filter(queryable).ToArray();
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _cache.Clear();
    }
}

/// <summary>
/// Demonstrates memory allocation improvements with caching
/// </summary>
[SimpleJob(RuntimeMoniker.Net60)]
[MemoryDiagnoser]
public class MemoryAllocationBenchmark
{
    private List<ObjectA> _testData = null!;

    [GlobalSetup]
    public void Setup()
    {
        _testData = Enumerable.Range(0, 1000)
            .Select(i => new ObjectA
            {
                Id = i,
                Name = $"Test{i}",
                Age = 20 + (i % 40),
                IsActive = i % 2 == 0
            })
            .ToList();
    }

    [Benchmark(Baseline = true)]
    public void WithoutCaching_RepeatedFilterCreation()
    {
        var queryable = _testData.AsQueryable();
        
        for (int i = 0; i < 100; i++)
        {
            var filter = EntityFilter<ObjectA>.Where(o => o.Age > 25);
            var expression = filter.GetExpression();
            if (expression != null)
            {
                var compiled = expression.Compile();
                _ = queryable.Where(compiled).First();
            }
        }
    }

    [Benchmark]
    public void WithCaching_RepeatedFilterCreation()
    {
        var queryable = _testData.AsQueryable();
        
        for (int i = 0; i < 100; i++)
        {
            var filter = AsyncEntityFilter<ObjectA>.Where(o => o.Age > 25);
            var expression = filter.GetExpression();
            if (expression != null)
            {
                var compiled = expression.Compile();
                _ = queryable.Where(compiled).First();
            }
        }
    }
}