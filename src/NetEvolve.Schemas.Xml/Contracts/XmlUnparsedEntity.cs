namespace NetEvolve.Schemas.Xml.Contracts;

using System;
using NetEvolve.Arguments;

/// <summary>An unparsed entity declaration of the document type: name, identifiers, and notation name.</summary>
public sealed class XmlUnparsedEntity
{
    /// <summary>Initializes a new instance of the <see cref="XmlUnparsedEntity"/> class.</summary>
    /// <param name="name">The entity name.</param>
    /// <param name="systemId">The system identifier.</param>
    /// <param name="publicId">The public identifier, <see langword="null"/> when absent.</param>
    /// <param name="notationName">The notation name.</param>
    /// <exception cref="ArgumentNullException">An argument other than <paramref name="publicId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    public XmlUnparsedEntity(string name, string systemId, string? publicId, string notationName)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(systemId);
        ArgumentNullException.ThrowIfNull(notationName);
        Name = name;
        SystemId = systemId;
        PublicId = publicId;
        NotationName = notationName;
    }

    /// <summary>Gets the entity name.</summary>
    public string Name { get; }

    /// <summary>Gets the system identifier.</summary>
    public string SystemId { get; }

    /// <summary>Gets the public identifier, <see langword="null"/> when absent.</summary>
    public string? PublicId { get; }

    /// <summary>Gets the notation name.</summary>
    public string NotationName { get; }
}
