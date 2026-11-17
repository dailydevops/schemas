namespace NetEvolve.Schemas.Schematron.Model;

using System.Collections.Generic;

/// <summary>The <c>dir</c> element. Its text is contributed to messages without markup.</summary>
public sealed class SchematronDirection : SchematronMessagePart
{
    internal SchematronDirection(DocumentLocation location, string? value, IReadOnlyList<SchematronMessagePart> parts)
        : base(location)
    {
        Value = value;
        Parts = parts;
    }

    /// <summary>Gets the <see langword="value"/> attribute, or <see langword="null"/> when absent.</summary>
    public string? Value { get; }

    /// <summary>Gets the inner parts.</summary>
    public IReadOnlyList<SchematronMessagePart> Parts { get; }
}
