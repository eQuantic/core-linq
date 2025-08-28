using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using eQuantic.Linq.SourceGenerator.Models;

namespace eQuantic.Linq.SourceGenerator.Generators;

[Generator]
public class SpecificationGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SpecificationSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not SpecificationSyntaxReceiver receiver)
            return;

        var compilation = context.Compilation;
        var classes = new List<ClassInfo>();

        foreach (var candidateClass in receiver.CandidateClasses)
        {
            var model = compilation.GetSemanticModel(candidateClass.SyntaxTree);
            if (model.GetDeclaredSymbol(candidateClass) is not INamedTypeSymbol classSymbol)
                continue;

            var classInfo = AnalyzeClass(classSymbol);
            if (classInfo != null)
                classes.Add(classInfo);
        }

        foreach (var classInfo in classes)
        {
            GenerateSpecifications(context, classInfo);
            
            if (classInfo.Configuration.IncludeFilters)
                GenerateFilters(context, classInfo);
                
            if (classInfo.Configuration.IncludeSorting)
                GenerateSorting(context, classInfo);
        }
    }

    private ClassInfo? AnalyzeClass(INamedTypeSymbol classSymbol)
    {
        var generateAttribute = classSymbol.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name == "GenerateSpecificationsAttribute");
            
        if (generateAttribute == null)
            return null;

        var classInfo = new ClassInfo
        {
            Name = classSymbol.Name,
            Namespace = classSymbol.ContainingNamespace.ToDisplayString(),
            FullName = classSymbol.ToDisplayString(),
            Configuration = ExtractConfiguration(generateAttribute)
        };

        foreach (var member in classSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            if (member.IsStatic || member.IsIndexer)
                continue;

            var specPropertyAttr = member.GetAttributes()
                .FirstOrDefault(attr => attr.AttributeClass?.Name == "SpecPropertyAttribute");

            var propConfig = specPropertyAttr != null ? ExtractPropertyConfiguration(specPropertyAttr) : null;

            if (propConfig?.Exclude == true)
                continue;

            var propertyInfo = new PropertyInfo
            {
                Name = member.Name,
                Type = member.Type.ToDisplayString(),
                IsNullable = member.Type.CanBeReferencedByName && member.NullableAnnotation == NullableAnnotation.Annotated,
                IsValueType = member.Type.IsValueType,
                Configuration = propConfig
            };

            classInfo.Properties.Add(propertyInfo);
        }

        return classInfo;
    }

    private GenerateSpecificationsAttribute ExtractConfiguration(AttributeData attributeData)
    {
        var config = new GenerateSpecificationsAttribute();
        
        foreach (var namedArgument in attributeData.NamedArguments)
        {
            switch (namedArgument.Key)
            {
                case nameof(GenerateSpecificationsAttribute.IncludeFilters):
                    config.IncludeFilters = (bool)namedArgument.Value.Value!;
                    break;
                case nameof(GenerateSpecificationsAttribute.IncludeSorting):
                    config.IncludeSorting = (bool)namedArgument.Value.Value!;
                    break;
                case nameof(GenerateSpecificationsAttribute.GenerateAsyncMethods):
                    config.GenerateAsyncMethods = (bool)namedArgument.Value.Value!;
                    break;
                case nameof(GenerateSpecificationsAttribute.IncludeValidation):
                    config.IncludeValidation = (bool)namedArgument.Value.Value!;
                    break;
                case nameof(GenerateSpecificationsAttribute.Namespace):
                    config.Namespace = (string?)namedArgument.Value.Value;
                    break;
            }
        }
        
        return config;
    }

    private SpecPropertyAttribute ExtractPropertyConfiguration(AttributeData attributeData)
    {
        var config = new SpecPropertyAttribute();
        
        foreach (var namedArgument in attributeData.NamedArguments)
        {
            switch (namedArgument.Key)
            {
                case nameof(SpecPropertyAttribute.Operators):
                    if (namedArgument.Value.Value is int operatorValue)
                        config.Operators = (FilterOperator)operatorValue;
                    break;
                case nameof(SpecPropertyAttribute.Exclude):
                    config.Exclude = (bool)namedArgument.Value.Value!;
                    break;
                case nameof(SpecPropertyAttribute.MethodName):
                    config.MethodName = (string?)namedArgument.Value.Value;
                    break;
                case nameof(SpecPropertyAttribute.IncludeInSorting):
                    config.IncludeInSorting = (bool)namedArgument.Value.Value!;
                    break;
            }
        }
        
        return config;
    }

    private void GenerateSpecifications(GeneratorExecutionContext context, ClassInfo classInfo)
    {
        var source = SpecificationCodeGenerator.Generate(classInfo);
        var fileName = $"{classInfo.Name}.Specifications.g.cs";
        context.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
    }

    private void GenerateFilters(GeneratorExecutionContext context, ClassInfo classInfo)
    {
        var source = FilterCodeGenerator.Generate(classInfo);
        var fileName = $"{classInfo.Name}.Filters.g.cs";
        context.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
    }

    private void GenerateSorting(GeneratorExecutionContext context, ClassInfo classInfo)
    {
        var source = SortingCodeGenerator.Generate(classInfo);
        var fileName = $"{classInfo.Name}.Sorting.g.cs";
        context.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
    }
}

internal class SpecificationSyntaxReceiver : ISyntaxReceiver
{
    public List<ClassDeclarationSyntax> CandidateClasses { get; } = new();

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is ClassDeclarationSyntax classDeclaration
            && classDeclaration.AttributeLists.Count > 0)
        {
            CandidateClasses.Add(classDeclaration);
        }
    }
}