namespace NetEvolve.Schemas.XPath.Api;

using System;

/// <summary>The set of language versions in which a function is available.</summary>
[Flags]
public enum XPathVersionMask
{
    /// <summary>No version.</summary>
    None = 0,

    /// <summary>XPath 2.0.</summary>
    V20 = 1,

    /// <summary>XPath 3.0.</summary>
    V30 = 2,

    /// <summary>XPath 3.1.</summary>
    V31 = 4,

    /// <summary>Every version.</summary>
    All = V20 | V30 | V31,
}
