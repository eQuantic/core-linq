namespace eQuantic.Linq.Web;

/// <summary>Sort direction of a query-string ordering segment.</summary>
public enum SortDirection
{
    /// <summary>Ascending order (default).</summary>
    Ascending,

    /// <summary>Descending order.</summary>
    Descending,
}

/// <summary>When to inject C# <c>?.</c>-style null guards into rebuilt predicates.</summary>
public enum NullGuardMode
{
    /// <summary>Guard only when the target is LINQ-to-objects (<see cref="System.Linq.EnumerableQuery"/>); relational providers translate nulls natively.</summary>
    Auto,

    /// <summary>Always guard.</summary>
    Always,

    /// <summary>Never guard.</summary>
    Never,
}
