namespace NetEvolve.Schemas.Xsd.Declarations11;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>Reads the list of heads of the <c>substitutionGroup</c> attribute.</summary>
internal static class SubstitutionGroupList
{
    /// <summary>Reads the heads an element declaration names.</summary>
    /// <param name="element">The <c>element</c> syntax.</param>
    /// <param name="scope">The namespace scope of the document.</param>
    /// <param name="version">The configured XSD version.</param>
    /// <param name="reporter">Receives the errors.</param>
    /// <returns>The expanded names of the heads.</returns>
    internal static XPathQName[] Read(
        SchemaSyntaxNode element,
        SchemaNamespaceScope scope,
        XsdVersion version,
        XsdIssueReporter reporter
    ) => throw new NotSupportedException();
}
