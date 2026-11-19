namespace NetEvolve.Schemas.Xml.Dtd.Validation.Document;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;

/// <summary>The validation step for the document-level validity constraints (design #122, XML-67).</summary>
internal sealed class DtdDocumentStep : IDtdValidationStep
{
    /// <inheritdoc />
    public void BeginDocument(DtdValidationContext context) => Argument.ThrowIfNull(context);

    /// <inheritdoc />
    public void StartElement(in ValidationElement element) { }

    /// <inheritdoc />
    public void EntityReference(in ValidationEntityReference reference) { }

    /// <inheritdoc />
    public void Text(ReadOnlySpan<char> text, XmlTextKind kind, in XmlPosition position) { }

    /// <inheritdoc />
    public void Comment(in XmlPosition position) { }

    /// <inheritdoc />
    public void ProcessingInstruction(in XmlPosition position) { }

    /// <inheritdoc />
    public void EndElement(in ValidationElement element) { }

    /// <inheritdoc />
    public void EndDocument() { }
}
