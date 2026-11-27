namespace NetEvolve.Schemas.Xsd.Validation.Assembly;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xsd.Compilation;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.Versioning.Constructs;

/// <summary>
/// The one table of the XSD 1.1 constructs that the assembled validator does not support yet (design #146 DP-16). Under the
/// default version every occurrence is a located schema error, so no schema is silently read with 1.0 semantics; XSD-78 (#360)
/// removes a row together with its tests when it implements the construct. The <c>assert</c> element of a complex type, the
/// <c>assertion</c> facet of a simple type, the <c>explicitTimezone</c> facet, the <see langword="override"/> element, the type
/// <c>alternative</c>, and the <c>openContent</c> and <c>defaultOpenContent</c> elements (#1424) are wired (they have no row), and so are the <c>inheritable</c> and <c>xpathDefaultNamespace</c> attributes, the
/// <c>targetNamespace</c> attribute of a local declaration, the <see langword="ref"/> attribute of an identity constraint, the
/// <c>notNamespace</c> and <c>notQName</c> attributes of a wildcard, and a list of substitution group heads (XSD-78, #360).
/// </summary>
/// <remarks>
/// The rows of the strict 1.0 table (<see cref="Strict10ConstructTable"/>) are the first rows; their not-yet-supported code is the
/// strict code plus 40, the offset that the rule codes share (NES4020 and NES4060). Two constructs have no strict 1.0 row because
/// they are rules of a value, not of a name: the datatypes <c>xs:precisionDecimal</c> and <c>xs:error</c>, which are names that exist in 1.1 but not in the registry of the datatypes yet (DEF-52, #64).
/// Limitation: conditional inclusion (<c>vc:</c> attributes) is not applied by the pipeline yet (XSD-78), so a construct inside an
/// excluded block is reported as well.
/// </remarks>
internal static class Xsd11ConstructTable
{
    private const int NotYetSupportedOffset = 40;

    private static readonly SchemaNamespaceScope NoNamespace = new(string.Empty, false);

    private static readonly string[] TypeAttributes = ["type", "base", "itemType", "memberTypes"];

    internal static IReadOnlyList<Xsd11Construct> Entries { get; } = Create();

    private static List<Xsd11Construct> Create()
    {
        var entries = new List<Xsd11Construct>(Strict10ConstructTable.Entries.Count + 2);
        foreach (var strict in Strict10ConstructTable.Entries)
        {
            if (
                strict.IsAttribute
                    ? (strict.Name is "notNamespace" or "notQName" or "inheritable" or "xpathDefaultNamespace")
                        || (strict.Name == "targetNamespace" && strict.LocalOnly)
                    : strict.Name
                        is "assert"
                            or "assertion"
                            or "explicitTimezone"
                            or "override"
                            or "alternative"
                            or "openContent"
                            or "defaultOpenContent"
            )
            {
                continue;
            }

            entries.Add(
                new Xsd11Construct(
                    strict.IsAttribute,
                    [strict.Name],
                    XsdRuleLookup.Find(new DiagnosticCode(strict.Rule.Code.Number + NotYetSupportedOffset)),
                    strict.Owners,
                    strict.LocalOnly
                )
            );
        }

        entries.Add(
            new Xsd11Construct(
                true,
                TypeAttributes,
                XsdRuleLookup.Find(XsdRuleCodes.NotYetSupportedPrecisionDecimal),
                valueMatches: static (node, value) => NamesXsType(node, value, "precisionDecimal")
            )
        );
        entries.Add(
            new Xsd11Construct(
                true,
                TypeAttributes,
                XsdRuleLookup.Find(XsdRuleCodes.NotYetSupportedError),
                valueMatches: static (node, value) => NamesXsType(node, value, "error")
            )
        );

        return entries;
    }

    private static bool NamesXsType(SchemaSyntaxNode node, string value, string localName)
    {
        foreach (var token in SyntaxValues.Split(value))
        {
            if (
                NoNamespace.TryResolve(node, token, out var name)
                && string.Equals(name.NamespaceName, SchemaGrammar.Namespace, StringComparison.Ordinal)
                && string.Equals(name.LocalName, localName, StringComparison.Ordinal)
            )
            {
                return true;
            }
        }

        return false;
    }
}
