#pragma warning disable CA1707 // Approved shape (#123): the members are named after the specification versions.

namespace NetEvolve.Schemas.XPath.Api;

/// <summary>The language version of an expression.</summary>
/// <remarks>Limitation: the contract declares all three; compilation enables only <see cref="V2_0"/> until later packages enable the others.</remarks>
public enum XPathVersion
{
    /// <summary>XPath 2.0 (Second Edition).</summary>
    V2_0 = 0,

    /// <summary>XPath 3.0.</summary>
    V3_0 = 1,

    /// <summary>XPath 3.1.</summary>
    V3_1 = 2,
}
