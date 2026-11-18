namespace NetEvolve.Schemas.Xml.Dtd.Model;

/// <summary>The occurrence indicator of a content particle (production 47).</summary>
public enum DtdOccurrence
{
    /// <summary>No indicator: exactly once.</summary>
    Once,

    /// <summary><c>?</c>.</summary>
    Optional,

    /// <summary><c>*</c>.</summary>
    ZeroOrMore,

    /// <summary><c>+</c>.</summary>
    OneOrMore,
}
