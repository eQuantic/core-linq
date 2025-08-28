using eQuantic.Linq.Filter;

namespace eQuantic.Linq.Tests.Filter;

public class EntityFilterTests
{
    [Test]
    public void EntityFilter_Where()
    {
        var filtering = new List<IFiltering>
        {
            new Filtering<EntityFilterBuilderTests.TestObject>(o => o.Name, "test"),
            new CompositeFiltering(CompositeOperator.Or, 
                new Filtering<EntityFilterBuilderTests.TestObject>(o => o.Number, "1"),
                new Filtering<EntityFilterBuilderTests.TestObject>(o => o.Number, "2"))
        };
        
        var result = EntityFilter<EntityFilterBuilderTests.TestObject>
            .Where(filtering.ToArray())
            .GetExpression();
        
        Assert.That(result, Is.Not.Null);
    }
}