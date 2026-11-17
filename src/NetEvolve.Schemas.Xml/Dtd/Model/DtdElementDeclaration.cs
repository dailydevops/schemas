namespace NetEvolve.Schemas.Xml.Dtd.Model;

/// <summary>An element type declaration (production 45).</summary>
internal sealed class DtdElementDeclaration
{
    internal DtdElementDeclaration(string name, DtdContentKind contentKind, DtdContentParticle? content)
    {
        Name = name;
        ContentKind = contentKind;
        Content = content;
    }

    /// <summary>Gets the element type name.</summary>
    internal string Name { get; }

    /// <summary>Gets the kind of content.</summary>
    internal DtdContentKind ContentKind { get; }

    /// <summary>Gets the content model: for mixed content a choice of the allowed names (repeated when the group ends in <c>)*</c>), for element content the particle tree, otherwise <see langword="null"/>.</summary>
    internal DtdContentParticle? Content { get; }
}
