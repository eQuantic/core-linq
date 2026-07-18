using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace eQuantic.Linq.Web.AspNetCore;

/// <summary>
/// MVC model binder: lets controller actions receive <see cref="EntityQuery{T}"/> parameters bound
/// from the request query string. Parse errors are reported through model state (an
/// <c>[ApiController]</c> turns them into an automatic 400 response).
/// </summary>
public sealed class EntityQueryModelBinder : IModelBinder
{
    private static readonly ConcurrentDictionary<Type, Func<IEnumerable<KeyValuePair<string, string>>, QueryStringOptions?, object>> Parsers = new();

    /// <inheritdoc />
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext is null)
        {
            throw new ArgumentNullException(nameof(bindingContext));
        }

        var httpContext = bindingContext.HttpContext;
        var pairs = QueryStringHttpParser.Pairs(httpContext.Request.Query);
        var options = QueryStringHttpParser.ResolveOptions(httpContext, options: null);

        try
        {
            var parser = Parsers.GetOrAdd(bindingContext.ModelType, BuildParser);
            bindingContext.Result = ModelBindingResult.Success(parser(pairs, options));
        }
        catch (Exception exception) when (Unwrap(exception) is { } clientError && QueryStringHttpParser.IsClientError(clientError))
        {
            bindingContext.ModelState.TryAddModelError(
                bindingContext.ModelName.Length > 0 ? bindingContext.ModelName : "queryString",
                clientError.Message);
            bindingContext.Result = ModelBindingResult.Failed();
        }

        return Task.CompletedTask;
    }

    private static Exception Unwrap(Exception exception) =>
        exception is TargetInvocationException { InnerException: { } inner } ? inner : exception;

    private static Func<IEnumerable<KeyValuePair<string, string>>, QueryStringOptions?, object> BuildParser(Type modelType)
    {
        var rootType = modelType.GetGenericArguments()[0];

        var method = typeof(EntityQuery)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m =>
                m.Name == nameof(EntityQuery.Parse)
                && m.GetParameters()[0].ParameterType != typeof(string))
            .MakeGenericMethod(rootType);

        return (pairs, options) => method.Invoke(null, [pairs, options])!;
    }
}

/// <summary>Registers <see cref="EntityQueryModelBinder"/> for every <see cref="EntityQuery{T}"/> model type.</summary>
public sealed class EntityQueryModelBinderProvider : IModelBinderProvider
{
    private static readonly EntityQueryModelBinder Binder = new();

    /// <inheritdoc />
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var modelType = context.Metadata.ModelType;

        return modelType.IsGenericType && modelType.GetGenericTypeDefinition() == typeof(EntityQuery<>)
            ? Binder
            : null;
    }
}
