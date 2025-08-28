namespace eQuantic.Linq.Tests;

public class ObjectA
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public bool IsActive { get; set; }
    
    // Original properties
    public string PropertyA { get; set; } = string.Empty;
    public string CommonProperty { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public SubObjectA SubObject { get; set; } = new();

    public List<SubObjectA> SubObjects { get; set; } = new();
}