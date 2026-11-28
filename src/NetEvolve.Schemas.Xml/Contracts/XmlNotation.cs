namespace NetEvolve.Schemas.Xml.Contracts;

using System;

/// <summary>A notation declaration of the document type: name and identifiers.</summary>
public sealed class XmlNotation
{
    /// <summary>Initializes a new instance of the <see cref="XmlNotation"/> class.</summary>
    /// <param name="name">The notation name.</param>
    /// <param name="systemId">The system identifier, <see langword="null"/> when absent.</param>
    /// <param name="publicId">The public identifier, <see langword="null"/> when absent.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    public XmlNotation(string name, string? systemId, string? publicId)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(name);
        Name = name;
        SystemId = systemId;
        PublicId = publicId;
    }

    /// <summary>Gets the notation name.</summary>
    public string Name { get; }

    /// <summary>Gets the system identifier, <see langword="null"/> when absent.</summary>
    public string? SystemId { get; }

    /// <summary>Gets the public identifier, <see langword="null"/> when absent.</summary>
    public string? PublicId { get; }
}
