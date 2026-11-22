namespace NetEvolve.Schemas.Xsd.Declarations11;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>
/// Reads the heads that the <c>substitutionGroup</c> attribute of an element declaration names (Structures 1.1, 3.3.2: the
/// attribute is a list of QNames, in 1.0 it is one QName).
/// </summary>
/// <remarks>
/// <para>
/// Strict 1.0 mode reports a list of more than one head as <see cref="XsdRuleCodes.Strict10SubstitutionGroupList"/> at the
/// start tag of the element (design #108 AP9) and keeps the first head, so that the declaration is still built. A head with a
/// prefix that is not declared is a <c>src-resolve</c> error and is left out. A head that is named twice counts once. The
/// attribute is read for any declaration; that only a global declaration may have it is not checked here.
/// </para>
/// <para>
/// Limitation: the resolution of the heads to declarations and the affiliation checks are
/// <see cref="SubstitutionGroups11"/>; the wiring of the names into the component pipeline is #360.
/// </para>
/// </remarks>
internal static class SubstitutionGroupList
{
    private static readonly char[] Separators = [' ', '\t', '\r', '\n'];

    /// <summary>Reads the heads an element declaration names.</summary>
    /// <param name="element">The <c>element</c> syntax.</param>
    /// <param name="scope">The namespace scope of the document.</param>
    /// <param name="version">The configured XSD version.</param>
    /// <param name="reporter">Receives the errors.</param>
    /// <returns>The expanded names of the heads in list order, empty when the attribute is absent.</returns>
    internal static XPathQName[] Read(
        SchemaSyntaxNode element,
        SchemaNamespaceScope scope,
        XsdVersion version,
        XsdIssueReporter reporter
    )
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(reporter);

        var value = element.FindAttribute("substitutionGroup");
        if (value is null)
        {
            return [];
        }

        var tokens = value.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
        var count = tokens.Length;
        if (version == XsdVersion.V10 && count > 1)
        {
            reporter.Report(
                XsdRuleCodes.Strict10SubstitutionGroupList,
                element.Location,
                "The XSD 1.1 attribute value 'several substitution group heads' is not allowed in strict 1.0 mode."
            );
            count = 1;
        }

        var heads = new List<XPathQName>(count);
        for (var i = 0; i < count; i++)
        {
            if (!scope.TryResolve(element, tokens[i], out var head))
            {
                reporter.Report(
                    XsdRuleLookup.SrcResolve,
                    element.Location,
                    "The substitution group head '" + tokens[i] + "' is not a QName with a declared prefix."
                );
            }
            else if (!heads.Contains(head))
            {
                heads.Add(head);
            }
        }

        return heads.ToArray();
    }
}
