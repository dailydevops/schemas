namespace NetEvolve.Schemas.Schematron.Pipeline;

using NetEvolve.Schemas.Xml.Text.NameTable;

/// <summary>The state of one validation: the name table that the instance is read into.</summary>
/// <remarks>
/// Deviation from the shared name table of the Xml package: each pooled context owns a private <see cref="SpanInterningNameTable"/>, so
/// concurrent validations never write to one table; the names of the schema are interned in it once, when the context is created.
/// </remarks>
internal sealed class ValidationContext
{
    internal ValidationContext(SpanInterningNameTable names, int limit)
    {
        Names = names;
        Limit = limit;
    }

    internal SpanInterningNameTable Names { get; }

    internal int Limit { get; }
}
