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
        string? notationName
    )
    {
        Name = name;
        IsParameter = isParameter;
        Value = value;
        PublicId = publicId;
        SystemId = systemId;
        NotationName = notationName;
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
}
