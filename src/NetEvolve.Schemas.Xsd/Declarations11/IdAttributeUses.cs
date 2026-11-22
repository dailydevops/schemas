namespace NetEvolve.Schemas.Xsd.Declarations11;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;

/// <summary>
/// The rule that XSD 1.0 has and XSD 1.1 drops: no two attribute uses of a complex type or an attribute group have a type that
/// is or is derived from <c>ID</c> (Structures 1.0, 3.4.6 <c>ct-props-correct</c> clause 5 and 3.6.6 <c>ag-props-correct</c>
/// clause 3; XSD 1.1 allows several, appendix on changes since version 1.0).
/// </summary>
/// <remarks>
/// <para>
/// In strict 1.0 mode every use after the first one with an ID type is reported at its own location; a prohibited use takes no
/// value and does not count. In 1.1 mode the check reports nothing, and the validation run registers the value of every ID
/// attribute of an element in the id table (the listener of #303 handles each attribute on its own).
/// </para>
/// <para>
/// Limitation: a type that is a list of ID or a union with ID is not an ID type in the sense of 1.0, which names the type
/// itself and its restrictions; the attribute uses must be the complete set of the type, the inherited ones included.
/// </para>
/// </remarks>
internal static class IdAttributeUses
{
    /// <summary>Checks the attribute uses of a complex type.</summary>
    /// <param name="uses">The attribute uses.</param>
    /// <param name="version">The configured XSD version.</param>
    /// <param name="reporter">Receives the errors.</param>
    internal static void CheckType(IReadOnlyList<AttributeUse> uses, XsdVersion version, XsdIssueReporter reporter) =>
        Check(uses, version, reporter, XsdRuleLookup.CtPropsCorrect, "ct-props-correct.5", "complex type");

    /// <summary>Checks the attribute uses of an attribute group.</summary>
    /// <param name="uses">The attribute uses.</param>
    /// <param name="version">The configured XSD version.</param>
    /// <param name="reporter">Receives the errors.</param>
    internal static void CheckGroup(IReadOnlyList<AttributeUse> uses, XsdVersion version, XsdIssueReporter reporter) =>
        Check(uses, version, reporter, XsdRuleLookup.AgPropsCorrect, "ag-props-correct.3", "attribute group");

    /// <summary>Tells whether a type is the built-in <c>ID</c> or derived from it by restriction.</summary>
    /// <param name="type">The simple type.</param>
    /// <returns><see langword="true"/> for an ID type.</returns>
    internal static bool IsIdType(SimpleTypeDefinition type)
    {
        ArgumentNullException.ThrowIfNull(type);

        for (var current = type; current is not null; current = current.BaseType)
        {
            if (
                string.Equals(current.Name.LocalName, "ID", StringComparison.Ordinal)
                && string.Equals(current.Name.NamespaceName, SchemaGrammar.Namespace, StringComparison.Ordinal)
            )
            {
                return true;
            }
        }

        return false;
    }

    private static void Check(
        IReadOnlyList<AttributeUse> uses,
        XsdVersion version,
        XsdIssueReporter reporter,
        XsdRule rule,
        string clause,
        string target
    )
    {
        ArgumentNullException.ThrowIfNull(uses);
        ArgumentNullException.ThrowIfNull(reporter);

        if (version != XsdVersion.V10)
        {
            return;
        }

        var seen = false;
        for (var i = 0; i < uses.Count; i++)
        {
            var use = uses[i];
            if (use.Prohibited || !IsIdType(use.Declaration.TypeDefinition))
            {
                continue;
            }

            if (seen)
            {
                reporter.Report(
                    rule,
                    use.Location,
                    "The attribute '"
                        + use.Name
                        + "' has an ID type, and an earlier attribute of the "
                        + target
                        + " has one: XSD 1.0 allows one.",
                    clause
                );
            }

            seen = true;
        }
    }
}
