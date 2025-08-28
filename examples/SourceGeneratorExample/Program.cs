using SourceGeneratorExample.Models;
using eQuantic.Linq.Specification;
using eQuantic.Linq.Sorter;

namespace SourceGeneratorExample;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== eQuantic.Linq Source Generator Example ===\n");

        // Sample data
        var users = new List<User>
        {
            new() { Id = 1, Name = "John Doe", Email = "john@example.com", Age = 25, IsActive = true, CreatedAt = DateTime.Now.AddDays(-30) },
            new() { Id = 2, Name = "Jane Smith", Email = "jane@example.com", Age = 30, IsActive = false, CreatedAt = DateTime.Now.AddDays(-15) },
            new() { Id = 3, Name = "Bob Johnson", Email = "bob@example.com", Age = 35, IsActive = true, CreatedAt = DateTime.Now.AddDays(-5) }
        };

        var products = new List<Product>
        {
            new() { Id = 1, Name = "Laptop", Price = 1200.00m, IsAvailable = true, CreatedAt = DateTime.Now.AddDays(-20) },
            new() { Id = 2, Name = "Mouse", Price = 25.50m, IsAvailable = true, CreatedAt = DateTime.Now.AddDays(-10) },
            new() { Id = 3, Name = "Keyboard", Price = 80.00m, IsAvailable = false, CreatedAt = DateTime.Now.AddDays(-2) }
        };

        DemonstrateSpecifications(users);
        DemonstrateFilters(users);
        DemonstrateSorting(users);
        DemonstrateProducts(products);
    }

    static void DemonstrateSpecifications(List<User> users)
    {
        Console.WriteLine("1. SPECIFICATIONS EXAMPLES:");
        Console.WriteLine("---------------------------");

        // Using generated specifications
        var johnSpec = User.Specifications.NameEquals("John Doe");
        var activeSpec = User.Specifications.IsActiveIsTrue();
        var adultsSpec = User.Specifications.AgeGreaterThan(21);
        var nameContainsSpec = User.Specifications.NameContains("John");

        // Combining specifications
        var complexSpec = johnSpec.And(activeSpec);

        Console.WriteLine($"Users named 'John Doe': {users.AsQueryable().Where(johnSpec.SatisfiedBy()).Count()}");
        Console.WriteLine($"Active users: {users.AsQueryable().Where(activeSpec.SatisfiedBy()).Count()}");
        Console.WriteLine($"Adults (21+): {users.AsQueryable().Where(adultsSpec.SatisfiedBy()).Count()}");
        Console.WriteLine($"Names containing 'John': {users.AsQueryable().Where(nameContainsSpec.SatisfiedBy()).Count()}");
        Console.WriteLine($"John Doe AND Active: {users.AsQueryable().Where(complexSpec.SatisfiedBy()).Count()}");
        Console.WriteLine();
    }

    static void DemonstrateFilters(List<User> users)
    {
        Console.WriteLine("2. FILTERS EXAMPLES:");
        Console.WriteLine("--------------------");

        var queryable = users.AsQueryable();

        // Using generated filters
        var nameFilter = User.Filters.ByName("John Doe");
        var ageFilter = User.Filters.AgeGreaterThan(25);
        var activeFilter = User.Filters.IsActiveIsTrue();

        Console.WriteLine($"Filtered by name 'John Doe': {nameFilter.Filter(queryable).Count()}");
        Console.WriteLine($"Filtered by age > 25: {ageFilter.Filter(queryable).Count()}");
        Console.WriteLine($"Filtered by active: {activeFilter.Filter(queryable).Count()}");
        Console.WriteLine();
    }

    static void DemonstrateSorting(List<User> users)
    {
        Console.WriteLine("3. SORTING EXAMPLES:");
        Console.WriteLine("--------------------");

        var queryable = users.AsQueryable();

        // Using generated sorting
        var nameSorter = User.Sorting.ByNameAscending();
        var ageSorter = User.Sorting.ByAgeDescending();
        var complexSorter = User.Sorting.ByName()
            .ThenByAgeDescending()
            .ThenByCreatedAtAscending();

        var sortedByName = nameSorter.Sort(queryable).ToList();
        var sortedByAge = ageSorter.Sort(queryable).ToList();
        var complexSorted = complexSorter.Sort(queryable).ToList();

        Console.WriteLine("Sorted by name (ascending):");
        foreach (var user in sortedByName)
            Console.WriteLine($"  - {user.Name} (Age: {user.Age})");

        Console.WriteLine("\nSorted by age (descending):");
        foreach (var user in sortedByAge)
            Console.WriteLine($"  - {user.Name} (Age: {user.Age})");

        Console.WriteLine("\nComplex sorting (Name ASC, then Age DESC, then CreatedAt ASC):");
        foreach (var user in complexSorted)
            Console.WriteLine($"  - {user.Name} (Age: {user.Age}, Created: {user.CreatedAt:MM/dd})");

        Console.WriteLine();
    }

    static void DemonstrateProducts(List<Product> products)
    {
        Console.WriteLine("4. PRODUCTS EXAMPLES:");
        Console.WriteLine("---------------------");

        var queryable = products.AsQueryable();

        // Using generated specifications and filters for Product
        var expensiveSpec = Product.Specifications.PriceGreaterThan(50.00m);
        var availableFilter = Product.Filters.IsAvailableIsTrue();
        var priceSorter = Product.Sorting.ByPriceDescending();

        var expensiveProducts = queryable.Where(expensiveSpec.SatisfiedBy()).ToList();
        var availableProducts = availableFilter.Filter(queryable).ToList();
        var sortedByPrice = priceSorter.Sort(queryable).ToList();

        Console.WriteLine($"Expensive products (> $50): {expensiveProducts.Count}");
        Console.WriteLine($"Available products: {availableProducts.Count}");

        Console.WriteLine("\nProducts sorted by price (descending):");
        foreach (var product in sortedByPrice)
            Console.WriteLine($"  - {product.Name}: ${product.Price:F2}");
    }
}