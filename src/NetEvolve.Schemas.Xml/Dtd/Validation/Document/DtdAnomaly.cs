namespace NetEvolve.Schemas.Xml.Dtd.Validation.Document;

using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Formats;

/// <summary>One parse-time fact: its kind, where it was found, and the name it concerns.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly record struct DtdAnomaly
{
    /// <summary>Initializes a new instance of the <see cref="DtdAnomaly"/> struct.</summary>
    /// <param name="kind">The kind.</param>
    /// <param name="location">The location; a location whose line is unknown (zero) falls back to the document type declaration.</param>
    /// <param name="name">The element type name for <see cref="DtdAnomalyKind.ElementRedeclared"/>, otherwise <see langword="null"/>.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="kind"/> is not a defined value.</exception>
    internal DtdAnomaly(DtdAnomalyKind kind, DocumentLocation location, string? name = null)
    {
        Argument.ThrowIfGreaterThan((int)kind, (int)DtdAnomalyKind.ConditionalSectionNesting);
        Argument.ThrowIfLessThan((int)kind, (int)DtdAnomalyKind.ElementRedeclared);

        Kind = kind;
        Location = location;
        Name = name;
    }

    /// <summary>Gets the kind.</summary>
    internal DtdAnomalyKind Kind { get; }

    /// <summary>Gets the location.</summary>
    internal DocumentLocation Location { get; }

    /// <summary>Gets the element type name, <see langword="null"/> for the nesting kinds.</summary>
    internal string? Name { get; }
}
