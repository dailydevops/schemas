namespace NetEvolve.Schemas.Xml.Dtd.Model;

/// <summary>The default declaration of an attribute definition (production 60).</summary>
internal enum DtdDefaultKind : byte
{
    /// <summary><c>#IMPLIED</c>.</summary>
    Implied,

    /// <summary><c>#REQUIRED</c>.</summary>
    Required,

    /// <summary><c>#FIXED</c> with a value.</summary>
    Fixed,

    /// <summary>A plain default value.</summary>
    Default,
}
