namespace NetEvolve.Schemas.Schematron.Xslt.Documents;

using System;
using NetEvolve.Schemas.XPath.Functions.Binding;

/// <summary>
/// The functions of XSLT 2.0, 16.1 and 16.2 for the expressions of one schema: <c>document</c>, and <c>doc</c>, <c>doc-available</c>,
/// <c>unparsed-text</c>, and <c>unparsed-text-available</c> of the XPath package bound to the schema base URI, so that a relative reference
/// resolves against the schema and not against the instance. Every function loads through the resolver of the evaluation context,
/// which denies by default, and shares the one document pool of that context, so <c>document</c> and <c>fn:doc</c> agree on a node.
/// </summary>
internal static class XsltDocumentFunctions
{
    /// <summary>Creates the functions.</summary>
    /// <param name="schemaBaseUri">The schema base URI, <see langword="null"/> for none; a relative reference then cannot be resolved.</param>
    /// <returns>The functions.</returns>
    internal static XPathFunction[] Create(Uri? schemaBaseUri) =>
        [
            new XsltDocumentFunction(schemaBaseUri),
            new SchemaBoundFunction("doc", 1, 1, schemaBaseUri),
            new SchemaBoundFunction("doc-available", 1, 1, schemaBaseUri),
            new SchemaBoundFunction("unparsed-text", 1, 2, schemaBaseUri),
            new SchemaBoundFunction("unparsed-text-available", 1, 2, schemaBaseUri),
        ];
}
