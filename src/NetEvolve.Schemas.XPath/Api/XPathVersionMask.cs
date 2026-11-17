#pragma warning disable CA1707 // Approved shape (#123): the members are named after the specification versions.

namespace NetEvolve.Schemas.XPath.Api;

using System;

/// <summary>The set of language versions in which a function is available.</summary>
[Flags]
public enum XPathVersionMask
{
    /// <summary>No version.</summary>
    None = 0,

    /// <summary>XPath 2.0.</summary>
    V2_0 = 1,

    /// <summary>XPath 3.0.</summary>
    V3_0 = 2,

    /// <summary>XPath 3.1.</summary>
    V3_1 = 4,

    /// <summary>Every version.</summary>
    All = V2_0 | V3_0 | V3_1,
}
