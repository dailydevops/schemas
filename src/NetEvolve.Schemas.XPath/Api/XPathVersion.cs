namespace NetEvolve.Schemas.XPath.Api;

/// <summary>The language version of an expression.</summary>
/// <remarks>Limitation: the contract declares all three; compilation enables only <see cref="V20"/> until later packages enable the others.</remarks>
public enum XPathVersion
{
    /// <summary>XPath 2.0 (Second Edition).</summary>
    V20 = 0,

    /// <summary>XPath 3.0.</summary>
    V30 = 1,

    /// <summary>XPath 3.1.</summary>
    V31 = 2,
}
