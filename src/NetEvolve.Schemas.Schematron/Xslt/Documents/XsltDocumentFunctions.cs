namespace NetEvolve.Schemas.Schematron.Xslt.Documents;

using System;
using NetEvolve.Schemas.XPath.Functions.Binding;

internal static class XsltDocumentFunctions
{
    internal static XPathFunction[] Create(Uri? schemaBaseUri) => schemaBaseUri is null ? [] : [];
}
