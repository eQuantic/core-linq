using System.Diagnostics.CodeAnalysis;

namespace eQuantic.Linq.Filter.Casting;

[ExcludeFromCodeCoverage]
internal class FilteringCastDescriptor<TEntity>
{
    public string Name { get; set; }
    public IFilteringCastConfiguration<TEntity> Configuration { get; set; }

    public FilteringCastDescriptor(string name, IFilteringCastConfiguration<TEntity> configuration)
    {
        Name = name;
        Configuration = configuration;
    }
}