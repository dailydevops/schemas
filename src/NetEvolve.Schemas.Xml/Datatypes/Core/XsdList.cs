namespace NetEvolve.Schemas.Xml.Datatypes.Core;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>A valid list value: the range of the lexical form and the number of items. Walk the items with <see cref="XsdListCursor"/>.</summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct XsdList
{
    /// <summary>Initializes a new instance of the <see cref="XsdList"/> struct.</summary>
    /// <param name="range">The range of the whole lexical form in the caller's span.</param>
    /// <param name="itemCount">The number of items.</param>
    public XsdList(TextRange range, int itemCount)
    {
        Range = range;
        ItemCount = itemCount;
    }

    /// <summary>Gets the range of the whole lexical form in the caller's span.</summary>
    public TextRange Range { get; }

    /// <summary>Gets the number of items.</summary>
    public int ItemCount { get; }
}
