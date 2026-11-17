namespace NetEvolve.Schemas.Xml.Contracts;

using System;

/// <summary>The document type declaration, as spans valid only during <see cref="IXmlSink.Doctype"/>; delivered only when a doctype handler is installed.</summary>
public readonly ref struct XmlDoctypeInfo
{
    /// <summary>Initializes a new instance of the <see cref="XmlDoctypeInfo"/> struct.</summary>
    /// <param name="name">The root element name.</param>
    /// <param name="publicId">The public identifier, empty when absent.</param>
    /// <param name="systemId">The system identifier, empty when absent.</param>
    /// <param name="internalSubset">The internal subset without the brackets, empty when absent.</param>
    public XmlDoctypeInfo(
        ReadOnlySpan<char> name,
        ReadOnlySpan<char> publicId,
        ReadOnlySpan<char> systemId,
        ReadOnlySpan<char> internalSubset
    )
    {
        Name = name;
        PublicId = publicId;
        SystemId = systemId;
        InternalSubset = internalSubset;
    }

    /// <summary>Gets the root element name.</summary>
    public ReadOnlySpan<char> Name { get; }

    /// <summary>Gets the public identifier, empty when absent.</summary>
    public ReadOnlySpan<char> PublicId { get; }

    /// <summary>Gets the system identifier, empty when absent.</summary>
    public ReadOnlySpan<char> SystemId { get; }

    /// <summary>Gets the internal subset without the brackets, empty when absent.</summary>
    public ReadOnlySpan<char> InternalSubset { get; }
}
