namespace NetEvolve.Schemas.XPath.Casting.Core;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>One row of the casting table: a source type, a target type, the cell, and the conversion. Immutable.</summary>
internal sealed class CastRow
{
    /// <summary>Initializes a new instance of the <see cref="CastRow"/> class.</summary>
    /// <param name="source">The source type.</param>
    /// <param name="target">The target type.</param>
    /// <param name="cell">The cell, <see cref="CastCell.Always"/> or <see cref="CastCell.ValueDependent"/>; a pair that is not allowed has no row.</param>
    /// <param name="conversion">The conversion.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="conversion"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">A type is <see langword="default"/>, or <paramref name="cell"/> is <see cref="CastCell.NotAllowed"/> or not a defined cell.</exception>
    public CastRow(XdmTypeId source, XdmTypeId target, CastCell cell, CastConversion conversion)
    {
        Argument.ThrowIfLessThan(source.Value, 1);
        Argument.ThrowIfLessThan(target.Value, 1);
        Argument.ThrowIfLessThan((int)cell, (int)CastCell.Always);
        Argument.ThrowIfGreaterThan((int)cell, (int)CastCell.ValueDependent);
        Argument.ThrowIfNull(conversion);

        Source = source;
        Target = target;
        Cell = cell;
        Conversion = conversion;
    }

    /// <summary>Gets the source type.</summary>
    public XdmTypeId Source { get; }

    /// <summary>Gets the target type.</summary>
    public XdmTypeId Target { get; }

    /// <summary>Gets the cell.</summary>
    public CastCell Cell { get; }

    /// <summary>Gets the conversion.</summary>
    public CastConversion Conversion { get; }
}
