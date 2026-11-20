namespace NetEvolve.Schemas.Xsd.Declarations11;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>The <c>targetNamespace</c> attribute of local element and attribute declarations.</summary>
internal static class LocalTargetNamespace
{
    /// <summary>Checks the declarations of one schema document.</summary>
    /// <param name="root">The <c>schema</c> element.</param>
    /// <param name="version">The configured XSD version.</param>
    /// <param name="reporter">Receives the errors.</param>
    internal static void Check(SchemaSyntaxNode root, XsdVersion version, XsdIssueReporter reporter) =>
        throw new NotSupportedException();

    /// <summary>Gets the expanded name that the <c>targetNamespace</c> attribute gives a declaration.</summary>
    /// <param name="declaration">The <c>element</c> or <c>attribute</c> syntax.</param>
    /// <param name="version">The configured XSD version.</param>
    /// <param name="name">Receives the expanded name.</param>
    /// <returns><see langword="true"/> when the declaration names its namespace.</returns>
    internal static bool TryGetName(SchemaSyntaxNode declaration, XsdVersion version, out XPathQName name) =>
        throw new NotSupportedException();
}
