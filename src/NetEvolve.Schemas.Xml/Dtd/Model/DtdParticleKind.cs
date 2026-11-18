namespace NetEvolve.Schemas.Xml.Dtd.Model;

/// <summary>The kind of a content particle (production 48).</summary>
public enum DtdParticleKind
{
    /// <summary>An element type name.</summary>
    Name,

    /// <summary>A sequence (production 49).</summary>
    Sequence,

    /// <summary>A choice (production 50); also the name list of mixed content.</summary>
    Choice,
}
