namespace eQuantic.Linq.Casting;

public interface ICastConfiguration<in TCastOptions, TEntity> where TCastOptions : ICastOptions<TCastOptions, TEntity>
{
    void Configure(TCastOptions options);
}