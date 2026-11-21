namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Embedding;

using NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core;

/// <summary>Builds the elements of the embedding tests: elements of the XSLT namespace and elements in no namespace.</summary>
internal static class EmbeddingNodes
{
    internal const string XsltNamespace = "http://www.w3.org/1999/XSL/Transform";

    /// <summary>Creates an element of the XSLT namespace; the content mixes attributes, elements, and text.</summary>
    internal static XmlElem Xsl(string local, int line, int column, params object[] content) =>
        new(local, XsltNamespace, line, column, content);

    /// <summary>Creates an element in no namespace.</summary>
    internal static XmlElem Plain(string local, int line, int column, params object[] content) =>
        new(local, string.Empty, line, column, content);
}
