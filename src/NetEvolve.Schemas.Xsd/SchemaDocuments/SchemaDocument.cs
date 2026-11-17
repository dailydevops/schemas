namespace NetEvolve.Schemas.Xsd.SchemaDocuments;

using NetEvolve.Schemas.Xml.Contracts;

/// <summary>A parsed schema document: the document base URI and the located root element.</summary>
internal sealed class SchemaDocument
{
    internal SchemaDocument(string? baseUri, SchemaSyntaxNode? root, XmlDiagnostic? readError, bool isComplete)
    {
        BaseUri = baseUri;
        Root = root;
        ReadError = readError;
        IsComplete = isComplete;
    }

    /// <summary>Gets the document base URI, <see langword="null"/> when the caller gave none.</summary>
    internal string? BaseUri { get; }

    /// <summary>Gets the <c>schema</c> element, <see langword="null"/> when the root was rejected or the input ended early.</summary>
    internal SchemaSyntaxNode? Root { get; }

    /// <summary>Gets the error of the reader, <see langword="null"/> when it reported none.</summary>
    internal XmlDiagnostic? ReadError { get; }

    /// <summary>Gets a value indicating whether the reader reached the end of the document.</summary>
    internal bool IsComplete { get; }
}
