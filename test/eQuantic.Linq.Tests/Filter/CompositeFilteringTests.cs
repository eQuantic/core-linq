using eQuantic.Linq.Filter;

namespace eQuantic.Linq.Tests.Filter;

[TestFixture]
public class CompositeFilteringTests
{
    [TestCase("name:test")]
    [TestCase("name:eq(test)")]
    public void Parse_query_returns_simple_filter(string query)
    {
        var composite = CompositeFiltering.ParseComposite(query);

        Assert.That(composite, Is.Not.Null);
        Assert.That(composite, Is.InstanceOf<Filtering>());
    }

    [TestCase("or(name:eq(test1))")]
    [TestCase("and(name:eq(test1), name:eq(test2))")]
    [TestCase("or(name:eq(test1),name:eq(test2))")]
    public void Parse_query_successfully(string query)
    {
        var composite = CompositeFiltering.ParseComposite(query);

        Assert.That(composite, Is.Not.Null);
        Assert.That(composite, Is.InstanceOf<CompositeFiltering>());
    }
}