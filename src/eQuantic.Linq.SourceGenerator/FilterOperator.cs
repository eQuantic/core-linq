using System;

namespace eQuantic.Linq.SourceGenerator;

/// <summary>
/// Filter operators for source generator (mirrors eQuantic.Linq.Filter.FilterOperator)
/// </summary>
[Flags]
public enum FilterOperator
{
    None = 0,
    Equal = 1,
    NotEqual = 2,
    GreaterThan = 4,
    LessThan = 8,
    GreaterThanOrEqual = 16,
    LessThanOrEqual = 32,
    Contains = 64,
    StartsWith = 128,
    EndsWith = 256,
    IsNull = 512,
    IsNotNull = 1024,
    In = 2048,
    NotIn = 4096,
    Between = 8192,
    NotBetween = 16384
}