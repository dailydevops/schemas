namespace NetEvolve.Schemas.Xml.Datatypes.Facets.Pattern;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xml.Regex.Contracts;

/// <summary>A coded diagnostic of the pattern facet, with the text of the pattern at fault. <see langword="default"/> is no diagnostic.</summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct PatternDiagnostic
{
    internal PatternDiagnostic(
        PatternDiagnosticCode code,
        string? pattern,
        int patternIndex,
        RegexError regexError,
        int position
    )
    {
        Code = code;
        Pattern = pattern;
        PatternIndex = patternIndex;
        RegexError = regexError;
        Position = position;
    }

    /// <summary>Gets the code, <see cref="PatternDiagnosticCode.None"/> when there is no diagnostic.</summary>
    public PatternDiagnosticCode Code { get; }

    /// <summary>Gets the text of the pattern at fault, <see langword="null"/> when there is no diagnostic.</summary>
    public string? Pattern { get; }

    /// <summary>Gets the zero-based index of the pattern at fault in the facet.</summary>
    public int PatternIndex { get; }

    /// <summary>Gets the error of the pattern syntax; <see cref="NetEvolve.Schemas.Xml.Regex.Contracts.RegexError.None"/> unless <see cref="Code"/> is <see cref="PatternDiagnosticCode.InvalidPattern"/>.</summary>
    public RegexError RegexError { get; }

    /// <summary>
    /// Gets the zero-based UTF-16 index in <see cref="Pattern"/> of the construct at fault for <see cref="PatternDiagnosticCode.InvalidPattern"/>;
    /// zero when the pattern is too large as a whole (<see cref="NetEvolve.Schemas.Xml.Regex.Contracts.RegexError.LimitExceeded"/> of the program size); -1 for a step limit.
    /// </summary>
    public int Position { get; }
}
