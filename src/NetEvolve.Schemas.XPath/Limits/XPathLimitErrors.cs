namespace NetEvolve.Schemas.XPath.Limits;

using System.Globalization;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>
/// The engine codes of the evaluation limits, one per budget (the engine range <c>XP0xxx</c> of the error model, #123). The
/// specifications name no code for an implementation limit, so each breach has a code of the library: an expression that is valid but
/// exceeds a budget fails with it. The message names the limit and its configured value, and the diagnostic carries the location.
/// </summary>
/// <remarks>Limitation: the limits are implementation limits that XPath 2.0 (2.3.1) allows; the codes and the defaults are documented here and in <see cref="XPathLimits"/>.</remarks>
internal static class XPathLimitErrors
{
    /// <summary>The namespace of the engine codes.</summary>
    internal const string EngineNamespace = "urn:netevolve:schemas:xpath";

    /// <summary>The recursion depth exceeds <see cref="XPathLimits.MaxRecursionDepth"/>.</summary>
    internal static readonly XPathErrorCode RecursionDepthExceeded = new(EngineNamespace, "XP0003");

    /// <summary>A sequence has more items than <see cref="XPathLimits.MaxSequenceSize"/>.</summary>
    internal static readonly XPathErrorCode SequenceSizeExceeded = new(EngineNamespace, "XP0004");

    /// <summary>A range has more items than <see cref="XPathLimits.MaxRangeSize"/>.</summary>
    internal static readonly XPathErrorCode RangeSizeExceeded = new(EngineNamespace, "XP0005");

    /// <summary>A string is longer than <see cref="XPathLimits.MaxStringLength"/>.</summary>
    internal static readonly XPathErrorCode StringLengthExceeded = new(EngineNamespace, "XP0006");

    /// <summary>The magnitude of an integer has more bits than <see cref="XPathLimits.MaxIntegerBits"/>.</summary>
    internal static readonly XPathErrorCode IntegerWidthExceeded = new(EngineNamespace, "XP0008");

    /// <summary>The absolute scale of a decimal is above <see cref="XPathLimits.MaxDecimalScale"/>.</summary>
    internal static readonly XPathErrorCode DecimalScaleExceeded = new(EngineNamespace, "XP0009");

    /// <summary>Builds the exception for a breach of <see cref="XPathLimits.MaxRecursionDepth"/>.</summary>
    /// <param name="limit">The configured value.</param>
    /// <param name="location">The location of the expression.</param>
    /// <returns>The exception to throw.</returns>
    internal static XPathException RecursionDepth(int limit, in SourceSpan location) =>
        Breach(RecursionDepthExceeded, nameof(XPathLimits.MaxRecursionDepth), limit, location);

    /// <summary>Builds the exception for a breach of <see cref="XPathLimits.MaxSequenceSize"/>.</summary>
    /// <param name="limit">The configured value.</param>
    /// <param name="location">The location of the expression.</param>
    /// <returns>The exception to throw.</returns>
    internal static XPathException SequenceSize(int limit, in SourceSpan location) =>
        Breach(SequenceSizeExceeded, nameof(XPathLimits.MaxSequenceSize), limit, location);

    /// <summary>Builds the exception for a breach of <see cref="XPathLimits.MaxRangeSize"/>.</summary>
    /// <param name="limit">The configured value.</param>
    /// <param name="location">The location of the expression.</param>
    /// <returns>The exception to throw.</returns>
    internal static XPathException RangeSize(long limit, in SourceSpan location) =>
        Breach(RangeSizeExceeded, nameof(XPathLimits.MaxRangeSize), limit, location);

    /// <summary>Builds the exception for a breach of <see cref="XPathLimits.MaxStringLength"/>.</summary>
    /// <param name="limit">The configured value.</param>
    /// <param name="location">The location of the expression.</param>
    /// <returns>The exception to throw.</returns>
    internal static XPathException StringLength(long limit, in SourceSpan location) =>
        Breach(StringLengthExceeded, nameof(XPathLimits.MaxStringLength), limit, location);

    /// <summary>Builds the exception for a breach of <see cref="XPathLimits.MaxIntegerBits"/>.</summary>
    /// <param name="limit">The configured value.</param>
    /// <param name="location">The location of the expression.</param>
    /// <returns>The exception to throw.</returns>
    internal static XPathException IntegerWidth(int limit, in SourceSpan location) =>
        Breach(IntegerWidthExceeded, nameof(XPathLimits.MaxIntegerBits), limit, location);

    /// <summary>Builds the exception for a breach of <see cref="XPathLimits.MaxDecimalScale"/>.</summary>
    /// <param name="limit">The configured value.</param>
    /// <param name="location">The location of the expression.</param>
    /// <returns>The exception to throw.</returns>
    internal static XPathException DecimalScale(int limit, in SourceSpan location) =>
        Breach(DecimalScaleExceeded, nameof(XPathLimits.MaxDecimalScale), limit, location);

    // Built on the failure path only; the checks themselves compare and call this.
    private static XPathException Breach(XPathErrorCode code, string name, long limit, in SourceSpan location) =>
        new(
            new XPathDiagnostic(
                code,
                XPathErrorKind.Engine,
                "The evaluation limit "
                    + name
                    + " of "
                    + limit.ToString(CultureInfo.InvariantCulture)
                    + " was exceeded.",
                location.Offset,
                location.Length
            )
        );
}
