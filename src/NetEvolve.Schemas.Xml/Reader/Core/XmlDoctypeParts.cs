namespace NetEvolve.Schemas.Xml.Reader.Core;

using System;
using System.Runtime.InteropServices;

/// <summary>The offsets of the parts of a document type declaration in the span of the call that produced it.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct XmlDoctypeParts
{
    internal XmlDoctypeParts(
        (int Start, int Length) name,
        (int Start, int Length) publicId,
        (int Start, int Length) systemId,
        (int Start, int Length) subset
    )
    {
        Name = name;
        PublicId = publicId;
        SystemId = systemId;
        Subset = subset;
    }

    internal (int Start, int Length) Name { get; }

    internal (int Start, int Length) PublicId { get; }

    internal (int Start, int Length) SystemId { get; }

    internal (int Start, int Length) Subset { get; }

    internal static ReadOnlySpan<char> Slice(ReadOnlySpan<char> input, (int Start, int Length) part) =>
        input.Slice(part.Start, part.Length);
}
