namespace NetEvolve.Schemas.Xml.Datatypes.Contracts;

using System;

/// <summary>A datatype descriptor with a typed value: parse, compare, and identity.</summary>
/// <typeparam name="TValue">The value type, always a non-ref struct.</typeparam>
public interface IDatatypeDescriptor<TValue> : IDatatypeDescriptor
{
    /// <summary>Parses a lexical form into a value.</summary>
    /// <param name="lexical">The lexical form.</param>
    /// <returns>The value, or a coded error with its offset; invalid input never throws.</returns>
    ParseResult<TValue> Parse(ReadOnlySpan<char> lexical);

    /// <summary>Compares two values in the value space of <see cref="IDatatypeDescriptor.LexicalMode"/>.</summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <returns>The order, <see cref="XsdOrder.Incomparable"/> when the values are not ordered against each other.</returns>
    XsdOrder Compare(in TValue a, in TValue b);

    /// <summary>Determines whether two values are identical, the spec relation that is finer than equality: 1.1 distinguishes 0 from -0 and equal instants with different offsets.</summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <returns><see langword="true"/> when identical.</returns>
    bool IsIdentical(in TValue a, in TValue b);
}
