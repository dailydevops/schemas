namespace NetEvolve.Schemas.Xml.Dtd.Validation.Identity;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;

/// <summary>The validation step for the identity constraints ID, One ID per Element Type, ID Attribute Default, and IDREF (XML 1.0, section 3.3.1; design #122, XML-65).</summary>
/// <remarks>Placeholder until the implementation commit: the step accepts every event and reports nothing.</remarks>
internal sealed class DtdIdentityStep : IDtdValidationStep
{
    internal DtdIdentityStep(XmlReaderLimits? limits = null) => Limits = limits ?? XmlReaderLimits.Default;

    internal XmlReaderLimits Limits { get; }

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
