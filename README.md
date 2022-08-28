# eQuantic.Linq Library

This package adds some features and patterns to queryable objects.

## Specification Pattern
The specification pattern is a particular software design pattern, whereby business rules can be recombined by chaining the business rules together using expression boolean logic. The pattern is frequently used in the context of domain-driven design.

A specification pattern outlines a business rule that is combinable with other business rules. In this pattern, a unit of business logic inherits its functionality from the abstract aggregate `CompositeSpecification` class. The `CompositeSpecification` class has one function called `SatisfiedBy` that returns a expression boolean value. After instantiation, the specification is "chained" with other specifications, making new specifications easily maintainable, yet highly customizable business logic. Furthermore, upon instantiation the business logic may, through method invocation or inversion of control, have its state altered in order to become a delegate of other classes such as a persistence repository.

### Example

The following are two specifications for consulting users:

```csharp
public class FindUserByNameSpecification : Specification<User>
{
    private readonly string name;

    public FindUserByNameSpecification(string name)
    {
        this.name = name;
    }

    public override Expression<Func<User, bool>> SatisfiedBy()
    {
        return o => o.Name.StartWith(this.name);
    }
}

public class NonExcludedUserSpecification : Specification<User>
{
    public override Expression<Func<User, bool>> SatisfiedBy()
    {
        return o => o.ExcludedOn == null;
    }
}
```

The following are two ways to use combinations of specifications:

Through inheritance:

```csharp

public class FindUsersSpecification : NonExcludedUserSpecification
{
    private readonly string name;

    public FindUsersSpecification(string name)
    {
        this.name = name;
    }

    public override Expression<Func<User, bool>> SatisfiedBy()
    {
        var expression = base.SatisfiedBy();

        return expression.AndAlso(o => o.Name.StartWith(this.name))
    }
}
```

Through implicit operation:

```csharp
using (var db = ContextFactory.CreateContext())
{
    IQueryable<User> query = db.Users;

    var specification = new NonExcludedUserSpecification() && new FindUserByNameSpecification(name);

    var result = query.Where(specification.SatisfiedBy());
}
```

## Sorter

The `EntitySorter<TEntity>` allows the Presentation Layer to delegate sorting to the Service Layer. Service Layer methods can expose an `IEntitySorter<TEntity>` interface as method argument, as follows:

```csharp
public static User[] GetAllUsers(IEntitySorter<User> sorter)
{
    using (var db = ContextFactory.CreateContext())
    {
        IOrderedQueryable<User> sortedList = sorter.Sort(db.Users);

        return sortedList.ToArray();
    }
}
```

This method can be called from the Presentation Layer by supplying an implementation of this interface:

```csharp
IEntitySorter<User> sorter =
    from user in EntitySorter<User>.AsQueryable()
    orderby user.Name descending, user.Id
    select user;

var users = UserServices.GetAllUsers(sorter);
```

The following code shows other ways of creating IEntitySorter<T> implementations, all through the EntitySorter<T> facade:

```csharp
// Using lambda expressions.
var sorter1 = EntitySorter<User>.OrderBy(p => p.Id);
var sorter2 = EntitySorter<User>.OrderByDescending(p => p.Id);
var sorter3 = EntitySorter<User>
    .OrderBy(p => p.Name)
    .ThenBy(p => p.Id);

// Using strings
var sorter4 = EntitySorter<User>.OrderBy("Name");
// Specifying chains of properties.
var sorter5 = EntitySorter<User>.OrderBy("Address.City");
```

The `ISorting` allows you to work in a simplified way the delegation of sorting to the Service Layer.
See the application of the `eQuantic.Linq.Extensions.QueryableExtensions` extension in the code below.

```csharp
var sorting1 = new Sorting("Name", SortDirection.Ascending);
var sorting2 = new Sorting<User>(t => t.Id, SortDirection.Descending);

using (var db = ContextFactory.CreateContext())
{
    IOrderedQueryable<User> sortedList = db.Users.OrderBy(sorting1, sorting2);

    return sortedList.ToArray();
}
```

## Filter

The `EntityFilter<TEntity>` allows the Presentation Layer to delegate filtering to the Service Layer. Service Layer methods can expose an `IEntityFilter<TEntity>` interface as method argument, as follows:

```csharp
public static User[] GetAllUsers(IEntityFilter<User> filter)
{
    using (var db = ContextFactory.CreateContext())
    {
        IQueryable<User> filteredList = filter.Filter(db.Users);

        return filteredList .ToArray();
    }
}
```

This method can be called from the Presentation Layer by supplying an implementation of this interface:

```csharp
IEntityFilter<User> filter =
    from user in EntityFilter<User>.AsQueryable()
    where user.Name.StartsWith("a")
    where user.Id < 100
    select user;

var users = UserServices.GetAllUsers(filter);
```

The following code shows other ways of creating `IEntityFilter<TEntity>` implementations, all through the `EntityFilter<TEntity>` facade:

```csharp
// Using lambda expressions.
var filter1 = EntityFilter<User>.Where(p => p.Id < 10);
var filter2 = EntityFilter<User>
    .Where(p => p.Id < 10)
    .Where(p => p.Name.StartsWith("a"));
```

## Entities

Below some entities in common use among other packages:

### Pagination

- `eQuantic.Linq.IPagedEnumerable<>`
- `eQuantic.Linq.PagedList<>`

### Filtering

- `eQuantic.Linq.Filter.IFiltering`
- `eQuantic.Linq.Filter.Filtering`

## Casting

Sometimes we need to map a collection of input filters/sorters to output filters/sorters (often from DTOs to database entities).
For this, just use the `Cast<TEntity>`. Example:

```csharp

var source = new IFiltering[]
{
    new Filtering("test", "test"),
    new Filtering("test2", "test2"),
    new Filtering("test3", "test3"),
};

var actual = source.Cast<DatabaseObject>(options =>

    options
        .Map("test", o => o.StringProperty, value => $"new_{value}")
        .CustomMap("test2", originalFiltering => new []
        {
            new Filtering<DatabaseObject>(o => o.BooleanProperty, originalFiltering.StringValue == "test2" ? true.ToString() : false.ToString())
        })
        .ExcludeUnmapped()
);
```

### Dependency Injection

it is possible to create reusable configuration classes that will be resolved by dependency injection.

```csharp

// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddFilteringCastConfig<FilteringDatabaseObjectConfig, DatabaseObject>("SomethingToDatabaseObject");
    
var app = builder.Build();

app.UseLinq();

...

// Configuration class
public class FilteringDatabaseObjectConfig : IFilteringCastConfiguration<DatabaseObject>
{
    public void Configure(FilteringCastOptions<DatabaseObject> options)
    {
        options
            .Map("test", o => o.StringProperty, value => $"new_{value}")
            .CustomMap("test2", originalFiltering => new []
            {
                new Filtering<DatabaseObject>(o => o.BooleanProperty, originalFiltering.StringValue == "test2" ? true.ToString() : false.ToString())
            })
            .ExcludeUnmapped();
    }
}

...

// Usage

var source = new IFiltering[]
{
    new Filtering("test", "test"),
    new Filtering("test2", "test2"),
    new Filtering("test3", "test3"),
};

var actual = source.Cast<DatabaseObject>("SomethingToDatabaseObject");
```

To install **eQuantic.Linq**, run the following command in the [Package Manager Console](https://docs.nuget.org/docs/start-here/using-the-package-manager-console)
```dos
Install-Package eQuantic.Linq
```