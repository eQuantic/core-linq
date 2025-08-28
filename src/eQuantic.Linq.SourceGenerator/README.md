# eQuantic.Linq.SourceGenerator

Source generator for eQuantic.Linq that provides compile-time type-safe specifications, filters, and sorting.

## Features

- ✨ **Type-Safe Code Generation**: Generate specifications, filters, and sorting methods at compile-time
- 🚀 **Zero Runtime Reflection**: All code is generated during compilation for maximum performance
- 🧠 **IntelliSense Support**: Full code completion and type safety
- 🎯 **Customizable**: Configure generation behavior with attributes

## Quick Start

1. Install the package:
```xml
<PackageReference Include="eQuantic.Linq.SourceGenerator" Version="1.0.0" 
                  OutputItemType="Analyzer" 
                  ReferenceOutputAssembly="true" />
```

2. Mark your classes:
```csharp
[GenerateSpecifications(IncludeFilters = true, IncludeSorting = true)]
public partial class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
    public bool IsActive { get; set; }
}
```

3. Use the generated code:
```csharp
// Type-safe specifications
var spec = User.Specifications.NameContains("John")
    .And(User.Specifications.AgeGreaterThan(18));

// Type-safe filters
var filter = User.Filters.ByName("John Doe");

// Type-safe sorting
var sorter = User.Sorting.ByNameAscending()
    .ThenByAgeDescending();
```

## Documentation

For complete documentation, visit: https://github.com/equantic/core-linq

## License

MIT License