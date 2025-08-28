# eQuantic.Linq

> A modern, powerful cross-platform library for building dynamic LINQ queries with fluent APIs, advanced filtering, sorting, and specifications. Available for both .NET and TypeScript/JavaScript.

[![NuGet Version](https://img.shields.io/nuget/v/eQuantic.Linq.svg)](https://www.nuget.org/packages/eQuantic.Linq/)
[![NuGet SourceGenerator](https://img.shields.io/nuget/v/eQuantic.Linq.SourceGenerator.svg?label=SourceGenerator)](https://www.nuget.org/packages/eQuantic.Linq.SourceGenerator/)
[![npm version](https://img.shields.io/npm/v/@equantic/linq.svg)](https://www.npmjs.com/package/@equantic/linq)
[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)
[![.NET Version](https://img.shields.io/badge/.NET-6.0%7C8.0%7C9.0-purple.svg)](https://dotnet.microsoft.com/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.0+-3178c6.svg)](https://www.typescriptlang.org/)

## 🚀 Features

### .NET Core Features
- **🔥 Modern Fluent Query API** - Build complex queries with an intuitive, chainable syntax
- **⚡ Source Generator (.NET 6+)** - Zero-reflection, compile-time code generation for maximum performance
- **🚀 High Performance** - Optimized with expression caching and modern C# patterns
- **🔍 Advanced Filtering** - Type-safe filtering with support for all comparison operators
- **📊 Flexible Sorting** - Multi-level sorting with lambda expressions or property names  
- **🎯 Specification Pattern** - Composable business rules with boolean logic
- **🔄 Async Support** - Full async/await support for modern applications
- **🎨 Type Safety** - Strongly typed queries with compile-time validation
- **🛠 Extensible** - Easy to extend with custom filters and sorters

### TypeScript/JavaScript Features
- **🌐 Cross-Platform** - Works in Node.js, browsers, React, Vue, Angular, and more
- **🔒 100% Type Safety** - Full TypeScript support with advanced generics and path-based types
- **🎯 Nested Property Support** - Type-safe access to deeply nested object properties  
- **⚡ Zero Dependencies** - Lightweight with no external dependencies
- **🎨 Modern ES Modules** - Full ESM support with CommonJS compatibility
- **🧪 Comprehensive Testing** - 69+ tests ensuring reliability and correctness

## 📦 Installation

### Core Library

```bash
# Package Manager
Install-Package eQuantic.Linq

# .NET CLI
dotnet add package eQuantic.Linq

# PackageReference
<PackageReference Include="eQuantic.Linq" Version="latest" />
```

### Source Generator (.NET 6+)

For compile-time code generation and enhanced performance:

```bash
# Package Manager
Install-Package eQuantic.Linq.SourceGenerator

# .NET CLI
dotnet add package eQuantic.Linq.SourceGenerator

# PackageReference
<PackageReference Include="eQuantic.Linq.SourceGenerator" Version="latest" />
```

### TypeScript/JavaScript

```bash
# npm
npm install @equantic/linq

# yarn
yarn add @equantic/linq

# pnpm  
pnpm add @equantic/linq
```

## 🌟 Quick Start

### Modern Fluent Query API

The new fluent API provides an intuitive way to build complex queries:

```csharp
// Basic filtering and sorting
var users = await Query.For<User>()
    .Where(u => u.IsActive)
    .WhereGreaterThan(u => u.Age, 18)
    .OrderByDescending(u => u.CreatedAt)
    .ApplyToAsync(dbContext.Users);

// Advanced chaining with multiple conditions
var results = Query.For<Product>()
    .Where(p => p.Category == "Electronics")
    .And(p => p.Price > 100)
    .Or(p => p.OnSale)
    .WhereIn(p => p.Brand, new[] { "Apple", "Samsung", "Sony" })
    .WhereBetween(p => p.Rating, 4.0, 5.0)
    .OrderBy(p => p.Name)
    .ThenByDescending(p => p.Price)
    .ApplyTo(products);
```

### Value Types and Records Support

Perfect for working with primitives and modern record types:

```csharp
// Value types
var numbers = ValueQuery.For<int>()
    .Where(x => x > 10)
    .WhereLessThanOrEqual(x => x, 100)
    .OrderByDescending(x => x)
    .ApplyTo(numberCollection);

// Record types  
var people = ValueQuery.For<PersonRecord>()
    .WhereContains(p => p.Name, "John")
    .WhereGreaterThan(p => p.Age, 25)
    .ApplyTo(personRecords);

public record PersonRecord(string Name, int Age);
```

## 📋 Comprehensive Examples

### 1. Entity Filtering

```csharp
// Traditional approach
var filter = EntityFilter<User>.Where(u => u.Age > 18)
    .Where(u => u.IsActive)
    .Where(u => u.Department == "IT");

var users = filter.Filter(dbContext.Users).ToArray();

// New fluent approach  
var users = Query.For<User>()
    .Where(u => u.Age > 18)
    .And(u => u.IsActive) 
    .And(u => u.Department == "IT")
    .ApplyTo(dbContext.Users)
    .ToArray();
```

### 2. Advanced Sorting

```csharp
// Multiple sorting criteria
var sorter = EntitySorter<Product>
    .OrderBy(p => p.Category)
    .ThenByDescending(p => p.Rating)
    .ThenBy(p => p.Price);

// Or with the new fluent API
var products = Query.For<Product>()
    .OrderBy(p => p.Category)
    .ThenByDescending(p => p.Rating) 
    .ThenBy(p => p.Price)
    .ApplyTo(productQuery);

// String-based sorting
var sorting = new Sorting("Name", SortDirection.Ascending);
var results = dbContext.Users.OrderBy(sorting);
```

### 3. Specification Pattern

Encapsulate business rules in reusable, composable specifications:

```csharp
public class ActiveUserSpecification : Specification<User>
{
    public override Expression<Func<User, bool>> SatisfiedBy()
    {
        return u => u.IsActive && u.DeletedAt == null;
    }
}

public class AdultUserSpecification : Specification<User>
{
    public override Expression<Func<User, bool>> SatisfiedBy()
    {
        return u => u.Age >= 18;
    }
}

// Combine specifications with boolean logic
var specification = new ActiveUserSpecification() 
    && new AdultUserSpecification();

var users = dbContext.Users
    .Where(specification.SatisfiedBy())
    .ToArray();
```

### 4. Dynamic Query Building

Perfect for search interfaces and dynamic filtering:

```csharp
public async Task<User[]> SearchUsers(UserSearchCriteria criteria)
{
    var query = Query.For<User>();
    
    if (!string.IsNullOrEmpty(criteria.Name))
        query = query.WhereContains(u => u.Name, criteria.Name);
        
    if (criteria.MinAge.HasValue)
        query = query.WhereGreaterThanOrEqual(u => u.Age, criteria.MinAge.Value);
        
    if (criteria.Departments?.Any() == true)
        query = query.WhereIn(u => u.Department, criteria.Departments);
        
    if (!string.IsNullOrEmpty(criteria.SortBy))
        query = criteria.SortDescending 
            ? query.OrderByDescending(criteria.SortBy)
            : query.OrderBy(criteria.SortBy);
    
    return await query.ApplyToAsync(dbContext.Users);
}
```

### 5. Async Operations

Full support for asynchronous operations:

```csharp
// Async filtering and sorting
var users = await Query.For<User>()
    .Where(u => u.IsActive)
    .OrderByDescending(u => u.LastLoginAt)
    .ApplyToAsync(dbContext.Users, cancellationToken);

// Async enumerable for streaming large datasets
await foreach (var user in Query.For<User>()
    .Where(u => u.CreatedAt > DateTime.Now.AddYears(-1))
    .ApplyToAsyncEnumerable(dbContext.Users, cancellationToken))
{
    await ProcessUserAsync(user);
}
```

## ⚡ Source Generator (.NET 6+)

The **eQuantic.Linq.SourceGenerator** provides compile-time code generation for zero-reflection, high-performance query building:

### 🎯 Key Benefits

- **🚀 Zero Reflection**: All code generated at compile-time
- **⚡ Maximum Performance**: No runtime expression compilation
- **🎨 Type Safety**: Full IntelliSense support with generated code
- **🔒 AOT Compatible**: Works with Native AOT and trimming

### 📝 Basic Usage

Simply add the `[GenerateSpecifications]` attribute to your models:

```csharp
[GenerateSpecifications]
public class User
{
    public int Id { get; set; }
    
    [SpecProperty]
    public string Name { get; set; } = string.Empty;
    
    [SpecProperty]
    public string Email { get; set; } = string.Empty;
    
    [SpecProperty]
    public int Age { get; set; }
    
    [SpecProperty]
    public bool IsActive { get; set; }
    
    [SpecProperty]
    public DateTime CreatedAt { get; set; }
}
```

### 🎉 Generated Code

The source generator automatically creates specifications, filters, and sorters:

```csharp
// Generated automatically at compile-time
public partial class User
{
    public static class Specifications
    {
        public static Specification<User> NameEquals(string value)
            => new DirectSpecification<User>(u => u.Name == value);
            
        public static Specification<User> NameContains(string value)
            => new DirectSpecification<User>(u => u.Name.Contains(value));
            
        public static Specification<User> AgeGreaterThan(int value)
            => new DirectSpecification<User>(u => u.Age > value);
            
        public static Specification<User> IsActiveIsTrue()
            => new DirectSpecification<User>(u => u.IsActive);
    }
    
    public static class Filters
    {
        public static IEntityFilter<User> ByName(string name)
            => EntityFilter.Where<User>(u => u.Name == name);
            
        public static IEntityFilter<User> AgeGreaterThan(int age)
            => EntityFilter.Where<User>(u => u.Age > age);
    }
    
    public static class Sorting
    {
        public static IEntitySorter<User> ByNameAscending()
            => EntitySorter.OrderBy<User>(u => u.Name);
            
        public static IEntitySorter<User> ByAgeDescending()
            => EntitySorter.OrderByDescending<User>(u => u.Age);
    }
}
```

### 🛠 Advanced Configuration

#### Custom Operators

```csharp
[GenerateSpecifications]
public class Product
{
    [SpecProperty(FilterOperator.GreaterThan | FilterOperator.LessThan)]
    public decimal Price { get; set; }
    
    [SpecProperty(FilterOperator.Contains | FilterOperator.StartsWith)]
    public string Name { get; set; } = string.Empty;
    
    [SpecProperty(FilterOperator.Equals | FilterOperator.In)]
    public string Category { get; set; } = string.Empty;
}
```

#### Generated Methods by Operator

| Operator | Generated Methods | Example |
|----------|-------------------|---------|
| `Equals` | `{Property}Equals(value)` | `NameEquals("John")` |
| `Contains` | `{Property}Contains(value)` | `NameContains("Jo")` |
| `StartsWith` | `{Property}StartsWith(value)` | `NameStartsWith("J")` |
| `EndsWith` | `{Property}EndsWith(value)` | `NameEndsWith("n")` |
| `GreaterThan` | `{Property}GreaterThan(value)` | `AgeGreaterThan(18)` |
| `LessThan` | `{Property}LessThan(value)` | `AgeLessThan(65)` |
| `In` | `{Property}In(params values[])` | `CategoryIn("Electronics", "Books")` |

### 🎮 Usage Examples

```csharp
// Using generated specifications
var adultUsers = users.Where(User.Specifications.AgeGreaterThan(18).SatisfiedBy());
var activeJohns = users.Where(User.Specifications.NameEquals("John")
    .And(User.Specifications.IsActiveIsTrue()).SatisfiedBy());

// Using generated filters
var johnFilter = User.Filters.ByName("John");
var filteredUsers = johnFilter.Filter(users.AsQueryable()).ToList();

// Using generated sorting
var nameSorter = User.Sorting.ByNameAscending();
var sortedUsers = nameSorter.Sort(users.AsQueryable()).ToList();

// Combining everything
var complexQuery = User.Filters.ByName("John")
    .Filter(users.AsQueryable())
    .Where(User.Specifications.AgeGreaterThan(25).SatisfiedBy())
    .OrderBy(User.Sorting.ByAgeDescending());
```

### 🏗 Integration with Dependency Injection

```csharp
// Generated code works seamlessly with DI
public class UserService
{
    private readonly IUserRepository _repository;
    
    public async Task<User[]> GetActiveAdultUsersAsync()
    {
        var specification = User.Specifications.IsActiveIsTrue()
            .And(User.Specifications.AgeGreaterThan(17));
            
        return await _repository.FindAsync(specification);
    }
}
```

## 🌐 TypeScript/JavaScript Support

**eQuantic.Linq** also provides a TypeScript implementation with complete type safety:

### 🎯 Modern Type-Safe API

```typescript
import { Filtering, Sorting, FilteringCollection } from '@equantic/linq';

interface User {
  id: number;
  name: string;
  email: string;
  age: number;
  isActive: boolean;
}

// Type-safe filtering with IntelliSense
const nameFilter = Filtering.eq<User, 'name'>('name', 'John');
const ageFilter = Filtering.gte<User, 'age'>('age', 18);
const activeFilter = Filtering.eq<User, 'isActive'>('isActive', true);

// Collection operations
const filters = new FilteringCollection<User>([nameFilter, ageFilter, activeFilter]);

// Type-safe sorting
const nameSort = Sorting.asc<User>('name');
const ageSort = Sorting.desc<User>('age');
```

### 🔥 Advanced TypeScript Features

```typescript
// Nested property type safety
interface UserProfile {
  user: {
    profile: {
      firstName: string;
      settings: {
        theme: 'light' | 'dark';
      };
    };
  };
  metadata: {
    createdAt: Date;
    isActive: boolean;
  };
}

// Fully type-safe nested filtering
const nestedFilter = Filtering.eq<UserProfile, 'user.profile.firstName'>('user.profile.firstName', 'John');
const settingsFilter = Filtering.eq<UserProfile, 'user.profile.settings.theme'>('user.profile.settings.theme', 'dark');

// Type-safe operators based on property type
const stringFilter = Filtering.contains<User, 'name'>('name', 'Jo'); // Only works with string properties
const numberFilter = Filtering.gte<User, 'age'>('age', 18); // Only works with number/Date properties
```

### 📦 Framework Integrations

#### React/Vue/Angular

```typescript
// Perfect for search forms and dynamic filtering
const useUserFilters = () => {
  const [filters, setFilters] = useState<FilteringCollection<User>>(
    new FilteringCollection<User>()
  );
  
  const addNameFilter = (name: string) => {
    const nameFilter = Filtering.contains<User, 'name'>('name', name);
    setFilters(prev => prev.push(nameFilter));
  };
  
  return { filters, addNameFilter };
};
```

#### Node.js/Express APIs

```typescript
// Express middleware for query parsing
app.get('/users', (req, res) => {
  const filters = new FilteringCollection<User>();
  
  if (req.query.name) {
    filters.push(Filtering.contains<User, 'name'>('name', req.query.name as string));
  }
  
  if (req.query.minAge) {
    filters.push(Filtering.gte<User, 'age'>('age', parseInt(req.query.minAge as string)));
  }
  
  // Apply to database query...
});
```

## 🔧 Advanced Features

### Expression Caching

Automatically caches compiled expressions for improved performance:

```csharp
// Expressions are automatically cached for reuse
var activeUsersQuery = Query.For<User>().Where(u => u.IsActive);
// Subsequent uses of the same expression benefit from caching
```

### Type Conversion & Filtering

Supports automatic type conversion for filtering operations:

```csharp
// Automatic string to int conversion
var products = Query.For<Product>()
    .WhereEqual(p => p.Id, "123") // String "123" auto-converted to int
    .ApplyTo(productQuery);

// Complex type conversions
var orders = Query.For<Order>()
    .WhereEqual(o => o.Status, "Active") // String to enum conversion
    .WhereBetween(o => o.CreatedAt, "2024-01-01", "2024-12-31") // String to DateTime
    .ApplyTo(orderQuery);
```

### Mapping and Casting

#### Modern Fluent Casting (v2.0+)

Use the new `CastWith()` method with `FluentCastBuilder` for enhanced type safety and performance:

```csharp
// Modern fluent approach
var userFilters = sourceFilters.CastWith<User>(builder => builder
    .MapFilter("name", u => u.FullName)
    .MapFilter("email", u => u.EmailAddress, value => value.ToLowerInvariant())
    .ExcludeUnmapped());

// Sorting with fluent builder
var userSortings = sourceSortings.CastWith<User>(builder => builder
    .MapSorting("created", u => u.CreatedDate)
    .MapSorting("name", u => u.DisplayName));
```

#### Traditional Casting (Backward Compatible)

Legacy casting methods are still supported:

```csharp
// Traditional approach - still works but uses modern implementation internally
var userFilters = sourceFilters.Cast<User>(options => options
    .Map("name", u => u.FullName)
    .Map("email", u => u.EmailAddress));
```

#### Performance Benefits

The new casting system provides:
- **Expression Caching**: Compiled expressions are cached for reuse
- **Type Safety**: Enhanced compile-time type checking 
- **Memory Efficiency**: Reduced allocations in hot paths
- **Fluent API**: Intuitive, discoverable interface

Transform filters and sorters between different entity types:

```csharp
var dtoFilters = new IFiltering[]
{
    new Filtering("name", "John"),
    new Filtering("age", "25"),
    new Filtering("department", "IT")
};

// Map DTO filters to entity filters
var entityFilters = dtoFilters.Cast<User>(options =>
    options
        .Map("name", u => u.FullName)
        .Map("age", u => u.Age, value => int.Parse(value))
        .CustomMap("department", filter => new[]
        {
            new Filtering<User>(u => u.Department.Name, filter.StringValue)
        })
        .ExcludeUnmapped()
);
```

## 🏗 Architecture & Design

### Modern C# Features

The library leverages the latest C# features for optimal performance:

- **Switch Expressions** - Clean, efficient operator parsing
- **Pattern Matching** - Advanced type checking and conversion
- **Expression Trees** - Compile-time query optimization
- **Nullable Reference Types** - Enhanced type safety
- **AsyncEnumerable** - Memory-efficient streaming

### Performance Optimizations

- ⚡ **Expression Caching** - Compiled expressions are cached for reuse
- 🎯 **Lazy Evaluation** - Queries are built lazily and executed efficiently  
- 🔄 **Memory Efficient** - Minimal allocations with object pooling
- 📊 **Query Optimization** - Smart query plan generation

## ⚠️ Breaking Changes (v2.0.0)

This major version introduces several breaking changes to improve performance and API consistency:

### 🎯 Target Framework Changes
- **Minimum .NET version**: Now requires .NET 6.0 or higher
- **Removed**: .NET Framework and .NET Standard 2.1 support
- **New targets**: .NET 6.0, 8.0, and 9.0

### 🔧 API Improvements
- **Enhanced Type Safety**: Stricter nullable reference types throughout the API
- **Modern C# Features**: Leverages pattern matching, switch expressions, and collection expressions
- **Performance Optimizations**: 
  - Expression caching for repeated operations
  - Optimized reflection with `ConcurrentDictionary`
  - .NET 8+ specific optimizations with conditional compilation

### 🚀 Migration Guide

#### Updating Target Framework
```xml
<!-- Before (v1.x) -->
<TargetFramework>netstandard2.1</TargetFramework>

<!-- After (v2.0+) -->
<TargetFramework>net6.0</TargetFramework>
<!-- or net8.0, net9.0 -->
```

#### Enhanced Null Safety
```csharp
// V2.0 provides better null safety and clearer exceptions
// Code that previously might have failed silently now provides clear error messages
var query = Query.For<User>()
    .Where(u => u.Name != null) // Now validates null parameters at compile time
    .OrderBy(u => u.CreatedAt);
```

#### Performance Benefits
```csharp
// V2.0 automatically caches expressions for better performance
var filter = new Filtering<User>(u => u.Age, "25", FilterOperator.GreaterThan);
// Repeated usage of similar filters is now significantly faster
```

### ✅ Compatibility
- **Source Compatible**: Most existing code will compile without changes
- **Binary Compatible**: May require recompilation due to target framework changes
- **Behavioral Compatible**: All existing functionality preserved with improved performance

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes with proper tests
4. Commit with conventional commits (`git commit -m 'feat: add amazing feature'`)
5. Push to the branch (`git push origin feature/amazing-feature`)
6. Open a Pull Request

## 📄 License

This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with modern .NET and C# best practices
- Inspired by Entity Framework and LINQ patterns
- Community-driven development and feedback

---

<div align="center">

## 📚 Resources

**[NuGet Package](https://www.nuget.org/packages/eQuantic.Linq/) • [NPM Package](https://www.npmjs.com/package/@equantic/linq) • [Source Generator](https://www.nuget.org/packages/eQuantic.Linq.SourceGenerator/) • [Examples](examples/) • [Issues](https://github.com/equantic/core-linq/issues)**

### 📈 Project Statistics

![GitHub stars](https://img.shields.io/github/stars/equantic/core-linq?style=social)
![NuGet Downloads](https://img.shields.io/nuget/dt/eQuantic.Linq)
![npm Downloads](https://img.shields.io/npm/dm/@equantic/linq)

Made with ❤️ by the eQuantic team

</div>