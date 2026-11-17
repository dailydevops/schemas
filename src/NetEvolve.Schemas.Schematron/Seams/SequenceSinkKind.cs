namespace NetEvolve.Schemas.Schematron.Seams;

/// <summary>The mode of a sequence sink (design #150, EP-8).</summary>
internal enum SequenceSinkKind : byte
{
    /// <summary>An item sequence; nodes stay references.</summary>
    Items,

    /// <summary>Tree content: everything pushed becomes the children of a document node.</summary>
    Tree,

    /// <summary>Text only.</summary>
    Text,
}
