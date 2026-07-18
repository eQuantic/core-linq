using System.Text.Json;
using eQuantic.Linq.Expressions.Casting;
using eQuantic.Linq.Expressions.Tests.Support;
using eQuantic.Linq.Expressions.Tests.TestModel;
using eQuantic.Linq.Web;

namespace eQuantic.Linq.Expressions.Tests;

/// <summary>Round 5: query envelope transport, reverse projection and OpenAPI metadata.</summary>
[TestFixture]
public class RoundFiveTests
{
    [Test]
    public void Query_model_envelope_round_trips_the_whole_query_as_json()
    {
        var options = new QueryStringOptions { Serializer = Verify.Serializer() };
        var query = EntityQuery.Parse<Order>(
            "?filter=total:gt(100)&orderBy=total:desc,id&skip=0&take=3&select=id,customer.name", options);

        var json = JsonSerializer.Serialize(query.ToQueryModel(), options.Serializer.JsonOptions);
        var revived = JsonSerializer.Deserialize<QueryModel<Order>>(json, options.Serializer.JsonOptions)!
            .ToEntityQuery(options);

        var results = revived.ApplyWithSelection(TestData.OrdersQuery).Cast<object>().ToList();

        Assert.That(results.Select(r => r.ToString()), Is.EqualTo(new[]
        {
            "{ Id = 3, CustomerName = Carla }",
            "{ Id = 5, CustomerName = Diego }",
            "{ Id = 1, CustomerName = Alice }",
        }));
    }

    [Test]
    public void Cast_project_materializes_entities_into_dtos()
    {
        var cast = ExpressionCast.Create<OrderDto, Order>(o => o
            .Map(d => d.CustomerName, e => e.Customer.Name)
            .Map(d => d.Revenue, e => e.Items.Sum(i => i.Price * i.Quantity))
            .Map(d => d.Display, e => e.Customer.Name + " #" + e.Id)
            .Map(d => d.StatusName, e => e.Status.ToString())
            .Nested<ItemDto, OrderItem>(n => n.Map(i => i.Cost, e => e.Price)));

        var project = cast.Project().Compile();
        var dto = project(TestData.Orders[0]);

        Assert.That(dto.Id, Is.EqualTo(1));
        Assert.That(dto.CustomerName, Is.EqualTo("Alice"));
        Assert.That(dto.Revenue, Is.EqualTo(250.50m));
        Assert.That(dto.Display, Is.EqualTo("Alice #1"));
        Assert.That(dto.StatusName, Is.EqualTo("Paid"));
        Assert.That(dto.Amount, Is.EqualTo(250.50m), "column fallback: [Column(\"Total\")] → Order.Total");
        Assert.That(dto.Items, Has.Count.EqualTo(2));
        Assert.That(dto.Items[1].Cost, Is.EqualTo(65.25m));
        Assert.That(dto.Items[1].Product, Is.EqualTo("Mouse"));
        Assert.That(dto.Legacy, Is.Empty, "members without counterpart are skipped");
    }

    [Test]
    public void Entity_query_model_advertises_bad_request_metadata()
    {
        var builder = new Microsoft.AspNetCore.Routing.RouteEndpointBuilder(
            requestDelegate: null,
            Microsoft.AspNetCore.Routing.Patterns.RoutePatternFactory.Parse("/orders"),
            order: 0);

        eQuantic.Linq.Web.AspNetCore.EntityQueryModel<Order>.PopulateMetadata(
            typeof(RoundFiveTests).GetMethods()[0].ReturnParameter!, builder);

        var metadata = builder.Metadata.OfType<Microsoft.AspNetCore.Http.Metadata.IProducesResponseTypeMetadata>().Single();
        Assert.That(metadata.StatusCode, Is.EqualTo(400));
    }
}
