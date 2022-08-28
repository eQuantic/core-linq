namespace eQuantic.Linq.Expressions;

public interface ILambdaBuilderFactory
{
    ILambdaBuilder Create(Type type, Type keyType);
}