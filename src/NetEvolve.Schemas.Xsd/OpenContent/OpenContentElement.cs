namespace NetEvolve.Schemas.Xsd.OpenContent;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>
/// The XML representation of open content: an <c>openContent</c> child of a complex type, or the <c>defaultOpenContent</c> child
/// of a <c>schema</c> (Structures 1.1, 3.4.2.3.3 and 3.4.3).
/// </summary>
internal sealed class OpenContentElement
{
    private OpenContentElement(
        OpenContentMode mode,
        IWildcard? wildcard,
        bool appliesToEmpty,
        DocumentLocation location
    )
    {
        Mode = mode;
        Wildcard = wildcard;
        AppliesToEmpty = appliesToEmpty;
        Location = location;
    }

    /// <summary>Gets the mode.</summary>
    public OpenContentMode Mode { get; }

    /// <summary>Gets the wildcard of the <c>any</c> child, <see langword="null"/> for mode <see cref="OpenContentMode.None"/>.</summary>
    public IWildcard? Wildcard { get; }

    /// <summary>Gets a value indicating whether a default applies to types with empty explicit content (<c>appliesToEmpty</c>).</summary>
    public bool AppliesToEmpty { get; }

    /// <summary>Gets the location of the element.</summary>
    public DocumentLocation Location { get; }

    /// <summary>Creates the representation of an <c>openContent</c> child.</summary>
    /// <param name="mode">The <c>mode</c> attribute, <see cref="OpenContentMode.Interleave"/> when absent.</param>
    /// <param name="wildcard">The wildcard of the <c>any</c> child, <see langword="null"/> when there is none.</param>
    /// <param name="location">The location of the element.</param>
    /// <param name="reporter">The reporter.</param>
    /// <returns>The representation, <see langword="null"/> after an issue was reported.</returns>
    public static OpenContentElement? Create(
        OpenContentMode mode,
        IWildcard? wildcard,
        DocumentLocation location,
        XsdIssueReporter reporter
    )
    {
        ArgumentOutOfRangeException.ThrowIfLessThan((int)mode, (int)OpenContentMode.None);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)mode, (int)OpenContentMode.Suffix);
        ArgumentNullException.ThrowIfNull(reporter);

        var none = mode == OpenContentMode.None;
        if (none == (wildcard is not null))
        {
            ReportRepresentation(reporter, location, none);
            return null;
        }

        return new(mode, wildcard, false, location);
    }

    private static void ReportRepresentation(
        XsdIssueReporter reporter,
        DocumentLocation location,
        bool noneWithWildcard
    ) =>
        reporter.Report(
            XsdRuleLookup.Find(XsdRuleCodes.SrcCt),
            location,
            noneWithWildcard
                ? "An openContent element with mode none must not have an any element."
                : "An openContent element with a mode other than none must have an any element.",
            noneWithWildcard ? "src-ct.4" : "src-ct.3"
        );

    /// <summary>Creates the representation of the <c>defaultOpenContent</c> child of a schema.</summary>
    /// <param name="mode">The <c>mode</c> attribute, <see cref="OpenContentMode.Interleave"/> when absent.</param>
    /// <param name="wildcard">The wildcard of the <c>any</c> child.</param>
    /// <param name="appliesToEmpty">The <c>appliesToEmpty</c> attribute, <see langword="false"/> when absent.</param>
    /// <param name="location">The location of the element.</param>
    /// <returns>The representation.</returns>
    public static OpenContentElement CreateDefault(
        OpenContentMode mode,
        IWildcard wildcard,
        bool appliesToEmpty,
        DocumentLocation location
    )
    {
        ArgumentOutOfRangeException.ThrowIfLessThan((int)mode, (int)OpenContentMode.Interleave);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)mode, (int)OpenContentMode.Suffix);
        ArgumentNullException.ThrowIfNull(wildcard);

        return new(mode, wildcard, appliesToEmpty, location);
    }
}
