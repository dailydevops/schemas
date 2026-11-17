namespace NetEvolve.Schemas.Xml.Dtd.Model;

/// <summary>The type of an attribute definition (production 54).</summary>
internal enum DtdAttributeType : byte
{
    /// <summary><c>CDATA</c>.</summary>
    Cdata,

    /// <summary><c>ID</c>.</summary>
    Id,

    /// <summary><c>IDREF</c>.</summary>
    IdRef,

    /// <summary><c>IDREFS</c>.</summary>
    IdRefs,

    /// <summary><c>ENTITY</c>.</summary>
    Entity,

    /// <summary><c>ENTITIES</c>.</summary>
    Entities,

    /// <summary><c>NMTOKEN</c>.</summary>
    NmToken,

    /// <summary><c>NMTOKENS</c>.</summary>
    NmTokens,

    /// <summary><c>NOTATION</c> with its notation names.</summary>
    Notation,

    /// <summary>An enumeration of name tokens.</summary>
    Enumeration,
}
