using System.Linq.Expressions;
using eQuantic.Linq.Expressions.Resolution;
using eQuantic.Linq.Expressions.Tests.Support;
using eQuantic.Linq.Expressions.Tests.TestModel;

namespace eQuantic.Linq.Expressions.Tests;

[TestFixture]
public class TypeResolverTests
{
    public class NestedHolder
    {
        public class Inner
        {
            public int Value { get; set; }
        }
    }

    [Test]
    public void Type_references_round_trip_for_representative_shapes()
    {
        var resolver = new DefaultTypeResolver();

        Type[] shapes =
        [
            typeof(int),
            typeof(int?),
            typeof(string),
            typeof(void),
            typeof(Guid),
            typeof(DateOnly),
            typeof(int[]),
            typeof(int[,]),
            typeof(int[][]),
            typeof(string[]),
            typeof(Order),
            typeof(List<int>),
            typeof(List<Order>),
            typeof(Dictionary<string, List<Order[]>>),
            typeof(Nullable<Guid>),
            typeof(Func<Order, bool>),
            typeof(Expression<Func<Order, int>>),
            typeof(IQueryable<Order>),
            typeof(IGrouping<OrderStatus, Order>),
            typeof(NestedHolder.Inner),
            typeof(OrderStatus),
            typeof(int).MakeByRefType(),
            typeof(ValueTuple<int, string>),
        ];

        foreach (var type in shapes)
        {
            var reference = resolver.GetTypeRef(type);
            var resolved = resolver.ResolveType(reference);
            Assert.That(resolved, Is.EqualTo(type), $"round-trip failed for {type}");
        }
    }

    [Test]
    public void Well_known_types_use_aliases_and_omit_assembly()
    {
        var json = Verify.Serializer().ToJson((Expression<Func<int, int>>)(x => x + 1));

        Assert.That(json, Does.Contain("\"int\""));
        Assert.That(json, Does.Not.Contain("Int32"));
        Assert.That(json, Does.Not.Contain("CoreLib"));
    }

    [Test]
    public void Custom_types_carry_simple_assembly_name()
    {
        var resolver = new DefaultTypeResolver();
        var reference = resolver.GetTypeRef(typeof(Order));

        Assert.That(reference.Name, Is.EqualTo(typeof(Order).FullName));
        Assert.That(reference.Assembly, Is.EqualTo("eQuantic.Linq.Expressions.Tests"));
    }

    [Test]
    public void Strict_mode_blocks_unregistered_types()
    {
        Expression<Func<Order, bool>> lambda = o => o.Total > 100m;
        var json = Verify.Serializer().ToJson(lambda);

        var strict = Verify.Serializer(o => o.TypeResolver = new DefaultTypeResolver(new TypeResolutionOptions
        {
            Strict = true,
        }));

        Assert.Throws<TypeResolutionException>(() => strict.FromJson(json));
    }

    [Test]
    public void Strict_mode_allows_alias_only_payloads()
    {
        Expression<Func<int, bool>> lambda = x => x > 10;
        var json = Verify.Serializer().ToJson(lambda);

        var strict = Verify.Serializer(o => o.TypeResolver = new DefaultTypeResolver(new TypeResolutionOptions
        {
            Strict = true,
        }));

        var rebuilt = strict.FromJson<Func<int, bool>>(json);
        Assert.That(rebuilt.Compile()(11), Is.True);
    }

    [Test]
    public void Strict_mode_allows_registered_types()
    {
        Expression<Func<Order, bool>> lambda = o => o.Total > 100m && o.Items.Any(i => i.Price > 50m);
        var json = Verify.Serializer().ToJson(lambda);

        var strict = Verify.Serializer(o => o.TypeResolver = new DefaultTypeResolver(new TypeResolutionOptions
        {
            Strict = true,
        }
        .RegisterType<Order>()
        .RegisterType<OrderItem>()));

        var rebuilt = strict.FromJson<Func<Order, bool>>(json);
        Assert.That(rebuilt.Compile()(TestData.Orders[0]), Is.True);
    }

    [Test]
    public void Strict_mode_allows_namespace_allowlist()
    {
        Expression<Func<Order, decimal>> lambda = o => o.Total;
        var json = Verify.Serializer().ToJson(lambda);

        var options = new TypeResolutionOptions { Strict = true };
        options.AllowedNamespaces.Add("eQuantic.Linq.Expressions.Tests");

        var strict = Verify.Serializer(o => o.TypeResolver = new DefaultTypeResolver(options));

        var rebuilt = strict.FromJson<Func<Order, decimal>>(json);
        Assert.That(rebuilt.Compile()(TestData.Orders[0]), Is.EqualTo(250.50m));
    }

    [Test]
    public void Custom_alias_is_emitted_and_resolved()
    {
        var serializer = Verify.Serializer(o => o.TypeResolver = new DefaultTypeResolver(
            new TypeResolutionOptions().RegisterType<Order>("order")));

        Expression<Func<Order, bool>> lambda = o => o.Id == 3;
        var json = serializer.ToJson(lambda);

        Assert.That(json, Does.Contain("\"order\""));
        Assert.That(json, Does.Not.Contain(typeof(Order).FullName!));

        var rebuilt = serializer.FromJson<Func<Order, bool>>(json);
        Assert.That(rebuilt.Compile()(TestData.Orders[2]), Is.True);
    }

    [Test]
    public void Unknown_type_fails_with_actionable_message()
    {
        var json = Verify.Serializer()
            .ToJson((Expression<Func<Order, int>>)(o => o.Id))
            .Replace(typeof(Order).FullName!, "Ghost.Namespace.MissingType");

        var exception = Assert.Throws<TypeResolutionException>(() => Verify.Serializer().FromJson(json));
        Assert.That(exception!.Message, Does.Contain("MissingType"));
    }
}
