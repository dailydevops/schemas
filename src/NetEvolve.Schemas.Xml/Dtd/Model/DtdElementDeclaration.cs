namespace NetEvolve.Schemas.Xml.Dtd.Model;

/// <summary>An element type declaration (production 45).</summary>
public sealed class DtdElementDeclaration
{
    internal DtdElementDeclaration(
        string name,
        DtdContentKind contentKind,
        DtdContentParticle? content,
        bool inExternalText = false
    )
    {
        Name = name;
        ContentKind = contentKind;
        Content = content;
        InExternalText = inExternalText;
    }

    /// <summary>Gets the element type name.</summary>
    public string Name { get; }

    /// <summary>Gets the kind of content.</summary>
    public DtdContentKind ContentKind { get; }

    /// <summary>Gets the content model: for mixed content a choice of the allowed names (repeated when the group ends in <c>)*</c>), for element content the particle tree, otherwise <see langword="null"/>.</summary>
    public DtdContentParticle? Content { get; }

    /// <summary>Gets a value indicating whether the declaration stands in the external subset or in an external parameter entity; with <c>standalone="yes"</c> an element with element content declared there must not contain white space (section 2.9).</summary>
    internal bool InExternalText { get; }
}
