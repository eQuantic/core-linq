using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using eQuantic.Linq.Filter;

namespace eQuantic.Linq.Tests.Filter
{
    [TestFixture]
    public class EntityFilterBuilderTests
    {
        private static object[] TestCases =
        {
            new object[] {
                nameof(TestObject.Name), "name", FilterOperator.Equal,
                (Expression<Func<TestObject, bool>>)(entity => entity.Name == "name")
            },
            new object[] {
                nameof(TestObject.Name), "name", FilterOperator.NotEqual,
                (Expression<Func<TestObject, bool>>)(entity => entity.Name != "name")
            },
            new object[] {
                nameof(TestObject.Name), null, FilterOperator.NotEqual,
                (Expression<Func<TestObject, bool>>)(entity => entity.Name != null)
            },
            new object[] {
                nameof(TestObject.Name), "name", FilterOperator.StartsWith,
                (Expression<Func<TestObject, bool>>)(entity => entity.Name.StartsWith("name"))
            },
            new object[] {
                nameof(TestObject.Name), "name", FilterOperator.EndsWith,
                (Expression<Func<TestObject, bool>>)(entity => entity.Name.EndsWith("name"))
            },
            new object[] {
                nameof(TestObject.Number), 1, FilterOperator.Equal,
                (Expression<Func<TestObject, bool>>)(entity => entity.Number == 1)
            },
            new object[] {
                nameof(TestObject.Number), 1, FilterOperator.GreaterThan,
                (Expression<Func<TestObject, bool>>)(entity => entity.Number > 1)
            },
            new object[] {
                nameof(TestObject.Number), 1, FilterOperator.GreaterThanOrEqual,
                (Expression<Func<TestObject, bool>>)(entity => entity.Number >= 1)
            },
            new object[] {
                nameof(TestObject.Number), 1, FilterOperator.LessThan,
                (Expression<Func<TestObject, bool>>)(entity => entity.Number < 1)
            },
            new object[] {
                nameof(TestObject.Number), 1, FilterOperator.LessThanOrEqual,
                (Expression<Func<TestObject, bool>>)(entity => entity.Number <= 1)
            },
            new object[] {
                nameof(TestObject.NullableNumber), "", FilterOperator.Equal,
                (Expression<Func<TestObject, bool>>)(entity => entity.NullableNumber == null)
            },
            new object[] {
                nameof(TestObject.Double), "6.5", FilterOperator.Equal,
                (Expression<Func<TestObject, bool>>)(entity => entity.Double == 6.5)
            },
            new object[] {
                $"{nameof(TestObject.Child)}.{nameof(TestChildObject.Value)}", "value", FilterOperator.Equal,
                (Expression<Func<TestObject, bool>>)(entity => entity.Child.Value == "value")
            },
            new object[] {
                nameof(TestObject.Name), "test", FilterOperator.Contains,
                (Expression<Func<TestObject, bool>>)(entity => entity.Name.Contains("test"))
            },
            new object[] {
                nameof(TestObject.Name), "test", FilterOperator.NotContains,
                (Expression<Func<TestObject, bool>>)(entity => !entity.Name.Contains("test"))
            },
        };

        private static object[] TestCasesComplexCasts =
        {
            new object[] {nameof(TestObject.Type), "Foo", FilterOperator.Equal},
            new object[] {nameof(TestObject.UniqueId), "3fcbcf67-393a-4df3-94cc-9fa3654964bc", FilterOperator.Equal},
            new object[] {nameof(TestObject.NullableNumber), 1, FilterOperator.Equal},
            new object[] {nameof(TestObject.NullableNumber), "1", FilterOperator.Equal}
        };

        private static object[] TestCasesWithColumns =
        {
            new object[] {nameof(TestObject.NumberWithDifferentColumnName), "1", FilterOperator.Equal},
            new object[] {"Number2", "1", FilterOperator.Equal}
        };

        public enum Type
        {
            Foo,
            Bar
        }

        [Test]
        [TestCaseSource(nameof(TestCasesComplexCasts))]
        public void BuildWhereEntityFilter_returns_filter_When_casts_are_complex(string propertyName, object value, FilterOperator filterOperator)
        {
            // Arrange
            var builder = new EntityFilterBuilder<TestObject>(propertyName, value, filterOperator);

            // Act
            var actualSorter = builder.BuildWhereEntityFilter();

            // Assert
            Assert.NotNull(actualSorter);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void EntityFilterBuilder_throws_ArgumentException_When_property_does_not_exist(bool useColumnFallback)
        {
            // Arrange

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new EntityFilterBuilder<TestObject>("non-existant", 1, FilterOperator.Equal, useColumnFallback));
        }

        [TestCase(nameof(TestObject.NullableNumber), "a", FilterOperator.Contains)]
        public void EntityFilterBuilder_throws_FormatException_When_conversion_fails(string propertyName, object value, FilterOperator filterOperator)
        {
            // Arrange

            // Act & Assert
            Assert.Throws<FormatException>(() => new EntityFilterBuilder<TestObject>(propertyName, value, filterOperator));
        }

        [Test]
        [TestCaseSource(nameof(TestCasesWithColumns))]
        public void BuildWhereEntityFilter_returns_filter_When_column_name_has_attribute(string propertyName, object value, FilterOperator filterOperator)
        {
            // Arrange
            var builder = new EntityFilterBuilder<TestObject>(propertyName, value, filterOperator, true);

            // Act
            var actualSorter = builder.BuildWhereEntityFilter();

            // Assert
            Assert.NotNull(actualSorter);
        }

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void Test_EntityFilterBuilder_BuildWhereEntityFilter(string propertyName, object value, FilterOperator filterOperator, Expression<Func<TestObject, bool>> expression)
        {
            // Arrange
            var expectedFilter = new WhereEntityFilter<TestObject>(expression);

            var builder = new EntityFilterBuilder<TestObject>(propertyName, value, filterOperator);

            // Act
            var actualSorter = builder.BuildWhereEntityFilter();

            // Assert
            Assert.AreEqual(expectedFilter, actualSorter);
        }

        [Test]
        public void Test_EntityFilterBuilder_BuildWhereEntityFilter_WithContainsTypeNotString()
        {
            // Arrange
            const string propertyName = nameof(TestObject.Number);
            object value = "1";
            const FilterOperator filterOperator = FilterOperator.Contains;
            var builder = new EntityFilterBuilder<TestObject>(propertyName, value, filterOperator);
            const string expected = "entity => value(System.Collections.Generic.List`1[System.Int32]).Contains(entity.Number)";

            // Act
            var actualSorter = builder.BuildWhereEntityFilter();

            // Assert
            Assert.AreEqual(expected, actualSorter.ToString());
        }

        [Test]
        public void Test_EntityFilterBuilder_BuildWhereEntityFilter_WithNotContainsTypeNotString()
        {
            // Arrange
            const string propertyName = nameof(TestObject.Number);
            object value = "1";
            const FilterOperator filterOperator = FilterOperator.NotContains;
            var builder = new EntityFilterBuilder<TestObject>(propertyName, value, filterOperator);
            const string expected = "entity => Not(value(System.Collections.Generic.List`1[System.Int32]).Contains(entity.Number))";

            // Act
            var actualSorter = builder.BuildWhereEntityFilter();

            // Assert
            Assert.AreEqual(expected, actualSorter.ToString());
        }

        public class TestChildObject
        {
            public string Value { get; set; }
        }

        public class TestObject
        {
            public TestChildObject Child { get; set; }
            public DateTime DateTime { get; set; }
            public double Double { get; set; }
            public string Name { get; set; }
            public int? NullableNumber { get; set; }
            public int Number { get; set; }
            public Type Type { get; set; }
            public Guid UniqueId { get; set; }

            [Column("Number2")]
            public int NumberWithDifferentColumnName { get; set; }
        }
    }
}