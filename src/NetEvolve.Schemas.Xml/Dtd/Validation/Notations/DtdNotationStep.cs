namespace NetEvolve.Schemas.Xml.Dtd.Validation.Notations;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;

/// <summary>The validation step for Entity Name, Notation Attributes, One Notation Per Element Type, No Notation on Empty Element, Notation Declared, and Unique Notation Name (XML 1.0, sections 3.3.1, 4.2.2, and 4.7; design #122, XML-66).</summary>
/// <remarks>Placeholder until the implementation commit: the step accepts every event and reports nothing.</remarks>
internal sealed class DtdNotationStep : IDtdValidationStep
{
    /// <inheritdoc />
    public void BeginDocument(DtdValidationContext context) { }

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
