namespace NetEvolve.Schemas.Xsd.Compilation;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>What the pipeline does to a parsed schema document before any component is built.</summary>
internal static class DocumentPreparation
{
    /// <summary>Prepares the syntax of a document.</summary>
    /// <param name="root">The <c>schema</c> element.</param>
    /// <param name="version">The XSD version.</param>
    /// <param name="reporter">The reporter.</param>
    internal static void Prepare(SchemaSyntaxNode root, XsdVersion version, XsdIssueReporter reporter)
    {
        Argument.ThrowIfNull(root);
        Argument.ThrowIfNull(reporter);

        _ = version;
    }
}
