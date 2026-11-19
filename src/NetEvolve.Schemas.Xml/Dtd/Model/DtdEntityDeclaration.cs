namespace NetEvolve.Schemas.Xml.Dtd.Model;

/// <summary>A general or parameter entity declaration (productions 71 to 73).</summary>
public sealed class DtdEntityDeclaration
{
    internal DtdEntityDeclaration(
        string name,
        bool isParameter,
        string? value,
        string? publicId,
        string? systemId,
        string? notationName,
        string? baseUri = null
    )
    {
        Name = name;
        IsParameter = isParameter;
        Value = value;
        PublicId = publicId;
        SystemId = systemId;
        NotationName = notationName;
        BaseUri = baseUri;
    }

    /// <summary>Gets the entity name.</summary>
    public string Name { get; }

    /// <summary>Gets a value indicating whether this is a parameter entity.</summary>
    public bool IsParameter { get; }

    /// <summary>Gets the replacement text of an internal entity (character references expanded, general-entity references kept), <see langword="null"/> for an external entity.</summary>
    public string? Value { get; }

    /// <summary>Gets the public identifier of an external entity, <see langword="null"/> when absent.</summary>
    public string? PublicId { get; }

    /// <summary>Gets the system identifier of an external entity, <see langword="null"/> for an internal entity.</summary>
    public string? SystemId { get; }

    /// <summary>Gets the notation name of an unparsed entity, otherwise <see langword="null"/>.</summary>
    public string? NotationName { get; }

    /// <summary>Gets the base URI of the entity that contains the declaration; a relative <see cref="SystemId"/> resolves against it (XML 1.0 section 4.2.2). <see langword="null"/> when it is not known.</summary>
    internal string? BaseUri { get; }

    /// <summary>Gets a value indicating whether the declaration stands in the external subset or in an external parameter entity; with <c>standalone="yes"</c> such an entity does not satisfy WFC Entity Declared (section 4.1).</summary>
    internal bool InExternalText => throw new System.NotSupportedException();
}
