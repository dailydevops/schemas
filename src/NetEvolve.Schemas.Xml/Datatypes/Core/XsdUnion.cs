namespace NetEvolve.Schemas.Xml.Datatypes.Core;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>A valid union value: the range of the lexical form and the index of the member type that accepted it.</summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct XsdUnion
{
    /// <summary>Initializes a new instance of the <see cref="XsdUnion"/> struct.</summary>
    /// <param name="range">The range of the whole lexical form in the caller's span.</param>
    /// <param name="memberIndex">The zero-based index of the first accepting member in declaration order.</param>
    public XsdUnion(TextRange range, int memberIndex)
    {
        Range = range;
        MemberIndex = memberIndex;
    }

    /// <summary>Gets the range of the whole lexical form in the caller's span.</summary>
    public TextRange Range { get; }

    /// <summary>Gets the zero-based index of the first accepting member in declaration order.</summary>
    public int MemberIndex { get; }
}
