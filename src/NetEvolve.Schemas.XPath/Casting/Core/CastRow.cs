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
        ArgumentOutOfRangeException.ThrowIfLessThan(source.Value, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(target.Value, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan((int)cell, (int)CastCell.Always);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)cell, (int)CastCell.ValueDependent);
        ArgumentNullException.ThrowIfNull(conversion);

        Source = source;
        Target = target;
        Cell = cell;
        Conversion = conversion;
    }

    /// <summary>Initializes a new instance of the <see cref="CastRow"/> class for a conversion that needs the namespaces in scope.</summary>
    /// <param name="source">The source type.</param>
    /// <param name="target">The target type.</param>
    /// <param name="cell">The cell, <see cref="CastCell.Always"/> or <see cref="CastCell.ValueDependent"/>; a pair that is not allowed has no row.</param>
    /// <param name="conversion">The conversion, which receives the namespaces of the cast.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="conversion"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">A type is <see langword="default"/>, or <paramref name="cell"/> is <see cref="CastCell.NotAllowed"/> or not a defined cell.</exception>
    /// <remarks>
    /// <see cref="Conversion"/> of such a row has no static context to hand and resolves against <see cref="NoNamespaceContext"/>: a name without a prefix and
    /// without a default namespace is in no namespace, and every prefix is unbound.
    /// </remarks>
    public CastRow(XdmTypeId source, XdmTypeId target, CastCell cell, NamespaceCastConversion conversion)
        : this(source, target, cell, Wrap(conversion)) => NamespaceConversion = conversion;

    /// <summary>Gets the source type.</summary>
    public XdmTypeId Source { get; }

    /// <summary>Gets the target type.</summary>
    public XdmTypeId Target { get; }

    /// <summary>Gets the cell.</summary>
    public CastCell Cell { get; }

    /// <summary>Gets the conversion without a static context.</summary>
    public CastConversion Conversion { get; }

    /// <summary>Gets the conversion that takes the namespaces of the cast, <see langword="null"/> when the row does not depend on them.</summary>
    public NamespaceCastConversion? NamespaceConversion { get; }

    private static CastConversion Wrap(NamespaceCastConversion conversion)
    {
        ArgumentNullException.ThrowIfNull(conversion);

        return (in XdmItem source, out XdmItem result) =>
            conversion(in source, NoNamespaceContext.Instance, out result);
    }
}
