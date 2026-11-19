namespace NetEvolve.Schemas.Xml.Datatypes.Facets.Pattern;

/// <summary>The stable codes of the pattern facet; the value is the number of the <c>PTnnnn</c> code.</summary>
/// <remarks>Codes are added, never renumbered. The codes of the pattern syntax are the <c>RXnnnn</c> codes of <see cref="Regex.Contracts.RegexError"/>, reported beside <see cref="InvalidPattern"/>.</remarks>
public enum PatternDiagnosticCode
{
    /// <summary>No diagnostic.</summary>
    None = 0,

    /// <summary><c>PT0001</c>: a pattern does not compile; the <c>RXnnnn</c> error and the position in the pattern are reported separately.</summary>
    InvalidPattern = 1,

    /// <summary><c>PT0002</c>: a match reached the step ceiling before its answer was known; the value is treated as not validated.</summary>
    StepLimitExceeded = 2,
}
