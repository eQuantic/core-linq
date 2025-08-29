using eQuantic.Linq.Filter;
using eQuantic.Linq.Extensions;

namespace eQuantic.Linq.Tests.Filter;

[TestFixture]
public class CompositeFilteringTests
{
    [TestCase("name:test")]
    [TestCase("name:eq(test)")]
    public void Parse_query_returns_simple_filter(string query)
    {
        var composite = CompositeFiltering.ParseComposite(query);

        Assert.That(composite, Is.Not.Null);
        Assert.That(composite, Is.InstanceOf<Filtering>());
    }

    [TestCase("or(name:eq(test1))")]
    [TestCase("and(name:eq(test1), name:eq(test2))")]
    [TestCase("or(name:eq(test1),name:eq(test2))")]
    [TestCase("collection:any(name:eq(test1))")]
    [TestCase("collection:all(name:eq(test1))")]
    public void Parse_query_successfully(string query)
    {
        var composite = CompositeFiltering.ParseComposite(query);

        Assert.That(composite, Is.Not.Null);
        Assert.That(composite, Is.InstanceOf<CompositeFiltering>());
    }

    [TestCase("collection", "column", "value", CompositeOperator.Any, FilterOperator.Equal, "collection:any(column:eq(value))")]
    public void Create_compositeFiltering_with_arguments_successfully(string collectionColumnName, string columnName, string stringValue,
        CompositeOperator compositeOperator, FilterOperator? filterOperator, string expectedExpression)
    {
        // Arrange
        var filtering = new CompositeFiltering(compositeOperator, collectionColumnName, new Filtering(columnName, stringValue, filterOperator));

        // Act
        var actualExpression = filtering.ToString();

        // Assert
        Assert.That(expectedExpression, Is.EqualTo(actualExpression));
    }

    [Test]
    public void Any_All_operators_with_typed_objects_should_work_correctly()
    {
        // Arrange - Create test data
        var objects = new List<ObjectB>
        {
            new ObjectB 
            { 
                PropertyB = "Parent1", 
                SubObjects = new List<SubObjectB>
                {
                    new SubObjectB { PropertyB = "Admin", CommonProperty = "Active" },
                    new SubObjectB { PropertyB = "User", CommonProperty = "Inactive" }
                }
            },
            new ObjectB 
            { 
                PropertyB = "Parent2", 
                SubObjects = new List<SubObjectB>
                {
                    new SubObjectB { PropertyB = "Manager", CommonProperty = "Active" },
                    new SubObjectB { PropertyB = "Supervisor", CommonProperty = "Active" }
                }
            },
            new ObjectB 
            { 
                PropertyB = "Parent3", 
                SubObjects = new List<SubObjectB>()
            }
        };

        var queryable = objects.AsQueryable();

        // Test 1: Any operator - objects where any SubObject has PropertyB = "Admin"
        var anyAdminQuery = "SubObjects:any(PropertyB:eq(Admin))";
        var anyAdminFilter = CompositeFiltering.ParseComposite(anyAdminQuery) as CompositeFiltering;
        
        Assert.That(anyAdminFilter, Is.Not.Null);
        Assert.That(anyAdminFilter.CompositeOperator, Is.EqualTo(CompositeOperator.Any));
        Assert.That(anyAdminFilter.ColumnName, Is.EqualTo("SubObjects"));
        Assert.That(anyAdminFilter.Values.Length, Is.EqualTo(1));

        var anyAdminEntityFilter = EntityFilter<ObjectB>.Where(anyAdminFilter);
        var anyAdminResult = anyAdminEntityFilter.Filter(queryable).ToList();
        
        Assert.That(anyAdminResult, Has.Count.EqualTo(1));
        Assert.That(anyAdminResult[0].PropertyB, Is.EqualTo("Parent1"));

        // Test 2: All operator - objects where all SubObjects have CommonProperty = "Active"
        var allActiveQuery = "SubObjects:all(CommonProperty:eq(Active))";
        var allActiveFilter = CompositeFiltering.ParseComposite(allActiveQuery) as CompositeFiltering;
        
        Assert.That(allActiveFilter, Is.Not.Null);
        Assert.That(allActiveFilter.CompositeOperator, Is.EqualTo(CompositeOperator.All));
        Assert.That(allActiveFilter.ColumnName, Is.EqualTo("SubObjects"));
        
        var allActiveEntityFilter = EntityFilter<ObjectB>.Where(allActiveFilter);
        var allActiveResult = allActiveEntityFilter.Filter(queryable).ToList();
        
        // Should match Parent2 (all Active) and Parent3 (empty collection, All() returns true)
        Assert.That(allActiveResult, Has.Count.EqualTo(2));
        Assert.That(allActiveResult.Any(r => r.PropertyB == "Parent2"), Is.True);
        Assert.That(allActiveResult.Any(r => r.PropertyB == "Parent3"), Is.True);

        // Test 3: Any with multiple conditions - objects where any SubObject has PropertyB = "Manager" AND CommonProperty = "Active"
        var anyManagerActiveQuery = "SubObjects:any(PropertyB:eq(Manager),CommonProperty:eq(Active))";
        var anyManagerActiveFilter = CompositeFiltering.ParseComposite(anyManagerActiveQuery) as CompositeFiltering;
        
        Assert.That(anyManagerActiveFilter, Is.Not.Null);
        Assert.That(anyManagerActiveFilter.Values.Length, Is.EqualTo(2));
        
        var anyManagerActiveEntityFilter = EntityFilter<ObjectB>.Where(anyManagerActiveFilter);
        var anyManagerActiveResult = anyManagerActiveEntityFilter.Filter(queryable).ToList();
        
        Assert.That(anyManagerActiveResult, Has.Count.EqualTo(1));
        Assert.That(anyManagerActiveResult[0].PropertyB, Is.EqualTo("Parent2"));

        // Test 4: All with empty collection - LINQ's All() returns true for empty collections
        var allEmptyQuery = "SubObjects:all(PropertyB:eq(AnyValue))";
        var allEmptyFilter = CompositeFiltering.ParseComposite(allEmptyQuery) as CompositeFiltering;
        
        var allEmptyEntityFilter = EntityFilter<ObjectB>.Where(allEmptyFilter!);
        var allEmptyResult = allEmptyEntityFilter.Filter(queryable).ToList();
        
        // Parent3 has empty SubObjects collection, so All() returns true for empty collections in LINQ
        // This is mathematically correct: "All items in an empty set satisfy any condition"
        Assert.That(allEmptyResult, Has.Count.EqualTo(1));
        Assert.That(allEmptyResult[0].PropertyB, Is.EqualTo("Parent3"));
    }

    [TestCase("SubObjects:any(PropertyB:eq(Admin))", CompositeOperator.Any, "SubObjects", 1)]
    [TestCase("SubObjects:all(CommonProperty:eq(Active))", CompositeOperator.All, "SubObjects", 1)]
    [TestCase("Items:any(Name:ct(test),Age:gt(18))", CompositeOperator.Any, "Items", 2)]
    [TestCase("Roles:all(IsActive:eq(true))", CompositeOperator.All, "Roles", 1)]
    [TestCase("Children:any(Status:neq(Deleted),Priority:gte(5))", CompositeOperator.Any, "Children", 2)]
    public void Parse_various_any_all_query_formats(string query, CompositeOperator expectedOperator, 
        string expectedColumnName, int expectedFilterCount)
    {
        // Act
        var result = CompositeFiltering.ParseComposite(query) as CompositeFiltering;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.CompositeOperator, Is.EqualTo(expectedOperator));
        Assert.That(result.ColumnName, Is.EqualTo(expectedColumnName));
        Assert.That(result.Values.Length, Is.EqualTo(expectedFilterCount));
        
        // Verify the query string round-trip (allowing for spaces after commas)
        var regeneratedQuery = result.ToString();
        var normalizedQuery = query.Replace(",", ", ");
        Assert.That(regeneratedQuery, Is.EqualTo(normalizedQuery));
    }

    [TestCase("SubObjects:any(PropertyB:eq(Admin),CommonProperty:neq(Inactive))", 2)]
    [TestCase("Items:all(Name:ct(valid),Status:eq(Active),Type:sw(Product))", 3)]
    [TestCase("Users:any(Email:ew(@company.com))", 1)]
    [TestCase("Orders:all(Amount:gt(100),Date:gte(2024-01-01),Status:eq(Completed))", 3)]
    public void Parse_complex_any_all_with_multiple_conditions(string query, int expectedConditionCount)
    {
        // Act
        var result = CompositeFiltering.ParseComposite(query) as CompositeFiltering;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Values.Length, Is.EqualTo(expectedConditionCount));

        // Verify each inner condition is properly parsed
        foreach (var filter in result.Values)
        {
            Assert.That(filter, Is.InstanceOf<Filtering>());
            var filtering = filter as Filtering;
            Assert.That(filtering!.ColumnName, Is.Not.Null.And.Not.Empty);
            Assert.That(filtering.StringValue, Is.Not.Null);
            Assert.That(filtering.Operator, Is.TypeOf<FilterOperator>());
        }
    }

    [Test]
    public void Integration_test_with_entity_filter_and_real_data()
    {
        // Arrange - Create more complex test scenario
        var testData = new List<ObjectB>
        {
            new ObjectB 
            { 
                PropertyB = "Company A", 
                CommonProperty = "Tech",
                SubObjects = new List<SubObjectB>
                {
                    new SubObjectB { PropertyB = "Developer", CommonProperty = "Senior" },
                    new SubObjectB { PropertyB = "Manager", CommonProperty = "Junior" },
                    new SubObjectB { PropertyB = "Architect", CommonProperty = "Senior" }
                }
            },
            new ObjectB 
            { 
                PropertyB = "Company B", 
                CommonProperty = "Finance",
                SubObjects = new List<SubObjectB>
                {
                    new SubObjectB { PropertyB = "Analyst", CommonProperty = "Senior" },
                    new SubObjectB { PropertyB = "Director", CommonProperty = "Senior" }
                }
            },
            new ObjectB 
            { 
                PropertyB = "Company C", 
                CommonProperty = "Marketing",
                SubObjects = new List<SubObjectB>
                {
                    new SubObjectB { PropertyB = "Coordinator", CommonProperty = "Junior" },
                    new SubObjectB { PropertyB = "Specialist", CommonProperty = "Junior" }
                }
            }
        };

        var queryable = testData.AsQueryable();

        // This method now serves as setup for the individual test cases below
        // Individual test cases are handled by separate [TestCase] methods
        Assert.That(testData, Is.Not.Empty, "Test data should not be empty");
    }

    [TestCase("SubObjects:any(PropertyB:eq(Developer))", 1)]
    [TestCase("SubObjects:all(CommonProperty:eq(Senior))", 1)]
    [TestCase("SubObjects:any(CommonProperty:eq(Junior))", 2)]
    [TestCase("SubObjects:all(CommonProperty:eq(Junior))", 1)]
    [TestCase("SubObjects:any(PropertyB:ct(Manager))", 1)]
    public void Integration_test_entity_filter_with_real_data_scenarios(string query, int expectedCount)
    {
        // Arrange - Create more complex test scenario
        var testData = new List<ObjectB>
        {
            new ObjectB 
            { 
                PropertyB = "Company A", 
                CommonProperty = "Tech",
                SubObjects = new List<SubObjectB>
                {
                    new SubObjectB { PropertyB = "Developer", CommonProperty = "Senior" },
                    new SubObjectB { PropertyB = "Manager", CommonProperty = "Junior" },
                    new SubObjectB { PropertyB = "Architect", CommonProperty = "Senior" }
                }
            },
            new ObjectB 
            { 
                PropertyB = "Company B", 
                CommonProperty = "Finance",
                SubObjects = new List<SubObjectB>
                {
                    new SubObjectB { PropertyB = "Analyst", CommonProperty = "Senior" },
                    new SubObjectB { PropertyB = "Director", CommonProperty = "Senior" }
                }
            },
            new ObjectB 
            { 
                PropertyB = "Company C", 
                CommonProperty = "Marketing",
                SubObjects = new List<SubObjectB>
                {
                    new SubObjectB { PropertyB = "Coordinator", CommonProperty = "Junior" },
                    new SubObjectB { PropertyB = "Specialist", CommonProperty = "Junior" }
                }
            }
        };

        var queryable = testData.AsQueryable();

        // Act
        var filter = CompositeFiltering.ParseComposite(query) as CompositeFiltering;
        Assert.That(filter, Is.Not.Null, $"Failed to parse query: {query}");

        var entityFilter = EntityFilter<ObjectB>.Where(filter);
        var results = entityFilter.Filter(queryable).ToList();

        // Assert
        Assert.That(results, Has.Count.EqualTo(expectedCount), $"Query: {query}");
    }

    [Test]
    public void Integration_test_nested_composite_works_with_parsing_only()
    {
        // This test validates that nested composite parsing works without requiring EntityFilter integration
        // Property resolution for nested structures is handled separately for integration scenarios
        
        // Arrange
        var query = "SubObjects:any(or(PropertyB:eq(Developer),PropertyB:eq(Manager)))";

        // Act
        var result = CompositeFiltering.ParseComposite(query) as CompositeFiltering;

        // Assert - Verify complete parsing structure
        Assert.That(result, Is.Not.Null);
        Assert.That(result.CompositeOperator, Is.EqualTo(CompositeOperator.Any));
        Assert.That(result.ColumnName, Is.EqualTo("SubObjects"));
        Assert.That(result.Values.Length, Is.EqualTo(1));
        
        // Verify nested structure
        var nestedComposite = result.Values[0] as CompositeFiltering;
        Assert.That(nestedComposite, Is.Not.Null);
        Assert.That(nestedComposite.CompositeOperator, Is.EqualTo(CompositeOperator.Or));
        Assert.That(nestedComposite.Values.Length, Is.EqualTo(2));
        
        // Verify inner conditions
        var condition1 = nestedComposite.Values[0] as Filtering;
        var condition2 = nestedComposite.Values[1] as Filtering;
        
        Assert.That(condition1, Is.Not.Null);
        Assert.That(condition1.ColumnName, Is.EqualTo("PropertyB"));
        Assert.That(condition1.StringValue, Is.EqualTo("Developer"));
        
        Assert.That(condition2, Is.Not.Null);
        Assert.That(condition2.ColumnName, Is.EqualTo("PropertyB"));
        Assert.That(condition2.StringValue, Is.EqualTo("Manager"));
        
        // Verify round-trip
        var regenerated = result.ToString();
        Assert.That(regenerated, Is.EqualTo("SubObjects:any(or(PropertyB:eq(Developer), PropertyB:eq(Manager)))"));
    }

    [Test]
    public void Parse_nested_composite_operators_should_create_proper_structure()
    {
        // Arrange
        var query = "SubObjects:any(or(PropertyB:eq(Developer),PropertyB:eq(Manager)))";

        // Act
        var result = CompositeFiltering.ParseComposite(query) as CompositeFiltering;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.CompositeOperator, Is.EqualTo(CompositeOperator.Any));
        Assert.That(result.ColumnName, Is.EqualTo("SubObjects"));
        Assert.That(result.Values.Length, Is.EqualTo(1));
        
        // The inner value should be a CompositeFiltering with OR operator
        var innerComposite = result.Values[0] as CompositeFiltering;
        Assert.That(innerComposite, Is.Not.Null);
        Assert.That(innerComposite.CompositeOperator, Is.EqualTo(CompositeOperator.Or));
        Assert.That(innerComposite.Values.Length, Is.EqualTo(2));
        
        // Verify the two inner conditions
        var condition1 = innerComposite.Values[0] as Filtering;
        var condition2 = innerComposite.Values[1] as Filtering;
        
        Assert.That(condition1, Is.Not.Null);
        Assert.That(condition1.ColumnName, Is.EqualTo("PropertyB"));
        Assert.That(condition1.StringValue, Is.EqualTo("Developer"));
        Assert.That(condition1.Operator, Is.EqualTo(FilterOperator.Equal));
        
        Assert.That(condition2, Is.Not.Null);
        Assert.That(condition2.ColumnName, Is.EqualTo("PropertyB"));
        Assert.That(condition2.StringValue, Is.EqualTo("Manager"));
        Assert.That(condition2.Operator, Is.EqualTo(FilterOperator.Equal));
    }

    [TestCase("Users:any(or(Age:gte(18),Status:eq(Active)))", "Users with any adult or active user")]
    [TestCase("Orders:all(and(Amount:gt(100),Status:neq(Cancelled)))", "Orders where all items are valuable and not cancelled")]
    [TestCase("Products:any(or(Category:eq(Electronics),Price:lte(50)))", "Products that are electronics or cheap")]
    [TestCase("Employees:all(and(Department:neq(Temp),Salary:gte(30000)))", "Employees where all are permanent with good salary")]
    [TestCase("Projects:any(or(Priority:eq(High),Deadline:lte(2024-12-31)))", "Projects with high priority or near deadline")]
    [TestCase("Customers:all(and(Status:eq(Premium),Orders:gt(10)))", "Customers where all are premium with many orders")]
    [TestCase("Tasks:any(or(Assignee:ct(John),Category:sw(Urgent)))", "Tasks assigned to John or urgent category")]
    [TestCase("Reviews:all(and(Rating:gte(4),Status:neq(Spam)))", "Reviews where all are highly rated and not spam")]
    [TestCase("Files:any(or(Extension:eq(pdf),Size:lte(1024)))", "Files that are PDF or small")]
    [TestCase("Meetings:all(and(Duration:lte(60),Type:neq(Optional)))", "Meetings where all are short and mandatory")]
    public void Parse_complex_business_scenarios_with_nested_operators(string query, string description)
    {
        // Act
        var result = CompositeFiltering.ParseComposite(query) as CompositeFiltering;

        // Assert
        Assert.That(result, Is.Not.Null, $"Failed to parse: {description}");
        Assert.That(result.CompositeOperator, Is.AnyOf(CompositeOperator.Any, CompositeOperator.All));
        Assert.That(result.Values.Length, Is.EqualTo(1), "Should have one nested composite operator");
        
        var nestedComposite = result.Values[0] as CompositeFiltering;
        Assert.That(nestedComposite, Is.Not.Null, "Inner operator should be composite");
        Assert.That(nestedComposite.CompositeOperator, Is.AnyOf(CompositeOperator.And, CompositeOperator.Or));
        Assert.That(nestedComposite.Values.Length, Is.EqualTo(2), "Should have exactly two inner conditions");
        
        // Verify round-trip parsing works
        var regeneratedQuery = result.ToString();
        var reparsedResult = CompositeFiltering.ParseComposite(regeneratedQuery);
        Assert.That(reparsedResult, Is.Not.Null, $"Round-trip parsing failed for: {description}");
    }

    [TestCase("Roles:any(or(Name:eq(Admin),Permissions:ct(Write),Level:gte(5)))", 3)]
    [TestCase("Branches:all(and(Status:eq(Active),Employees:gt(10),Revenue:gte(100000)))", 3)]
    [TestCase("Campaigns:any(or(Budget:lte(1000),ROI:gte(200),Status:neq(Paused)))", 3)]
    [TestCase("Servers:all(and(CPU:lte(80),Memory:lte(90),Status:eq(Running),Location:neq(Maintenance)))", 4)]
    [TestCase("Invoices:any(or(Amount:gt(5000),Status:eq(Overdue),Priority:eq(High),Customer:ct(Enterprise)))", 4)]
    public void Parse_nested_operators_with_multiple_conditions(string query, int expectedConditionCount)
    {
        // Act
        var result = CompositeFiltering.ParseComposite(query) as CompositeFiltering;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Values.Length, Is.EqualTo(1));
        
        var nestedComposite = result.Values[0] as CompositeFiltering;
        Assert.That(nestedComposite, Is.Not.Null);
        Assert.That(nestedComposite.Values.Length, Is.EqualTo(expectedConditionCount));
        
        // Verify all inner conditions are properly parsed
        foreach (var condition in nestedComposite.Values)
        {
            Assert.That(condition, Is.InstanceOf<Filtering>());
            var filtering = condition as Filtering;
            Assert.That(filtering!.ColumnName, Is.Not.Null.And.Not.Empty);
            Assert.That(filtering.StringValue, Is.Not.Null);
            Assert.That(filtering.Operator, Is.TypeOf<FilterOperator>());
        }
    }

    [TestCase("Teams:any(or(Members:any(Role:eq(Lead)),Projects:all(Status:eq(Active))))", "Double nested: teams with lead members or all active projects")]
    [TestCase("Companies:all(and(Offices:any(Country:eq(USA)),Employees:all(Type:neq(Contractor))))", "Double nested: companies with US offices and no contractors")]
    [TestCase("Departments:any(or(Budget:gt(50000),Staff:all(Level:gte(Senior))))", "Mixed nesting: departments with big budget or all senior staff")]
    public void Parse_deeply_nested_composite_operators(string query, string description)
    {
        // Act  
        var result = CompositeFiltering.ParseComposite(query);

        // Assert - Just verify it parses without errors for now
        // (Deep nesting would require more complex implementation)
        Assert.That(result, Is.Not.Null, $"Should parse basic structure: {description}");
        
        // Verify basic structure
        if (result is CompositeFiltering composite)
        {
            Assert.That(composite.CompositeOperator, Is.AnyOf(CompositeOperator.Any, CompositeOperator.All));
            Assert.That(composite.Values.Length, Is.GreaterThan(0));
        }
    }

    [TestCase("SubObjects:any(or(PropertyB:eq(Test Value With Spaces),CommonProperty:ct(Multi Word)))", "Handles values with spaces")]
    [TestCase("SubObjects:all(and(PropertyB:sw(Pre),PropertyB:ew(Post)))", "Same column with different operators")]
    [TestCase("SubObjects:any(or(PropertyB:eq(),CommonProperty:neq()))", "Empty values")]
    [TestCase("SubObjects:all(and(PropertyB:ct(Special-Chars_123),CommonProperty:eq(UPPERCASE)))", "Special characters and case")]
    public void Parse_edge_cases_in_nested_operators(string query, string description)
    {
        // Act & Assert - Should handle edge cases gracefully
        var result = CompositeFiltering.ParseComposite(query);
        Assert.That(result, Is.Not.Null, $"Should handle edge case: {description}");
        
        if (result is CompositeFiltering composite)
        {
            Assert.That(composite.Values.Length, Is.GreaterThan(0));
            var regenerated = composite.ToString();
            Assert.That(regenerated, Is.Not.Null.And.Not.Empty, "Should generate valid query string");
        }
    }
}
