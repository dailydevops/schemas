namespace NetEvolve.Schemas.Schematron.Seams;

/// <summary>The shape of a <see cref="SchemaSource"/>.</summary>
internal enum SchemaSourceKind : byte
{
    Stream = 0,
    Bytes = 1,
    Chars = 2,
}
