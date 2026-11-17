namespace NetEvolve.Schemas.Schematron.Parsing.Core;

using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>
/// The <c>include</c> and <c>extends</c> elements and the documents they point at (design #137 AP-4, AP-22; #202).
/// </summary>
/// <remarks>
/// <para>
/// Limitations: <c>include</c> is read in <c>schema</c>, <c>library</c>, <c>pattern</c>, <see langword="group"/>, and <c>rule</c>; in
/// <c>diagnostics</c> and <c>phase</c> it stays <c>SCH-UNSUPPORTED</c>. <c>extends</c> is read in <c>rule</c> only (top-level
/// <c>extends</c> is #389). An included document is parsed as a fragment of one of the roots listed at
/// <see cref="FragmentTarget"/>; the other roots the grammar allows at the place report <see cref="FragmentStatus.Unsupported"/>.
/// </para>
/// </remarks>
internal sealed partial class CoreParseRun
{
    /// <summary>Parses an included document as the root element the place allows.</summary>
    internal FragmentStatus RunFragment(
        XmlNode start,
        SchematronEdition edition,
        bool xsltBinding,
        FragmentTarget target,
        out SchematronNode? fragment,
        out string rootName
    )
    {
        fragment = null;
        rootName = string.Empty;
        _edition = edition;
        _rejectXslt = xsltBinding;
        if (!TryGetRoot(start, out var root))
        {
            return FragmentStatus.Failed;
        }

        var kind = Classify(root);
        rootName = LocalName(root);
        if (!IsAllowedRoot(target, kind))
        {
            return IsSupportedLater(target, kind) ? FragmentStatus.Unsupported : FragmentStatus.WrongRoot;
        }

        fragment = kind switch
        {
            CoreElement.Library => ReadLibrary(root),
            CoreElement.Pattern => ParsePattern(root, isGroup: false),
            CoreElement.Group => Gate2025(root, "group") ? ParsePattern(root, isGroup: true) : null,
            CoreElement.Rule => ParseRule(root),
            CoreElement.Assert => ParseAssert(root),
            CoreElement.Report => ParseReport(root),
            _ => ParseParagraph(root),
        };
        return _failed || fragment is null ? FragmentStatus.Failed : FragmentStatus.Parsed;
    }

    private static bool IsAllowedRoot(FragmentTarget target, CoreElement kind) =>
        target switch
        {
            FragmentTarget.Container => kind is CoreElement.Library or CoreElement.Pattern or CoreElement.Group,
            FragmentTarget.Pattern => kind == CoreElement.Rule,
            FragmentTarget.Rule => kind is CoreElement.Assert or CoreElement.Report or CoreElement.Paragraph,
            _ => kind == CoreElement.Library,
        };

    // The roots the grammar allows at the place, which no release-1 work package reads as an included document.
    private static bool IsSupportedLater(FragmentTarget target, CoreElement kind) =>
        target switch
        {
            FragmentTarget.Container => kind
                is CoreElement.Title
                    or CoreElement.Ns
                    or CoreElement.Paragraph
                    or CoreElement.Param
                    or CoreElement.Let
                    or CoreElement.Phase
                    or CoreElement.Rules
                    or CoreElement.Diagnostics
                    or CoreElement.Properties
                    or CoreElement.Extends,
            FragmentTarget.Pattern => kind
                is CoreElement.Title
                    or CoreElement.Paragraph
                    or CoreElement.Param
                    or CoreElement.Let,
            FragmentTarget.Rule => kind is CoreElement.Title or CoreElement.Let or CoreElement.Extends,
            _ => false,
        };

    private SchematronInclude ParseInclude(
        XmlNode element,
        int index = 0,
        int letIndex = 0,
        int parameterIndex = 0,
        int foreignIndex = 0
    )
    {
        var href = Required(element, "href");
        RejectChildren(element, "include");
        return new SchematronInclude(Location(element), href, index, letIndex, parameterIndex, foreignIndex);
    }

    private SchematronExtends ParseExtends(XmlNode element)
    {
        var rule = Attribute(element, "rule");
        var href = Attribute(element, "href");
        if (rule is null == href is null)
        {
            Report(
                rule is null ? SchematronDiagnosticCodes.AttributeMissing : SchematronDiagnosticCodes.AttributeInvalid,
                "The element 'extends' requires exactly one of the attributes 'rule' and 'href'.",
                Location(element)
            );
        }

        RejectChildren(element, "extends");
        return new SchematronExtends(Location(element), rule, href);
    }
}
