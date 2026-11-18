namespace NetEvolve.Schemas.Xml.Dtd.Contracts;

/// <summary>What the reader found when it looked up the entity of a reference.</summary>
internal enum DtdEntityOutcome
{
    /// <summary>The entity is declared with an internal replacement text.</summary>
    DeclaredInternal = 0,

    /// <summary>The entity is declared as an external entity.</summary>
    DeclaredExternal = 1,

    /// <summary>The entity is not declared.</summary>
    Undeclared = 2,
}
