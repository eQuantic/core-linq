namespace eQuantic.Linq.Tests;

public class ObjectB
{
    public string PropertyB { get; set; } = string.Empty;
    public string CommonProperty { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public SubObjectB SubObject { get; set; } = new();
    
    public List<SubObjectB> SubObjects { get; set; } = new();
}