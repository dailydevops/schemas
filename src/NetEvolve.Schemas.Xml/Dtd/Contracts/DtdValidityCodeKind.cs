namespace NetEvolve.Schemas.Xml.Dtd.Contracts;

/// <summary>The family of a <see cref="DtdValidityCode"/>.</summary>
public enum DtdValidityCodeKind
{
    /// <summary>One of the 26 validity constraints of XML 1.0 (Fifth Edition).</summary>
    ValidityConstraint = 0,

    /// <summary>A report that is not a validity constraint, such as an optional warning.</summary>
    Report = 1,
}
