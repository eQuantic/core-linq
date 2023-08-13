using eQuantic.Linq.Filter;

namespace eQuantic.Linq.Tests.Filter
{
    [TestFixture]
    public class FilteringTests
    {
        [TestCase("column", "value", FilterOperator.Equal, "column:eq(value)")]
        [TestCase("column", "eq(value)", null, "column:eq(value)")]
        [TestCase("column", "value", null, "column:eq(value)")]
        public void Create_filtering_with_arguments_successfully(string columnName, string stringValue,
            FilterOperator? filterOperator, string expectedExpression)
        {
            // Arrange
            var filtering = new Filtering(columnName, stringValue, filterOperator);

            // Act
            var actualExpression = filtering.ToString();

            // Assert
            Assert.That(expectedExpression, Is.EqualTo(actualExpression));
        }
        
        [TestCase("column", "invalid(value)")]
        public void Create_filtering_with_arguments_throws_FormatException(string columnName, string stringValue)
        {
            Assert.Throws<FormatException>(() =>
            {
                var filtering = new Filtering(columnName, stringValue);
            });
        }
        
        [TestCase("name:test", FilterOperator.Equal, "test")]
        [TestCase("name:eq(test1)", FilterOperator.Equal, "test1")]
        [TestCase("name:nct(test2)", FilterOperator.NotContains, "test2")]
        public void Parse_query_successfully(string query, FilterOperator expectedOperator, string expectedValue)
        {
            // Arrange and Act
            var filter = Filtering.Parse(query);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(filter, Is.Not.Null);
                Assert.That(filter, Is.InstanceOf<Filtering>());
                Assert.That(expectedOperator, Is.EqualTo(filter.Operator));
                Assert.That(expectedValue, Is.EqualTo(filter.StringValue));
            });
        }
    }
}