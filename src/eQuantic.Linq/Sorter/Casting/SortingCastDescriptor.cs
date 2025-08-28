using System.Diagnostics.CodeAnalysis;

namespace eQuantic.Linq.Sorter.Casting;

[ExcludeFromCodeCoverage]
internal class SortingCastDescriptor<TEntity>
{
    public string Name { get; set; }
    public ISortingCastConfiguration<TEntity> Configuration { get; set; }

    public SortingCastDescriptor(string name, ISortingCastConfiguration<TEntity> configuration)
    {
        Name = name;
        Configuration = configuration;
    }
}