using eQuantic.Linq.Expressions.Tests.TestModel;
using eQuantic.Linq.Web;
using eQuantic.Linq.Web.AspNetCore;
using eQuantic.Linq.Web.Documentation;
using eQuantic.Linq.Web.Swashbuckle;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace eQuantic.Linq.Expressions.Tests;

[TestFixture]
public class EntityQueryDocumentationTests
{
    [Test]
    public void Builds_the_five_parameters_with_default_keys()
    {
        var docs = EntityQueryDocumentation.For<Order>();

        Assert.That(docs.Parameters.Select(p => p.Name), Is.EqualTo(new[] { "filter", "orderBy", "skip", "take", "select" }));
        Assert.That(docs.EntityName, Is.EqualTo(nameof(Order)));
    }

    [Test]
    public void Honors_customized_key_names()
    {
        var options = new QueryStringOptions { FilterKey = "q", OrderByKey = "sort", SkipKey = "offset", TakeKey = "limit", SelectKey = "fields" };

        var docs = EntityQueryDocumentation.For<Order>(options);

        Assert.That(docs.Parameters.Select(p => p.Name), Is.EqualTo(new[] { "q", "sort", "offset", "limit", "fields" }));
    }

    [Test]
    public void Catalog_covers_nested_paths_enums_aliases_and_collections()
    {
        var docs = EntityQueryDocumentation.For<Order>();
        var filter = docs.Parameters.Single(p => p.Kind == EntityQueryParameterKind.Filter);

        Assert.Multiple(() =>
        {
            Assert.That(filter.Description, Does.Contain("`customer.name`"), "nested navigation path");
            Assert.That(filter.Description, Does.Contain("Paid"), "enum values listed");
            Assert.That(filter.Description, Does.Contain("customer_age"), "[Column] alias surfaced");
            Assert.That(filter.Description, Does.Contain("collection of OrderItem"), "collection hint");
            Assert.That(filter.Description, Does.Contain("price"), "collection element members");
            Assert.That(filter.Description, Does.Not.Contain("address"), "depth capped at one navigation level");
        });
    }

    [Test]
    public void Examples_derive_from_the_entity_members()
    {
        var docs = EntityQueryDocumentation.For<Order>();

        Assert.Multiple(() =>
        {
            Assert.That(docs.Parameters.Single(p => p.Kind == EntityQueryParameterKind.Filter).Example, Is.EqualTo("id:gt(0),notes:ct(a)"));
            Assert.That(docs.Parameters.Single(p => p.Kind == EntityQueryParameterKind.OrderBy).Example, Is.EqualTo("id:desc,reference"));
            Assert.That(docs.Parameters.Single(p => p.Kind == EntityQueryParameterKind.Select).Example, Is.EqualTo("id,reference"));
            Assert.That(docs.Parameters.Single(p => p.Kind == EntityQueryParameterKind.Skip).IsInteger, Is.True);
            Assert.That(docs.Parameters.Single(p => p.Kind == EntityQueryParameterKind.Take).IsInteger, Is.True);
        });
    }
}

[TestFixture]
public class SwashbuckleOperationFilterTests
{
    [TestCase(typeof(EntityQueryModel<Order>))]
    [TestCase(typeof(EntityQuery<Order>))]
    public void Documents_entity_query_parameters(Type boundType)
    {
        var operation = new OpenApiOperation
        {
            Parameters = [new OpenApiParameter { Name = "query", In = ParameterLocation.Query }],
        };

        new EntityQueryOperationFilter().Apply(operation, ContextFor(boundType, parameterName: "query"));

        var names = operation.Parameters!.Select(p => p.Name).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(names, Is.EqualTo(new[] { "filter", "orderBy", "skip", "take", "select" }), "bound parameter replaced by the five keys");
            Assert.That(operation.Parameters!.Single(p => p.Name == "filter").Description, Does.Contain("customer.name"));
            Assert.That(operation.Parameters!.Single(p => p.Name == "skip").Schema?.Type, Is.EqualTo(JsonSchemaType.Integer));
            Assert.That(operation.Responses, Does.ContainKey("400"));
        });
    }

    [Test]
    public void Ignores_operations_without_entity_query_parameters()
    {
        var operation = new OpenApiOperation();

        new EntityQueryOperationFilter().Apply(operation, ContextFor(typeof(int), parameterName: "id"));

        Assert.That(operation.Parameters, Is.Null.Or.Empty);
    }

    [Test]
    public void Honors_customized_key_names()
    {
        var operation = new OpenApiOperation();
        var filter = new EntityQueryOperationFilter(new QueryStringOptions { FilterKey = "q" });

        filter.Apply(operation, ContextFor(typeof(EntityQueryModel<Order>), parameterName: "query"));

        Assert.That(operation.Parameters!.Select(p => p.Name), Does.Contain("q"));
    }

    private static OperationFilterContext ContextFor(Type parameterType, string parameterName)
    {
        var apiDescription = new ApiDescription();
        apiDescription.ParameterDescriptions.Add(new ApiParameterDescription
        {
            Name = parameterName,
            Type = parameterType,
            Source = BindingSource.Custom,
        });

        return new OperationFilterContext(
            apiDescription,
            null!,
            new SchemaRepository(),
            new OpenApiDocument(),
            typeof(SwashbuckleOperationFilterTests).GetMethod(nameof(Ignores_operations_without_entity_query_parameters))!);
    }
}

#if NET10_0_OR_GREATER
[TestFixture]
public class OpenApiOperationTransformerTests
{
    [Test]
    public async Task Documents_entity_query_parameters()
    {
        var operation = new OpenApiOperation();
        var transformer = new eQuantic.Linq.Web.OpenApi.EntityQueryOperationTransformer();

        await transformer.TransformAsync(operation, ContextFor(typeof(EntityQueryModel<Order>), services: null), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(operation.Parameters!.Select(p => p.Name), Is.EqualTo(new[] { "filter", "orderBy", "skip", "take", "select" }));
            Assert.That(operation.Parameters!.Single(p => p.Name == "filter").Description, Does.Contain("customer.name"));
            Assert.That(operation.Responses, Does.ContainKey("400"));
        });
    }

    [Test]
    public async Task Resolves_query_options_from_di_when_not_supplied()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton(
            services, new QueryStringOptions { FilterKey = "q" });
        var provider = Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(services);

        var operation = new OpenApiOperation();
        var transformer = new eQuantic.Linq.Web.OpenApi.EntityQueryOperationTransformer();

        await transformer.TransformAsync(operation, ContextFor(typeof(EntityQueryModel<Order>), provider), CancellationToken.None);

        Assert.That(operation.Parameters!.Select(p => p.Name), Does.Contain("q"));
    }

    private static Microsoft.AspNetCore.OpenApi.OpenApiOperationTransformerContext ContextFor(
        Type parameterType, IServiceProvider? services)
    {
        var apiDescription = new ApiDescription();
        apiDescription.ParameterDescriptions.Add(new ApiParameterDescription
        {
            Name = "query",
            Type = parameterType,
            Source = BindingSource.Custom,
        });

        return new Microsoft.AspNetCore.OpenApi.OpenApiOperationTransformerContext
        {
            DocumentName = "v1",
            Description = apiDescription,
            ApplicationServices = services ?? Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions
                .BuildServiceProvider(new Microsoft.Extensions.DependencyInjection.ServiceCollection()),
        };
    }
}
#endif
