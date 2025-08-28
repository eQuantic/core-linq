using System.Diagnostics.CodeAnalysis;

namespace eQuantic.Linq.Filter.Casting;

[ExcludeFromCodeCoverage]
internal class FilteringCastDescriptor<TEntity>(string name, IFilteringCastConfiguration<TEntity> configuration)
{
    public string Name { get; set; } = name;
    public IFilteringCastConfiguration<TEntity> Configuration { get; set; } = configuration;
}
