namespace NetEvolve.Schemas.Xml.Reader.Declaration;

/// <summary>Selects the grammar <see cref="XmlDeclarationParser"/> applies.</summary>
internal enum XmlDeclarationKind
{
    /// <summary>The XML declaration, production 23: version is required, encoding and standalone are optional.</summary>
    Xml,

    /// <summary>The text declaration, production 77: version is optional, encoding is required, standalone is not allowed.</summary>
    Text,
}
