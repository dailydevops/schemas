namespace NetEvolve.Schemas.Xsd.OpenContent;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>
/// The mapping rules of Structures 1.1, 3.4.2.3.3 (steps 5 and 6) for the <c>{open content}</c> of a complex type with
/// complex content, and the extension constraint <c>cos-ct-extends</c> clauses 1.4.3.2.2.3 and 1.4.3.2.2.4 as far as they
/// concern open content.
/// </summary>
/// <remarks>
/// <para>
/// The wildcard element is the own <c>openContent</c> child, else the schema's <c>defaultOpenContent</c> when the explicit
/// content type is not empty or the default applies to empty content. No wildcard element, or one with mode
/// <see cref="OpenContentMode.None"/>, leaves the open content of the explicit content type: none for a restriction, and
/// that of the base type for an extension (so mode none does not remove inherited open content).
/// </para>
/// <para>
/// Limitation (deviation from the standard, decision on #276, follow-up #762): the standard maps the wildcard of an extension to the
/// union of the own wildcard and the inherited one, so an own wildcard that is narrower than the inherited one is absorbed.
/// Here an own <c>openContent</c> wildcard that is not a superset of the inherited one is a schema error
/// (<c>cos-ct-extends.1.4.3.2.2.4</c>), as the work package asks; the wildcard of a <c>defaultOpenContent</c> follows the
/// standard and is united with the inherited one. Subset and union use the 1.0 definitions of the wildcard component
/// (#238), so a union that they cannot express is reported as <c>cos-aw-union</c>.
/// </para>
/// </remarks>
internal static class OpenContentRules
{
    /// <summary>Applies steps 5 and 6 of the mapping rule to an explicit content type.</summary>
    /// <param name="own">The <c>openContent</c> child of the type, <see langword="null"/> for none.</param>
    /// <param name="schemaDefault">The <c>defaultOpenContent</c> child of the schema, <see langword="null"/> for none.</param>
    /// <param name="explicitContentIsEmpty">Whether the explicit content type has the variety empty.</param>
    /// <param name="inherited">The open content of the explicit content type: that of the base type for an extension, else none.</param>
    /// <param name="location">The location of the type, for issues.</param>
    /// <param name="reporter">The reporter.</param>
    /// <param name="open">The open content of the content type, <see langword="null"/> for none.</param>
    /// <returns><see langword="false"/> after a schema error was reported, otherwise <see langword="true"/>.</returns>
    internal static bool TryResolve(
        OpenContentElement? own,
        OpenContentElement? schemaDefault,
        bool explicitContentIsEmpty,
        OpenContentProperty? inherited,
        DocumentLocation location,
        XsdIssueReporter reporter,
        out OpenContentProperty? open
    )
    {
        ArgumentNullException.ThrowIfNull(reporter);

        open = inherited;
        var element = own ?? (schemaDefault is { } d && (!explicitContentIsEmpty || d.AppliesToEmpty) ? d : null);
        if (element is not { Wildcard: { } wildcard, Mode: not OpenContentMode.None })
        {
            return true;
        }

        if (inherited is null)
        {
            open = OpenContentProperty.Create(element.Mode, wildcard);
            return true;
        }

        if (inherited.Mode == OpenContentMode.Interleave && element.Mode == OpenContentMode.Suffix)
        {
            Report(
                reporter,
                location,
                "cos-ct-extends.1.4.3.2.2.3",
                "The open content of the base type is interleaved, so an extension cannot make it a suffix."
            );
            return false;
        }

        if (ReferenceEquals(element, own))
        {
            if (!inherited.Wildcard.IsSubsetOf(wildcard))
            {
                Report(
                    reporter,
                    location,
                    "cos-ct-extends.1.4.3.2.2.4",
                    "The open content wildcard of an extension must allow everything that the one of its base type allows."
                );
                return false;
            }
        }
        else
        {
            var union = wildcard.Union(inherited.Wildcard);
            if (union is null)
            {
                reporter.Report(
                    XsdRuleLookup.Find(XsdRuleCodes.CosAwUnion),
                    location,
                    "The default open content wildcard and the one of the base type have no expressible union."
                );
                return false;
            }

            wildcard = union;
        }

        open = OpenContentProperty.Create(element.Mode, wildcard);
        return true;
    }

    private static void Report(XsdIssueReporter reporter, DocumentLocation location, string clause, string message) =>
        reporter.Report(XsdRuleLookup.Find(XsdRuleCodes.CosCtExtends), location, message, clause);
}
