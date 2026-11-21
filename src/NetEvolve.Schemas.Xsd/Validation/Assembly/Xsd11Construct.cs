namespace NetEvolve.Schemas.Xsd.Validation.Assembly;

using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>One XSD 1.1 construct that the assembled validator does not support yet, with the rule it reports under the default version.</summary>
internal sealed class Xsd11Construct
{
    internal Xsd11Construct(XsdRule notYetSupported) => NotYetSupported = notYetSupported;

    /// <summary>Gets the not-yet-supported rule.</summary>
    internal XsdRule NotYetSupported { get; }
}
