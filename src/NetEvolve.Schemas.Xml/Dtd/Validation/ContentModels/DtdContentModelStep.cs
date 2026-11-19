namespace NetEvolve.Schemas.Xml.Dtd.Validation.ContentModels;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;

/// <summary>Validation step for the constraint Element Valid (XML 1.0, section 3); not implemented yet.</summary>
internal sealed class DtdContentModelStep : IDtdValidationStep
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
