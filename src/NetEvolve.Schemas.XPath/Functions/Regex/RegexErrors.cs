namespace NetEvolve.Schemas.XPath.Functions.Regex;

using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Limits;

/// <summary>
/// The errors of the regular expression functions: the codes of the specification (<c>FORX0001</c> to <c>FORX0004</c>) and the engine
/// code of a pattern or a search that exceeds a limit of the regex engine. The specifications name no code for an implementation
/// limit, so a valid pattern that the engine cannot run has a code of the library (decision D4 of #266), as the evaluation limits have.
/// </summary>
internal static class RegexErrors
{
    /// <summary>The engine code of a regular expression whose program or search exceeds a limit of the regex engine.</summary>
    internal static readonly XPathErrorCode LimitExceeded = new(XPathLimitErrors.EngineNamespace, "XP0007");

    /// <summary>Builds the exception for a pattern that does not compile.</summary>
    /// <param name="error">The error of the parser or of the compilation.</param>
    /// <returns><see cref="XPathErrorCodes.FORX0001"/> for an invalid flag, the engine code for a limit, and <see cref="XPathErrorCodes.FORX0002"/> for any other error.</returns>
    internal static XPathException Pattern(RegexError error) =>
        error switch
        {
            RegexError.InvalidFlag => XPathErrors.Exception(XPathErrorCodes.FORX0001),
            RegexError.LimitExceeded => Limit("The regular expression exceeds a limit of the regex engine."),
            _ => XPathErrors.Exception(XPathErrorCodes.FORX0002),
        };

    /// <summary>Builds the exception for a search that reached its step ceiling, so that no answer is known.</summary>
    /// <returns>The exception to throw.</returns>
    internal static XPathException StepCeiling() =>
        Limit("The search of the regular expression reached its step ceiling.");

    private static XPathException Limit(string message) =>
        new(new XPathDiagnostic(LimitExceeded, XPathErrorKind.Engine, message, -1, 0));
}
