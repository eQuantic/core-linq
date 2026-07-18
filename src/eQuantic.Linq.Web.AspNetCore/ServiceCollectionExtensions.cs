using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace eQuantic.Linq.Web.AspNetCore;

/// <summary>Service registration for eQuantic.Linq query-string binding.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Enables <see cref="EntityQuery{T}"/> binding: registers the MVC model binder provider, exposes
    /// the configured <see cref="QueryStringOptions"/> through DI (used by minimal-API binding too)
    /// and prepares both MVC and minimal-API JSON options to accept expression-model payloads
    /// (string enums, out-of-order <c>$type</c> discriminators, named floating-point literals).
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configure">Optional query-string syntax configuration.</param>
    public static IServiceCollection AddEntityQueryBinding(
        this IServiceCollection services,
        Action<QueryStringOptions>? configure = null)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        var options = new QueryStringOptions();
        configure?.Invoke(options);
        services.TryAddSingleton(options);

        services.Configure<MvcOptions>(mvc =>
        {
            if (!mvc.ModelBinderProviders.OfType<EntityQueryModelBinderProvider>().Any())
            {
                mvc.ModelBinderProviders.Insert(0, new EntityQueryModelBinderProvider());
            }
        });

        services.Configure<JsonOptions>(json => PrepareForExpressionModels(json.JsonSerializerOptions));
        services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(json => PrepareForExpressionModels(json.SerializerOptions));

        return services;
    }

    /// <summary>Adjusts serializer options so expression-model payloads (including hand-written ones) bind from request bodies.</summary>
    /// <param name="options">JSON serializer options to adjust.</param>
    public static void PrepareForExpressionModels(JsonSerializerOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        options.PropertyNameCaseInsensitive = true;
        options.AllowOutOfOrderMetadataProperties = true;
        options.NumberHandling |= JsonNumberHandling.AllowNamedFloatingPointLiterals;

        if (!options.Converters.OfType<JsonStringEnumConverter>().Any())
        {
            options.Converters.Add(new JsonStringEnumConverter());
        }
    }
}
