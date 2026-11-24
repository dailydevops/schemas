namespace NetEvolve.Schemas.Schematron.Pipeline;

using NetEvolve.Schemas.Xml.Text.NameTable;

/// <summary>The state of one validation: the name table that the instance is read into.</summary>
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
