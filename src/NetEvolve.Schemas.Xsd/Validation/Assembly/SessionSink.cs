namespace NetEvolve.Schemas.Xsd.Validation.Assembly;

using System;
using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xsd.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Entities;

/// <summary>
/// The sink of one run: forwards every reader event to the streaming validator, hands the document type declaration to the entity
/// listener as well when the schema has one, and observes the cancellation token at every element boundary (design #146 DP-11).
/// </summary>
internal sealed class SessionSink : IXmlSink
{
    private readonly StreamingValidator _validator;
    private readonly EntityListener? _entities;
    private readonly CancellationToken _cancellationToken;

    internal SessionSink(StreamingValidator validator, EntityListener? entities, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(validator);

        _validator = validator;
        _entities = entities;
        _cancellationToken = cancellationToken;
    }

    /// <inheritdoc />
    public XmlNameTable Names => _validator.Names;

    /// <inheritdoc />
    public void StartDocument(in XmlDeclaration declaration) => _validator.StartDocument(in declaration);

    /// <inheritdoc />
    public void EndDocument() => _validator.EndDocument();

    /// <inheritdoc />
    public void StartElement(XmlName name, XmlPosition position)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        _validator.StartElement(name, position);
    }

    /// <inheritdoc />
    public void NamespaceDeclaration(XmlSymbol prefix, XmlSymbol uri) => _validator.NamespaceDeclaration(prefix, uri);

    /// <inheritdoc />
    public void Attribute(XmlName name, ReadOnlySpan<char> value) => _validator.Attribute(name, value);

    /// <inheritdoc />
    public void EndAttributes() => _validator.EndAttributes();

    /// <inheritdoc />
    public void EndElement(XmlName name) => _validator.EndElement(name);

    /// <inheritdoc />
    public void Text(ReadOnlySpan<char> text, XmlTextKind kind) => _validator.Text(text, kind);

    /// <inheritdoc />
    public void Comment(ReadOnlySpan<char> text) => _validator.Comment(text);

    /// <inheritdoc />
    public void ProcessingInstruction(XmlSymbol target, ReadOnlySpan<char> data) =>
        _validator.ProcessingInstruction(target, data);

    /// <inheritdoc />
    public void Doctype(in XmlDoctypeInfo doctype)
    {
        _validator.Doctype(in doctype);
        _entities?.ReceiveDoctype(in doctype);
    }
}
