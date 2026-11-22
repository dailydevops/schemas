namespace NetEvolve.Schemas.Xml.Dtd.Validation.Attributes;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;

/// <summary>The validation step for the attribute constraints of XML 1.0 (design #122, XML-64); the checks follow with the implementation.</summary>
internal sealed class DtdAttributeStep : IDtdValidationStep
{
    /// <summary>Initializes a new instance of the <see cref="DtdAttributeStep"/> class.</summary>
    /// <param name="limits">The reader limits that bound the expansion of a default value, <see langword="null"/> for <see cref="XmlReaderLimits.Default"/>.</param>
    internal DtdAttributeStep(XmlReaderLimits? limits = null) => _ = limits;

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
