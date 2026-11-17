namespace NetEvolve.Schemas.Xml.Dtd.Model;

/// <summary>The kind of content an element declaration allows (production 46).</summary>
internal enum DtdContentKind : byte
{
    /// <summary><c>EMPTY</c>.</summary>
    Empty,

    /// <summary><c>ANY</c>.</summary>
    Any,

    /// <summary>Mixed content (production 51): character data and the listed element types.</summary>
    Mixed,

    /// <summary>Element content (production 47): a content particle tree.</summary>
    Children,
}
