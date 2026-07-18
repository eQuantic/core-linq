using System.Text.Json;
using eQuantic.Linq.Expressions.Tests.Support;
using eQuantic.Linq.Expressions.Tests.TestModel;
using eQuantic.Linq.Web;
using eQuantic.Linq.Web.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace eQuantic.Linq.Expressions.Tests;

/// <summary>
/// ASP.NET Core binding: minimal-API BindAsync wrapper, MVC model binder, request extensions and
/// JSON options preparation — exercised over real DefaultHttpContext instances.
/// </summary>
[TestFixture]
public class AspNetCoreBindingTests
{
    private static DefaultHttpContext Context(string queryString, IServiceProvider? services = null) => new()
    {
        RequestServices = services ?? new ServiceCollection().BuildServiceProvider(),
        Request = { QueryString = new QueryString(queryString) },
    };

    private static List<int> Ids(IQueryable<Order> query) => query.Select(o => o.Id).ToList();

    // ---------------------------------------------------------------- minimal APIs

    [Test]
    public async Task Minimal_api_wrapper_binds_the_full_query_string()
    {
        var context = Context("?filter=total:gt(100)&orderBy=id&take=2");

        var model = await EntityQueryModel<Order>.BindAsync(context, parameter: null!);

        Assert.That(model, Is.Not.Null);
        Assert.That(Ids(model!.Apply(TestData.OrdersQuery)), Is.EqualTo(new[] { 1, 3 }));

        // implicit unwrap
        EntityQuery<Order> unwrapped = model;
        Assert.That(unwrapped.Take, Is.EqualTo(2));
    }

    [Test]
    public void Minimal_api_wrapper_turns_parse_errors_into_http_400()
    {
        var context = Context("?filter=total:zz(1)");

        var exception = Assert.ThrowsAsync<BadHttpRequestException>(
            async () => await EntityQueryModel<Order>.BindAsync(context, parameter: null!));

        Assert.That(exception!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(exception.Message, Does.Contain("zz"));
    }

    [Test]
    public async Task Minimal_api_wrapper_honors_di_registered_options()
    {
        var services = new ServiceCollection()
            .AddSingleton(new QueryStringOptions { FilterKey = "q" })
            .BuildServiceProvider();

        var context = Context("?q=status:eq(Paid)", services);

        var model = await EntityQueryModel<Order>.BindAsync(context, parameter: null!);

        Assert.That(Ids(model!.Apply(TestData.OrdersQuery)), Is.EqualTo(new[] { 1, 6 }));
    }

    // ---------------------------------------------------------------- MVC

    [Test]
    public void Add_entity_query_binding_registers_the_binder_provider_first()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddEntityQueryBinding();

        using var provider = services.BuildServiceProvider();
        var mvcOptions = provider.GetRequiredService<IOptions<MvcOptions>>().Value;

        Assert.That(mvcOptions.ModelBinderProviders[0], Is.InstanceOf<EntityQueryModelBinderProvider>());
        Assert.That(provider.GetRequiredService<QueryStringOptions>(), Is.Not.Null);
    }

    [Test]
    public void Binder_provider_matches_only_entity_query_models()
    {
        var provider = new EntityQueryModelBinderProvider();
        var metadata = new EmptyModelMetadataProvider();

        Assert.That(provider.GetBinder(new TestProviderContext(metadata.GetMetadataForType(typeof(EntityQuery<Order>)))), Is.Not.Null);
        Assert.That(provider.GetBinder(new TestProviderContext(metadata.GetMetadataForType(typeof(Order)))), Is.Null);
    }

    private sealed class TestProviderContext(ModelMetadata metadata) : ModelBinderProviderContext
    {
        public override BindingInfo BindingInfo => new();

        public override ModelMetadata Metadata => metadata;

        public override IModelMetadataProvider MetadataProvider => new EmptyModelMetadataProvider();

        public override IModelBinder CreateBinder(ModelMetadata metadata) => throw new NotSupportedException();
    }

    private static DefaultModelBindingContext BindingContext(string queryString) => new()
    {
        ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(EntityQuery<Order>)),
        ModelName = "query",
        ModelState = new ModelStateDictionary(),
        ActionContext = new ActionContext(
            Context(queryString),
            new RouteData(),
            new ActionDescriptor()),
    };

    [Test]
    public async Task Mvc_binder_binds_entity_queries_from_the_query_string()
    {
        var bindingContext = BindingContext("?filter=items:any(price:gt(500))&orderBy=id");

        await new EntityQueryModelBinder().BindModelAsync(bindingContext);

        Assert.That(bindingContext.Result.IsModelSet, Is.True);
        var query = (EntityQuery<Order>)bindingContext.Result.Model!;
        Assert.That(Ids(query.Apply(TestData.OrdersQuery)), Is.EqualTo(new[] { 3 }));
    }

    [Test]
    public async Task Mvc_binder_reports_parse_errors_through_model_state()
    {
        var bindingContext = BindingContext("?filter=ghost:eq(1)");

        await new EntityQueryModelBinder().BindModelAsync(bindingContext);

        Assert.That(bindingContext.Result.IsModelSet, Is.False);
        Assert.That(bindingContext.ModelState.ErrorCount, Is.EqualTo(1));
        Assert.That(bindingContext.ModelState["query"]!.Errors[0].ErrorMessage, Does.Contain("ghost"));
    }

    // ---------------------------------------------------------------- extensions & JSON options

    [Test]
    public void Request_extension_parses_without_binding()
    {
        var context = Context("?filter=customer.isVip:true&orderBy=total:desc");

        var query = context.Request.ParseEntityQuery<Order>();

        Assert.That(Ids(query.Apply(TestData.OrdersQuery)), Is.EqualTo(new[] { 3, 1, 6, 4 }));
    }

    [Test]
    public void Prepared_json_options_accept_hand_written_model_payloads()
    {
        // $type deliberately NOT first — hand-written payloads must still bind from request bodies.
        const string body = """
        {
          "body": {
            "nodeType": "GreaterThan",
            "left": { "member": { "name": "total" }, "expression": { "$type": "parameter" }, "$type": "member" },
            "right": { "value": 100, "$type": "constant" },
            "$type": "binary"
          }
        }
        """;

        var jsonOptions = new JsonSerializerOptions();
        ServiceCollectionExtensions.PrepareForExpressionModels(jsonOptions);

        var model = JsonSerializer.Deserialize<ExpressionModel<Order>>(body, jsonOptions)!;
        var predicate = Verify.Serializer().ToPredicate(model);

        Assert.That(Ids(TestData.OrdersQuery.Where(predicate)), Is.EqualTo(new[] { 1, 3, 5, 6 }));
    }
}
