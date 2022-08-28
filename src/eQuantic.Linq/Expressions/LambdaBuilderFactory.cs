namespace eQuantic.Linq.Expressions;

public class LambdaBuilderFactory : ILambdaBuilderFactory
{
    public static ILambdaBuilderFactory Current { get; } = new LambdaBuilderFactory();

    public ILambdaBuilder Create(Type type, Type keyType)
    {
        var builderType = typeof(LambdaBuilder<,>).MakeGenericType(new[] { type, keyType });

        return (ILambdaBuilder)Activator.CreateInstance(builderType);
    }
}