namespace NetEvolve.Schemas.Xml.Dtd.Contracts;

/// <summary>
/// The treatment of a non-deterministic content model (XML 1.0, section 3.2.1 and Appendix E). The accepted language
/// is the same in every mode; the mode only decides whether and how severely a model is reported.
/// </summary>
public enum DtdNonDeterminism
{
    /// <summary>The model is accepted and nothing is reported.</summary>
    Accept = 0,

    /// <summary>The model is accepted and reported as the warning <see cref="DtdValidityCodes.XML5911"/>; the default.</summary>
    Warn = 1,

    /// <summary>The model is accepted and reported as an error with the code <see cref="DtdValidityCodes.XML5911"/>.</summary>
    Reject = 2,
}
