namespace NetEvolve.Schemas.Xsd.Versioning.Constructs;

using System;
using System.Collections.Generic;

/// <summary>
/// The one table of the XSD 1.1 elements and attributes that strict 1.0 mode rejects (Structures 1.1, appendix on changes
/// since version 1.0), each with its rule code.
/// </summary>
/// <remarks>
/// Limitation: <c>assertion</c> is listed because it is the 1.1 facet element, but the schema grammar does not yet admit it
/// (it admits <c>assert</c> as facet), so a parsed document never reaches this entry. Identity constraint <see langword="ref"/> and
/// substitution group lists are value rules of XSD-60, not part of this table.
/// </remarks>
internal static class Strict10ConstructTable
{
    private const string Declarations = "element attribute";

    internal static IReadOnlyList<Strict10Construct> Entries { get; } =
    [
        Element("assert", XsdRuleCodes.Strict10Assert),
        Element("assertion", XsdRuleCodes.Strict10AssertionFacet),
        Element("alternative", XsdRuleCodes.Strict10Alternative),
        Element("openContent", XsdRuleCodes.Strict10OpenContent),
        Element("defaultOpenContent", XsdRuleCodes.Strict10DefaultOpenContent),
        Element("override", XsdRuleCodes.Strict10Override),
        Element("explicitTimezone", XsdRuleCodes.Strict10ExplicitTimezone),
        Attribute("defaultAttributes", XsdRuleCodes.Strict10DefaultAttributes),
        Attribute("defaultAttributesApply", XsdRuleCodes.Strict10DefaultAttributesApply),
        Attribute("notNamespace", XsdRuleCodes.Strict10NotNamespace),
        Attribute("notQName", XsdRuleCodes.Strict10NotQName),
        Attribute("xpathDefaultNamespace", XsdRuleCodes.Strict10XPathDefaultNamespace),
        Attribute("inheritable", XsdRuleCodes.Strict10Inheritable),
        Attribute("targetNamespace", XsdRuleCodes.Strict10LocalTargetNamespace, Declarations, localOnly: true),
    ];

    /// <summary>Tells whether the table rejects an element.</summary>
    /// <param name="name">The local name.</param>
    /// <returns><see langword="true"/> when listed.</returns>
    internal static bool HasElement(string name) => Find(false, name) is not null;

    /// <summary>Tells whether the table rejects an attribute on some element.</summary>
    /// <param name="name">The local name.</param>
    /// <returns><see langword="true"/> when listed.</returns>
    internal static bool HasAttribute(string name) => Find(true, name) is not null;

    /// <summary>Finds the entry of an element or attribute.</summary>
    /// <param name="isAttribute">Whether to look for an attribute.</param>
    /// <param name="name">The local name.</param>
    /// <returns>The entry, <see langword="null"/> when 1.0 admits it.</returns>
    internal static Strict10Construct? Find(bool isAttribute, string name)
    {
        for (var i = 0; i < Entries.Count; i++)
        {
            var entry = Entries[i];
            if (entry.IsAttribute == isAttribute && string.Equals(entry.Name, name, StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }

    private static Strict10Construct Element(string name, DiagnosticCode code) => new(false, name, Rule(code));

    private static Strict10Construct Attribute(
        string name,
        DiagnosticCode code,
        string owners = "",
        bool localOnly = false
    ) => new(true, name, Rule(code), owners, localOnly);

    private static XsdRule Rule(DiagnosticCode code)
    {
        foreach (var rule in XsdRuleCatalog.All)
        {
            if (rule.Code == code)
            {
                return rule;
            }
        }

        throw new InvalidOperationException("The rule catalog has no rule " + code + ".");
    }
}
