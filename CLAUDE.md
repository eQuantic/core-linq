# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

eQuantic.Linq is a multi-platform library implementing LINQ extensions for filtering, sorting, and the specification pattern. The project maintains both C# (.NET) and TypeScript implementations with parallel functionality.

## Architecture

The project follows a dual-language architecture:

### C# Library (`src/eQuantic.Linq/`)
- **Specification Pattern**: `ISpecification<T>` interface with composite specifications for chainable business rules
- **Entity Filtering**: `IEntityFilter<TEntity>` for delegating filtering logic from presentation to service layer
- **Entity Sorting**: `IEntitySorter<TEntity>` for delegating sorting logic with fluent API
- **Casting System**: Type-safe mapping between DTOs and entities with `Cast<TEntity>()` extensions
- **Expression Builders**: Lambda expression construction utilities in `Expressions/` namespace

### TypeScript Library (`src/TypeScript/`)
- Parallel implementation of filtering and sorting functionality
- Modular structure with `filtering/`, `sorting/`, and `funcs/` directories
- Export-focused design for npm publication

## Development Commands

### C# (.NET) Development
```bash
# Build the solution
dotnet build

# Run tests
dotnet test

# Build in release mode (creates NuGet package)
dotnet build --configuration Release

# Run specific test project
dotnet test test/eQuantic.Linq.Tests/
```

### TypeScript Development
```bash
# Change to TypeScript directory first
cd src/TypeScript

# Install dependencies
npm ci

# Run tests
npm test

# Build TypeScript
npm run build

# Lint code
npm run lint

# Format code
npm run format
```

## Key Patterns and Conventions

### Specification Pattern Usage
- Specifications inherit from `Specification<T>` base class
- Override `SatisfiedBy()` to return `Expression<Func<T, bool>>`
- Chain specifications using `&&`, `||` operators or `And()`, `Or()` methods
- Used for composable business rules in domain-driven design

### Entity Filter Pattern
- Service layer methods expose `IEntityFilter<TEntity>` parameters
- Presentation layer creates filters using `EntityFilter<T>.Where()` or LINQ query syntax
- Enables separation of concerns between UI and business logic

### Entity Sorter Pattern  
- Service layer methods expose `IEntitySorter<TEntity>` parameters
- Create sorters using `EntitySorter<T>.OrderBy()` fluent API or LINQ query syntax
- Supports multi-column sorting with `ThenBy()` chaining

### Casting System
- Use `source.Cast<TEntity>(options => ...)` for type-safe mapping
- Configure mappings with `.Map()`, `.CustomMap()`, `.ExcludeUnmapped()`
- Supports dependency injection with configuration classes implementing `IFilteringCastConfiguration<T>`

## Project Structure

- `src/eQuantic.Linq/` - Main C# library
  - `Filter/` - Entity filtering implementation and extensions
  - `Sorter/` - Entity sorting implementation and extensions  
  - `Specification/` - Specification pattern implementation
  - `Expressions/` - Lambda expression building utilities
  - `Extensions/` - LINQ extension methods
  - `Casting/` - Type mapping and conversion utilities
- `src/TypeScript/` - TypeScript/JavaScript implementation
- `test/eQuantic.Linq.Tests/` - NUnit test project

## Testing Framework

- Uses NUnit for C# testing
- Jest with TypeScript support for JavaScript testing
- Test files follow pattern: `*Tests.cs` for C# and `*.test.ts` for TypeScript

## Build and CI/CD

- GitHub Actions workflow builds both NuGet and npm packages
- C# builds target multiple frameworks: .NET Standard 2.1, .NET 6, 7, 8
- TypeScript builds to CommonJS and ES modules
- Auto-publishes to NuGet.org and npmjs.com on push

## Important Notes

- Both implementations should maintain feature parity
- C# uses nullable reference types (`<Nullable>enable</Nullable>`)
- TypeScript uses strict mode with modern ES2022 target
- Expression trees are core to the C# implementation - preserve type safety
- Maintain backward compatibility across .NET framework versions