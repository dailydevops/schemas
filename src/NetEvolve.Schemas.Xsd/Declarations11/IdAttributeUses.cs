namespace NetEvolve.Schemas.Xsd.Declarations11;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;

/// <summary>The rule that XSD 1.0 has and XSD 1.1 drops: at most one attribute of type ID.</summary>
internal static class IdAttributeUses
{
    /// <summary>Checks the attribute uses of a complex type.</summary>
    /// <param name="uses">The attribute uses.</param>
    /// <param name="version">The configured XSD version.</param>
    /// <param name="reporter">Receives the errors.</param>
    internal static void CheckType(IReadOnlyList<AttributeUse> uses, XsdVersion version, XsdIssueReporter reporter) =>
        throw new NotSupportedException();

    /// <summary>Checks the attribute uses of an attribute group.</summary>
    /// <param name="uses">The attribute uses.</param>
    /// <param name="version">The configured XSD version.</param>
    /// <param name="reporter">Receives the errors.</param>
    internal static void CheckGroup(IReadOnlyList<AttributeUse> uses, XsdVersion version, XsdIssueReporter reporter) =>
        throw new NotSupportedException();

    /// <summary>Tells whether a type is the built-in <c>ID</c> or derived from it by restriction.</summary>
    /// <param name="type">The simple type.</param>
    /// <returns><see langword="true"/> for an ID type.</returns>
    internal static bool IsIdType(SimpleTypeDefinition type) => throw new NotSupportedException();
}
